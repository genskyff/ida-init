# IDA Initialization Tool

A small interactive Windows utility for initializing IDA 9.x.

It uses [`cliclack`](https://github.com/fadeevab/cliclack) to guide you through:

- disabling automatic update checks, update downloads, and Lumina;
- optionally configuring IDA to use its bundled Python installation;
- optionally creating an `IDA Pro` desktop shortcut.

Bundled Python is detected from IDA's `python3xx/python3xx.dll` layout, so the
tool is not tied to one Python 3 minor version.

## Usage

1. Build with `cargo build --release`.
2. Put `ida-init.exe` next to `ida.exe`, or run it with the IDA directory as the
   current directory.
3. Run `ida-init.exe` and follow the prompts.

Use `--ida-dir` to target another IDA directory. `--dry-run` does not require
`ida.exe`: it uses the specified directory, or the current directory when none
is specified, and prints every intended change without writing the registry or
a shortcut:

```powershell
ida-init.exe --ida-dir 'D:\Tools\IDA' --dry-run
ida-init.exe --help
```

Settings are written only for the current user under
`HKEY_CURRENT_USER\Software\Hex-Rays\IDA`; administrator privileges are not
required.

## Development

```powershell
cargo fmt --check
cargo test
cargo clippy --all-targets -- -D warnings
```
