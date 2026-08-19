using Krypton.Toolkit;
using MSFS2024AddonManager.Models;
using MSFS2024AddonManager.Services;
using MSFS2024AddonManager.UI.Controls;
using MSFS2024AddonManager.UI.Themes;
using ThemeService = MSFS2024AddonManager.UI.Themes.ThemeService;
using AppColors = MSFS2024AddonManager.UI.Colors.Colors;
using AppFonts = MSFS2024AddonManager.UI.Fonts.Fonts;

namespace MSFS2024AddonManager.Views;

public sealed class SettingsView : KryptonPanel
{
    private readonly SettingsService settingsService;
    private readonly AppSettings settings;
    private readonly KryptonTextBox communityFolderTextBox = new();
    private readonly KryptonTextBox community2024FolderTextBox = new();
    private readonly ListBox addonLibrariesList = new();
    private readonly CheckBox autoDetectCheckBox = new();
    private readonly CheckBox scanOnStartupCheckBox = new();
    private readonly Label feedbackLabel = new();
    private bool isLoadingSettings;

    public SettingsView()
    {
        settingsService = new SettingsService();
        settings = settingsService.Load();

        Dock = DockStyle.Fill;
        StateCommon.Color1 = AppColors.Background;
        BuildInterface();
        LoadSettingsIntoControls();
    }

    private void BuildInterface()
    {
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(36, 28, 36, 28),
            BackColor = AppColors.Background,
            AutoScroll = true,
            ColumnCount = 1,
            RowCount = 4
        };
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 208));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 290));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));

        page.Controls.Add(CreateHeading(), 0, 0);
        page.Controls.Add(CreateCommunitySection(), 0, 1);
        page.Controls.Add(CreateLibrariesSection(), 0, 2);
        page.Controls.Add(CreateOptionsSection(), 0, 3);
        Controls.Add(page);
    }

    private Control CreateHeading()
    {
        var panel = CreateSurfacePanel();
        panel.BackColor = AppColors.Background;

        panel.Controls.Add(new Label
        {
            Text = "SETTINGS / STORAGE CONFIGURATION",
            Font = AppFonts.Header,
            ForeColor = AppColors.Accent,
            AutoSize = true,
            Location = new Point(0, 0)
        });
        panel.Controls.Add(new Label
        {
            Text = "Choose where MSFS 2024 and your addon libraries are stored.",
            Font = AppFonts.Normal,
            ForeColor = AppColors.SecondaryText,
            AutoSize = true,
            Location = new Point(2, 38)
        });

        return panel;
    }

    private Control CreateCommunitySection()
    {
        var panel = CreateSurfacePanel();
        var title = CreateSectionTitle("Community folder (default)");
        title.Location = new Point(18, 14);

        communityFolderTextBox.Location = new Point(18, 52);
        communityFolderTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        communityFolderTextBox.Width = 650;
        ThemeService.StyleTextBox(communityFolderTextBox);

        var browseButton = CreateButton("Browse...", BrowseCommunityFolder);
        browseButton.Location = new Point(684, 50);
        browseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        var detectButton = CreateButton("Auto-detect", DetectCommunityFolder);
        detectButton.Location = new Point(794, 50);
        detectButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        var community2024Title = CreateSectionTitle("Community2024 folder (optional)");
        community2024Title.Location = new Point(18, 94);

        community2024FolderTextBox.Location = new Point(18, 132);
        community2024FolderTextBox.Anchor =
            AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        community2024FolderTextBox.Width = 650;
        ThemeService.StyleTextBox(community2024FolderTextBox);

        var browse2024Button = CreateButton("Browse...", BrowseCommunity2024Folder);
        browse2024Button.Location = new Point(684, 130);
        browse2024Button.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        var detect2024Button = CreateButton("Auto-detect", DetectCommunity2024Folder);
        detect2024Button.Location = new Point(794, 130);
        detect2024Button.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        panel.Controls.AddRange(
        [
            title,
            communityFolderTextBox,
            browseButton,
            detectButton,
            community2024Title,
            community2024FolderTextBox,
            browse2024Button,
            detect2024Button
        ]);
        panel.Resize += (_, _) =>
        {
            communityFolderTextBox.Width = Math.Max(240, panel.ClientSize.Width - 278);
            community2024FolderTextBox.Width =
                Math.Max(240, panel.ClientSize.Width - 278);
            browseButton.Left = panel.ClientSize.Width - 242;
            detectButton.Left = panel.ClientSize.Width - 132;
            browse2024Button.Left = panel.ClientSize.Width - 242;
            detect2024Button.Left = panel.ClientSize.Width - 132;
        };
        return panel;
    }

    private Control CreateLibrariesSection()
    {
        var panel = CreateSurfacePanel();
        var title = CreateSectionTitle("Addon libraries");
        title.Location = new Point(18, 14);

        var description = new Label
        {
            Text = "Add one or more folders containing addons. Libraries can be on another drive or a network share.",
            Font = AppFonts.Small,
            ForeColor = AppColors.SecondaryText,
            AutoSize = true,
            Location = new Point(18, 43)
        };

        addonLibrariesList.Location = new Point(18, 72);
        addonLibrariesList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        addonLibrariesList.Size = new Size(650, 186);
        addonLibrariesList.BackColor = AppColors.Control;
        addonLibrariesList.ForeColor = AppColors.Cyan;
        addonLibrariesList.Font = AppFonts.Readout;
        addonLibrariesList.BorderStyle = BorderStyle.FixedSingle;

        var addButton = CreateButton("Add library", AddLibrary);
        addButton.Location = new Point(684, 72);
        addButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        var removeButton = CreateButton("Remove", RemoveLibrary);
        removeButton.Location = new Point(684, 116);
        removeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        panel.Controls.AddRange([title, description, addonLibrariesList, addButton, removeButton]);
        panel.Resize += (_, _) =>
        {
            addonLibrariesList.Width = Math.Max(240, panel.ClientSize.Width - 150);
            addButton.Left = panel.ClientSize.Width - 116;
            removeButton.Left = panel.ClientSize.Width - 116;
        };
        return panel;
    }

    private Control CreateOptionsSection()
    {
        var panel = CreateSurfacePanel();

        autoDetectCheckBox.Text = "Automatically detect the MSFS Community folder";
        ThemeService.StyleCheckBox(autoDetectCheckBox);
        autoDetectCheckBox.AutoSize = true;
        autoDetectCheckBox.Location = new Point(18, 18);
        autoDetectCheckBox.CheckedChanged += (_, _) =>
        {
            if (!isLoadingSettings)
            {
                SaveSettings();
            }
        };

        scanOnStartupCheckBox.Text = "Scan addon libraries when the application starts";
        ThemeService.StyleCheckBox(scanOnStartupCheckBox);
        scanOnStartupCheckBox.AutoSize = true;
        scanOnStartupCheckBox.Location = new Point(18, 50);
        scanOnStartupCheckBox.CheckedChanged += (_, _) =>
        {
            if (!isLoadingSettings)
            {
                SaveSettings();
            }
        };

        feedbackLabel.Font = AppFonts.Small;
        feedbackLabel.ForeColor = AppColors.Success;
        feedbackLabel.AutoSize = true;
        feedbackLabel.Location = new Point(18, 84);

        panel.Controls.AddRange([autoDetectCheckBox, scanOnStartupCheckBox, feedbackLabel]);
        return panel;
    }

    private static Panel CreateSurfacePanel() => new AvionicsPanel
    {
        Dock = DockStyle.Fill,
        Margin = new Padding(0, 0, 0, 14),
        BackColor = AppColors.Surface
    };

    private static Label CreateSectionTitle(string text) => new()
    {
        Text = text.ToUpperInvariant(),
        Font = AppFonts.Title,
        ForeColor = AppColors.Accent,
        AutoSize = true
    };

    private static Button CreateButton(string text, EventHandler clickHandler)
    {
        var button = new Button
        {
            Text = text.ToUpperInvariant(),
            Size = new Size(100, 34)
        };
        ThemeService.StyleStandardButton(button);
        button.Click += clickHandler;
        return button;
    }

    private void LoadSettingsIntoControls()
    {
        isLoadingSettings = true;
        communityFolderTextBox.Text = settings.CommunityFolder;
        community2024FolderTextBox.Text = settings.Community2024Folder;
        addonLibrariesList.Items.AddRange(settings.AddonLibraries.Cast<object>().ToArray());
        autoDetectCheckBox.Checked = settings.AutoDetectMsfs;
        scanOnStartupCheckBox.Checked = settings.ScanOnStartup;
        isLoadingSettings = false;

        if (settings.AutoDetectMsfs)
        {
            bool detected = false;
            if (string.IsNullOrWhiteSpace(settings.CommunityFolder))
            {
                string? communityPath = settingsService.DetectCommunityFolder();
                if (communityPath is not null)
                {
                    communityFolderTextBox.Text = communityPath;
                    detected = true;
                }
            }

            if (string.IsNullOrWhiteSpace(settings.Community2024Folder))
            {
                string? community2024Path =
                    settingsService.DetectCommunity2024Folder();
                if (community2024Path is not null)
                {
                    community2024FolderTextBox.Text = community2024Path;
                    detected = true;
                }
            }

            if (detected)
            {
                SaveSettings("Community folders detected and saved.");
            }
        }
    }

    private void BrowseCommunityFolder(object? sender, EventArgs e)
    {
        using var dialog = new KryptonFolderBrowserDialog
        {
            Title = "Select the MSFS 2024 Community folder",
            InitialDirectory = communityFolderTextBox.Text,
            SelectedPath = communityFolderTextBox.Text
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        communityFolderTextBox.Text = dialog.SelectedPath;
        SaveSettings("Community folder saved.");
    }

    private void AddLibrary(object? sender, EventArgs e)
    {
        using var dialog = new KryptonFolderBrowserDialog
        {
            Title = "Select an addon library folder"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK ||
            addonLibrariesList.Items.Cast<string>().Contains(
                dialog.SelectedPath,
                StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        addonLibrariesList.Items.Add(dialog.SelectedPath);
        SaveSettings("Addon library added.");
    }

    private void BrowseCommunity2024Folder(object? sender, EventArgs e)
    {
        using var dialog = new KryptonFolderBrowserDialog
        {
            Title = "Select the optional Community2024 folder",
            InitialDirectory = community2024FolderTextBox.Text,
            SelectedPath = community2024FolderTextBox.Text
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        community2024FolderTextBox.Text = dialog.SelectedPath;
        SaveSettings("Community2024 folder saved.");
    }

    private void RemoveLibrary(object? sender, EventArgs e)
    {
        if (addonLibrariesList.SelectedItem is null)
        {
            return;
        }

        addonLibrariesList.Items.Remove(addonLibrariesList.SelectedItem);
        SaveSettings("Addon library removed.");
    }

    private void DetectCommunityFolder(object? sender, EventArgs e)
    {
        string? detectedPath = settingsService.DetectCommunityFolder();
        if (detectedPath is null)
        {
            feedbackLabel.ForeColor = AppColors.Warning;
            feedbackLabel.Text = "No Community folder was detected. Use Browse to select it manually.";
            return;
        }

        communityFolderTextBox.Text = detectedPath;
        SaveSettings("Community folder detected and saved.");
    }

    private void DetectCommunity2024Folder(object? sender, EventArgs e)
    {
        string? detectedPath = settingsService.DetectCommunity2024Folder();
        if (detectedPath is null)
        {
            feedbackLabel.ForeColor = AppColors.Warning;
            feedbackLabel.Text =
                "No Community2024 folder was detected. This optional folder can be left blank.";
            return;
        }

        community2024FolderTextBox.Text = detectedPath;
        SaveSettings("Community2024 folder detected and saved.");
    }

    private void SaveSettings(string? message = null)
    {
        settings.CommunityFolder = communityFolderTextBox.Text.Trim();
        settings.Community2024Folder = community2024FolderTextBox.Text.Trim();
        settings.AddonLibraries = addonLibrariesList.Items.Cast<string>().ToList();
        settings.AutoDetectMsfs = autoDetectCheckBox.Checked;
        settings.ScanOnStartup = scanOnStartupCheckBox.Checked;
        settingsService.Save(settings);

        if (message is not null)
        {
            feedbackLabel.ForeColor = AppColors.Success;
            feedbackLabel.Text = message;
        }
    }
}
