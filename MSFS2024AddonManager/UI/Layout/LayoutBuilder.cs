using Krypton.Toolkit;
using MSFS2024AddonManager.Views;
using AppColors = MSFS2024AddonManager.UI.Colors.Colors;
using AppFonts = MSFS2024AddonManager.UI.Fonts.Fonts;

namespace MSFS2024AddonManager.UI.Layout;

public static class LayoutBuilder
{
    public static void Build(KryptonForm form)
    {
        form.SuspendLayout();
        form.Controls.Clear();

        Panel contentHost = BuildContent();
        Panel body = BuildBody(contentHost);
        Control statusBar = BuildStatusBar();
        Control header = BuildHeader();
        Control navigation = BuildNavigation(contentHost);

        body.Controls.Add(contentHost);
        body.Controls.Add(navigation);
        form.Controls.Add(body);
        form.Controls.Add(statusBar);
        form.Controls.Add(header);

        ShowView(contentHost, new DashboardView());
        form.ResumeLayout(true);
    }

    private static Control BuildHeader()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = UIConstants.HeaderHeight,
            BackColor = AppColors.Surface,
            Padding = new Padding(24, 0, 24, 0)
        };

        var title = new Label
        {
            Text = UIConstants.ApplicationTitle,
            Font = AppFonts.Header,
            ForeColor = AppColors.Text,
            AutoSize = true,
            Location = new Point(24, 18)
        };

        var version = new Label
        {
            Text = $"Version {Application.ProductVersion}",
            Font = AppFonts.Small,
            ForeColor = AppColors.SecondaryText,
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        header.Controls.AddRange([title, version]);
        header.Resize += (_, _) =>
            version.Location = new Point(header.ClientSize.Width - version.Width - 24, 27);
        return header;
    }

    private static Control BuildNavigation(Panel contentHost)
    {
        var navigation = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            Width = UIConstants.NavigationWidth,
            BackColor = AppColors.Navigation,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(12, 22, 12, 12)
        };

        AddNavigationButton(navigation, "Dashboard", () => ShowView(contentHost, new DashboardView()));
        AddNavigationButton(navigation, "Addons", () => ShowView(contentHost, new AddonsView()));
        AddNavigationButton(navigation, "Profiles", () => ShowView(contentHost, new ProfilesView()));
        AddNavigationButton(navigation, "Scan", () => ShowView(contentHost, new ScanView()));
        AddNavigationButton(navigation, "Settings", () => ShowView(contentHost, new SettingsView()));

        return navigation;
    }

    private static Panel BuildBody(Panel contentHost) => new()
    {
        Dock = DockStyle.Fill,
        BackColor = AppColors.Background
    };

    private static Panel BuildContent() => new()
    {
        Dock = DockStyle.Fill,
        BackColor = AppColors.Background
    };

    private static Control BuildStatusBar()
    {
        var status = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = UIConstants.StatusHeight,
            BackColor = AppColors.Surface,
            Padding = new Padding(16, 0, 16, 0)
        };

        status.Controls.Add(new Label
        {
            Text = "Ready",
            Font = AppFonts.Status,
            ForeColor = AppColors.SecondaryText,
            AutoSize = true,
            Location = new Point(16, 9)
        });

        return status;
    }

    private static void AddNavigationButton(
        FlowLayoutPanel navigation,
        string text,
        Action onClick)
    {
        var button = new KryptonButton
        {
            Text = text,
            Size = new Size(UIConstants.NavigationWidth - 24, 46),
            Margin = new Padding(0, 0, 0, 8)
        };

        button.StateCommon.Back.Color1 = AppColors.Navigation;
        button.StateCommon.Back.Color2 = AppColors.Navigation;
        button.StateCommon.Content.ShortText.Color1 = AppColors.Text;
        button.StateCommon.Content.ShortText.Font = AppFonts.Button;
        button.StateTracking.Back.Color1 = AppColors.SurfaceLight;
        button.StateTracking.Back.Color2 = AppColors.SurfaceLight;
        button.StatePressed.Back.Color1 = AppColors.Accent;
        button.StatePressed.Back.Color2 = AppColors.Accent;
        button.Click += (_, _) => onClick();
        navigation.Controls.Add(button);
    }

    private static void ShowView(Panel contentHost, Control view)
    {
        contentHost.SuspendLayout();
        contentHost.Controls.Clear();
        view.Dock = DockStyle.Fill;
        contentHost.Controls.Add(view);
        contentHost.ResumeLayout(true);
    }
}
