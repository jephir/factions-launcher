using System;
using System.ComponentModel;
using System.Windows;
using Redress;
using System.Net;
using System.IO;
using System.Xml;

namespace FactionsLauncher
{
    public partial class LauncherWindow : Window
    {
        Launcher launcher;

        public LauncherWindow()
        {
            InitializeComponent();

            launcher = (Launcher)windowGrid.DataContext;
        }

        private void CriticalError(string message)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                MessageBox.Show(this, message, "Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            launcher.Shutdown();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var worker = new BackgroundWorker();
            worker.DoWork += DoWork;
            worker.RunWorkerAsync();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            launcher.Run();
        }

        private void DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                launcher.Update((browserUri) =>
                    {
                        Dispatcher.BeginInvoke((Action)(() =>
                        {
                            webBrowser.Navigate(browserUri);
                        }));
                    });
            }
            catch (WebException)
            {
                CriticalError("Can't start the launcher because the configuration server could not be found.\n\nThe server may be offline. Notify the developer at http://forums.factions.ca.");
            }
            catch (FileNotFoundException)
            {
                CriticalError("Can't start the launcher because a configuration file could not be found.\n\nDownload a new launcher from http://www.factions.ca. If the problem persists, notify the developer at http://forums.factions.ca.");
            }
            catch (XmlException)
            {
                CriticalError("Can't start the launcher because a configuration file is corrupt.\n\nDownload a new launcher from http://www.factions.ca. If the problem persists, notify the developer at http://forums.factions.ca.");
            }
        }
    }
}
