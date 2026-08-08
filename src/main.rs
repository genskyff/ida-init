#[cfg(windows)]
mod ida;

use std::{io, path::PathBuf, process::ExitCode};

use anyhow::Result;
use clap::{Parser, ValueHint};

#[derive(Debug, Parser)]
#[command(version, about = "Interactively initialize an IDA 9.x installation")]
struct Cli {
    /// IDA installation directory containing ida.exe
    #[arg(short, long, value_name = "PATH", value_hint = ValueHint::DirPath)]
    dir: Option<PathBuf>,

    /// Preview changes without writing anything; ida.exe is not required
    #[arg(short = 'n', long)]
    dry_run: bool,
}

fn main() -> ExitCode {
    match run(Cli::parse()) {
        Ok(()) => ExitCode::SUCCESS,
        Err(error) => {
            if error
                .downcast_ref::<io::Error>()
                .is_some_and(|error| error.kind() == io::ErrorKind::Interrupted)
            {
                return ExitCode::FAILURE;
            }
            if cliclack::outro_cancel(format!("{error:#}")).is_err() {
                eprintln!("Error: {error:#}");
            }
            ExitCode::FAILURE
        }
    }
}

#[cfg(windows)]
fn run(cli: Cli) -> Result<()> {
    use std::io::IsTerminal;

    use cliclack::{intro, log, select};
    use ida::{EmbeddedPython, IdaInstallation, InitOptions};

    #[derive(Clone, Copy, PartialEq, Eq)]
    enum SetupMode {
        Recommended,
        Custom,
    }

    intro(if cli.dry_run {
        "IDA Initialization Tool (dry run)"
    } else {
        "IDA Initialization Tool"
    })?;
    if cli.dry_run {
        log::warning("Dry-run mode: no registry or shortcut changes will be written")?;
    }

    let installation = if cli.dry_run {
        IdaInstallation::preview(cli.dir.as_deref())?
    } else if let Some(root) = cli.dir.as_deref() {
        IdaInstallation::discover(Some(root))?
    } else if let Some(installation) = IdaInstallation::discover_default()? {
        installation
    } else if !io::stdin().is_terminal() {
        IdaInstallation::discover(None)?
    } else {
        let root: PathBuf = cliclack::input("Where is IDA installed?")
            .placeholder("Directory containing ida.exe")
            .validate(|input: &String| {
                if std::path::Path::new(input).join("ida.exe").is_file() {
                    Ok(())
                } else {
                    Err("ida.exe was not found in this directory")
                }
            })
            .interact()?;
        IdaInstallation::discover(Some(&root))?
    };
    if installation.executable_exists() {
        log::success(format!(
            "Found IDA at {}",
            installation.executable().display()
        ))?;
    } else {
        log::warning(format!(
            "ida.exe was not found at {}; using the path for this preview",
            installation.executable().display()
        ))?;
    }

    let mode = match installation.embedded_python() {
        EmbeddedPython::Available(path) => {
            log::info(format!("Bundled Python: {}", path.display()))?;
            select("Choose an initialization mode")
                .item(
                    SetupMode::Recommended,
                    "Recommended",
                    "Configure bundled Python and create a desktop shortcut",
                )
                .item(SetupMode::Custom, "Custom", "Choose the optional settings")
                .initial_value(SetupMode::Recommended)
                .interact()?
        }
        EmbeddedPython::MissingDll(path) => {
            log::warning(format!(
                "Bundled Python DLL was not found at {}",
                path.display()
            ))?;
            SetupMode::Custom
        }
        EmbeddedPython::NotFound => {
            log::warning("No bundled Python installation was found")?;
            SetupMode::Custom
        }
    };

    let options = match mode {
        SetupMode::Recommended => InitOptions {
            configure_python: true,
            create_shortcut: true,
        },
        SetupMode::Custom => InitOptions {
            configure_python: installation.embedded_python().is_available()
                && cliclack::confirm("Configure IDA to use the bundled Python?")
                    .initial_value(true)
                    .interact()?,
            create_shortcut: cliclack::confirm("Create a desktop shortcut?")
                .initial_value(true)
                .interact()?,
        },
    };

    let plan = installation.plan(options)?;
    if cli.dry_run {
        log::info("Would disable automatic update checks, downloads, and Lumina")?;
        if let Some(path) = plan.python() {
            log::info(format!(
                "Would configure bundled Python: {}",
                path.display()
            ))?;
        }
        if let Some(path) = plan.shortcut() {
            log::info(format!("Would create desktop shortcut: {}", path.display()))?;
        }
        cliclack::outro("Dry run complete; no changes were made")?;
        return Ok(());
    }

    plan.apply()?;
    log::success("Disabled automatic update checks, downloads, and Lumina")?;
    if let Some(path) = plan.python() {
        log::success(format!("Configured bundled Python: {}", path.display()))?;
    }
    if let Some(path) = plan.shortcut() {
        log::success(format!("Created desktop shortcut: {}", path.display()))?;
    }

    cliclack::outro("IDA is ready to use")?;
    Ok(())
}

#[cfg(not(windows))]
fn run(_cli: Cli) -> Result<()> {
    anyhow::bail!("ida-init only supports Windows")
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn parses_directory_and_dry_run_options() {
        for option in ["--dir", "-d"] {
            let cli = Cli::try_parse_from(["ida-init", option, "test-installation", "--dry-run"])
                .unwrap();

            assert_eq!(cli.dir, Some(PathBuf::from("test-installation")));
            assert!(cli.dry_run);
        }
    }

    #[test]
    fn uses_interactive_defaults_without_options() {
        let cli = Cli::try_parse_from(["ida-init"]).unwrap();

        assert_eq!(cli.dir, None);
        assert!(!cli.dry_run);
    }
}
