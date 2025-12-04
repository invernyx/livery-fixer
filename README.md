# liveryfixer

A small CLI utility to inspect, fix, and package Microsoft Flight Simulator livery packages. Supports three main operations: `extract`, `fix`, and `pack`.

## Requirements

- .NET Framework 4.7.2
- `MSFSLayoutGenerator.exe` available on `PATH` or in the working directory (used to regenerate `layout.json`)

## Quick usage

- Build the project to produce `liveryfixer.exe`.
- Run the executable and provide inputs interactively or via command-line arguments.

## Command-line arguments

- Arguments use the form `--key value`.
- Keys are normalized by lowercasing and removing spaces (for example, `--Source Dir` becomes `--sourcedir`).

Common keys:

- `--options <path>` — Load an options JSON file. If omitted, the program uses the default `new Options()`.
- `--operation <extract|fix|pack>` — Select an operation instead of typing it interactively.
- `--sourcedir <path>` — Source directory for `extract`, `fix`, and `pack` operations.
- `--outputdir <path>` — Output directory for `extract` and `pack` operations.

If a value contains spaces, quote it: `"C:\My Liveries"`.

## Examples

Interactive:

```
liveryfixer.exe
# then respond to prompts: Operation:, Source Dir:, Output Dir:
```

Fully non-interactive:

```
# Extract all .zip files recursively into an output directory
liveryfixer.exe --operation extract --sourcedir "C:\path\to\zips" --outputdir "C:\path\to\out"

# Fix liveries under a directory
liveryfixer.exe --operation fix --sourcedir "C:\path\to\liveries"

# Pack top-level livery folders that contain manifest.json into .zip files
liveryfixer.exe --operation pack --sourcedir "C:\path\to\liveries" --outputdir "C:\path\to\outputzips"
```

## What each operation does

### `extract`

- Recursively finds `.zip` files under the provided source directory and extracts them into the provided output directory.
- Extraction runs in parallel.

### `fix`

- Traverses each subfolder of the provided `Source Dir` looking for valid livery packages (folders containing `layout.json` and `manifest.json`).
- Parses `manifest.json` to read `creator` and `title`.
- Scans `SimObjects\Airplanes` subfolders and parses `aircraft.cfg` and `texture.cfg` to collect livery data (title, variation, type, registration, ICAO, texture fallbacks).
- Runs verification and fix routines (registration validation, ICAO checks, texture fallback fixes, folder renames, manifest fixes, etc.) and prints any reported changes or errors.
- Attempts to regenerate `layout.json` using `MSFSLayoutGenerator.exe` for each package.

### `pack`

- Iterates top-level subfolders in the `Source Dir` and creates a `.zip` in `Output Dir` for each folder that contains a `manifest.json`.
- Zipping runs in parallel.

## Notes and troubleshooting

- The program prints errors and actions to the console; review output for fixes or failures.
- Ensure `MSFSLayoutGenerator.exe` is available for successful `layout.json` regeneration.
- Use quotes for argument values that contain spaces.

## Contributing

Open issues or PRs in the repository for bugs or feature requests.
