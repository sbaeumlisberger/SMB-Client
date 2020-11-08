using System;
using System.Collections.Generic;
using System.Text;

namespace SMBClient.ViewModels
{
    public class LoginDialogModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool SaveCredentials { get; set; } = false;
        public bool Canceled { get; set; } = true;
    }
}
