use std::{
    env, fs,
    path::{Path, PathBuf},
};

use anyhow::{Context, Result, bail};
use known_folders::{KnownFolder, get_known_folder_path};
use lnks::Shortcut;
use winreg::HKCU;

const IDA_EXECUTABLE: &str = "ida.exe";
const REGISTRY_KEY: &str = r"Software\Hex-Rays\IDA";

#[derive(Debug, Clone, PartialEq, Eq)]
pub enum EmbeddedPython {
    Available(PathBuf),
    MissingDll(PathBuf),
    NotFound,
}

impl EmbeddedPython {
    pub fn is_available(&self) -> bool {
        matches!(self, Self::Available(_))
    }

    fn dll(&self) -> Option<&Path> {
        match self {
            Self::Available(path) => Some(path),
            Self::MissingDll(_) | Self::NotFound => None,
        }
    }
}

#[derive(Debug)]
pub struct IdaInstallation {
    root: PathBuf,
    executable: PathBuf,
    embedded_python: EmbeddedPython,
}

#[derive(Debug, Clone, Copy)]
pub struct InitOptions {
    pub configure_python: bool,
    pub create_shortcut: bool,
}

#[derive(Debug, PartialEq, Eq)]
pub struct InitializationPlan {
    executable: PathBuf,
    python: Option<PathBuf>,
    shortcut: Option<ShortcutPlan>,
}

#[derive(Debug, PartialEq, Eq)]
struct ShortcutPlan {
    path: PathBuf,
    target: PathBuf,
    working_directory: PathBuf,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
enum ExecutableRequirement {
    Required,
    Optional,
}

impl IdaInstallation {
    pub fn discover(root: Option<&Path>) -> Result<Self> {
        Self::locate(root, ExecutableRequirement::Required)
    }

    pub fn preview(root: Option<&Path>) -> Result<Self> {
        Self::locate(root, ExecutableRequirement::Optional)
    }

    fn locate(root: Option<&Path>, requirement: ExecutableRequirement) -> Result<Self> {
        if let Some(root) = root {
            return Self::from_root(root.to_owned(), requirement);
        }

        let current_dir =
            env::current_dir().context("could not determine the current directory")?;
        if current_dir.join(IDA_EXECUTABLE).is_file() {
            return Self::from_root(current_dir, requirement);
        }

        let current_exe = env::current_exe().context("could not locate ida-init.exe")?;
        if let Some(root) = current_exe.parent()
            && root.join(IDA_EXECUTABLE).is_file()
        {
            return Self::from_root(root.to_owned(), requirement);
        }

        match requirement {
            ExecutableRequirement::Required => bail!(
                "ida.exe was not found; place ida-init.exe in the IDA directory or run it from there"
            ),
            ExecutableRequirement::Optional => Self::from_root(current_dir, requirement),
        }
    }

    fn from_root(root: PathBuf, requirement: ExecutableRequirement) -> Result<Self> {
        let root = std::path::absolute(root).context("could not resolve the IDA directory")?;
        let executable = root.join(IDA_EXECUTABLE);
        if requirement == ExecutableRequirement::Required && !executable.is_file() {
            bail!("ida.exe was not found in {}", root.display());
        }

        let embedded_python = if root.is_dir() {
            find_embedded_python(&root)?
        } else {
            EmbeddedPython::NotFound
        };
        Ok(Self {
            root,
            executable,
            embedded_python,
        })
    }

    pub fn executable(&self) -> &Path {
        &self.executable
    }

    pub fn executable_exists(&self) -> bool {
        self.executable.is_file()
    }

    pub fn embedded_python(&self) -> &EmbeddedPython {
        &self.embedded_python
    }

    pub fn plan(&self, options: InitOptions) -> Result<InitializationPlan> {
        let python = if options.configure_python {
            Some(
                self.embedded_python
                    .dll()
                    .context("bundled Python is unavailable")?
                    .to_owned(),
            )
        } else {
            None
        };

        let shortcut = if options.create_shortcut {
            let desktop = get_known_folder_path(KnownFolder::Desktop)
                .context("could not locate the desktop directory")?;
            Some(ShortcutPlan {
                path: desktop.join("IDA Pro.lnk"),
                target: self.executable.clone(),
                working_directory: self.root.clone(),
            })
        } else {
            None
        };

        Ok(InitializationPlan {
            executable: self.executable.clone(),
            python,
            shortcut,
        })
    }
}

impl InitializationPlan {
    pub fn python(&self) -> Option<&Path> {
        self.python.as_deref()
    }

    pub fn shortcut(&self) -> Option<&Path> {
        self.shortcut
            .as_ref()
            .map(|shortcut| shortcut.path.as_path())
    }

    pub fn apply(&self) -> Result<()> {
        if !self.executable.is_file() {
            bail!(
                "ida.exe was not found at {}; a preview plan cannot be applied",
                self.executable.display()
            );
        }

        configure_registry(self.python()).context("could not update the IDA registry settings")?;

        if let Some(shortcut) = &self.shortcut {
            save_shortcut(
                &shortcut.path,
                &shortcut.target,
                &shortcut.working_directory,
            )?;
        }
        Ok(())
    }
}

fn save_shortcut(path: &Path, target: &Path, working_directory: &Path) -> Result<()> {
    let mut shortcut = Shortcut::new(target);
    shortcut.working_dir = Some(working_directory.to_owned());
    shortcut.description = Some("The Interactive Disassembler".to_owned());
    shortcut
        .save(path)
        .with_context(|| format!("could not create {}", path.display()))
}

fn find_embedded_python(root: &Path) -> Result<EmbeddedPython> {
    let mut candidates = Vec::new();

    for entry in
        fs::read_dir(root).with_context(|| format!("could not inspect {}", root.display()))?
    {
        let entry = entry?;
        if !entry.file_type()?.is_dir() {
            continue;
        }

        let name = entry.file_name();
        let Some(name) = name.to_str() else {
            continue;
        };
        let Some(version) = name.strip_prefix("python3") else {
            continue;
        };
        let Ok(version) = version.parse::<u32>() else {
            continue;
        };

        let dll = entry.path().join(format!("{name}.dll"));
        candidates.push((version, dll));
    }

    candidates.sort_unstable_by_key(|candidate| candidate.0);

    if let Some((_, dll)) = candidates.iter().rev().find(|(_, dll)| dll.is_file()) {
        return Ok(EmbeddedPython::Available(dll.clone()));
    }

    Ok(candidates
        .pop()
        .map_or(EmbeddedPython::NotFound, |(_, dll)| {
            EmbeddedPython::MissingDll(dll)
        }))
}

fn configure_registry(python: Option<&Path>) -> Result<()> {
    let (key, _) = HKCU.create_subkey(REGISTRY_KEY)?;
    key.set_value("AutoCheckUpdates", &0_u32)?;
    key.set_value("AutoRequestUpdates", &0_u32)?;
    key.set_value("AutoUseLumina", &0_u32)?;
    if let Some(python) = python {
        key.set_value("Python3TargetDLL", &python.as_os_str())?;
    }
    Ok(())
}

#[cfg(test)]
mod tests {
    use std::fs::{File, create_dir};

    use tempfile::tempdir;

    use super::*;

    #[test]
    fn finds_ida_and_the_newest_complete_python_installation() -> Result<()> {
        let directory = tempdir()?;
        File::create(directory.path().join(IDA_EXECUTABLE))?;

        for version in ["python39", "python310"] {
            let python = directory.path().join(version);
            create_dir(&python)?;
            File::create(python.join(format!("{version}.dll")))?;
        }

        let installation = IdaInstallation::from_root(
            directory.path().to_owned(),
            ExecutableRequirement::Required,
        )?;

        assert_eq!(
            installation.embedded_python(),
            &EmbeddedPython::Available(directory.path().join("python310").join("python310.dll"))
        );
        Ok(())
    }

    #[test]
    fn reports_the_expected_dll_for_an_incomplete_python_installation() -> Result<()> {
        let directory = tempdir()?;
        File::create(directory.path().join(IDA_EXECUTABLE))?;
        create_dir(directory.path().join("python312"))?;

        let installation = IdaInstallation::from_root(
            directory.path().to_owned(),
            ExecutableRequirement::Required,
        )?;

        assert_eq!(
            installation.embedded_python(),
            &EmbeddedPython::MissingDll(directory.path().join("python312").join("python312.dll"))
        );
        Ok(())
    }

    #[test]
    fn ignores_unrelated_python_directories() -> Result<()> {
        let directory = tempdir()?;
        File::create(directory.path().join(IDA_EXECUTABLE))?;
        create_dir(directory.path().join("python"))?;
        create_dir(directory.path().join("python3x"))?;

        let installation = IdaInstallation::from_root(
            directory.path().to_owned(),
            ExecutableRequirement::Required,
        )?;

        assert_eq!(installation.embedded_python(), &EmbeddedPython::NotFound);
        Ok(())
    }

    #[test]
    fn rejects_a_directory_without_ida() {
        let directory = tempdir().unwrap();

        let error = IdaInstallation::from_root(
            directory.path().to_owned(),
            ExecutableRequirement::Required,
        )
        .unwrap_err();

        assert!(error.to_string().contains("ida.exe was not found"));
    }

    #[test]
    fn discovers_an_explicit_ida_directory() -> Result<()> {
        let directory = tempdir()?;
        File::create(directory.path().join(IDA_EXECUTABLE))?;

        let installation = IdaInstallation::discover(Some(directory.path()))?;

        assert_eq!(
            installation.executable(),
            directory.path().join(IDA_EXECUTABLE)
        );
        Ok(())
    }

    #[test]
    fn previews_without_an_ida_executable() -> Result<()> {
        let directory = tempdir()?;

        let installation = IdaInstallation::preview(Some(directory.path()))?;

        assert_eq!(
            installation.executable(),
            directory.path().join(IDA_EXECUTABLE)
        );
        assert!(!installation.executable_exists());
        Ok(())
    }

    #[test]
    fn refuses_to_apply_a_preview_without_an_ida_executable() -> Result<()> {
        let directory = tempdir()?;
        let installation = IdaInstallation::preview(Some(directory.path()))?;
        let plan = installation.plan(InitOptions {
            configure_python: false,
            create_shortcut: false,
        })?;

        let error = plan.apply().unwrap_err();

        assert!(error.to_string().contains("preview plan cannot be applied"));
        Ok(())
    }

    #[test]
    fn writes_a_windows_shortcut_to_the_requested_location() -> Result<()> {
        let directory = tempdir()?;
        let executable = directory.path().join(IDA_EXECUTABLE);
        let shortcut = directory.path().join("IDA Pro.lnk");
        File::create(&executable)?;

        save_shortcut(&shortcut, &executable, directory.path())?;

        assert!(shortcut.is_file());
        Ok(())
    }
}
