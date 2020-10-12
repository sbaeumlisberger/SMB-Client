using System;
using System.Collections.Generic;
using System.Text;

namespace SMBClient.ViewModels
{
    public class ConfirmDialogModel
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool Canceled { get; set; } = true;
    }
}
