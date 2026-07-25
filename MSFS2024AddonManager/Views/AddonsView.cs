using Krypton.Toolkit;
using MSFS2024AddonManager.Models;
using MSFS2024AddonManager.Services;
using AppColors = MSFS2024AddonManager.UI.Colors.Colors;
using AppFonts = MSFS2024AddonManager.UI.Fonts.Fonts;

namespace MSFS2024AddonManager.Views;

public sealed class AddonsView : KryptonPanel
{
    private readonly SettingsService settingsService = new();
    private readonly AddonScanner addonScanner = new();
    private readonly KryptonTextBox searchBox = new();
    private readonly ComboBox categoryBox = new();
    private readonly FlowLayoutPanel cardsPanel = new();
    private readonly Label resultLabel = new();
    private readonly Label emptyLabel = new();
    private readonly KryptonButton refreshButton = new();
    private readonly Panel detailsPanel = new();
    private readonly Label detailName = new();
    private readonly Label detailCategory = new();
    private readonly Label detailStatus = new();
    private readonly Label detailAuthor = new();
    private readonly Label detailVersion = new();
    private readonly Label detailFolder = new();
    private readonly Label detailLibrary = new();
    private IReadOnlyList<Addon> addons = [];
    private bool hasLoaded;

    public AddonsView()
    {
        Dock = DockStyle.Fill;
        StateCommon.Color1 = AppColors.Background;
        BuildInterface();
    }

    protected override async void OnCreateControl()
    {
        base.OnCreateControl();

        if (hasLoaded || DesignMode)
        {
            return;
        }

        hasLoaded = true;
        await LoadAddonsAsync();
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
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        page.Controls.Add(CreateHeading(), 0, 0);
        page.Controls.Add(CreateCommandBar(), 0, 1);
        page.Controls.Add(CreateResultsArea(), 0, 2);
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
            Text = "Addons",
            Font = AppFonts.Header,
            ForeColor = AppColors.Text,
            AutoSize = true,
            Location = new Point(0, 0)
        });
        panel.Controls.Add(new Label
        {
            Text = "Browse packages found in your configured addon libraries.",
            Font = AppFonts.Normal,
            ForeColor = AppColors.SecondaryText,
            AutoSize = true,
            Location = new Point(2, 40)
        });
        return panel;
    }

    private Control CreateCommandBar()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppColors.Surface,
            Padding = new Padding(14, 12, 14, 12)
        };

        searchBox.Text = string.Empty;
        searchBox.CueHint.CueHintText = "Search addons...";
        searchBox.Location = new Point(14, 13);
        searchBox.Size = new Size(330, 34);
        searchBox.TextChanged += (_, _) => ApplyFilters();

        categoryBox.Items.AddRange(
            ["All categories", "Aircraft", "Airports", "Scenery", "Liveries", "Utilities", "Other"]);
        categoryBox.SelectedIndex = 0;
        categoryBox.DropDownStyle = ComboBoxStyle.DropDownList;
        categoryBox.Font = AppFonts.Normal;
        categoryBox.BackColor = AppColors.SurfaceLight;
        categoryBox.ForeColor = AppColors.Text;
        categoryBox.Location = new Point(358, 14);
        categoryBox.Size = new Size(160, 32);
        categoryBox.SelectedIndexChanged += (_, _) => ApplyFilters();

        refreshButton.Text = "Refresh";
        refreshButton.Size = new Size(100, 34);
        refreshButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        refreshButton.StateCommon.Back.Color1 = AppColors.Accent;
        refreshButton.StateCommon.Back.Color2 = AppColors.Accent;
        refreshButton.StateCommon.Content.ShortText.Color1 = Color.White;
        refreshButton.StateCommon.Content.ShortText.Font = AppFonts.Button;
        refreshButton.Click += async (_, _) => await LoadAddonsAsync();

        resultLabel.Font = AppFonts.Small;
        resultLabel.ForeColor = AppColors.SecondaryText;
        resultLabel.AutoSize = true;
        resultLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        panel.Controls.AddRange([searchBox, categoryBox, resultLabel, refreshButton]);
        panel.Resize += (_, _) =>
        {
            refreshButton.Location = new Point(panel.ClientSize.Width - 114, 13);
            resultLabel.Location = new Point(
                refreshButton.Left - resultLabel.Width - 18,
                22);
        };
        return panel;
    }

    private Control CreateResultsArea()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppColors.Background
        };

        cardsPanel.Dock = DockStyle.Fill;
        cardsPanel.AutoScroll = true;
        cardsPanel.WrapContents = true;
        cardsPanel.FlowDirection = FlowDirection.LeftToRight;
        cardsPanel.BackColor = AppColors.Background;
        cardsPanel.Padding = new Padding(0, 16, 0, 0);

        emptyLabel.Text = "No addons found.\n\nOpen Settings and add an addon library, then select Refresh.";
        emptyLabel.Font = AppFonts.Title;
        emptyLabel.ForeColor = AppColors.SecondaryText;
        emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
        emptyLabel.Dock = DockStyle.Fill;

        BuildDetailsPanel();
        panel.Controls.Add(cardsPanel);
        panel.Controls.Add(detailsPanel);
        panel.Controls.Add(emptyLabel);
        return panel;
    }

    private void BuildDetailsPanel()
    {
        detailsPanel.Dock = DockStyle.Right;
        detailsPanel.Width = 350;
        detailsPanel.BackColor = AppColors.Surface;
        detailsPanel.Padding = new Padding(24);
        detailsPanel.Visible = false;

        var closeButton = new Button
        {
            Text = "×",
            Font = new Font("Segoe UI", 16f, FontStyle.Regular),
            ForeColor = AppColors.SecondaryText,
            BackColor = AppColors.Surface,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(38, 38),
            Location = new Point(detailsPanel.Width - 54, 12),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand,
            TabStop = false
        };
        closeButton.FlatAppearance.BorderSize = 0;
        closeButton.FlatAppearance.MouseOverBackColor = AppColors.SurfaceLight;
        closeButton.Click += (_, _) => detailsPanel.Visible = false;

        ConfigureDetailLabel(detailName, AppFonts.Header, AppColors.Text, 24, 56, 302, 72);
        ConfigureDetailLabel(detailCategory, AppFonts.Small, AppColors.Accent, 24, 132, 302, 24);
        ConfigureDetailLabel(detailStatus, AppFonts.Button, AppColors.Success, 24, 164, 302, 28);

        var separator = new Panel
        {
            BackColor = AppColors.SurfaceLight,
            Location = new Point(24, 205),
            Size = new Size(302, 1),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        AddDetailRow("AUTHOR", detailAuthor, 228);
        AddDetailRow("VERSION", detailVersion, 294);
        AddDetailRow("PACKAGE FOLDER", detailFolder, 360, 58);
        AddDetailRow("ADDON LIBRARY", detailLibrary, 444, 72);

        var notice = new Label
        {
            Text = "Enable and disable controls will be added after symbolic-link testing on the MSFS machine.",
            Font = AppFonts.Small,
            ForeColor = AppColors.SecondaryText,
            Location = new Point(24, 550),
            Size = new Size(302, 60),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        detailsPanel.Controls.AddRange(
            [closeButton, detailName, detailCategory, detailStatus, separator, notice]);
    }

    private void AddDetailRow(string heading, Label valueLabel, int top, int valueHeight = 28)
    {
        detailsPanel.Controls.Add(new Label
        {
            Text = heading,
            Font = AppFonts.Small,
            ForeColor = AppColors.SecondaryText,
            AutoSize = true,
            Location = new Point(24, top)
        });

        ConfigureDetailLabel(
            valueLabel,
            AppFonts.Normal,
            AppColors.Text,
            24,
            top + 22,
            302,
            valueHeight);
        detailsPanel.Controls.Add(valueLabel);
    }

    private static void ConfigureDetailLabel(
        Label label,
        Font font,
        Color color,
        int left,
        int top,
        int width,
        int height)
    {
        label.Font = font;
        label.ForeColor = color;
        label.Location = new Point(left, top);
        label.Size = new Size(width, height);
        label.AutoEllipsis = true;
        label.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
    }

    private async Task LoadAddonsAsync()
    {
        refreshButton.Enabled = false;
        refreshButton.Text = "Scanning...";
        resultLabel.Text = "Reading libraries";

        try
        {
            addons = await addonScanner.FindAddonsAsync(settingsService.Load());
            ApplyFilters();
        }
        finally
        {
            refreshButton.Enabled = true;
            refreshButton.Text = "Refresh";
        }
    }

    private void ApplyFilters()
    {
        string searchText = searchBox.Text.Trim();
        string category = categoryBox.SelectedItem?.ToString() ?? "All categories";

        Addon[] filtered = addons
            .Where(addon =>
                (searchText.Length == 0 ||
                 addon.Name.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                 addon.Author.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)) &&
                (category == "All categories" || addon.Category == category))
            .ToArray();

        cardsPanel.SuspendLayout();
        cardsPanel.Controls.Clear();
        foreach (Addon addon in filtered)
        {
            cardsPanel.Controls.Add(CreateAddonCard(addon));
        }
        cardsPanel.ResumeLayout(true);

        emptyLabel.Visible = filtered.Length == 0;
        cardsPanel.Visible = filtered.Length > 0;
        resultLabel.Text = $"{filtered.Length} of {addons.Count} addons";
    }

    private Control CreateAddonCard(Addon addon)
    {
        var card = new Panel
        {
            Size = new Size(310, 142),
            BackColor = AppColors.Surface,
            Margin = new Padding(0, 0, 16, 16),
            Padding = new Padding(18)
        };

        var name = new Label
        {
            Text = addon.Name,
            Font = AppFonts.Title,
            ForeColor = AppColors.Text,
            AutoEllipsis = true,
            Location = new Point(18, 16),
            Size = new Size(274, 25)
        };
        var category = new Label
        {
            Text = addon.Category,
            Font = AppFonts.Small,
            ForeColor = AppColors.Accent,
            AutoSize = true,
            Location = new Point(18, 49)
        };
        var details = new Label
        {
            Text = $"{addon.Author}  •  Version {addon.Version}",
            Font = AppFonts.Small,
            ForeColor = AppColors.SecondaryText,
            AutoEllipsis = true,
            Location = new Point(18, 76),
            Size = new Size(274, 22)
        };
        var status = new Label
        {
            Text = addon.IsEnabled ? "● ENABLED" : "○ DISABLED",
            Font = AppFonts.Small,
            ForeColor = addon.IsEnabled ? AppColors.Success : AppColors.SecondaryText,
            AutoSize = true,
            Location = new Point(18, 108)
        };

        card.Controls.AddRange([name, category, details, status]);
        AttachSelectionHandler(card, addon);
        return card;
    }

    private void AttachSelectionHandler(Control control, Addon addon)
    {
        control.Cursor = Cursors.Hand;
        control.Click += (_, _) => ShowDetails(addon);

        foreach (Control child in control.Controls)
        {
            AttachSelectionHandler(child, addon);
        }
    }

    private void ShowDetails(Addon addon)
    {
        detailName.Text = addon.Name;
        detailCategory.Text = addon.Category.ToUpperInvariant();
        detailStatus.Text = addon.IsEnabled ? "● ENABLED" : "○ DISABLED";
        detailStatus.ForeColor = addon.IsEnabled
            ? AppColors.Success
            : AppColors.SecondaryText;
        detailAuthor.Text = addon.Author;
        detailVersion.Text = addon.Version;
        detailFolder.Text = addon.Path;
        detailLibrary.Text = addon.LibraryPath;
        detailsPanel.Visible = true;
        detailsPanel.BringToFront();
    }
}
