# Release process

Pushing a version tag runs `.github/workflows/release.yml`. The workflow builds and tests the solution, publishes both Windows x64 distributions, optionally Authenticode-signs their executables, creates SHA-256 checksums, attaches the curated release notes, and publishes a GitHub Release.

Manual workflow runs can publish an existing historical tag. For those runs, the workflow checks out release tooling from the default branch and source from the selected tag. This allows tags that predate GitHub Actions to be backfilled without rebuilding them from newer application source. Historical tags without a test project are built and marked with a workflow warning; tags containing tests must pass them.

## Distribution variants

- `win-x64-self-contained` includes the .NET Desktop Runtime and can run without a separate .NET installation. It is the recommended download for most users.
- `win-x64-framework-dependent` is smaller and requires a supported .NET 10 Desktop Runtime. Its launcher follows Microsoft's `aka.ms` servicing-channel URL, so it does not pin a runtime patch.

Both variants are single-file application publishes. Trimming is deliberately disabled because Windows Forms and third-party UI libraries rely on reflection and are not safely trim-compatible by default.

## Configure Authenticode signing

Obtain a publicly trusted Windows code-signing certificate and add these GitHub Actions repository secrets:

- `AUTHENTICODE_CERTIFICATE_BASE64`: the Base64 encoding of the complete PFX file.
- `AUTHENTICODE_CERTIFICATE_PASSWORD`: the PFX password.

For example, create the Base64 value locally in PowerShell without committing the certificate:

```powershell
$bytes = [IO.File]::ReadAllBytes("C:\secure\code-signing.pfx")
[Convert]::ToBase64String($bytes) | Set-Clipboard
```

The workflow writes the certificate only to the hosted runner's temporary directory and deletes it after packaging. SignTool uses SHA-256 file digests and an RFC 3161 SHA-256 timestamp, then verifies every signature before the archives are created. If the secrets are absent, the workflow still creates unsigned artifacts and records that fact in `SIGNING.txt` and the workflow summary.

For stronger key protection, migrate the signing step to a managed signing service or hardware-backed certificate when one is available; the publishing and verification stages can remain unchanged.

## Publish a release

1. Update `<Version>` in `MSFS2024AddonManager/MSFS2024AddonManager.csproj` and add `Installer/RELEASE-NOTES-<version>.txt`.
2. Merge the release commit into `master`.
3. Create and push the matching tag, such as `v1.1.0`.
4. Confirm that the Release workflow completed and that `SHA256SUMS.txt` matches both attached ZIP files.

The workflow rejects malformed tags, tags that do not match the project version, and releases without a matching notes file. The notes file is used as the GitHub Release description, attached as a separate asset, and included in both distribution archives.

To backfill `v1.0.1` or `v1.0.2`, run the Release workflow manually and enter the existing tag. Leave **Mark as Latest** off for older versions; enable it only for the release that should receive GitHub's Latest label. Rerunning a tag replaces its generated assets and refreshes its release description.
