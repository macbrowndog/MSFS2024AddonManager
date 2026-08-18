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
    private readonly ComboBox locationBox = new();
    private readonly TreeView libraryTree = new();
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
    private readonly PictureBox detailThumbnail = new();
    private readonly Label activeProfileLabel = new();
    private readonly Button profileAssignmentButton = new();
    private readonly ComboBox linkTargetBox = new();
    private readonly Button linkActionButton = new();
    private readonly Label linkFeedbackLabel = new();
    private IReadOnlyList<Addon> addons = [];
    private Addon? selectedAddon;
    private string? selectedTreePath;
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

        locationBox.Items.AddRange(
        [
            "All locations",
            "Addon libraries",
            "Community",
            "Community2024"
        ]);
        locationBox.SelectedIndex = 0;
        locationBox.DropDownStyle = ComboBoxStyle.DropDownList;
        locationBox.Font = AppFonts.Normal;
        locationBox.BackColor = AppColors.SurfaceLight;
        locationBox.ForeColor = AppColors.Text;
        locationBox.Location = new Point(532, 14);
        locationBox.Size = new Size(170, 32);
        locationBox.SelectedIndexChanged += (_, _) => ApplyFilters();

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

        panel.Controls.AddRange(
            [searchBox, categoryBox, locationBox, resultLabel, refreshButton]);
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
        var splitView = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            FixedPanel = FixedPanel.None,
            IsSplitterFixed = false,
            SplitterWidth = 7,
            BackColor = AppColors.SurfaceLight
        };
        ConfigureSplitterWhenReady(splitView);
        var resultsHost = new Panel
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
        cardsPanel.Resize += (_, _) => ResizeSectionHeaders();

        emptyLabel.Text = "No addons found.\n\nOpen Settings and add an addon library, then select Refresh.";
        emptyLabel.Font = AppFonts.Title;
        emptyLabel.ForeColor = AppColors.SecondaryText;
        emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
        emptyLabel.Dock = DockStyle.Fill;

        ConfigureLibraryTree();
        BuildDetailsPanel();
        resultsHost.Controls.Add(cardsPanel);
        resultsHost.Controls.Add(emptyLabel);
        splitView.Panel1.BackColor = AppColors.Navigation;
        splitView.Panel1.Controls.Add(CreateLibraryTreePanel());
        splitView.Panel2.BackColor = AppColors.Background;
        splitView.Panel2.Controls.Add(resultsHost);
        splitView.Panel2.Controls.Add(detailsPanel);
        panel.Controls.Add(splitView);
        return panel;
    }

    private static void ConfigureSplitterWhenReady(SplitContainer splitView)
    {
        bool hasConfiguredInitialSize = false;
        splitView.SizeChanged += (_, _) =>
        {
            if (hasConfiguredInitialSize || splitView.ClientSize.Width < 720)
            {
                return;
            }

            const int treeMinimumWidth = 220;
            const int addonsMinimumWidth = 400;
            int maximumTreeWidth =
                splitView.ClientSize.Width -
                splitView.SplitterWidth -
                addonsMinimumWidth;
            int initialTreeWidth = Math.Clamp(
                340,
                treeMinimumWidth,
                maximumTreeWidth);

            splitView.Panel1MinSize = 0;
            splitView.Panel2MinSize = 0;
            splitView.SplitterDistance = initialTreeWidth;
            splitView.Panel1MinSize = treeMinimumWidth;
            splitView.Panel2MinSize = addonsMinimumWidth;
            hasConfiguredInitialSize = true;
        };
    }

    private Control CreateLibraryTreePanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppColors.Navigation,
            Padding = new Padding(12, 14, 8, 12)
        };
        var heading = new Label
        {
            Text = "LIBRARY FOLDERS",
            Dock = DockStyle.Top,
            Height = 32,
            Font = AppFonts.Small,
            ForeColor = AppColors.SecondaryText,
            TextAlign = ContentAlignment.MiddleLeft
        };

        panel.Controls.Add(libraryTree);
        panel.Controls.Add(heading);
        return panel;
    }

    private void ConfigureLibraryTree()
    {
        libraryTree.Dock = DockStyle.Fill;
        libraryTree.BackColor = AppColors.Navigation;
        libraryTree.ForeColor = AppColors.Text;
        libraryTree.Font = AppFonts.Normal;
        libraryTree.BorderStyle = BorderStyle.None;
        libraryTree.HideSelection = false;
        libraryTree.FullRowSelect = true;
        libraryTree.ShowLines = true;
        libraryTree.ShowRootLines = true;
        libraryTree.ShowPlusMinus = true;
        libraryTree.AfterSelect += (_, eventArgs) =>
        {
            selectedTreePath = eventArgs.Node?.Tag as string;
            ApplyFilters();
        };
    }

    private void BuildDetailsPanel()
    {
        detailsPanel.Dock = DockStyle.Right;
        detailsPanel.Width = 350;
        detailsPanel.BackColor = AppColors.Surface;
        detailsPanel.Padding = new Padding(24);
        detailsPanel.AutoScroll = true;
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

        detailThumbnail.Location = new Point(24, 56);
        detailThumbnail.Size = new Size(302, 120);
        detailThumbnail.SizeMode = PictureBoxSizeMode.Zoom;
        detailThumbnail.BackColor = AppColors.Navigation;
        detailThumbnail.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        ConfigureDetailLabel(detailName, AppFonts.Header, AppColors.Text, 24, 190, 302, 72);
        ConfigureDetailLabel(detailCategory, AppFonts.Small, AppColors.Accent, 24, 266, 302, 24);
        ConfigureDetailLabel(detailStatus, AppFonts.Button, AppColors.Success, 24, 298, 302, 28);

        var separator = new Panel
        {
            BackColor = AppColors.SurfaceLight,
            Location = new Point(24, 339),
            Size = new Size(302, 1),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        AddDetailRow("AUTHOR", detailAuthor, 362);
        AddDetailRow("VERSION", detailVersion, 428);
        AddDetailRow("PACKAGE FOLDER", detailFolder, 494, 58);
        AddDetailRow("ADDON LIBRARY", detailLibrary, 578, 72);

        detailsPanel.Controls.Add(new Label
        {
            Text = "LINK DESTINATION",
            Font = AppFonts.Small,
            ForeColor = AppColors.SecondaryText,
            AutoSize = true,
            Location = new Point(24, 660)
        });

        linkTargetBox.DropDownStyle = ComboBoxStyle.DropDownList;
        linkTargetBox.Font = AppFonts.Normal;
        linkTargetBox.BackColor = AppColors.SurfaceLight;
        linkTargetBox.ForeColor = AppColors.Text;
        linkTargetBox.Location = new Point(24, 682);
        linkTargetBox.Size = new Size(240, 32);
        linkTargetBox.SelectedIndexChanged += (_, _) =>
            UpdateLinkActionForSelectedTarget();

        activeProfileLabel.Font = AppFonts.Small;
        activeProfileLabel.ForeColor = AppColors.SecondaryText;
        activeProfileLabel.Location = new Point(24, 730);
        activeProfileLabel.Size = new Size(302, 48);
        activeProfileLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        profileAssignmentButton.Text = "Add to active profile";
        profileAssignmentButton.Location = new Point(24, 784);
        profileAssignmentButton.Size = new Size(190, 38);
        StyleDarkButton(profileAssignmentButton);
        profileAssignmentButton.Click += (_, _) => ToggleActiveProfileAssignment();

        linkActionButton.Text = "Enable addon";
        linkActionButton.Location = new Point(24, 834);
        linkActionButton.Size = new Size(190, 38);
        StyleDarkButton(linkActionButton);
        linkActionButton.Click += ToggleAddonLink;

        linkFeedbackLabel.Text =
            "Enable creates a directory symbolic link. Your source addon folder is never moved.";
        linkFeedbackLabel.Font = AppFonts.Small;
        linkFeedbackLabel.ForeColor = AppColors.SecondaryText;
        linkFeedbackLabel.Location = new Point(24, 886);
        linkFeedbackLabel.Size = new Size(302, 90);
        linkFeedbackLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        detailsPanel.Controls.AddRange(
        [
            closeButton,
            detailThumbnail,
            detailName,
            detailCategory,
            detailStatus,
            separator,
            linkTargetBox,
            activeProfileLabel,
            profileAssignmentButton,
            linkActionButton,
            linkFeedbackLabel
        ]);
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
            BuildLibraryTree();
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
        string location = locationBox.SelectedItem?.ToString() ?? "All locations";
        AppSettings settings = settingsService.Load();

        Addon[] filtered = addons
            .Where(addon =>
                (searchText.Length == 0 ||
                 addon.Name.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                 addon.Author.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                 addon.Path.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)) &&
                (category == "All categories" || addon.Category == category) &&
                MatchesLocation(addon, location, settings) &&
                MatchesSelectedTreePath(addon))
            .ToArray();

        cardsPanel.SuspendLayout();
        DisposeChildControls(cardsPanel);
        AddAddonSection(
            "ACTIVE ADDONS",
            filtered.Where(addon => addon.IsEnabled).ToArray(),
            AppColors.Success);
        AddAddonSection(
            "INACTIVE ADDONS",
            filtered.Where(addon => !addon.IsEnabled).ToArray(),
            AppColors.SecondaryText);
        cardsPanel.ResumeLayout(true);

        emptyLabel.Visible = filtered.Length == 0;
        cardsPanel.Visible = filtered.Length > 0;
        resultLabel.Text = $"{filtered.Length} of {addons.Count} addons";
    }

    private void BuildLibraryTree()
    {
        string? pathToRestore = selectedTreePath;
        libraryTree.BeginUpdate();
        libraryTree.Nodes.Clear();

        var allNode = new TreeNode("All addon libraries");
        libraryTree.Nodes.Add(allNode);
        AppSettings settings = settingsService.Load();

        foreach (string configuredLibrary in settings.AddonLibraries)
        {
            string libraryPath = NormalizePath(configuredLibrary);
            if (libraryPath.Length == 0)
            {
                continue;
            }

            var libraryNode = new TreeNode(Path.GetFileName(libraryPath))
            {
                Tag = libraryPath,
                ToolTipText = libraryPath
            };
            allNode.Nodes.Add(libraryNode);

            foreach (Addon addon in addons.Where(addon =>
                         addon.IsManagedLibraryAddon &&
                         NormalizePath(addon.LibraryPath).Equals(
                             libraryPath,
                             StringComparison.OrdinalIgnoreCase)))
            {
                AddAddonPathToTree(libraryNode, libraryPath, addon.Path);
            }
        }

        allNode.Expand();
        libraryTree.SelectedNode =
            FindTreeNodeByPath(libraryTree.Nodes, pathToRestore) ?? allNode;
        selectedTreePath = libraryTree.SelectedNode.Tag as string;
        libraryTree.EndUpdate();
    }

    private static void AddAddonPathToTree(
        TreeNode libraryNode,
        string libraryPath,
        string addonPath)
    {
        string relativePath;
        try
        {
            relativePath = Path.GetRelativePath(libraryPath, addonPath);
        }
        catch (ArgumentException)
        {
            return;
        }

        TreeNode parent = libraryNode;
        string currentPath = libraryPath;
        foreach (string segment in relativePath.Split(
                     Path.DirectorySeparatorChar,
                     StringSplitOptions.RemoveEmptyEntries))
        {
            currentPath = Path.Combine(currentPath, segment);
            TreeNode? child = parent.Nodes
                .Cast<TreeNode>()
                .FirstOrDefault(node =>
                    node.Text.Equals(segment, StringComparison.OrdinalIgnoreCase));
            if (child is null)
            {
                child = new TreeNode(segment)
                {
                    Tag = currentPath,
                    ToolTipText = currentPath
                };
                parent.Nodes.Add(child);
            }

            parent = child;
        }
    }

    private static TreeNode? FindTreeNodeByPath(
        TreeNodeCollection nodes,
        string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        foreach (TreeNode node in nodes)
        {
            if (node.Tag is string nodePath &&
                NormalizePath(nodePath).Equals(
                    NormalizePath(path),
                    StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            TreeNode? childMatch = FindTreeNodeByPath(node.Nodes, path);
            if (childMatch is not null)
            {
                return childMatch;
            }
        }

        return null;
    }

    private bool MatchesSelectedTreePath(Addon addon)
    {
        if (string.IsNullOrWhiteSpace(selectedTreePath))
        {
            return true;
        }

        string selectedPath = NormalizePath(selectedTreePath);
        string addonPath = NormalizePath(addon.Path);
        if (addonPath.Equals(selectedPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string prefix = selectedPath + Path.DirectorySeparatorChar;
        return addonPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private void AddAddonSection(
        string title,
        IReadOnlyCollection<Addon> sectionAddons,
        Color accentColor)
    {
        if (sectionAddons.Count == 0)
        {
            return;
        }

        var header = new Panel
        {
            Name = "AddonSectionHeader",
            Size = new Size(GetSectionWidth(), 44),
            BackColor = AppColors.Background,
            Margin = new Padding(0, 0, 0, 8)
        };
        header.Controls.Add(new Label
        {
            Text = $"{title}  ({sectionAddons.Count})",
            Font = AppFonts.Title,
            ForeColor = accentColor,
            AutoSize = true,
            Location = new Point(2, 10)
        });
        cardsPanel.Controls.Add(header);
        cardsPanel.SetFlowBreak(header, true);

        Control? lastCard = null;
        foreach (Addon addon in sectionAddons)
        {
            lastCard = CreateAddonCard(addon);
            cardsPanel.Controls.Add(lastCard);
        }

        if (lastCard is not null)
        {
            cardsPanel.SetFlowBreak(lastCard, true);
        }
    }

    private void ResizeSectionHeaders()
    {
        foreach (Control control in cardsPanel.Controls)
        {
            if (control.Name == "AddonSectionHeader")
            {
                control.Width = GetSectionWidth();
            }
        }
    }

    private int GetSectionWidth()
    {
        return Math.Max(
            310,
            cardsPanel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8);
    }

    private Control CreateAddonCard(Addon addon)
    {
        Image? thumbnailImage = LoadThumbnail(addon.ThumbnailPath);
        bool hasThumbnail = thumbnailImage is not null;
        int textLeft = hasThumbnail ? 116 : 18;
        int textWidth = hasThumbnail ? 176 : 274;

        var card = new Panel
        {
            Size = new Size(310, 158),
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
            Location = new Point(textLeft, 16),
            Size = new Size(textWidth, 44)
        };
        var category = new Label
        {
            Text = addon.Category,
            Font = AppFonts.Small,
            ForeColor = AppColors.Accent,
            AutoSize = true,
            Location = new Point(textLeft, 65)
        };
        var details = new Label
        {
            Text = $"{addon.Author}  •  Version {addon.Version}",
            Font = AppFonts.Small,
            ForeColor = AppColors.SecondaryText,
            AutoEllipsis = true,
            Location = new Point(textLeft, 92),
            Size = new Size(textWidth, 22)
        };
        var status = new Label
        {
            Text = addon.IsEnabled
                ? $"● ENABLED • {GetEnabledLocationText(addon)}"
                : "○ DISABLED",
            Font = AppFonts.Small,
            ForeColor = addon.IsEnabled ? AppColors.Success : AppColors.SecondaryText,
            AutoSize = true,
            Location = new Point(textLeft, 126)
        };

        var thumbnail = new PictureBox
        {
            Location = new Point(16, 16),
            Size = new Size(86, 126),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = AppColors.Navigation,
            Image = thumbnailImage,
            Visible = hasThumbnail
        };

        card.Controls.AddRange([thumbnail, name, category, details, status]);
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
        selectedAddon = addon;
        detailName.Text = addon.Name;
        detailCategory.Text = addon.Category.ToUpperInvariant();
        detailStatus.Text = addon.IsEnabled
            ? $"● ENABLED • {GetEnabledLocationText(addon)}"
            : "○ DISABLED";
        detailStatus.ForeColor = addon.IsEnabled
            ? AppColors.Success
            : AppColors.SecondaryText;
        detailAuthor.Text = addon.Author;
        detailVersion.Text = addon.Version;
        detailFolder.Text = addon.Path;
        detailLibrary.Text = addon.IsManagedLibraryAddon
            ? addon.LibraryPath
            : $"Installed directly in {Path.GetFileName(addon.LibraryPath)}";
        Image? detailImage = LoadThumbnail(addon.ThumbnailPath);
        detailThumbnail.Visible = detailImage is not null;
        ReplaceImage(detailThumbnail, detailImage);
        RefreshProfileAssignment(addon);
        ConfigureLinkTargets(addon);
        detailsPanel.Visible = true;
        detailsPanel.BringToFront();
    }

    private void RefreshProfileAssignment(Addon addon)
    {
        ProfileCollection profiles = new ProfileService().Load();
        AddonProfile? activeProfile = profiles.Profiles.FirstOrDefault(
            profile => profile.Id == profiles.ActiveProfileId);

        if (activeProfile is null)
        {
            activeProfileLabel.Text = "No active profile.\r\nCreate one on the Profiles page.";
            profileAssignmentButton.Enabled = false;
            profileAssignmentButton.Text = "No active profile";
            return;
        }

        bool isAssigned = ProfileAssignmentService.IsAssigned(activeProfile, addon);
        activeProfileLabel.Text = $"EDITING PROFILE: {activeProfile.Name}";
        profileAssignmentButton.Enabled = true;
        profileAssignmentButton.Text = isAssigned
            ? "Remove from profile"
            : "Add to active profile";
    }

    private void ToggleActiveProfileAssignment()
    {
        if (selectedAddon is null)
        {
            return;
        }

        var profileService = new ProfileService();
        ProfileCollection profiles = profileService.Load();
        AddonProfile? activeProfile = profiles.Profiles.FirstOrDefault(
            profile => profile.Id == profiles.ActiveProfileId);
        if (activeProfile is null)
        {
            RefreshProfileAssignment(selectedAddon);
            return;
        }

        ProfileAssignmentService.Toggle(activeProfile, selectedAddon);

        profileService.Save(profiles);
        RefreshProfileAssignment(selectedAddon);
    }

    private async void ToggleAddonLink(object? sender, EventArgs e)
    {
        if (selectedAddon is null)
        {
            return;
        }

        var linkService = new LinkService();
        if (linkTargetBox.SelectedItem is not CommunityTarget target)
        {
            linkFeedbackLabel.ForeColor = AppColors.Warning;
            linkFeedbackLabel.Text =
                "Configure a Community or Community2024 folder in Settings first.";
            return;
        }

        bool isEnabledAtTarget = IsEnabledAtTarget(selectedAddon, target.Path);
        if (isEnabledAtTarget)
        {
            DialogResult confirmation = KryptonMessageBox.Show(
                this,
                $"Disable \"{selectedAddon.Name}\"?\r\n\r\nOnly its symbolic link will be removed. The source addon folder will not be deleted.",
                "Disable addon",
                KryptonMessageBoxButtons.YesNo,
                KryptonMessageBoxIcon.Warning);

            if (confirmation != DialogResult.Yes)
            {
                return;
            }
        }

        linkActionButton.Enabled = false;
        LinkOperationResult result = isEnabledAtTarget
            ? linkService.Disable(selectedAddon, target.Path)
            : linkService.Enable(selectedAddon, target.Path);

        linkFeedbackLabel.ForeColor = result.Success
            ? AppColors.Success
            : AppColors.Error;
        linkFeedbackLabel.Text = result.Message;

        if (result.Success)
        {
            string packageIdentity = AddonIdentity.GetPackageIdentity(selectedAddon);
            string canonicalPath = AddonIdentity.GetCanonicalPath(selectedAddon);
            await LoadAddonsAsync();
            Addon? refreshedAddon = addons.FirstOrDefault(addon =>
                AddonIdentity.GetPackageIdentity(addon).Equals(
                    packageIdentity,
                    StringComparison.OrdinalIgnoreCase) &&
                AddonIdentity.GetCanonicalPath(addon).Equals(
                    canonicalPath,
                    StringComparison.OrdinalIgnoreCase));
            if (refreshedAddon is not null)
            {
                ShowDetails(refreshedAddon);
                linkFeedbackLabel.ForeColor = AppColors.Success;
                linkFeedbackLabel.Text = result.Message;
            }
        }

        linkActionButton.Enabled = true;
    }

    private static void StyleDarkButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = AppColors.AccentDark;
        button.ForeColor = Color.White;
        button.Font = AppFonts.Button;
        button.Cursor = Cursors.Hand;
        button.TabStop = false;
        button.UseVisualStyleBackColor = false;
        button.FlatAppearance.BorderColor = AppColors.Accent;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = AppColors.Accent;
        button.FlatAppearance.MouseDownBackColor = AppColors.AccentDark;
    }

    private void ConfigureLinkTargets(Addon addon)
    {
        AppSettings settings = settingsService.Load();
        linkTargetBox.Items.Clear();

        if (!string.IsNullOrWhiteSpace(settings.CommunityFolder))
        {
            linkTargetBox.Items.Add(new CommunityTarget(
                "Community (default)",
                settings.CommunityFolder));
        }

        if (!string.IsNullOrWhiteSpace(settings.Community2024Folder))
        {
            linkTargetBox.Items.Add(new CommunityTarget(
                "Community2024",
                settings.Community2024Folder));
        }

        CommunityTarget? preferredTarget = GetPreferredTargetFromLocationFilter();
        if (preferredTarget is not null)
        {
            preferredTarget = linkTargetBox.Items
                .Cast<CommunityTarget>()
                .FirstOrDefault(target => NormalizePath(target.Path).Equals(
                    NormalizePath(preferredTarget.Path),
                    StringComparison.OrdinalIgnoreCase));
        }

        CommunityTarget? enabledTarget = linkTargetBox.Items
            .Cast<CommunityTarget>()
            .FirstOrDefault(target => IsEnabledAtTarget(addon, target.Path));

        if (preferredTarget is not null)
        {
            linkTargetBox.SelectedItem = preferredTarget;
        }
        else if (enabledTarget is not null)
        {
            linkTargetBox.SelectedItem = enabledTarget;
        }
        else if (linkTargetBox.Items.Count > 0)
        {
            linkTargetBox.SelectedIndex = 0;
        }
        else
        {
            UpdateLinkActionForSelectedTarget();
        }
    }

    private void UpdateLinkActionForSelectedTarget()
    {
        if (selectedAddon is null ||
            linkTargetBox.SelectedItem is not CommunityTarget target)
        {
            linkActionButton.Enabled = false;
            linkActionButton.Text = "No link destination";
            linkFeedbackLabel.ForeColor = AppColors.Warning;
            linkFeedbackLabel.Text =
                "Configure a Community or Community2024 folder in Settings first.";
            return;
        }

        if (!selectedAddon.IsManagedLibraryAddon)
        {
            linkActionButton.Enabled = false;
            linkActionButton.Text = "Directly installed";
            linkFeedbackLabel.ForeColor = AppColors.Warning;
            linkFeedbackLabel.Text =
                "This package exists only inside a Community folder. Move it to an addon library before managing it with symbolic links.";
            return;
        }

        bool isEnabled = IsEnabledAtTarget(selectedAddon, target.Path);
        linkActionButton.Enabled = true;
        linkActionButton.Text = isEnabled ? "Disable from this folder" : "Enable in this folder";
        linkActionButton.BackColor = isEnabled
            ? Color.FromArgb(118, 48, 54)
            : AppColors.AccentDark;
        linkFeedbackLabel.ForeColor = AppColors.SecondaryText;
        linkFeedbackLabel.Text = isEnabled
            ? $"Disable removes only the symbolic link from {target.Name}. The source addon folder is preserved."
            : $"Enable creates a directory symbolic link in {target.Name}. The source addon folder is never moved.";
    }

    private static bool IsEnabledAtTarget(Addon addon, string targetPath)
    {
        string normalizedTarget = NormalizePath(targetPath);
        return addon.EnabledCommunityPaths.Any(path =>
            NormalizePath(path).Equals(
                normalizedTarget,
                StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesLocation(
        Addon addon,
        string location,
        AppSettings settings)
    {
        return location switch
        {
            "Addon libraries" => addon.IsManagedLibraryAddon,
            "Community" => IsEnabledAtConfiguredPath(
                addon,
                settings.CommunityFolder),
            "Community2024" => IsEnabledAtConfiguredPath(
                addon,
                settings.Community2024Folder),
            _ => true
        };
    }

    private static bool IsEnabledAtConfiguredPath(Addon addon, string path)
    {
        return !string.IsNullOrWhiteSpace(path) &&
               IsEnabledAtTarget(addon, path);
    }

    private CommunityTarget? GetPreferredTargetFromLocationFilter()
    {
        AppSettings settings = settingsService.Load();
        return locationBox.SelectedItem?.ToString() switch
        {
            "Community" when !string.IsNullOrWhiteSpace(settings.CommunityFolder) =>
                new CommunityTarget("Community (default)", settings.CommunityFolder),
            "Community2024" when
                !string.IsNullOrWhiteSpace(settings.Community2024Folder) =>
                new CommunityTarget("Community2024", settings.Community2024Folder),
            _ => null
        };
    }

    private static string GetEnabledLocationText(Addon addon)
    {
        return string.Join(
            " + ",
            addon.EnabledCommunityPaths
                .Select(Path.GetFileName)
                .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path).TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);
    }

    private static Image? LoadThumbnail(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            using FileStream stream = File.OpenRead(path);
            using Image source = Image.FromStream(stream);
            return new Bitmap(source);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    private static void ReplaceImage(PictureBox pictureBox, Image? image)
    {
        Image? previousImage = pictureBox.Image;
        pictureBox.Image = image;
        previousImage?.Dispose();
    }

    private static void DisposeChildControls(Control parent)
    {
        Control[] controls = parent.Controls.Cast<Control>().ToArray();
        parent.Controls.Clear();
        foreach (Control control in controls)
        {
            control.Dispose();
        }
    }

    private sealed record CommunityTarget(string Name, string Path)
    {
        public override string ToString() => Name;
    }
}
