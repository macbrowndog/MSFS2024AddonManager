using Krypton.Toolkit;
using MSFS2024AddonManager.Models;
using MSFS2024AddonManager.Services;
using AppColors = MSFS2024AddonManager.UI.Colors.Colors;
using AppFonts = MSFS2024AddonManager.UI.Fonts.Fonts;

namespace MSFS2024AddonManager.Views;

public sealed class ScanView : KryptonPanel
{
    private readonly SettingsService settingsService = new();
    private readonly ScanDiagnosticsService diagnosticsService = new();
    private readonly ListView resultsList = new();
    private readonly Label summaryLabel = new();
    private readonly KryptonButton scanButton = new();
    private readonly KryptonButton exportButton = new();
    private DiagnosticReport? lastReport;

    public ScanView()
    {
        Dock = DockStyle.Fill;
        StateCommon.Color1 = AppColors.Background;
        BuildInterface();
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
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
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
            Text = "Scan & Diagnostics",
            Font = AppFonts.Header,
            ForeColor = AppColors.Text,
            AutoSize = true,
            Location = new Point(0, 0)
        });
        panel.Controls.Add(new Label
        {
            Text = "Validate paths and package metadata without changing any MSFS files.",
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
            Padding = new Padding(14)
        };

        scanButton.Text = "Run diagnostics";
        scanButton.Location = new Point(14, 16);
        scanButton.Size = new Size(150, 38);
        StylePrimaryButton(scanButton);
        scanButton.Click += async (_, _) => await RunDiagnosticsAsync();

        exportButton.Text = "Export report";
        exportButton.Location = new Point(176, 16);
        exportButton.Size = new Size(130, 38);
        exportButton.Enabled = false;
        exportButton.StateCommon.Back.Color1 = AppColors.SurfaceLight;
        exportButton.StateCommon.Back.Color2 = AppColors.SurfaceLight;
        exportButton.StateCommon.Content.ShortText.Color1 = AppColors.Text;
        exportButton.StateCommon.Content.ShortText.Font = AppFonts.Button;
        exportButton.Click += ExportReport;

        summaryLabel.Font = AppFonts.Small;
        summaryLabel.ForeColor = AppColors.SecondaryText;
        summaryLabel.AutoSize = true;
        summaryLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        summaryLabel.Text = "Ready to scan";

        panel.Controls.AddRange([scanButton, exportButton, summaryLabel]);
        panel.Resize += (_, _) =>
            summaryLabel.Location = new Point(
                panel.ClientSize.Width - summaryLabel.Width - 18,
                27);
        return panel;
    }

    private Control CreateResultsArea()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppColors.Background,
            Padding = new Padding(0, 16, 0, 0)
        };

        resultsList.Dock = DockStyle.Fill;
        resultsList.View = View.Details;
        resultsList.FullRowSelect = true;
        resultsList.GridLines = false;
        resultsList.HideSelection = false;
        resultsList.BackColor = AppColors.Surface;
        resultsList.ForeColor = AppColors.Text;
        resultsList.Font = AppFonts.Normal;
        resultsList.BorderStyle = BorderStyle.None;
        resultsList.Columns.Add("Status", 100);
        resultsList.Columns.Add("Check", 170);
        resultsList.Columns.Add("Result", 450);
        resultsList.Columns.Add("Path", 500);
        panel.Controls.Add(resultsList);
        return panel;
    }

    private async Task RunDiagnosticsAsync()
    {
        scanButton.Enabled = false;
        exportButton.Enabled = false;
        scanButton.Text = "Scanning...";
        summaryLabel.Text = "Checking paths and manifests";
        resultsList.Items.Clear();

        try
        {
            lastReport = await diagnosticsService.RunAsync(settingsService.Load());
            DisplayReport(lastReport);
            exportButton.Enabled = true;
        }
        finally
        {
            scanButton.Enabled = true;
            scanButton.Text = "Run diagnostics";
        }
    }

    private void DisplayReport(DiagnosticReport report)
    {
        foreach (DiagnosticItem item in report.Items)
        {
            var row = new ListViewItem(item.Severity.ToString().ToUpperInvariant())
            {
                ForeColor = GetSeverityColor(item.Severity),
                BackColor = AppColors.Surface
            };
            row.SubItems.Add(item.Check);
            row.SubItems.Add(item.Result);
            row.SubItems.Add(item.Path);
            resultsList.Items.Add(row);
        }

        int issues = report.Items.Count(item =>
            item.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error);
        summaryLabel.ForeColor = issues == 0 ? AppColors.Success : AppColors.Warning;
        summaryLabel.Text =
            $"{report.PackageFolders} packages • {report.ValidManifests} valid manifests • {issues} issues";
    }

    private void ExportReport(object? sender, EventArgs e)
    {
        if (lastReport is null)
        {
            return;
        }

        using var dialog = new KryptonSaveFileDialog
        {
            Title = "Export scan diagnostics",
            FileName = $"MSFS2024-diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.txt",
            DefaultExt = "txt",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        File.WriteAllText(
            dialog.FileName,
            diagnosticsService.FormatReport(lastReport));
        summaryLabel.ForeColor = AppColors.Success;
        summaryLabel.Text = "Diagnostics report exported";
    }

    private static void StylePrimaryButton(KryptonButton button)
    {
        button.StateCommon.Back.Color1 = AppColors.Accent;
        button.StateCommon.Back.Color2 = AppColors.Accent;
        button.StateCommon.Content.ShortText.Color1 = Color.White;
        button.StateCommon.Content.ShortText.Font = AppFonts.Button;
    }

    private static Color GetSeverityColor(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Success => AppColors.Success,
        DiagnosticSeverity.Warning => AppColors.Warning,
        DiagnosticSeverity.Error => AppColors.Error,
        _ => AppColors.SecondaryText
    };
}
