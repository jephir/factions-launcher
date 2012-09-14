using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.Configuration;

namespace Redress
{
    /// <summary>
    /// Represents the launcher configuration.
    /// </summary>
    class LauncherConfiguration
    {
        /// <summary>
        /// Provides the path to the directory containing the launcher configuration files.
        /// </summary>
        public const string LauncherPath = @"Launcher\";

        /// <summary>
        /// Provides the path to the directory containing the managed application.
        /// </summary>
        public const string ContentPath = @"Content\";

        /// <summary>
        /// Provides the path to the text file containing the configuration server address.
        /// </summary>
        public static readonly string ConfigurationServerAddressFile = Path.Combine(LauncherPath, "server.txt");

        /// <summary>
        /// Provides the path to the file containing the cached launcher configuration.
        /// </summary>
        public static readonly string CachedConfigurationFile = Path.Combine(LauncherPath, "config.xml");

        /// <summary>
        /// Provides the path to the text file containing the version number of the launcher on the local system.
        /// </summary>
        public static readonly string LauncherLocalVersionFile = Path.Combine(LauncherPath, "version.txt");

        /// <summary>
        /// Provides the path to the file indicating if the application has been installed on the local machine.
        /// </summary>
        public static readonly string InstalledFlagFile = Path.Combine(LauncherPath, "installed.txt");

        /// <summary>
        /// Initializes a new instance of the <see cref="LauncherConfiguration"/> class.
        /// </summary>
        /// <param name="document">The XML document that will be parsed to create the configuration.</param>
        /// <param name="localVersion">The version number of the launcher on the system.</param>
        LauncherConfiguration(XDocument document, int localVersion)
        {
            LauncherLocalVersion = localVersion;

            try
            {
                // Parse the XML document
                var node = document.Element("config");
                ManifestUri = new Uri(node.Element("manifest").Attribute("uri").Value);
                BrowserUri = new Uri(node.Element("browser").Attribute("uri").Value);
                LauncherRemoteVersion = Convert.ToInt32(node.Element("launcher").Attribute("version").Value);

                // Load the launcher download URI from the document if it exists; the attribute is optional
                var launcherUriAttribute = node.Element("launcher").Attribute("uri");
                if (launcherUriAttribute != null) LauncherDownloadUri = new Uri(launcherUriAttribute.Value);
            }
            catch (NullReferenceException)
            {
                throw new XmlException("The configuration is missing required elements or attributes.");
            }
        }

        /// <summary>
        /// Gets the web address of the application manifest.
        /// </summary>
        public Uri ManifestUri { get; private set; }

        /// <summary>
        /// Gets the web address of the site to display in the launcher web browser.
        /// </summary>
        public Uri BrowserUri { get; private set; }

        /// <summary>
        /// Gets the web address of the latest version of the launcher executable.
        /// </summary>
        public Uri LauncherDownloadUri { get; private set; }

        /// <summary>
        /// Gets the version number of the latest version of the launcher.
        /// </summary>
        public int LauncherRemoteVersion { get; private set; }

        /// <summary>
        /// Gets the version number of the launcher on the local system.
        /// </summary>
        public int LauncherLocalVersion { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the launcher matches the latest version available.
        /// </summary>
        public bool IsLauncherUpToDate { get { return LauncherLocalVersion == LauncherRemoteVersion; } }

        /// <summary>
        /// Returns the configuration parsed from the server.
        /// </summary>
        /// <returns>The configuration object.</returns>
        public static LauncherConfiguration Load()
        {
            var xml = LoadXml();

            var document = XDocument.Parse(xml);

            int localLauncherVersionNumber;

            // Create the local version file if it does not exist
            if (!File.Exists(LauncherLocalVersionFile))
            {
                localLauncherVersionNumber = 1;
                File.WriteAllText(LauncherLocalVersionFile, localLauncherVersionNumber.ToString());
            }
            else
            {
                localLauncherVersionNumber = Convert.ToInt32(File.ReadAllText(LauncherLocalVersionFile));
            }

            return new LauncherConfiguration(document, localLauncherVersionNumber);
        }

        /// <summary>
        /// Returns a value indicating whether or not the given path is under the content directory.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>TRUE if the specified path is under the content directory; otherwise, FALSE.</returns>
        public static bool IsPathUnderContentDirectory(string path)
        {
            var absolutePath = Path.GetFullPath(path);
            var absoluteContentPath = Path.GetFullPath(ContentPath);

            return absolutePath.Contains(absoluteContentPath);
        }

        /// <summary>
        /// Returns an XML string of the launcher configuration.
        /// </summary>
        /// <returns>An XML string representing the launcher configuration.</returns>
        private static string LoadXml()
        {
            String serverAddress;

            if (File.Exists(ConfigurationServerAddressFile)) serverAddress = File.ReadAllText(ConfigurationServerAddressFile);
            else if (File.Exists(CachedConfigurationFile)) return File.ReadAllText(CachedConfigurationFile);
            else throw new FileNotFoundException("Could not find server address file '" + ConfigurationServerAddressFile + "'.");

            using (var client = new WebClient())
            {
                client.Encoding = System.Text.Encoding.UTF8;

                String xml;

                try
                {
                    xml = client.DownloadString(serverAddress);

                    // Cache the downloaded configuration
                    File.WriteAllText(CachedConfigurationFile, xml);
                }
                catch (WebException)
                {
                    if (File.Exists(CachedConfigurationFile)) xml = File.ReadAllText(CachedConfigurationFile);
                    else throw;
                }

                return xml;
            }
        }

        /// <summary>
        /// Replaces the launcher with the latest version available.
        /// </summary>
        public void UpdateLauncher()
        {
            var launcherPath = Process.GetCurrentProcess().MainModule.FileName;
            var launcherFileName = Path.GetFileName(launcherPath);
            var backupFileName = Path.Combine(LauncherPath, launcherFileName);

            if (File.Exists(backupFileName)) File.Delete(backupFileName);
            
            File.Move(launcherPath, backupFileName);

            using (var client = new WebClient())
            {
                client.DownloadFile(LauncherDownloadUri, launcherPath);
            }

            File.WriteAllText(LauncherLocalVersionFile, LauncherRemoteVersion.ToString());
        }
    }
}
