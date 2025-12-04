# liveryfixer

A small CLI utility to inspect, fix, list, and package Microsoft Flight Simulator livery packages. Supports four main operations: `extract`, `fix`, `pack`, and `list`.

## Requirements

- .NET Framework 4.7.2
- `MSFSLayoutGenerator.exe` available on `PATH` or in the working directory (used to regenerate `layout.json` during `fix`)

## Build

- Open the solution in Visual Studio or build with MSBuild/`dotnet build` for the project targeting .NET Framework 4.7.2.
- The compiled executable is `liveryfixer.exe` in the `bin` output folder.

## Command-line arguments

- Arguments use the form `--key value`.
- The program expects keys to be provided lowercase and without spaces (for example, `--sourcedir` not `--Source Dir`).
- If a key is not provided on the command line the program will prompt for it interactively.

Common keys

- `--options <path>` — Load an options JSON file. If omitted the program will use default options.
- `--operation <extract|fix|pack|list>` — Select the operation to run instead of typing it interactively.
- `--sourcedir <path>` — Source directory used by `extract`, `fix`, and `pack` operations.
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

# Pack top-level livery folders that contain manifest.json into .zip files
liveryfixer.exe --operation pack --sourcedir "C:\path\to\liveries" --outputdir "C:\path\to\outputzips"

# Create a JSON file listing detected liveries
liveryfixer.exe --operation list --sourcedir "C:\path\to\liveries" --outputfile "C:\path\to\liveries.json"
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
- Attempts to regenerate `layout.json` for each package by invoking `MSFSLayoutGenerator.exe <path-to-layout.json>` and prints any output or errors.

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

- Traverses the top-level subfolders of `Source Dir` (non-recursive). For each folder containing `manifest.json`, creates a `.zip` archive with the folder name in `Output Dir`.
- Packing runs in parallel.

### list

- Scans `Source Dir` for valid livery packages and writes a JSON array of detected packages to the provided `Output File`.
- The JSON output contains the internal `LiveryPackage` model (title, path, creator, groups, liveries, and texture fallback lists).

## Options (options.json)

You can pass an options JSON file with `--options` to control behavior. Fields (and defaults) are:

- `renamePackage` (bool, default: `true`) — enable or disable automatic package renaming during `RenameFolders`.
- `packagePathPrefix` (string, default: empty) — prefix to apply when renaming package folders.
- `requiredTextureFallbacksByType` (object) — dictionary keyed by livery `ui_type` (or empty string for defaults) with arrays of required fallback paths.
- `unneededTextureFallbacksByType` (object) — dictionary keyed by livery `ui_type` (or empty string) with arrays of fallbacks to remove.
- `creatorNameCorrections` (object) — mapping of incorrect creator names to corrected names used by `FixManifests`.

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
  }
}
```

The program will create a default `Options` instance if loading the JSON file fails.

## Implementation notes

- `CfgFile` is a lightweight INI/CFG parser used to parse `aircraft.cfg` and `texture.cfg` sections and lines.
- `LiveryPackage.GetLiveryPackages` builds the internal model used by the fix routines and `list` output.
- The application is intentionally conservative: it prints errors and actions rather than silently overwriting many things.

## Troubleshooting

- Ensure `MSFSLayoutGenerator.exe` is reachable for `fix` to regenerate `layout.json` successfully.
- Provide command-line keys in lowercase without spaces to avoid interactive prompts being required.
- Review console output for details about what the tool changed or detected.

## Contributing

Open issues or PRs in the project repository for bugs, feature requests, or improvements.
