# Contributing

Thanks for helping improve MSFS 2024 Addons Manager.

## Before opening an issue

- Check that the problem still occurs on the latest release.
- Close Microsoft Flight Simulator before reproducing link-management issues.
- Remove personal paths, usernames, and network-share credentials from screenshots and logs.
- Include the application version, Windows version, MSFS distribution, storage type, and exact steps to reproduce.

## Development setup

Requirements:

- Windows 10 or Windows 11
- .NET 10 SDK

Restore and build from the repository root:

```powershell
dotnet restore MSFS2024AddonManager.slnx
dotnet build MSFS2024AddonManager.slnx --configuration Release --no-restore
dotnet test MSFS2024AddonManager.slnx --configuration Release --no-build
```

## Pull requests

- Keep each pull request focused on one change.
- Preserve the rule that disabling an addon removes only the matching symbolic link.
- Do not add operations that delete or overwrite real Community-folder content.
- Describe manual tests for local, removable, and network-backed addon libraries when relevant.
- Update release notes for user-visible changes.

## Contribution licence

By submitting a contribution, you confirm that you have the right to provide it and agree that it will be licensed under the repository's [MIT License](LICENSE).
