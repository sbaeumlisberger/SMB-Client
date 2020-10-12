using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SMBClient.ViewModels
{
    public class SaveFileDialogModel
    {
        public string? FilePath { get; set; }
        public string InitialFileName { get; set; } = string.Empty;
    }
}
