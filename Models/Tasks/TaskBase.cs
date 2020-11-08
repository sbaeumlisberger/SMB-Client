using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SMBClient.Models
{
    public abstract class TaskBase
    {
        protected readonly SMBFileShare smbFileShare;

        private readonly List<IOperation> operations = new List<IOperation>();

        private long progressTarget = 0;

        protected TaskBase(SMBFileShare smbFileShare) 
        {
            this.smbFileShare = smbFileShare;
        }

        public async Task ExecuteAsync(Progress progress)
        {
            await Task.Run(() =>
            {
                CreateOperations();
                progress.Initialize(progressTarget);
                foreach (var operation in operations)
                {
                    operation.Execute(smbFileShare, progress);
                }
            });
        }

        protected void AddOperation(IOperation operation)
        {
            operations.Add(operation);
            progressTarget += operation.ProgressAmount;
        }

        protected abstract void CreateOperations();
    }
}
