using System.Text;
using Terminal.Gui;

namespace DodiDownloader.Ui
{
    public static class AboutWindow
    {
        public static void Run()
        {
            StringBuilder message = new StringBuilder();

            message.AppendLine(" ");
            message.AppendLine("This app scrapes its data from dodi-repacks.site. As such, all links to files originate from there.");
            message.AppendLine(" ");

            MessageBox.Query("Dodi Repack Downloader", message.ToString(), "OK");
        }
    }
}