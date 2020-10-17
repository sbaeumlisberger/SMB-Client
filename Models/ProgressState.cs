using System;
using System.Collections.Generic;
using System.Text;

namespace SMBClient.Models
{
    public enum ProgressState
    {
        Running,
        Canceled,
        Failed,
        Finished
    }
}
