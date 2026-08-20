using Krypton.Toolkit;
using AppColors = MSFS2024AddonManager.UI.Colors.Colors;
using AppFonts = MSFS2024AddonManager.UI.Fonts.Fonts;

namespace MSFS2024AddonManager.UI.Themes;

public static class ThemeService
{
    public static void ApplyTheme()
    {
        Application.SetDefaultFont(AppFonts.Normal);
    }

    public static void StylePrimaryButton(KryptonButton button)
    {
        button.StateCommon.Back.Color1 = AppColors.Accent;
        button.StateCommon.Back.Color2 = AppColors.Accent;
        button.StateCommon.Border.Color1 = AppColors.Accent;
        button.StateCommon.Border.Color2 = AppColors.Accent;
        button.StateCommon.Border.DrawBorders = PaletteDrawBorders.All;
        button.StateCommon.Border.Rounding = 1;
        button.StateCommon.Border.Width = 1;
        button.StateCommon.Content.ShortText.Color1 = AppColors.Background;
        button.StateCommon.Content.ShortText.Color2 = AppColors.Background;
        button.StateCommon.Content.ShortText.Font = AppFonts.Button;
    }

    public static void StyleSecondaryButton(KryptonButton button)
    {
        button.StateCommon.Back.Color1 = AppColors.Control;
        button.StateCommon.Back.Color2 = AppColors.Control;
        button.StateCommon.Border.Color1 = AppColors.Accent;
        button.StateCommon.Border.Color2 = AppColors.Accent;
        button.StateCommon.Border.DrawBorders = PaletteDrawBorders.All;
        button.StateCommon.Border.Rounding = 1;
        button.StateCommon.Border.Width = 1;
        button.StateCommon.Content.ShortText.Color1 = AppColors.Accent;
        button.StateCommon.Content.ShortText.Color2 = AppColors.Accent;
        button.StateCommon.Content.ShortText.Font = AppFonts.Button;
    }

    public static void StyleTextBox(KryptonTextBox textBox)
    {
        textBox.StateCommon.Back.Color1 = AppColors.Control;
        textBox.StateCommon.Content.Color1 = AppColors.Cyan;
        textBox.StateCommon.Content.Font = AppFonts.Readout;
        textBox.StateCommon.Border.Color1 = AppColors.Border;
        textBox.StateCommon.Border.Color2 = AppColors.Border;
        textBox.StateCommon.Border.DrawBorders = PaletteDrawBorders.All;
        textBox.StateCommon.Border.Rounding = 1;
        textBox.StateCommon.Border.Width = 1;
    }

    public static void StyleStandardButton(Button button, bool primary = false)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = primary ? AppColors.Accent : AppColors.Control;
        button.ForeColor = primary ? AppColors.Background : AppColors.Accent;
        button.Font = AppFonts.Button;
        button.Cursor = Cursors.Hand;
        button.TabStop = false;
        button.UseVisualStyleBackColor = false;
        button.FlatAppearance.BorderColor = AppColors.Accent;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = primary
            ? Color.FromArgb(218, 160, 60)
            : AppColors.ControlHover;
        button.FlatAppearance.MouseDownBackColor = AppColors.AccentDark;
    }

    public static void StyleComboBox(ComboBox comboBox)
    {
        comboBox.Font = AppFonts.Readout;
        comboBox.BackColor = AppColors.Control;
        comboBox.ForeColor = AppColors.Cyan;
        comboBox.FlatStyle = FlatStyle.Flat;
    }

    public static void StyleListView(ListView listView)
    {
        listView.BackColor = AppColors.Control;
        listView.ForeColor = AppColors.Text;
        listView.Font = AppFonts.Readout;
        listView.BorderStyle = BorderStyle.FixedSingle;
    }

    public static void StyleTreeView(TreeView treeView)
    {
        treeView.BackColor = AppColors.Navigation;
        treeView.ForeColor = AppColors.Cyan;
        treeView.Font = AppFonts.LibraryTree;
        treeView.ItemHeight = 24;
        treeView.BorderStyle = BorderStyle.FixedSingle;
        treeView.LineColor = AppColors.Border;
    }

    public static void StyleCheckBox(CheckBox checkBox)
    {
        checkBox.Font = AppFonts.Normal;
        checkBox.ForeColor = AppColors.Text;
        checkBox.FlatStyle = FlatStyle.Flat;
        checkBox.FlatAppearance.BorderColor = AppColors.Accent;
    }
}
