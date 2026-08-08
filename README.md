# IDA Initialization Tool

A small interactive Windows utility for initializing IDA 9.x.

## Features

- Disable automatic update checks, update downloads, and Lumina.
- Optionally configure IDA to use its bundled Python installation.
- Optionally create an `IDA` desktop shortcut.
- Preview every change without modifying the system.

## Usage

Put `ida-init.exe` next to `ida.exe`, then run:

```shell
ida-init.exe
```

Use `--dir` (or `-d`) when IDA is in another directory:

```shell
ida-init.exe --dir '<IDA installation directory>'
```

Use `--dry-run` to preview all changes without writing the registry or creating
a shortcut. This mode does not require `ida.exe`:

```shell
ida-init.exe --dir '<IDA installation directory>' --dry-run
ida-init.exe --help
```

Settings are written only for the current user under
`HKEY_CURRENT_USER\Software\Hex-Rays\IDA`; administrator privileges are not
required.
