using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using Redress.Support;

namespace Redress
{
    /// <summary>
    /// Represents a set of application files.
    /// </summary>
    class Package : AsyncOperation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Package"/> class.
        /// </summary>
        /// <param name="document">The XML document that will be parsed to create the package.</param>
        Package(XDocument document)
        {
            try
            {
                var package = document.Element("package");

                Items = (from node in package.Elements("item")
                         select new Item(
                             node.Attribute("path").Value,
                             node.Attribute("uri").Value,
                             node.Attribute("size").Value,
                             node.Attribute("sha256").Value
                         )).ToList();

                var applicationPath = Path.Combine(LauncherConfiguration.ContentPath, package.Element("application").Attribute("path").Value);
                if (LauncherConfiguration.IsPathUnderContentDirectory(applicationPath)) ApplicationPath = applicationPath;
                else throw new ArgumentException("Application path is not under content directory.", "document");

                ApplicationArguments = package.Element("application").Attribute("arguments").Value;

                var installerPath = Path.Combine(LauncherConfiguration.ContentPath, package.Element("installer").Attribute("path").Value);
                if (LauncherConfiguration.IsPathUnderContentDirectory(installerPath)) InstallerPath = installerPath;
                else throw new ArgumentException("Installer path is not under content directory.", "document");
            }
            catch (NullReferenceException)
            {
                throw new XmlException("The package manifest is missing required elements or attributes.");
            }
        }

        /// <summary>
        /// Gets a list of items in the current package.
        /// </summary>
        public IList<Item> Items { get; private set; }

        /// <summary>
        /// Gets a list of items that need to be updated.
        /// </summary>
        public IList<Item> UpdateItems { get; private set; }

        /// <summary>
        /// Gets the path to the application executable.
        /// </summary>
        public string ApplicationPath { get; private set; }

        /// <summary>
        /// Gets the arguments the the application will be run with.
        /// </summary>
        public string ApplicationArguments { get; private set; }

        /// <summary>
        /// Gets the path to the installer executable.
        /// </summary>
        public string InstallerPath { get; private set; }

        /// <summary>
        /// Gets the size of the package in bytes.
        /// </summary>
        public long SizeBytes { get { return Items.Sum((item) => { return item.SizeBytes; }); } }

        /// <summary>
        /// Gets the number of items in the package.
        /// </summary>
        public long Count { get { return Items.Count; } }

        /// <summary>
        /// Returns the package loaded from the specified configuration.
        /// </summary>
        /// <param name="config">The configuration to use.</param>
        /// <returns>The package object.</returns>
        public static Package Load(LauncherConfiguration config)
        {
            using (var client = new WebClient())
            {
                client.Encoding = System.Text.Encoding.UTF8;

                var manifest = client.DownloadString(config.ManifestUri);
                var document = XDocument.Parse(manifest);
                var package = new Package(document);

                return package;
            }
        }

        /// <summary>
        /// Populates the current package's list of items that need to be updated.
        /// </summary>
        public void VerifyAsync()
        {
            UpdateItems = new List<Item>();

            var checkedBytes = 0L;

            foreach (var item in Items)
            {
                if (CancellationPending)
                {
                    OnOperationCompleted(new EventArgs());
                    return;
                }

                OnOperationProgressChanged(new AsyncOperationProgressChangedEventArgs
                {
                    PackageProgress = (double)checkedBytes / SizeBytes * 100,
                    CompletedBytes = checkedBytes,
                    ItemPath = item.LocalPath,
                });

                if (!item.Validate()) UpdateItems.Add(item);

                checkedBytes += item.SizeBytes;
            }

            OnOperationCompleted(new EventArgs());
        }

        /// <summary>
        /// Returns the packge update object containing the items that need to be updated.
        /// </summary>
        /// <returns>The package update object.</returns>
        public PackageUpdate GetUpdate()
        {
            return new PackageUpdate(UpdateItems);
        }

        /// <summary>
        /// Runs the application.
        /// </summary>
        public void RunApplication()
        {
            // Run the installer if the application is not installed
            if (!File.Exists(LauncherConfiguration.InstalledFlagFile))
            {
                var installer = new ProcessStartInfo();
                installer.FileName = InstallerPath;

                using (var process = Process.Start(installer))
                {
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        using (var writer = File.CreateText(LauncherConfiguration.InstalledFlagFile))
                        {
                            writer.WriteLine(DateTime.Now.ToBinary());
                        }
                    }
                    else
                    {
                        Launcher.Notify("The application installer reported failure. The application will not be run.");
                        throw new ApplicationException("The application installer reported failure.");
                    }
                }
            }

            Process.Start(ApplicationPath, ApplicationArguments);
        }
    }
}
