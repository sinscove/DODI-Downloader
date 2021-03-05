using System;
using System.IO;
using System.Threading.Tasks;
using DodiDownloader.Ui;
using NStack;
using Terminal.Gui;

namespace DodiDownloader
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            DodiCache dodiCache = new DodiCache();
            await dodiCache.LoadCache(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DodiCache.json"));

            Application.Init();

            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                Exception exception = (Exception)eventArgs.ExceptionObject;
                MessageBox.ErrorQuery($"Unhandled {exception.GetType().FullName}", exception.Message, "OK");
            };

            MainWindow mainWindow = new MainWindow(dodiCache);

            StatusBar statusBar = new StatusBar();

            statusBar.Items = new []
            {
                new StatusItem(Key.Unknown, "(Ctrl+Q) Quit", Application.RequestStop),
                new StatusItem(Key.ControlS, ustring.Make("(Ctrl+S) Search"), () =>
                {
                    Application.Top.Remove(statusBar);
                    mainWindow.Search(() => 
                    {
                        mainWindow.Height = Dim.Fill(1);
                        Application.Top.Add(statusBar);
                    });
                }),
                new StatusItem(Key.ControlH, ustring.Make("(Ctrl+H) Home"), () => _ = mainWindow.SetListViewSource(false)),
                new StatusItem(Key.ControlR, ustring.Make("(Ctrl+R) Regenerate Cache"), () => _ = mainWindow.SetListViewSource(true))
            };

            Application.Top.Add(mainWindow, statusBar);

            AboutWindow.Run();
            Application.Run();
            Application.Shutdown();
        }
    }
}