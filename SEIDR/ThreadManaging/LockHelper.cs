using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.ThreadManaging
{
    public class LockHelper: IDisposable
    {
        public LockHelper(LockManager manager, Lock lockLevel = Lock.Shared)
        {
            if (lockLevel == Lock.Unlocked)
                throw new ArgumentOutOfRangeException("lockLevel", "Lock is below Locking Boundary");
            mgr = manager;
            if (mgr.HasLock)
                throw new ArgumentException("The LockManager has already obtained a lock", "manager");
            mgr.Acquire(lockLevel);
        }
        LockManager mgr;
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (mgr.MyLock > Lock.Unlocked)
                        mgr.Release();
                }
                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        //~LockHelper()
        //{
        //    // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //    Dispose(false);
        //}

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
