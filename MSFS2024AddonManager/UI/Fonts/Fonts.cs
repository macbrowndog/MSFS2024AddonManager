using System.Drawing;

namespace MSFS2024AddonManager.UI.Fonts;

public static class Fonts
{
    public static readonly Font Header =
        CreateTechnical(21f, FontStyle.Bold);

    public static readonly Font Title =
        CreateTechnical(13f, FontStyle.Bold);

    public static readonly Font Normal =
        CreateInterface(12.5f, FontStyle.Regular);

    public static readonly Font Small =
        CreateTechnical(10.5f, FontStyle.Regular);

    public static readonly Font Button =
        CreateTechnical(11.5f, FontStyle.Bold);

    public static readonly Font Status =
        CreateTechnical(10f, FontStyle.Regular);

    public static readonly Font DashboardValue =
        CreateTechnical(38f, FontStyle.Bold);

    public static readonly Font CategoryValue =
        CreateTechnical(26f, FontStyle.Bold);

    public static readonly Font Instrument =
        CreateTechnical(11f, FontStyle.Bold);

    public static readonly Font Readout =
        CreateTechnical(12f, FontStyle.Regular);

    public static readonly Font LibraryTree =
        CreateTechnical(15f, FontStyle.Regular);

    private static Font CreateInterface(float pixelSize, FontStyle style)
    {
        return new Font(
            "Segoe UI",
            pixelSize,
            style,
            GraphicsUnit.Pixel);
    }

    private static Font CreateTechnical(float pixelSize, FontStyle style)
    {
        return new Font(
            "Consolas",
            pixelSize,
            style,
            GraphicsUnit.Pixel);
    }
}
