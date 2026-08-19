using Krypton.Toolkit;
using MSFS2024AddonManager.Models;
using MSFS2024AddonManager.Services;
using MSFS2024AddonManager.UI.Controls;
using MSFS2024AddonManager.UI.Themes;
using ThemeService = MSFS2024AddonManager.UI.Themes.ThemeService;
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
    private readonly Dictionary<string, Label> enabledCategoryValues =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Label pathLabel = new();
    private readonly Label scanStatusLabel = new();
    private readonly Button scanButton = new();
    private bool hasAutoScanned;

    public DashboardView()
    {
        Dock = DockStyle.Fill;
        StateCommon.Color1 = AppColors.Background;
        BuildInterface();
        Shown();
    }

    protected override async void OnCreateControl()
    {
        base.OnCreateControl();

        if (hasAutoScanned || DesignMode)
        {
            return;
        }

        hasAutoScanned = true;
        if (settingsService.Load().ScanOnStartup)
        {
            await RunQuickScanAsync();
        }
    }

    private void BuildInterface()
    {
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(36, 28, 36, 28),
            BackColor = AppColors.Background,
            ColumnCount = 1,
            RowCount = 4
        };
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 178));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 158));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        page.Controls.Add(CreateHeading(), 0, 0);
        page.Controls.Add(CreateStatistics(), 0, 1);
        page.Controls.Add(CreateEnabledCategoryPanel(), 0, 2);
        page.Controls.Add(CreateCommunityPanel(), 0, 3);
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
            Text = "DASHBOARD / LIBRARY STATUS",
            Font = AppFonts.Header,
            ForeColor = AppColors.Accent,
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

    private Control CreateEnabledCategoryPanel()
    {
        string[] categories =
            ["Aircraft", "Airports", "Scenery", "Liveries", "Utilities", "Other"];
        var section = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppColors.Background,
            Padding = new Padding(0, 0, 0, 14)
        };
        var categoryLayout = new AvionicsTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = AppColors.Surface,
            ColumnCount = 1,
            RowCount = 2
        };
        categoryLayout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));
        categoryLayout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 38));
        categoryLayout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = AppColors.Surface,
            Padding = new Padding(14, 0, 14, 12),
            ColumnCount = categories.Length,
            RowCount = 1
        };
        var heading = new Label
        {
            Text = "ENABLED CATEGORIES / ACTIVE PACKAGES",
            Font = AppFonts.Title,
            ForeColor = AppColors.Accent,
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 10, 0, 0),
            BackColor = AppColors.Surface,
            TextAlign = ContentAlignment.TopLeft
        };

        categoryLayout.Controls.Add(heading, 0, 0);
        categoryLayout.Controls.Add(grid, 0, 1);
        section.Controls.Add(categoryLayout);

        foreach (string category in categories)
        {
            grid.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100f / categories.Length));
            Label value = CreateCategoryValueLabel();
            enabledCategoryValues.Add(category, value);
            grid.Controls.Add(CreateCategoryTile(category, value));
        }

        return section;
    }

    private static Control CreateCategoryTile(string category, Label value)
    {
        var panel = new AvionicsPanel
        {
            Dock = DockStyle.Fill,
            BackColor = AppColors.SurfaceLight,
            Margin = new Padding(4)
        };
        panel.Controls.Add(new Label
        {
            Text = category.ToUpperInvariant(),
            Font = AppFonts.Small,
            ForeColor = AppColors.SecondaryText,
            AutoSize = true,
            Location = new Point(12, 10)
        });
        value.Location = new Point(10, 35);
        panel.Controls.Add(value);
        return panel;
    }

    private static Control CreateTile(string title, Label value)
    {
        var panel = new AvionicsPanel
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
        var panel = new AvionicsPanel
        {
            Dock = DockStyle.Fill,
            BackColor = AppColors.Surface,
            Padding = new Padding(20)
        };

        panel.Controls.Add(new Label
        {
            Text = "COMMUNITY FOLDER / LINK DESTINATION",
            Font = AppFonts.Title,
            ForeColor = AppColors.Accent,
            AutoSize = true,
            Location = new Point(20, 18)
        });

        pathLabel.Font = AppFonts.Readout;
        pathLabel.ForeColor = AppColors.Cyan;
        pathLabel.AutoEllipsis = true;
        pathLabel.Location = new Point(20, 57);
        pathLabel.Size = new Size(650, 58);
        pathLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        scanStatusLabel.Font = AppFonts.Small;
        scanStatusLabel.ForeColor = AppColors.SecondaryText;
        scanStatusLabel.AutoSize = true;
        scanStatusLabel.Location = new Point(20, 126);

        scanButton.Text = "QUICK SCAN";
        scanButton.Size = new Size(126, 40);
        scanButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        ThemeService.StyleStandardButton(scanButton, primary: true);
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
        string[] communityFolders = AddonScanner
            .GetCommunityFolders(
                settings.CommunityFolder,
                settings.Community2024Folder)
            .ToArray();
        pathLabel.Text = communityFolders.Length == 0
            ? string.IsNullOrWhiteSpace(settings.CommunityFolder)
                ? "Not configured — open Settings to choose a folder."
                : settings.CommunityFolder
            : string.Join(
                Environment.NewLine,
                communityFolders.Select(path => $"{Path.GetFileName(path)}: {path}"));
        communityValue.Text = Directory.Exists(settings.CommunityFolder) ? "CONNECTED" : "NOT SET";
        communityValue.ForeColor = Directory.Exists(settings.CommunityFolder)
            ? AppColors.Success
            : AppColors.Warning;
        librariesValue.Text = settings.AddonLibraries.Count.ToString();
        enabledValue.Text = "—";
        disabledValue.Text = "—";
        SetEnabledCategoryValues(null);
        scanStatusLabel.Text = "Run Quick Scan to refresh addon totals.";
    }

    private async void ScanButton_Click(object? sender, EventArgs e)
    {
        await RunQuickScanAsync();
    }

    private async Task RunQuickScanAsync()
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
            SetEnabledCategoryValues(summary.EnabledByCategory);
            pathLabel.Text = summary.CommunityFolders.Count == 0
                ? "No Community folders were available."
                : string.Join(
                    Environment.NewLine,
                    summary.CommunityFolders.Select(folder =>
                        $"{folder.Name}: {folder.ItemCount} items — {folder.Path}"));
            scanStatusLabel.ForeColor = AppColors.Success;
            scanStatusLabel.Text =
                $"Scan complete: {summary.TotalAddons} addons found; {summary.CommunityFolders.Count} Community folder(s) scanned at {summary.CompletedAt:HH:mm}.";
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
            scanButton.Text = "QUICK SCAN";
        }
    }

    private static Label CreateValueLabel() => new()
    {
        Text = "—",
        Font = AppFonts.DashboardValue,
        ForeColor = AppColors.Cyan,
        AutoSize = true
    };

    private static Label CreateCategoryValueLabel() => new()
    {
        Text = "—",
        Font = AppFonts.Title,
        ForeColor = AppColors.Success,
        AutoSize = true
    };

    private void SetEnabledCategoryValues(
        IReadOnlyDictionary<string, int>? categoryCounts)
    {
        foreach ((string category, Label value) in enabledCategoryValues)
        {
            value.Text = categoryCounts is null
                ? "—"
                : categoryCounts.TryGetValue(category, out int count)
                    ? count.ToString()
                    : "0";
        }
    }
}
