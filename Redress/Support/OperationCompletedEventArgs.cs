using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Redress.Support
{
    public enum OperationResult
    {
        Success,
        Error,
        Cancelled,
        NoAction
    }

    class OperationCompletedEventArgs : EventArgs
    {
        public OperationResult Result { get; private set; }

        public OperationCompletedEventArgs(OperationResult result)
        {
            Result = result;
        }
    }
}
