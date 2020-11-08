using System;
using System.Collections.Generic;
using System.Text;

namespace SMBClient.Models
{
    public struct Credential
    {
        public string Username { get; }

        public string Password { get; }

        public Credential(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}
