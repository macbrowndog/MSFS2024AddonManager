using Krypton.Toolkit;
using MSFS2024AddonManager.Models;
using MSFS2024AddonManager.Services;
using AppColors = MSFS2024AddonManager.UI.Colors.Colors;
using AppFonts = MSFS2024AddonManager.UI.Fonts.Fonts;

namespace MSFS2024AddonManager.Views;

public sealed class DashboardView : KryptonPanel
{
    private readonly SettingsService settingsService = new();
    private readonly AddonScanner addonScanner = new();
    private readonly Label communityValue = CreateValueLabel();
    private readonly Label librariesValue = CreateValueLabel();
    private readonly Label enabledValue = CreateValueLabel();
    private readonly Label disabledValue = CreateValueLabel();
    private readonly Label pathLabel = new();
    private readonly Label scanStatusLabel = new();
    private readonly KryptonButton scanButton = new();

    public DashboardView()
    {
        Dock = DockStyle.Fill;
        StateCommon.Color1 = AppColors.Background;
        BuildInterface();
        Shown();
    }

    private void BuildInterface()
    {
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(36, 28, 36, 28),
            BackColor = AppColors.Background,
            ColumnCount = 1,
            RowCount = 3
        };
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 178));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        page.Controls.Add(CreateHeading(), 0, 0);
        page.Controls.Add(CreateStatistics(), 0, 1);
        page.Controls.Add(CreateCommunityPanel(), 0, 2);
        Controls.Add(page);
    }

    private static Control CreateHeading()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppColors.Background
        };
        panel.Controls.Add(new Label
        {
            Text = "Dashboard",
            Font = AppFonts.Header,
            ForeColor = AppColors.Text,
            AutoSize = true,
            Location = new Point(0, 0)
        });
        panel.Controls.Add(new Label
        {
            Text = "Your MSFS 2024 addon library at a glance.",
            Font = AppFonts.Normal,
            ForeColor = AppColors.SecondaryText,
            AutoSize = true,
            Location = new Point(2, 40)
        });
        return panel;
    }

    private Control CreateStatistics()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = AppColors.Background,
            ColumnCount = 4,
            RowCount = 1
        };

        for (int index = 0; index < 4; index++)
        {
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }

        grid.Controls.Add(CreateTile("COMMUNITY", communityValue), 0, 0);
        grid.Controls.Add(CreateTile("LIBRARIES", librariesValue), 1, 0);
        grid.Controls.Add(CreateTile("ENABLED", enabledValue), 2, 0);
        grid.Controls.Add(CreateTile("DISABLED", disabledValue), 3, 0);
        return grid;
    }

    private static Control CreateTile(string title, Label value)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppColors.Surface,
            Margin = new Padding(0, 0, 14, 14)
        };
        panel.Controls.Add(new Label
        {
            Text = title,
            Font = AppFonts.Small,
            ForeColor = AppColors.SecondaryText,
            AutoSize = true,
            Location = new Point(18, 18)
        });
        value.Location = new Point(16, 53);
        panel.Controls.Add(value);
        return panel;
    }

    private Control CreateCommunityPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppColors.Surface,
            Padding = new Padding(20)
        };

        panel.Controls.Add(new Label
        {
            Text = "Community folder",
            Font = AppFonts.Title,
            ForeColor = AppColors.Text,
            AutoSize = true,
            Location = new Point(20, 18)
        });

        pathLabel.Font = AppFonts.Normal;
        pathLabel.ForeColor = AppColors.SecondaryText;
        pathLabel.AutoEllipsis = true;
        pathLabel.Location = new Point(20, 57);
        pathLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        scanStatusLabel.Font = AppFonts.Small;
        scanStatusLabel.ForeColor = AppColors.SecondaryText;
        scanStatusLabel.AutoSize = true;
        scanStatusLabel.Location = new Point(20, 96);

        scanButton.Text = "Quick Scan";
        scanButton.Size = new Size(126, 40);
        scanButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        scanButton.StateCommon.Back.Color1 = AppColors.Accent;
        scanButton.StateCommon.Back.Color2 = AppColors.Accent;
        scanButton.StateCommon.Content.ShortText.Color1 = Color.White;
        scanButton.StateCommon.Content.ShortText.Font = AppFonts.Button;
        scanButton.Click += ScanButton_Click;

        panel.Controls.AddRange([pathLabel, scanStatusLabel, scanButton]);
        panel.Resize += (_, _) =>
        {
            pathLabel.Width = Math.Max(200, panel.ClientSize.Width - 190);
            scanButton.Location = new Point(panel.ClientSize.Width - 148, 48);
        };
        return panel;
    }

    private void Shown()
    {
        AppSettings settings = settingsService.Load();
        pathLabel.Text = string.IsNullOrWhiteSpace(settings.CommunityFolder)
            ? "Not configured — open Settings to choose a folder."
            : settings.CommunityFolder;
        communityValue.Text = Directory.Exists(settings.CommunityFolder) ? "CONNECTED" : "NOT SET";
        communityValue.ForeColor = Directory.Exists(settings.CommunityFolder)
            ? AppColors.Success
            : AppColors.Warning;
        librariesValue.Text = settings.AddonLibraries.Count.ToString();
        enabledValue.Text = "—";
        disabledValue.Text = "—";
        scanStatusLabel.Text = "Run Quick Scan to refresh addon totals.";
    }

    private async void ScanButton_Click(object? sender, EventArgs e)
    {
        scanButton.Enabled = false;
        scanButton.Text = "Scanning...";
        scanStatusLabel.ForeColor = AppColors.SecondaryText;
        scanStatusLabel.Text = "Checking configured addon libraries...";

        try
        {
            ScanSummary summary = await addonScanner.ScanAsync(settingsService.Load());
            communityValue.Text = summary.CommunityAvailable
                ? summary.CommunityItems.ToString()
                : "OFFLINE";
            communityValue.ForeColor = summary.CommunityAvailable
                ? AppColors.Success
                : AppColors.Warning;
            librariesValue.Text = $"{summary.AvailableLibraries}/{summary.ConfiguredLibraries}";
            enabledValue.Text = summary.EnabledAddons.ToString();
            disabledValue.Text = summary.DisabledAddons.ToString();
            scanStatusLabel.ForeColor = AppColors.Success;
            scanStatusLabel.Text =
                $"Scan complete: {summary.TotalAddons} addons found at {summary.CompletedAt:HH:mm}.";
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            scanStatusLabel.ForeColor = AppColors.Error;
            scanStatusLabel.Text = $"Scan could not complete: {exception.Message}";
        }
        finally
        {
            scanButton.Enabled = true;
            scanButton.Text = "Quick Scan";
        }
    }

    private static Label CreateValueLabel() => new()
    {
        Text = "—",
        Font = AppFonts.DashboardValue,
        ForeColor = AppColors.Text,
        AutoSize = true
    };
}
