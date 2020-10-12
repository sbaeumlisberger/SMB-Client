using System;
using System.Collections.Generic;
using System.Text;

namespace SMBClient.ViewModels
{
    public class InputDialogModel
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool Canceled { get; set; } = true;
    }
}
