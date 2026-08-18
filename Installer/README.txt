MSFS 2024 ADDONS MANAGER
Andrew Brown © 2026

DESCRIPTION
-----------
MSFS 2024 Addons Manager is a Windows utility for organising Microsoft
Flight Simulator 2024 addons without moving the original addon files.

Addons can remain in libraries on another folder, drive, or supported
network location. The manager enables an addon by creating a directory
symbolic link in Community or Community2024, and disables it by safely
removing that link.

Main features:

- Scan one or more addon libraries.
- Manage both Community and Community2024.
- View active and inactive addons separately.
- Search and filter addons by category and location.
- Display addon details and thumbnails when available.
- Enable or disable addons using symbolic links.
- Create profiles and assign addons to them.
- Automatically detect common MSFS 2024 folder locations.
- Manually select folders for custom MSFS installations.
- Quick Scan dashboard with addon and Community folder totals.
- Recursive package discovery for addons stored several folders deep.
- Resizable addon-library directory tree.
- Dashboard breakdown of enabled addons by category.
- DPI-safe typography for resized windows and high-resolution displays.


INSTALLATION
------------
Two Windows x64 downloads are available:

- Self-contained: includes .NET and is the recommended download.
- Framework-dependent: smaller, but requires the .NET 10 Desktop Runtime.

Verify the downloaded ZIP against SHA256SUMS.txt on the GitHub Release,
then extract every file into one folder.

For the self-contained download, run MSFS2024AddonManager.exe. For the
framework-dependent download, run "Install and Run.cmd". If .NET is missing,
the launcher offers to download the current .NET 10 Desktop Runtime servicing
release from Microsoft and verifies its Authenticode signature before running
it. Approve elevation when required for symbolic-link management.

Keep all files from the ZIP together in the same folder.


FIRST-TIME SETUP
----------------
1. Open Settings.
2. Check the Community folder path.
3. Add or check the optional Community2024 folder if you use it.
4. Under Addon libraries, select Add library.
5. Choose a folder containing your stored addons.
6. Add more libraries if required.
7. Enable "Scan addon libraries when the application starts" if wanted.
8. Open Scan and run a scan, or use Quick Scan on the Dashboard.

The default Community folder remains the primary destination.
Community2024 is optional and can be used when required by an addon.


VIEWING ADDONS
--------------
1. Open Addons.
2. Use the search box to find an addon.
3. Use the category dropdown to filter the addon type.
4. Use the location dropdown to view:
   - All locations
   - Addon libraries
   - Community
   - Community2024
5. Select an addon card to view its details.

Active addons are shown separately from inactive addons.


ENABLING AN ADDON
-----------------
1. Run the application as administrator.
2. Open Addons.
3. Select an inactive addon.
4. Choose Community or Community2024 as the destination.
5. Select Enable addon.

The original addon folder is not moved or copied. A directory symbolic
link is created in the selected Community folder.


DISABLING AN ADDON
------------------
1. Open Addons.
2. Select an active addon.
3. Select its Community or Community2024 location.
4. Select Disable addon.

Only a symbolic link created for the matching addon is removed. The
original addon files in the library remain untouched.


PROFILES
--------
1. Open Profiles.
2. Create a profile and give it a name.
3. Select Edit addons on the profile.
4. Open Addons and select an addon.
5. Add the addon to the editing profile.
6. Repeat for other addons required by that profile.
7. Return to Profiles and select Preview & apply.
8. Review the proposed changes for the default Community folder.
9. Select Apply profile.

Applying a profile enables assigned managed addons and disables managed addons
that are not assigned to it. Real folders and Community-only packages are not
removed. Missing assigned addons and unresolved legacy assignments stop the
operation before changes begin. If an operation fails, earlier changes are
rolled back in reverse order and any rollback failure is reported.

Assignments store a stable package identity and canonical source path, so
packages with the same folder name in different libraries remain distinct.
Older folder-name-only assignments are migrated when they identify exactly
one package. Reassign a package after moving it to confirm its new source.

Profiles can represent groups such as Airliners, VFR, VR, Helicopters,
Testing, or specific destinations.


IMPORTANT SAFETY NOTES
----------------------
- Administrator permission is required to create directory symbolic links
  on systems where Windows Developer Mode is not providing permission.
- Do not delete original addon library folders while their addons are active.
- Close Microsoft Flight Simulator before changing enabled addons. Enable and
  disable operations are blocked while FlightSimulator2024.exe is running.
- The manager does not overwrite real folders in Community or Community2024.
- Back up important simulator configuration before making major changes.


ERROR LOGS AND REPORTS
----------------------
Unexpected errors are written to a rolling local log under:

%LOCALAPPDATA%\MSFS2024AddonManager\logs

The application retains at most five log files of approximately 1 MiB each.
An error dialog provides an incident ID that can be matched to the log. Raw
local logs can contain full filesystem paths. Reports exported from Scan &
Diagnostics include recent error context but redact user-profile, configured,
drive-letter, and UNC paths. Review any report before sharing it.


STARTING THE APPLICATION LATER
------------------------------
Run MSFS2024AddonManager.exe. The framework-dependent download can also be
started with "Install and Run.cmd" so that its runtime prerequisite is checked.


SYSTEM REQUIREMENTS
-------------------
- Windows 10 or Windows 11, 64-bit
- Microsoft .NET 10 Desktop Runtime (framework-dependent download only)
- Administrator permission for symbolic-link management
- Microsoft Flight Simulator 2024 for simulator use

The framework-dependent launcher's Microsoft servicing-channel URL always
selects a current .NET 10 Desktop Runtime patch instead of pinning one version.


LICENCE
-------
MSFS 2024 Addons Manager is distributed under the MIT License, copyright
Andrew Brown © 2026. See LICENSE for the complete terms. Third-party software
remains under its own licence; see THIRD-PARTY-NOTICES.md for required notices.
