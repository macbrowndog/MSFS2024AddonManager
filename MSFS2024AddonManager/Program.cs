using MSFS2024AddonManager.Services;
using MSFS2024AddonManager.UI.Themes;

namespace MSFS2024AddonManager;

internal static class Program
{
    private static int errorDialogVisible;

    [STAThread]
    static void Main()
    {
        ConfigureExceptionHandling();
        AppLog.Information("Application starting.");

        try
        {
            ApplicationConfiguration.Initialize();

            ThemeService.ApplyTheme();

            Application.Run(new MainForm());
            AppLog.Information("Application stopped normally.");
        }
        catch (Exception exception) when (!IsFatal(exception))
        {
            string incidentId = AppLog.UnexpectedException(
                "Application startup or shutdown failed.",
                exception);
            ShowErrorDialog(incidentId, canContinue: false);
        }
    }

    private static void ConfigureExceptionHandling()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, args) =>
        {
            if (IsFatal(args.Exception))
            {
                AppLog.UnexpectedException("Fatal UI-thread exception.", args.Exception);
                Application.Exit();
                return;
            }

            string incidentId = AppLog.UnexpectedException(
                "Unexpected UI-thread exception; the current operation was stopped.",
                args.Exception);
            ShowErrorDialog(incidentId, canContinue: true);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Exception exception = args.ExceptionObject as Exception ??
                new InvalidOperationException(args.ExceptionObject?.ToString());
            AppLog.UnexpectedException(
                args.IsTerminating
                    ? "Unhandled exception is terminating the application."
                    : "Unhandled application-domain exception.",
                exception);
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            AppLog.UnexpectedException(
                "Unobserved background task exception.",
                args.Exception);
            args.SetObserved();
        };
    }

    private static bool IsFatal(Exception exception)
    {
        return exception is OutOfMemoryException or
            StackOverflowException or
            AccessViolationException;
    }

    private static void ShowErrorDialog(string incidentId, bool canContinue)
    {
        if (Interlocked.Exchange(ref errorDialogVisible, 1) != 0)
        {
            return;
        }

        try
        {
            string recoveryMessage = canContinue
                ? "The current operation was stopped. You can continue using the application, but restart it if the interface behaves unexpectedly."
                : "The application cannot continue and will close.";

            MessageBox.Show(
                $"An unexpected error occurred.\r\n\r\n{recoveryMessage}\r\n\r\nIncident: {incidentId}\r\nLog: {AppLog.LogPath}",
                MainForm.ApplicationName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception dialogError) when (!IsFatal(dialogError))
        {
            AppLog.UnexpectedException("Could not display the error dialog.", dialogError);
        }
        finally
        {
            Interlocked.Exchange(ref errorDialogVisible, 0);
        }
    }
}
