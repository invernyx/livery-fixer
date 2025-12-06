# liveryfixer

A small CLI utility to inspect, fix, list, find, and package Microsoft Flight Simulator livery packages. Supports five main operations: `extract`, `fix`, `pack`, `list`, and `find`.

## Requirements

- .NET Framework 4.7.2
- `MSFSLayoutGenerator.exe` (the specific version included with this repository/package) available on `PATH` or in the working directory (used to regenerate `layout.json` during `fix`)

## Build

- Open the solution in Visual Studio or build with MSBuild/`dotnet build` for the project targeting .NET Framework 4.7.2.
- The compiled executable is `liveryfixer.exe` in the `bin` output folder.

## Command-line arguments

- Arguments use the form `--key value`.
- The program expects keys to be provided lowercase and without spaces (for example, `--sourcedir` not `--Source Dir`).
- If a key is not provided on the command line the program will prompt for it interactively.

Common keys

- `--options <path>` — Load an options JSON file. If omitted the program will use default options.
- `--operation <extract|fix|pack|list|find>` — Select the operation to run instead of typing it interactively.
- `--sourcedir <path>` — Source directory used by `extract`, `fix`, `pack`, and `find` operations.
- `--outputdir <path>` — Output directory used by `extract` and `pack` operations.
- `--outputfile <path>` — Output file used by the `list` operation.

If a value contains spaces, quote it: `"C:\My Liveries"`.

## Examples

Interactive

```
liveryfixer.exe
# respond to prompts: Operation:, Source Dir:, Output Dir: etc.
```

Non-interactive

```
# Extract all .zip files recursively into an output directory
liveryfixer.exe --operation extract --sourcedir "C:\path\to\zips" --outputdir "C:\path\to\out"

# Fix liveries under a directory using options.json
liveryfixer.exe --operation fix --sourcedir "C:\path\to\liveries" --options "C:\path\to\options.json"

# Pack detected livery packages into .zip files (and optionally extract thumbnails)
liveryfixer.exe --operation pack --sourcedir "C:\path\to\liveries" --outputdir "C:\path\to\outputzips"

# Create a JSON file listing detected liveries
liveryfixer.exe --operation list --sourcedir "C:\path\to\liveries" --outputfile "C:\path\to\liveries.json"

# Enter an interactive registration lookup session
liveryfixer.exe --operation find --sourcedir "C:\path\to\liveries"
```

## Operations

### extract

- Recursively finds `.zip` files under the provided `Source Dir` and extracts each into the provided `Output Dir`.
- Extraction is executed in parallel.

### fix

- Scans each immediate subfolder of `Source Dir` and treats folders containing both `layout.json` and `manifest.json` as livery packages.
- Validates `manifest.json` has `content_type` equal to `livery` and reads `creator` and `title`.
- Looks under `SimObjects\Airplanes` for aircraft folders, parses `aircraft.cfg` (using `CfgFile`) and any `texture.cfg` files to build an internal model of packages, groups, and liveries.
- Runs a sequence of fix/inspection routines (see list below) and prints results to the console.
- Attempts to regenerate `layout.json` for each package by invoking `MSFSLayoutGenerator.exe <path-to-layout.json>` and prints any output or errors. The generator must be the specific `MSFSLayoutGenerator.exe` distributed with this package to ensure compatible output.

Fix/inspection routines executed by `fix` (in order):

- `CheckRegistrations` — Detects missing or duplicate registrations across all liveries.
- `ListTypes` — Groups liveries by `ui_type` and prints their paths.
- `VerifyICAOs` — Checks that each livery has an `icao_airline` value.
- `FixTextureFallbacks` — Adds/removes texture fallback entries in `texture.cfg` based on options (and writes updated `texture.cfg` when changes are made).
- `RenameFolders` — Optionally renames package folders based on their contained liveries and `Options` settings.
- `ListCreators` — Groups and prints liveries by `creator` from `manifest.json`.
- `FixManifests` — Applies configured creator name corrections to `manifest.json` files.

All routines print actions taken or detected errors to the console.

### pack

- Identifies valid livery packages under `Source Dir` (same detection logic as `fix`) and creates a `.zip` archive for each detected package into `Output Dir`.
- Packing runs in parallel over the list of detected packages (not raw top-level folders).
- Optionally extracts `thumbnail.jpg` from the first livery texture folder and copies it beside the generated `.zip` as `<packageName>.jpg` when `extractThumbnailWhenPacking` is enabled in the options.

### list

- Scans `Source Dir` for valid livery packages and writes a JSON array of detected packages to the provided `Output File`.
- The JSON output contains the internal `LiveryPackage` model (title, path, creator, groups, liveries, and texture fallback lists).

### find

- Starts an interactive prompt to search liveries by registration.
- Requires `Source Dir` pointing to the root folder containing livery packages. The tool will build its internal package list and then prompt repeatedly for a `Registration:` value.
- Enter a registration (e.g. `N12345`) to search. Matching liveries are printed with package title and livery path.
- Enter `quit` to exit the `find` loop.
- When a match is found the tool attempts to open the livery folder using `Process.Start(<livery folder path>)` (on Windows this opens File Explorer). This may fail on non-Windows platforms or restricted environments.

## Options (options.json)

You can pass an options JSON file with `--options` to control behavior. Fields (and defaults) are:

- `renamePackage` (bool, default: `true`) — enable or disable automatic package renaming during `RenameFolders`.
- `packagePathPrefix` (string, default: empty) — prefix to apply when renaming package folders.
- `requiredTextureFallbacksByType` (object) — dictionary keyed by livery `ui_type` (or empty string for defaults) with arrays of required fallback paths.
- `unneededTextureFallbacksByType` (object) — dictionary keyed by livery `ui_type` (or empty string) with arrays of fallbacks to remove.
- `creatorNameCorrections` (object) — mapping of incorrect creator names to corrected names used by `FixManifests`.
- `extractThumbnailWhenPacking` (bool, default: `false`) — when `true`, the pack operation will try to copy `thumbnail.jpg` from the first detected texture folder of the package into the `Output Dir` alongside the generated `.zip` (named `<package>.jpg`).

Example `options.json`

```json
{
  "renamePackage": true,
  "packagePathPrefix": "",
  "requiredTextureFallbacksByType": {
    "": ["CommonTextures\\base"],
    "airliner": ["AirlinerFallbacks\\common"]
  },
  "unneededTextureFallbacksByType": {
    "": ["old_fallback"]
  },
  "creatorNameCorrections": {
    "old_creator": "Correct Creator Name"
  },
  "extractThumbnailWhenPacking": false
}
```

The program will create a default `Options` instance if loading the JSON file fails.

## Implementation notes

- `CfgFile` is a lightweight INI/CFG parser used to parse `aircraft.cfg` and `texture.cfg` sections and lines.
- `LiveryPackage.GetLiveryPackages` builds the internal model used by the fix routines and `list` output.
- The application is intentionally conservative: it prints errors and actions rather than silently overwriting many things.

## Troubleshooting

- Ensure the specific `MSFSLayoutGenerator.exe` included with this repository/package is reachable for `fix` to regenerate `layout.json` successfully.
- The `find` operation attempts to open livery folders with the system default application; this works best on Windows where `Process.Start(path)` opens File Explorer.
- Provide command-line keys in lowercase without spaces to avoid interactive prompts being required.
- Review console output for details about what the tool changed or detected.

## Contributing

Open issues or PRs in the project repository for bugs, feature requests, or improvements.
