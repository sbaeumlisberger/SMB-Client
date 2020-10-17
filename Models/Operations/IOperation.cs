using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SMBClient.Models
{
    public interface IOperation
    {
        long ProgressAmount { get; }

        void Execute(SMBFileShare smbFileShare, Progress progress);
    }
}
