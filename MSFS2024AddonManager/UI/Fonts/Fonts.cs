using System.Drawing;

namespace MSFS2024AddonManager.UI.Fonts;

public static class Fonts
{
    // Window Title
    public static readonly Font Header =
        Create(24f, FontStyle.Bold);

    // Section Titles
    public static readonly Font Title =
        Create(16f, FontStyle.Bold);

    // Standard UI Text
    public static readonly Font Normal =
        Create(13.5f, FontStyle.Regular);

    // Smaller Labels
    public static readonly Font Small =
        Create(12f, FontStyle.Regular);

    // Buttons
    public static readonly Font Button =
        Create(13.5f, FontStyle.Bold);

    // Status Bar
    public static readonly Font Status =
        Create(12f, FontStyle.Regular);

    // Large Dashboard Numbers
    public static readonly Font DashboardValue =
        Create(37f, FontStyle.Bold);

    private static Font Create(float pixelSize, FontStyle style)
    {
        return new Font(
            "Segoe UI",
            pixelSize,
            style,
            GraphicsUnit.Pixel);
    }
}
