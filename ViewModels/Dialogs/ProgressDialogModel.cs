using ReactiveUI;
using SMBClient.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SMBClient.ViewModels
{
    public class ProgressDialogModel : ViewModelBase
    {
        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public Progress Progess { get; }

        public ProgressDialogModel(Progress progess)
        {
            Progess = progess;
        }

        /* design time */
        public ProgressDialogModel()
        {
            Progess = new Progress();
            Progess.Initialize(10000);
            Progess.Report(5438);
        }
    }
}
