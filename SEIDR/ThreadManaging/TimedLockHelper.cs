using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.ThreadManaging
{
    /// <summary>
    /// Helper for using <see cref="LockManager"/> class
    /// </summary>
    public class TimedLockHelper : IDisposable
    {
        /// <summary>        
        /// Helper for using <see cref="LockManager"/> class
        /// </summary>
        /// <param name="manager">Manager whose lock level is managed. Should be unlocked when creating the helper.</param>
        /// <param name="lockLevel">Target lock level for the helper to acquire.</param>
        public TimedLockHelper(TimedLockManager manager, Lock lockLevel = Lock.Shared)
        {
            if (lockLevel == Lock.Unlocked)
                throw new ArgumentOutOfRangeException("lockLevel", "Lock is below Locking Boundary");
            mgr = manager;
            if (mgr.HasLock)
                throw new ArgumentException("The LockManager has already obtained a lock", "manager");
            mgr.Acquire(lockLevel);
        }
        /// <summary>
        /// Helper for using <see cref="LockManager"/> class
        /// </summary>
        /// <param name="lockLevel"></param>
        /// <param name="target"></param>
        /// <param name="timeout">Set to a value > 0 to limit how long it can take to acquire the lock.</param>
        public TimedLockHelper(Lock lockLevel, string target = "DEFAULT", uint timeout = 0)
        {
            if (lockLevel == Lock.Unlocked)
            {
                throw new ArgumentOutOfRangeException(nameof(lockLevel), "Lock is below locking boundary");
            }
            else if (lockLevel == Lock.Exclusive_Intent)
            {
                throw new ArgumentException(nameof(lockLevel), "Lock Level Exclusive Intent is not valid if a LockManager is not passed to the helper");
            }
            mgr = new TimedLockManager(target, timeout);
            mgr.Acquire(lockLevel);

        }
        TimedLockManager mgr;
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
