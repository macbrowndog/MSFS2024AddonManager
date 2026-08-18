using Krypton.Toolkit;
using MSFS2024AddonManager.Services;
using AppColors = MSFS2024AddonManager.UI.Colors.Colors;
using AppFonts = MSFS2024AddonManager.UI.Fonts.Fonts;

namespace MSFS2024AddonManager.Views;

public sealed class ProfileApplyPreviewDialog : KryptonForm
{
    public ProfileApplyPreviewDialog(ProfileApplyPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        Text = $"Apply profile - {plan.Profile.Name}";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(780, 570);
        MinimumSize = new Size(680, 480);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = AppColors.Background;
        Font = AppFonts.Normal;

        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            BackColor = AppColors.Background,
            ColumnCount = 1,
            RowCount = 5
        };
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        page.Controls.Add(CreateHeading(plan), 0, 0);
        page.Controls.Add(CreateTargetLabel(plan), 0, 1);
        page.Controls.Add(CreateOperationsList(plan), 0, 2);
        page.Controls.Add(CreateSafetyNote(), 0, 3);
        page.Controls.Add(CreateButtons(), 0, 4);
        Controls.Add(page);
    }

    private static Control CreateHeading(ProfileApplyPlan plan)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppColors.Background
        };
        panel.Controls.Add(new Label
        {
            Text = $"Apply {plan.Profile.Name}",
            Font = AppFonts.Title,
            ForeColor = AppColors.Text,
            AutoSize = true,
            Location = new Point(0, 0)
        });
        panel.Controls.Add(new Label
        {
            Text = $"{plan.EnableCount} to enable  •  {plan.DisableCount} to disable",
            Font = AppFonts.Normal,
            ForeColor = AppColors.SecondaryText,
            AutoSize = true,
            Location = new Point(1, 36)
        });
        return panel;
    }

    private static Control CreateTargetLabel(ProfileApplyPlan plan) => new Label
    {
        Text = $"Target: {plan.CommunityFolder}",
        Font = AppFonts.Small,
        ForeColor = AppColors.SecondaryText,
        AutoEllipsis = true,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private static Control CreateOperationsList(ProfileApplyPlan plan)
    {
        var list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            BackColor = AppColors.Surface,
            ForeColor = AppColors.Text,
            BorderStyle = BorderStyle.FixedSingle
        };
        list.Columns.Add("Action", 100);
        list.Columns.Add("Addon", 250);
        list.Columns.Add("Source library", 360);

        foreach (ProfileApplyOperation operation in plan.Operations)
        {
            var item = new ListViewItem(operation.Type.ToString())
            {
                ForeColor = operation.Type == ProfileApplyOperationType.Enable
                    ? AppColors.Success
                    : AppColors.Warning
            };
            item.SubItems.Add(operation.Addon.Name);
            item.SubItems.Add(operation.Addon.Path);
            list.Items.Add(item);
        }

        if (plan.Operations.Count == 0)
        {
            var item = new ListViewItem("No change")
            {
                ForeColor = AppColors.SecondaryText
            };
            item.SubItems.Add("The Community folder already matches this profile.");
            item.SubItems.Add(string.Empty);
            list.Items.Add(item);
        }

        return list;
    }

    private static Control CreateSafetyNote() => new Label
    {
        Text = "Only managed symbolic links in the default Community folder are changed. If any operation fails, completed changes are rolled back in reverse order.",
        Font = AppFonts.Small,
        ForeColor = AppColors.SecondaryText,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private Control CreateButtons()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = AppColors.Background,
            Padding = new Padding(0, 8, 0, 0)
        };

        var applyButton = new KryptonButton
        {
            Text = "Apply profile",
            DialogResult = DialogResult.OK,
            Size = new Size(130, 38)
        };
        applyButton.StateCommon.Back.Color1 = AppColors.Accent;
        applyButton.StateCommon.Back.Color2 = AppColors.Accent;
        applyButton.StateCommon.Content.ShortText.Color1 = Color.White;
        applyButton.StateCommon.Content.ShortText.Font = AppFonts.Button;

        var cancelButton = new KryptonButton
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Size = new Size(104, 38)
        };
        cancelButton.StateCommon.Content.ShortText.Font = AppFonts.Button;

        AcceptButton = applyButton;
        CancelButton = cancelButton;
        panel.Controls.Add(applyButton);
        panel.Controls.Add(cancelButton);
        return panel;
    }
}
