using Krypton.Toolkit;

namespace MSFS2024AddonManager;

public partial class MainForm : KryptonForm
{
    public const string ApplicationName = "MSFS 2024 Addons Manager";

    public MainForm()
    {
        InitializeComponent();

        ConfigureWindow();
    }

    private void ConfigureWindow()
    {
        string version = Application.ProductVersion;

        Text = $"{ApplicationName} • Version {version}";

        StartPosition = FormStartPosition.CenterScreen;

        MinimumSize = new Size(1200, 750);
        Size = new Size(1500, 900);
    }
}