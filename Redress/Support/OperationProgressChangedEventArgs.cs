using System;

namespace Redress.Support
{
    class AsyncOperationProgressChangedEventArgs : EventArgs
    {
        public long SavedBytes { get; set; }
        public long CompletedBytes { get; set; }
        public float SpeedBytes { get; set; }
        public double PackageProgress { get; set; }
        public double ItemProgress { get; set; }
        public string ItemPath { get; set; }
        public long ItemSizeBytes { get; set; }
    }
}
