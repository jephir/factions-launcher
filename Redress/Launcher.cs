using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Xml;

namespace Redress
{
    public class Launcher : INotifyPropertyChanged
    {
        const string ErrorFile = "errors.txt";

        static Launcher launcher;
        static string errorMessage;

        Package package;
        PackageUpdate update;

        public Launcher()
        {
            launcher = this;
            Status = "Loading...";
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        public bool Ready { get; private set; }
        public string Status { get; private set; }
        public double PackageProgress { get; private set; }
        public float RemainingMegabytes { get; private set; }
        public string ItemName { get; private set; }
        public float ItemSizeMegabytes { get; private set; }
        public double ItemProgress { get; private set; }
        public float SpeedKilobytes { get; private set; }

        public static void Notify(string message)
        {
            launcher.Status = errorMessage = message;

            using (var writer = File.AppendText(ErrorFile))
            {
                writer.WriteLine(message);
            }
        }

        public void Update(Action<Uri> callback)
        {
            var configuration = Configuration.Load();

            callback(configuration.BrowserUri);

            if (configuration.IsLauncherUpToDate) UpdateGameAsync(configuration);
            else UpdateLauncher(configuration);
        }

        public void Shutdown()
        {
            if (package != null) package.CancelAsync();
            if (update != null) update.CancelAsync();
        }

        public void Run()
        {
            if (package != null)
            {
                try
                {
                    package.RunApplication();
                }
                catch
                {
                    Launcher.Notify("The application could not be launched because it reported an error. Try restarting the launcher to verify the application content.");
                    return;
                }
                Environment.Exit(0);
            }
        }

        private void UpdateGameAsync(Configuration config)
        {
            if (File.Exists(ErrorFile)) File.Delete(ErrorFile);

            try
            {
                package = Package.Load(config);
            }
            catch (WebException)
            {
                Launcher.Notify("The update check has been skipped because the content server could not be found.");
                SetReady();
                return;
            }
            catch (XmlException)
            {
                Launcher.Notify("The update check has been skipped because the configuration on the content server is not valid.");
                SetReady();
                return;
            }

            Status = "Verifying...";

            package.OperationProgressChanged += (sender, e) =>
            {
                PackageProgress = e.PackageProgress;
                ItemName = e.ItemPath;
            };

            package.OperationCompleted += (sender0, e0) =>
            {
                update = package.GetUpdate();

                Status = "Downloading...";

                RemainingMegabytes = (float)update.Size / 1024 / 1024;

                var lastSpeedUpdateTime = DateTime.Now;

                update.OperationProgressChanged += (sender, e) =>
                {
                    ItemProgress = e.ItemProgress;
                    PackageProgress = (double)(package.SizeBytes - update.Size + e.CompletedBytes) / package.SizeBytes * 100;
                    RemainingMegabytes = (float)(update.Size - e.SavedBytes) / 1024 / 1024;
                    if (DateTime.Now - lastSpeedUpdateTime > TimeSpan.FromSeconds(1))
                    {
                        SpeedKilobytes = e.SpeedBytes / 1024;
                        lastSpeedUpdateTime = DateTime.Now;
                    }
                    ItemName = e.ItemPath;
                    ItemSizeMegabytes = (float)e.ItemSizeBytes / 1024 / 1024;
                };

                update.OperationCompleted += (sender, e) =>
                {
                    SetReady();
                };

                update.StartAsync();
            };

            package.VerifyAsync();
        }

        private void UpdateLauncher(Configuration config)
        {
            Status = "Updating...";

            try
            {
                config.UpdateLauncher();
            }
            catch (WebException)
            {
                Launcher.Notify("Can't update the launcher because the new version could not be downloaded. The launcher will continue using the current old version.");
            }

            UpdateGameAsync(config);
        }

        private void SetReady()
        {
            Status = "Play";
            if (errorMessage != null) ItemName = errorMessage;
            else ItemName = "Update complete";
            Ready = true;
            ItemProgress = 0;
            SpeedKilobytes = 0;
            ItemSizeMegabytes = 0;
            RemainingMegabytes = 0;
            PackageProgress = 100;
        }
    }
}
