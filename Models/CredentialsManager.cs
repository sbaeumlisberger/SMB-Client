using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using Windows.Security.Credentials;

namespace SMBClient.Models
{
    public class CredentialsManager
    {

        private readonly PasswordVault vault = new PasswordVault();

        public Credential? Retrieve(string server)
        {
            try
            {
                PasswordCredential passwordCredential = vault.FindAllByResource(server).First();
                passwordCredential = vault.Retrieve(server, passwordCredential.UserName);
                return new Credential(passwordCredential.UserName, passwordCredential.Password);
            }
            catch
            {
                return null;
            }
        }

        internal void Save(string server, Credential credential)
        {
            vault.Add(new PasswordCredential(server, credential.Username, credential.Password));
        }
    }
}
