using Krypton.Toolkit;
using System.Drawing;
using MSFS2024AddonManager.UI.Layout;
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
        string version = Application.ProductVersion;

        Text = $"{ApplicationName} • Version {version}";

        StartPosition = FormStartPosition.CenterScreen;

        MinimumSize = new Size(1200, 750);
        Size = new Size(1500, 900);

        BackColor = Color.FromArgb(27, 29, 33);
    }

    private void MainForm_Load(object sender, EventArgs e)
    {

    }
}