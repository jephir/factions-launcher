using System;
using System.Collections.Generic;
using System.Linq;
using Redress.Support;
using System.IO;

namespace Redress
{
    /// <summary>
    /// Represents a collection of items to update.
    /// </summary>
    class PackageUpdate : AsyncOperation
    {
        private Queue<Item> itemQueue;
        private Item item;
        private int itemsCompleted;
        private long downloadedBytes;
        private bool inProgress;
        private int version;

        /// <summary>
        /// Gets the total size of the update in bytes.
        /// </summary>
        public long Size { get; private set; }

        /// <summary>
        /// Gets the number of items remaining that need to be updated.
        /// </summary>
        public long Count { get { return itemQueue.Count; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageUpdate"/> class.
        /// </summary>
        /// <param name="items">A collection of items to update.</param>
        public PackageUpdate(int version, IEnumerable<Item> items)
        {
            this.version = version;
            itemQueue = new Queue<Item>(items);
            Size = itemQueue.Sum((item) => { return item.SizeBytes; });
        }

        /// <summary>
        /// Starts updating the package.
        /// </summary>
        public void StartAsync()
        {
            if (!inProgress)
            {
                inProgress = true;
                UpdateAsync();
            }
            else
            {
                Launcher.Notify("The update is already in progress.");
            }
        }

        /// <summary>
        /// Stops updating the package.
        /// </summary>
        public override void CancelAsync()
        {
            base.CancelAsync();

            if (item != null) item.CancelAsync();
        }

        protected override void OnOperationCompleted(OperationCompletedEventArgs e)
        {
            base.OnOperationCompleted(e);

            inProgress = false;

            var isUpToDate = e.Result == OperationResult.Success || e.Result == OperationResult.NoAction;

            if (isUpToDate) File.WriteAllText(LauncherConfiguration.LauncherLocalVersionFile, version.ToString());
        }

        /// <summary>
        /// Updates the next item in the update queue.
        /// </summary>
        private void UpdateAsync()
        {
            if (CancellationPending )
            {
                OnOperationCompleted(new OperationCompletedEventArgs(OperationResult.Cancelled));
                return;
            }
            else if (itemQueue.Count == 0)
            {
                OnOperationCompleted(new OperationCompletedEventArgs(OperationResult.NoAction));
                return;
            }

            item = itemQueue.Dequeue();

            item.OperationProgressChanged += (sender, e) =>
            {
                var currentDownloadedBytes = downloadedBytes + e.CompletedBytes;
                var percent = (double)currentDownloadedBytes / Size * 100;

                OnOperationProgressChanged(new OperationProgressChangedEventArgs
                {
                    PackageProgress = percent,
                    SpeedBytes = e.SpeedBytes,
                    CompletedBytes = currentDownloadedBytes,
                    SavedBytes = downloadedBytes,
                    ItemPath = item.LocalPath,
                    ItemProgress = e.ItemProgress,
                    ItemSizeBytes = item.SizeBytes,
                });
            };

            item.OperationCompleted += (sender, e) =>
            {
                itemsCompleted++;
                downloadedBytes += item.SizeBytes;

                if (itemQueue.Count > 0) UpdateAsync();
                else OnOperationCompleted(new OperationCompletedEventArgs(OperationResult.Success));
            };

            item.DownloadAsync();
        }
    }
}
