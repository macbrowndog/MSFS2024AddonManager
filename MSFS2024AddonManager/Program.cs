using MSFS2024AddonManager.Services;

namespace MSFS2024AddonManager;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        ThemeService.ApplyTheme();

        Application.Run(new MainForm());
    }
}