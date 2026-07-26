using Krypton.Toolkit;
using System.Drawing;
using MSFS2024AddonManager.UI.Layout;
using AppColors = MSFS2024AddonManager.UI.Colors.Colors;

namespace MSFS2024AddonManager;

public partial class MainForm : KryptonForm
{
    public const string ApplicationName = "MSFS 2024 Addons Manager";

    public MainForm()
    {
        InitializeComponent();

        ConfigureWindow();

        LayoutBuilder.Build(this);
    }

    private void ConfigureWindow()
    {
        Text = $"{ApplicationName} • Version {UI.UIConstants.ApplicationVersion} • {UI.UIConstants.Copyright}";

        StartPosition = FormStartPosition.CenterScreen;

        MinimumSize = new Size(1200, 750);
        Size = new Size(1500, 900);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;

        BackColor = AppColors.Background;

        Icon? applicationIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        if (applicationIcon is not null)
        {
            Icon = applicationIcon;
        }
    }

    private void MainForm_Load(object sender, EventArgs e)
    {

    }
}
