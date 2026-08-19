using Krypton.Toolkit;
using MSFS2024AddonManager.Models;
using MSFS2024AddonManager.Services;
using MSFS2024AddonManager.UI.Controls;
using MSFS2024AddonManager.UI.Themes;
using ThemeService = MSFS2024AddonManager.UI.Themes.ThemeService;
using AppColors = MSFS2024AddonManager.UI.Colors.Colors;
using AppFonts = MSFS2024AddonManager.UI.Fonts.Fonts;

namespace MSFS2024AddonManager.Views;

public sealed class ProfilesView : KryptonPanel
{
    private readonly ProfileService profileService = new();
    private readonly KryptonTextBox profileNameTextBox = new();
    private readonly FlowLayoutPanel profilesPanel = new();
    private readonly Label emptyLabel = new();
    private readonly Label summaryLabel = new();
    private ProfileCollection collection;
    private bool applyInProgress;

    public ProfilesView()
    {
        collection = profileService.Load();
        Dock = DockStyle.Fill;
        StateCommon.Color1 = AppColors.Background;
        BuildInterface();
        RefreshProfiles();
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
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        page.Controls.Add(CreateHeading(), 0, 0);
        page.Controls.Add(CreateCommandBar(), 0, 1);
        page.Controls.Add(CreateProfilesArea(), 0, 2);
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
            Text = "PROFILES / ADDON LOADOUTS",
            Font = AppFonts.Header,
            ForeColor = AppColors.Accent,
            AutoSize = true,
            Location = new Point(0, 0)
        });
        panel.Controls.Add(new Label
        {
            Text = "Build, preview, and apply addon sets for airliners, VFR, VR, testing, or any other scenario.",
            Font = AppFonts.Normal,
            ForeColor = AppColors.SecondaryText,
            AutoSize = true,
            Location = new Point(2, 40)
        });
        return panel;
    }

    private Control CreateCommandBar()
    {
        var panel = new AvionicsPanel
        {
            Dock = DockStyle.Fill,
            BackColor = AppColors.Surface,
            Padding = new Padding(14)
        };

        profileNameTextBox.CueHint.CueHintText = "New profile name...";
        profileNameTextBox.Location = new Point(14, 17);
        profileNameTextBox.Size = new Size(330, 34);
        ThemeService.StyleTextBox(profileNameTextBox);
        profileNameTextBox.KeyDown += (_, eventArgs) =>
        {
            if (eventArgs.KeyCode != Keys.Enter)
            {
                return;
            }

            CreateProfile();
            eventArgs.SuppressKeyPress = true;
        };

        KryptonButton createButton = CreateButton("Create profile", CreateProfile);
        createButton.Location = new Point(358, 16);

        summaryLabel.Font = AppFonts.Small;
        summaryLabel.ForeColor = AppColors.SecondaryText;
        summaryLabel.AutoSize = true;
        summaryLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        panel.Controls.AddRange([profileNameTextBox, createButton, summaryLabel]);
        panel.Resize += (_, _) =>
            summaryLabel.Location = new Point(
                panel.ClientSize.Width - summaryLabel.Width - 18,
                25);
        return panel;
    }

    private Control CreateProfilesArea()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppColors.Background
        };

        profilesPanel.Dock = DockStyle.Fill;
        profilesPanel.AutoScroll = true;
        profilesPanel.WrapContents = true;
        profilesPanel.FlowDirection = FlowDirection.LeftToRight;
        profilesPanel.BackColor = AppColors.Background;
        profilesPanel.Padding = new Padding(0, 16, 0, 0);

        emptyLabel.Text = "No profiles yet.\n\nEnter a name above to create your first profile.";
        emptyLabel.Font = AppFonts.Title;
        emptyLabel.ForeColor = AppColors.SecondaryText;
        emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
        emptyLabel.Dock = DockStyle.Fill;

        panel.Controls.Add(profilesPanel);
        panel.Controls.Add(emptyLabel);
        return panel;
    }

    private void CreateProfile()
    {
        string profileName = profileNameTextBox.Text.Trim();
        if (profileName.Length == 0)
        {
            return;
        }

        if (collection.Profiles.Any(
                profile => profile.Name.Equals(
                    profileName,
                    StringComparison.CurrentCultureIgnoreCase)))
        {
            KryptonMessageBox.Show(
                this,
                "A profile with that name already exists.",
                "Duplicate profile",
                KryptonMessageBoxButtons.OK,
                KryptonMessageBoxIcon.Information);
            return;
        }

        var profile = new AddonProfile
        {
            Name = profileName
        };
        collection.Profiles.Add(profile);
        collection.ActiveProfileId ??= profile.Id;
        profileService.Save(collection);
        profileNameTextBox.Clear();
        RefreshProfiles();
    }

    private void ActivateProfile(AddonProfile profile)
    {
        collection.ActiveProfileId = profile.Id;
        profileService.Save(collection);
        RefreshProfiles();
    }

    private async void ApplyProfile(AddonProfile profile)
    {
        if (applyInProgress)
        {
            return;
        }

        applyInProgress = true;
        RefreshProfiles();
        try
        {
            AppSettings settings = new SettingsService().Load();
            if (string.IsNullOrWhiteSpace(settings.CommunityFolder) ||
                !Directory.Exists(settings.CommunityFolder))
            {
                KryptonMessageBox.Show(
                    this,
                    "Configure an available default Community folder in Settings before applying a profile.",
                    "Community folder unavailable",
                    KryptonMessageBoxButtons.OK,
                    KryptonMessageBoxIcon.Warning);
                return;
            }

            IReadOnlyList<Addon> addons = await new AddonScanner()
                .FindAddonsAsync(settings);
            if (ProfileAssignmentService.MigrateLegacyAssignments(
                    profile,
                    addons) > 0)
            {
                profileService.Save(collection);
            }

            var applyService = new ProfileApplyService(new LinkService());
            ProfileApplyPlan plan = applyService.BuildPlan(
                profile,
                addons,
                settings.CommunityFolder);

            if (!plan.CanApply)
            {
                KryptonMessageBox.Show(
                    this,
                    string.Join("\r\n\r\n", plan.Errors),
                    "Profile cannot be applied",
                    KryptonMessageBoxButtons.OK,
                    KryptonMessageBoxIcon.Warning);
                return;
            }

            using var preview = new ProfileApplyPreviewDialog(plan);
            if (preview.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            ProfileApplyResult result = await Task.Run(() => applyService.Apply(plan));
            if (result.Success)
            {
                collection.ActiveProfileId = profile.Id;
                profileService.Save(collection);
            }

            KryptonMessageBox.Show(
                this,
                result.Message,
                result.Success ? "Profile applied" : "Profile apply failed",
                KryptonMessageBoxButtons.OK,
                result.Success
                    ? KryptonMessageBoxIcon.Information
                    : KryptonMessageBoxIcon.Error);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            KryptonMessageBox.Show(
                this,
                $"The profile could not be prepared: {exception.Message}",
                "Profile apply failed",
                KryptonMessageBoxButtons.OK,
                KryptonMessageBoxIcon.Error);
        }
        finally
        {
            applyInProgress = false;
            collection = profileService.Load();
            RefreshProfiles();
        }
    }

    private void DeleteProfile(AddonProfile profile)
    {
        DialogResult result = KryptonMessageBox.Show(
            this,
            $"Delete the profile \"{profile.Name}\"?",
            "Delete profile",
            KryptonMessageBoxButtons.YesNo,
            KryptonMessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
        {
            return;
        }

        collection.Profiles.RemoveAll(item => item.Id == profile.Id);
        if (collection.ActiveProfileId == profile.Id)
        {
            collection.ActiveProfileId = collection.Profiles.FirstOrDefault()?.Id;
        }

        profileService.Save(collection);
        RefreshProfiles();
    }

    private void RefreshProfiles()
    {
        profilesPanel.SuspendLayout();
        profilesPanel.Controls.Clear();

        foreach (AddonProfile profile in collection.Profiles.OrderBy(
                     item => item.Name,
                     StringComparer.CurrentCultureIgnoreCase))
        {
            profilesPanel.Controls.Add(CreateProfileCard(profile));
        }

        profilesPanel.ResumeLayout(true);
        bool hasProfiles = collection.Profiles.Count > 0;
        profilesPanel.Visible = hasProfiles;
        emptyLabel.Visible = !hasProfiles;
        summaryLabel.Text = hasProfiles
            ? $"{collection.Profiles.Count} profile{(collection.Profiles.Count == 1 ? string.Empty : "s")}"
            : "No profiles";
    }

    private Control CreateProfileCard(AddonProfile profile)
    {
        bool isActive = collection.ActiveProfileId == profile.Id;
        var card = new AvionicsPanel
        {
            Size = new Size(340, 238),
            BackColor = isActive ? AppColors.Selection : AppColors.Surface,
            BorderColor = isActive ? AppColors.Accent : AppColors.Border,
            Margin = new Padding(0, 0, 16, 16)
        };

        card.Controls.Add(new Label
        {
            Text = profile.Name,
            Font = AppFonts.Title,
            ForeColor = AppColors.Text,
            AutoEllipsis = true,
            Location = new Point(20, 20),
            Size = new Size(296, 26)
        });
        card.Controls.Add(new Label
        {
            Text = isActive ? "● EDITING PROFILE" : "○ NOT SELECTED",
            Font = AppFonts.Small,
            ForeColor = isActive ? AppColors.Success : AppColors.SecondaryText,
            AutoSize = true,
            Location = new Point(20, 56)
        });
        card.Controls.Add(new Label
        {
            Text = $"{profile.AssignmentCount} assigned addons",
            Font = AppFonts.Normal,
            ForeColor = AppColors.SecondaryText,
            AutoSize = true,
            Location = new Point(20, 88)
        });

        KryptonButton applyButton = CreateButton(
            "Preview & apply",
            () => ApplyProfile(profile));
        applyButton.Location = new Point(20, 126);
        applyButton.Size = new Size(216, 38);
        applyButton.Enabled = !applyInProgress;

        KryptonButton activateButton = CreateButton(
            isActive ? "Editing" : "Edit addons",
            () => ActivateProfile(profile));
        activateButton.Location = new Point(20, 178);
        activateButton.Enabled = !isActive && !applyInProgress;
        ThemeService.StyleSecondaryButton(activateButton);

        KryptonButton deleteButton = CreateButton(
            "Delete",
            () => DeleteProfile(profile));
        deleteButton.Location = new Point(136, 178);
        deleteButton.Enabled = !applyInProgress;
        ThemeService.StyleSecondaryButton(deleteButton);
        deleteButton.StateCommon.Back.Color1 = AppColors.ErrorDark;
        deleteButton.StateCommon.Back.Color2 = AppColors.ErrorDark;
        deleteButton.StateCommon.Border.Color1 = AppColors.Error;
        deleteButton.StateCommon.Border.Color2 = AppColors.Error;
        deleteButton.StateCommon.Content.ShortText.Color1 = AppColors.Error;

        card.Controls.AddRange([applyButton, activateButton, deleteButton]);
        return card;
    }

    private static KryptonButton CreateButton(string text, Action action)
    {
        var button = new KryptonButton
        {
            Text = text.ToUpperInvariant(),
            Size = new Size(104, 38)
        };
        ThemeService.StylePrimaryButton(button);
        button.Click += (_, _) => action();
        return button;
    }
}
