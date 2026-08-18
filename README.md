# MSFS 2024 Addons Manager

A Windows desktop utility for organising Microsoft Flight Simulator 2024 addons without moving their source folders. Addons stay in libraries on local drives, removable storage, or supported network locations and are enabled through directory symbolic links in a Community folder.

[![Licence: MIT](https://img.shields.io/badge/licence-MIT-blue.svg)](LICENSE)

> [!CAUTION]
> Close Microsoft Flight Simulator before changing enabled addons. The manager blocks enable and disable operations while `FlightSimulator2024.exe` is running. Keep backups of important simulator configuration and do not delete a source library while any of its addons are enabled.

## Features

- Scan one or more addon libraries recursively.
- Manage `Community` and the optional `Community2024` folder.
- Enable and disable packages without copying their source data.
- Search and filter addons by category and location.
- Inspect package metadata and thumbnails when available.
- Group packages into profiles, preview the required changes, and apply them transactionally with rollback.
- Detect common Microsoft Store and Steam Community-folder locations.
- Diagnose unavailable libraries, malformed manifests, and Community-folder state.
- Record unexpected UI and background errors in a local rolling log with incident IDs.

## How link management works

Enabling a managed addon creates a directory symbolic link in the selected Community folder. Disabling it removes that link only after confirming that it points to the selected source addon. The application refuses to overwrite or delete a real file or directory with the same name.

Creating symbolic links requires either Windows Developer Mode or an elevated application session. Network and removable-storage libraries must be available whenever linked addons are used.

## Install a release

Choose one of the Windows x64 archives on the latest GitHub Release:

- **Self-contained** — recommended; includes .NET and needs no runtime installation.
- **Framework-dependent** — smaller; requires the Microsoft .NET 10 Desktop Runtime. Its launcher downloads the current servicing patch from Microsoft when necessary.

Then:

1. Verify the archive against the attached `SHA256SUMS.txt` file.
2. Extract every file into one folder.
3. For the self-contained build, run `MSFS2024AddonManager.exe`. For the framework-dependent build, run `Install and Run.cmd`.
4. Approve elevation when required for symbolic-link management.
5. For the framework-dependent build, allow installation of the Microsoft .NET 10 Desktop Runtime if prompted.

Release tags automatically build, test, package, checksum, and publish both variants. The matching curated release-notes file becomes the GitHub Release description, is attached separately, and is included in each archive. Executables are Authenticode-signed when the repository signing secrets are configured, and each archive contains `SIGNING.txt` stating its signing status. The detailed user guide is included as `Installer/README.txt` in the source tree and release package. Maintainer and historical-tag backfill instructions are in [RELEASING.md](RELEASING.md).

## First-time setup

1. Open **Settings** and verify the detected Community folder.
2. Configure `Community2024` only if your installation uses it.
3. Add one or more folders containing stored addon packages.
4. Run a scan from **Scan** or **Quick Scan** on the dashboard.
5. Select an addon and choose the Community destination before enabling it.

## Apply a profile

1. Select **Edit addons** on a profile to make it the destination for assignments.
2. Add or remove packages from that profile on the **Addons** page.
3. Return to **Profiles** and select **Preview & apply**.
4. Review every proposed enable and disable operation for the default Community folder.
5. Select **Apply profile** to continue.

Only managed library addons are included. Community-only packages and real folders are never removed by a profile. Missing assigned packages and unresolved legacy assignments block the apply operation. If a link operation fails, completed changes are reversed in the opposite order; any rollback failure is reported explicitly.

Profile assignments store both a deterministic package identity and the canonical source path. This keeps packages with identical folder names in separate libraries distinct. Older folder-name-only profiles are migrated automatically when a name resolves to exactly one package; ambiguous legacy assignments remain unchanged until you choose the intended source. If an assigned package moves, reassign it to confirm its new location before applying the profile.

A package root normally contains `manifest.json`, `layout.json`, or both. Extra wrapper folders are discovered recursively up to the scanner's safety limit.

## Build from source

Requirements:

- Windows 10 or Windows 11
- .NET 10 SDK

```powershell
dotnet restore MSFS2024AddonManager.slnx
dotnet build MSFS2024AddonManager.slnx --configuration Release --no-restore
dotnet test MSFS2024AddonManager.slnx --configuration Release --no-build
```

The application project is `MSFS2024AddonManager/MSFS2024AddonManager.csproj`.

## Data and privacy

Settings and profiles are stored locally under `%LOCALAPPDATA%\MSFS2024AddonManager`. Each save is written to a same-folder temporary file, flushed, read back for JSON validation, and then atomically replaces the live file. One previous version is retained as `settings.json.bak` or `profiles.json.bak`. If a live file is corrupt or missing, the application restores it from the valid backup; if both copies are invalid, it leaves them untouched and reports an error instead of loading empty data.

Unexpected errors are recorded under `%LOCALAPPDATA%\MSFS2024AddonManager\logs`. The rolling logger retains at most five files of approximately 1 MiB each. Local logs can contain full filesystem paths because they are intended for diagnosis on the same computer. **Scan & Diagnostics → Export report** includes recent error context but replaces user-profile, configured, drive-letter, and UNC paths with redaction markers before writing the report. Review any report before sharing it.

The application does not require an account. Only the framework-dependent launcher accesses the network, and only when the required .NET Desktop Runtime is missing and the user agrees to download Microsoft's current servicing release.

## Reporting problems

Open a GitHub issue with the application version, Windows version, MSFS distribution, storage type, incident ID, and reproduction steps. Prefer the redacted report exported from **Scan & Diagnostics** over sharing a raw local log. Remove usernames, personal paths, and network credentials from screenshots and review all reports before attaching them. Report security-sensitive filesystem or installer problems using the process in [SECURITY.md](SECURITY.md).

## Project status

See GitHub Releases and the files under `Installer/RELEASE-NOTES-*.txt` for release history.

Contributions are welcome; read [CONTRIBUTING.md](CONTRIBUTING.md) before submitting a change.

## Licence

MSFS 2024 Addons Manager is licensed under the [MIT License](LICENSE), copyright © 2026 Andrew Brown. Third-party components remain under their respective licences; required notices are listed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) and included in release archives.
