using System.ComponentModel;
using AppColors = MSFS2024AddonManager.UI.Colors.Colors;

namespace MSFS2024AddonManager.UI.Controls;

public sealed class AvionicsPanel : Panel
{
    public AvionicsPanel()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = AppColors.Border;

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        if (ClientSize.Width < 2 || ClientSize.Height < 2)
        {
            return;
        }

        using var pen = new Pen(BorderColor);
        eventArgs.Graphics.DrawRectangle(
            pen,
            0,
            0,
            ClientSize.Width - 1,
            ClientSize.Height - 1);
    }
}

public sealed class AvionicsTableLayoutPanel : TableLayoutPanel
{
    public AvionicsTableLayoutPanel()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = AppColors.Border;

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        if (ClientSize.Width < 2 || ClientSize.Height < 2)
        {
            return;
        }

        using var pen = new Pen(BorderColor);
        eventArgs.Graphics.DrawRectangle(
            pen,
            0,
            0,
            ClientSize.Width - 1,
            ClientSize.Height - 1);
    }
}
