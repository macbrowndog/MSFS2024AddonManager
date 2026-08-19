using Krypton.Toolkit;
using MSFS2024AddonManager.Views;
using MSFS2024AddonManager.UI.Controls;
using MSFS2024AddonManager.UI.Themes;
using ThemeService = MSFS2024AddonManager.UI.Themes.ThemeService;
using System.Security.Principal;
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
        var header = new AvionicsPanel
        {
            Dock = DockStyle.Top,
            Height = UIConstants.HeaderHeight,
            BackColor = AppColors.Surface,
            Padding = new Padding(20, 0, 20, 0)
        };

        var title = new Label
        {
            Text = "FLIGHT DECK / MSFS 2024 ADDONS MANAGER",
            Font = AppFonts.Header,
            ForeColor = AppColors.Accent,
            AutoSize = true,
            Location = new Point(20, 14)
        };

        var subtitle = new Label
        {
            Text = "ADDON CONTROL • PROFILE MANAGEMENT • SAFE LINK OPERATIONS",
            Font = AppFonts.Small,
            ForeColor = AppColors.SecondaryText,
            AutoSize = true,
            Location = new Point(22, 52)
        };

        var version = new Label
        {
            Text = $"{UIConstants.Copyright.ToUpperInvariant()} • VERSION {UIConstants.ApplicationVersion}",
            Font = AppFonts.Small,
            ForeColor = AppColors.Cyan,
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        var readyFrame = new AvionicsPanel
        {
            Size = new Size(154, 38),
            BackColor = AppColors.Navigation,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        readyFrame.Controls.Add(new Label
        {
            Text = "●",
            Font = AppFonts.Instrument,
            ForeColor = AppColors.Success,
            AutoSize = true,
            Location = new Point(12, 11)
        });
        readyFrame.Controls.Add(new Label
        {
            Text = "SYSTEM READY",
            Font = AppFonts.Instrument,
            ForeColor = AppColors.Accent,
            AutoSize = true,
            Location = new Point(32, 11)
        });

        header.Controls.AddRange([title, subtitle, version, readyFrame]);
        void PositionHeaderReadouts()
        {
            readyFrame.Location = new Point(
                header.ClientSize.Width - readyFrame.Width - 20,
                11);
            version.Location = new Point(
                header.ClientSize.Width - version.Width - 20,
                57);
        }

        header.Resize += (_, _) => PositionHeaderReadouts();
        PositionHeaderReadouts();
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
            Padding = new Padding(12, 16, 12, 12)
        };

        navigation.Controls.Add(new Label
        {
            Text = "NAV / ADDON CONTROL",
            Font = AppFonts.Instrument,
            ForeColor = AppColors.SecondaryText,
            Size = new Size(UIConstants.NavigationWidth - 24, 30),
            Margin = new Padding(4, 0, 0, 9),
            TextAlign = ContentAlignment.MiddleLeft
        });

        var buttons = new List<Button>();
        Button dashboardButton = AddNavigationButton(
            navigation,
            "DASHBOARD",
            button =>
            {
                SelectNavigationButton(buttons, button);
                ShowView(contentHost, new DashboardView());
            });
        buttons.Add(dashboardButton);
        buttons.Add(AddNavigationButton(
            navigation,
            "ADDONS",
            button =>
            {
                SelectNavigationButton(buttons, button);
                ShowView(contentHost, new AddonsView());
            }));
        buttons.Add(AddNavigationButton(
            navigation,
            "PROFILES",
            button =>
            {
                SelectNavigationButton(buttons, button);
                ShowView(contentHost, new ProfilesView());
            }));
        buttons.Add(AddNavigationButton(
            navigation,
            "SCAN / DIAGNOSTICS",
            button =>
            {
                SelectNavigationButton(buttons, button);
                ShowView(contentHost, new ScanView());
            }));
        buttons.Add(AddNavigationButton(
            navigation,
            "SETTINGS",
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
        var status = new AvionicsPanel
        {
            Dock = DockStyle.Bottom,
            Height = UIConstants.StatusHeight,
            BackColor = AppColors.Surface,
            Padding = new Padding(16, 0, 16, 0)
        };

        var readyLabel = new Label
        {
            Text = "PIPELINE STANDBY",
            Font = AppFonts.Status,
            ForeColor = AppColors.SecondaryText,
            AutoSize = true,
            Location = new Point(16, 13)
        };

        bool isAdministrator = IsAdministrator();
        var privilegeLabel = new Label
        {
            Text = isAdministrator
                ? "● ADMINISTRATOR / LINK CONTROL READY"
                : "● STANDARD USER / ELEVATION REQUIRED FOR LINK CHANGES",
            Font = AppFonts.Instrument,
            ForeColor = isAdministrator ? AppColors.Success : AppColors.Accent,
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        status.Controls.AddRange([readyLabel, privilegeLabel]);
        void PositionPrivilegeLabel() =>
            privilegeLabel.Location = new Point(
                status.ClientSize.Width - privilegeLabel.Width - 16,
                12);
        status.Resize += (_, _) => PositionPrivilegeLabel();
        PositionPrivilegeLabel();

        return status;
    }

    private static bool IsAdministrator()
    {
        try
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(
                WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static Button AddNavigationButton(
        FlowLayoutPanel navigation,
        string text,
        Action<Button> onClick)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(UIConstants.NavigationWidth - 24, 42),
            Margin = new Padding(0, 0, 0, 7),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 0, 0)
        };

        ThemeService.StyleStandardButton(button);
        button.BackColor = AppColors.Navigation;
        button.ForeColor = AppColors.Text;
        button.FlatAppearance.BorderColor = AppColors.Border;
        button.FlatAppearance.MouseOverBackColor = AppColors.ControlHover;
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
            button.BackColor = isSelected ? AppColors.Selection : AppColors.Navigation;
            button.ForeColor = isSelected ? AppColors.Accent : AppColors.Text;
            button.FlatAppearance.BorderColor = isSelected
                ? AppColors.Accent
                : AppColors.Border;
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
