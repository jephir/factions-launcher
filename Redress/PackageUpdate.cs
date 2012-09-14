using System;
using System.Collections.Generic;
using System.Linq;
using Redress.Support;

namespace Redress
{
    /// <summary>
    /// Represents a package update.
    /// </summary>
    class PackageUpdate : AsyncOperation
    {
        private Queue<Item> itemQueue;
        private Item item;
        private int itemsCompleted;
        private long downloadedBytes;
        private bool inProgress;

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
        public PackageUpdate(IEnumerable<Item> items)
        {
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

        /// <summary>
        /// Updates the next item in the update queue.
        /// </summary>
        private void UpdateAsync()
        {
            if (CancellationPending || itemQueue.Count == 0)
            {
                OnOperationCompleted(new EventArgs());
                return;
            }

            item = itemQueue.Dequeue();

            item.OperationProgressChanged += (sender, e) =>
            {
                var currentDownloadedBytes = downloadedBytes + e.CompletedBytes;
                var percent = (double)currentDownloadedBytes / Size * 100;

                OnOperationProgressChanged(new AsyncOperationProgressChangedEventArgs
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
                else OnOperationCompleted(new EventArgs());
            };

            item.DownloadAsync();
        }
    }
}
