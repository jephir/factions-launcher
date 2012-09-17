using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using Redress.Support;

namespace Redress
{
    /// <summary>
    /// Represents a file in an application package.
    /// </summary>
    class Item : AsyncOperation
    {
        private static readonly SHA256 sha256 = new SHA256Managed();

        /// <summary>
        /// Initializes a new instance of the <see cref="Item"/> class.
        /// </summary>
        /// <param name="localPath">The path of the item on the local system.</param>
        /// <param name="remoteUri">The address of the item on the content server.</param>
        /// <param name="size">The size of the item in bytes.</param>
        /// <param name="hash">The SHA-256 hash of the item.</param>
        public Item(string localPath, string remoteUri, string size, string hash)
        {
            var adjustedLocalPath = Path.Combine(LauncherConfiguration.ContentPath, localPath.Replace('/', '\\'));

            // The item must be located under the content directory
            if (LauncherConfiguration.IsPathUnderContentDirectory(adjustedLocalPath)) LocalPath = adjustedLocalPath;
            else throw new ArgumentException("Local path is not under content directory.", "localPath");

            DownloadUri = new Uri(remoteUri);
            SizeBytes = Convert.ToInt64(size);
            Hash = hash;
        }

        /// <summary>
        /// Gets the path of the current item on the local system.
        /// </summary>
        public string LocalPath { get; private set; }

        /// <summary>
        /// Gets the address of the current item on the content server.
        /// </summary>
        public Uri DownloadUri { get; private set; }

        /// <summary>
        /// Gets the size of the current item in bytes.
        /// </summary>
        public long SizeBytes { get; private set; }

        /// <summary>
        /// Gets the SHA-256 hash of the current item.
        /// </summary>
        public string Hash { get; private set; }

        /// <summary>
        /// Returns a value indicating whether the current item matches the one on the content server.
        /// </summary>
        public bool Validate()
        {
            if (!File.Exists(LocalPath)) return false;
            var fileBytes = File.ReadAllBytes(LocalPath);
            return HashEquals(fileBytes);
        }

        /// <summary>
        /// Updates the current item to the version on the content server.
        /// </summary>
        public void UpdateAsync()
        {
            if (!Validate()) DownloadAsync();
            else OnOperationCompleted(new OperationCompletedEventArgs(OperationResult.NoAction));
        }

        /// <summary>
        /// Downloads the current item from the content server to the local system.
        /// </summary>
        public void DownloadAsync()
        {
            using (var client = new WebClient())
            {
                var lastUpdateTime = DateTime.Now;
                var lastBytesReceived = 0L;

                client.DownloadProgressChanged += (sender, e) =>
                {
                    if (CancellationPending) client.CancelAsync();

                    var now = DateTime.Now;
                    var timeSpan = now - lastUpdateTime;

                    if (timeSpan.Milliseconds == 0) return;

                    var bytesReceived = e.BytesReceived;
                    var bytesChange = bytesReceived - lastBytesReceived;
                    var bytesPerSecond = (float)bytesChange / timeSpan.Milliseconds * 1000;

                    OnOperationProgressChanged(new OperationProgressChangedEventArgs
                    {
                        CompletedBytes = e.BytesReceived,
                        SpeedBytes = bytesPerSecond,
                        ItemProgress = e.ProgressPercentage,
                    });

                    lastBytesReceived = bytesReceived;
                    lastUpdateTime = now;
                };

                client.DownloadDataCompleted += (sender, e) =>
                {
                    if (e.Error != null)
                    {
                        Launcher.Notify("Skipping updating file \"" + LocalPath + "\" because it could not be downloaded.");
                        OnOperationCompleted(new OperationCompletedEventArgs(OperationResult.Error));
                    }
                    else if (e.Cancelled)
                    {
                        OnOperationCompleted(new OperationCompletedEventArgs(OperationResult.Cancelled));
                    }
                    else if (HashEquals(e.Result))
                    {
                        CreateFile(e.Result);
                    }
                    else
                    {
                        Launcher.Notify("Skipping updating file \"" + LocalPath + "\" because it the downloaded file is invalid.");
                        OnOperationCompleted(new OperationCompletedEventArgs(OperationResult.Error));
                    }
                };

                client.DownloadDataAsync(DownloadUri);
            }
        }

        /// <summary>
        /// Determines whether the specified bytes are equal to the current item hash.
        /// </summary>
        /// <param name="inputBytes">The bytes to compare with the item hash.</param>
        /// <returns>TRUE if the specified bytes are equal to the current item hash; otherwise, FALSE.</returns>
        public bool HashEquals(byte[] inputBytes)
        {
            var digest = sha256.ComputeHash(inputBytes);
            var digestString = BitConverter.ToString(digest).Replace("-", string.Empty);

            return Hash.Equals(digestString, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates the current item on the local system using the specified bytes.
        /// </summary>
        /// <param name="hashBytes">The file bytes.</param>
        private void CreateFile(byte[] hashBytes)
        {
            var path = Path.GetDirectoryName(LocalPath);

            if (path.Length > 0 && !Directory.Exists(path)) Directory.CreateDirectory(path);

            File.WriteAllBytes(LocalPath, hashBytes);

            OnOperationCompleted(new OperationCompletedEventArgs(OperationResult.Success));
        }
    }
}
