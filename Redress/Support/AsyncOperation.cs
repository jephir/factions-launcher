using System;

namespace Redress.Support
{
    class AsyncOperation
    {
        public delegate void OperationProgressChangedEventHandler(object sender, OperationProgressChangedEventArgs e);

        public delegate void OperationCompletedEventHandler(object sender, OperationCompletedEventArgs e);

        public event OperationProgressChangedEventHandler OperationProgressChanged;

        public event OperationCompletedEventHandler OperationCompleted;

        /// <summary>
        /// Gets a value indicating whether the application has requested cancellation of the current operation.
        /// </summary>
        public bool CancellationPending { get; private set; }

        protected virtual void OnOperationProgressChanged(OperationProgressChangedEventArgs e)
        {
            var handler = OperationProgressChanged;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnOperationCompleted(OperationCompletedEventArgs e)
        {
            var handler = OperationCompleted;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Requests cancellation of the current operation.
        /// </summary>
        public virtual void CancelAsync()
        {
            CancellationPending = true;
        }
    }
}
