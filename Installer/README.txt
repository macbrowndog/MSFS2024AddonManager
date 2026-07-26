MSFS 2024 ADDONS MANAGER
Version 1.0.1
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


INSTALLATION
------------
1. Extract every file from the downloaded ZIP into one folder.
2. Double-click "Install and Run.cmd".
3. Approve the Windows administrator prompt.
4. If the Microsoft .NET 10 Desktop Runtime is missing, choose Yes when
   asked to download and install it.
5. The Addons Manager will open automatically.

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
3. Open Addons and select an addon.
4. Add the addon to the active profile.
5. Repeat for other addons required by that profile.

Profiles help organise groups such as Airliners, VFR, VR, Helicopters,
Testing, or specific destinations.


IMPORTANT SAFETY NOTES
----------------------
- Administrator permission is required to create directory symbolic links
  on systems where Windows Developer Mode is not providing permission.
- Do not delete original addon library folders while their addons are active.
- Close Microsoft Flight Simulator before changing enabled addons.
- The manager does not overwrite real folders in Community or Community2024.
- Back up important simulator configuration before making major changes.


STARTING THE APPLICATION LATER
------------------------------
Use "Install and Run.cmd" again, or right-click
MSFS2024AddonManager.exe and choose "Run as administrator".


SYSTEM REQUIREMENTS
-------------------
- Windows 10 or Windows 11, 64-bit
- Microsoft .NET 10 Desktop Runtime
- Administrator permission for symbolic-link management
- Microsoft Flight Simulator 2024 for simulator use

The included launcher checks for .NET 10 and offers to download the
official Microsoft runtime when it is not installed.
