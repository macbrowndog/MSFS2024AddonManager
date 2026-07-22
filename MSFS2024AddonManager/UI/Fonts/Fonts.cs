using System.Drawing;

namespace MSFS2024AddonManager.UI.Fonts;

public static class Fonts
{
    // Window Title
    public static readonly Font Header =
        new("Segoe UI", 18f, FontStyle.Bold);

    // Section Titles
    public static readonly Font Title =
        new("Segoe UI", 12f, FontStyle.Bold);

    // Standard UI Text
    public static readonly Font Normal =
        new("Segoe UI", 10f, FontStyle.Regular);

    // Smaller Labels
    public static readonly Font Small =
        new("Segoe UI", 9f, FontStyle.Regular);

    // Buttons
    public static readonly Font Button =
        new("Segoe UI", 10f, FontStyle.Bold);

    // Status Bar
    public static readonly Font Status =
        new("Segoe UI", 9f, FontStyle.Regular);

    // Large Dashboard Numbers
    public static readonly Font DashboardValue =
        new("Segoe UI", 28f, FontStyle.Bold);
}