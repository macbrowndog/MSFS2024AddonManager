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

        var buttons = new List<Button>();
        Button dashboardButton = AddNavigationButton(
            navigation,
            "Dashboard",
            button =>
            {
                SelectNavigationButton(buttons, button);
                ShowView(contentHost, new DashboardView());
            });
        buttons.Add(dashboardButton);
        buttons.Add(AddNavigationButton(
            navigation,
            "Addons",
            button =>
            {
                SelectNavigationButton(buttons, button);
                ShowView(contentHost, new AddonsView());
            }));
        buttons.Add(AddNavigationButton(
            navigation,
            "Profiles",
            button =>
            {
                SelectNavigationButton(buttons, button);
                ShowView(contentHost, new ProfilesView());
            }));
        buttons.Add(AddNavigationButton(
            navigation,
            "Scan",
            button =>
            {
                SelectNavigationButton(buttons, button);
                ShowView(contentHost, new ScanView());
            }));
        buttons.Add(AddNavigationButton(
            navigation,
            "Settings",
            button =>
            {
                SelectNavigationButton(buttons, button);
                ShowView(contentHost, new SettingsView());
            }));

        SelectNavigationButton(buttons, dashboardButton);

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

    private static Button AddNavigationButton(
        FlowLayoutPanel navigation,
        string text,
        Action<Button> onClick)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(UIConstants.NavigationWidth - 24, 46),
            Margin = new Padding(0, 0, 0, 8),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppColors.Navigation,
            ForeColor = AppColors.Text,
            Font = AppFonts.Button,
            Cursor = Cursors.Hand,
            TabStop = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(18, 0, 0, 0),
            UseVisualStyleBackColor = false
        };

        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = AppColors.SurfaceLight;
        button.FlatAppearance.MouseDownBackColor = AppColors.Accent;
        button.Click += (_, _) => onClick(button);
        navigation.Controls.Add(button);
        return button;
    }

    private static void SelectNavigationButton(
        IEnumerable<Button> buttons,
        Button selectedButton)
    {
        foreach (Button button in buttons)
        {
            bool isSelected = ReferenceEquals(button, selectedButton);
            button.BackColor = isSelected ? AppColors.Accent : AppColors.Navigation;
            button.ForeColor = Color.White;
        }
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
