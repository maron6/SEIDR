using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.ThreadManaging
{
    public sealed class LockHelper<RT> : LockHelper
    {
        public LockHelper(Lock lockLevel, int exclusiveTimeOut = 0, int ExclusiveIntentTimeout = 0, string alias = null)
            : base(lockLevel, 
                  CheckTarget(alias), 
                  ExclusiveTimeout:exclusiveTimeOut,
                  ExclusiveIntentTimeout: ExclusiveIntentTimeout)
        {            
        }
        /// <summary>
        /// Creates a simple alias that should be unique to the type and alias combination. Used for lock targets
        /// </summary>
        /// <param name="alias"></param>
        /// <returns></returns>
        public static string CheckTarget(string alias)
        {            
            return typeof(RT).AssemblyQualifiedName + $"." + (string.IsNullOrWhiteSpace(alias) ? string.Empty : alias);
        }
    }
    /// <summary>
    /// Helper for using <see cref="LockManager"/> class
    /// </summary>
    public class LockHelper: IDisposable
    {
        /// <summary>
        /// Target of the underlying LockManager
        /// </summary>
        public string Target => mgr.Target;
        /// <summary>        
        /// Helper for using <see cref="LockManager"/> class
        /// </summary>
        /// <param name="manager">Manager whose lock level is managed. Should be unlocked when creating the helper.</param>
        /// <param name="lockLevel">Target lock level for the helper to acquire.</param>
        public LockHelper(LockManager manager, Lock lockLevel)
            : this(lockLevel, manager)
        { 
            //Allow the original parameter order
        }
        /// <summary>        
        /// Helper for using <see cref="LockManager"/> class
        /// </summary>
        /// <param name="manager">Manager whose lock level is managed. Should be unlocked when creating the helper.</param>
        /// <param name="lockLevel">Target lock level for the helper to acquire.</param>
        public LockHelper(Lock lockLevel, LockManager manager)
        {
            if (lockLevel == Lock.Unlocked)
                throw new ArgumentOutOfRangeException("lockLevel", "Lock is below Locking Boundary");
            mgr = manager;
            if (mgr.HasLock)
                throw new ArgumentException("The LockManager has already obtained a lock", "manager");
            if (!mgr.Acquire(lockLevel))
                throw new LockManagerException("Unable to acquire lock");
        }
        /// <summary>
        /// Helper for using <see cref="LockManager"/> class
        /// </summary>
        /// <param name="lockLevel"></param>
        /// <param name="target"></param>
        /// <param name="ExclusiveTimeout"></param>
        /// <param name="ExclusiveIntentTimeout"></param>
        public LockHelper(Lock lockLevel, string target, int ExclusiveTimeout = 0, int ExclusiveIntentTimeout = 0)
        {
            if(lockLevel == Lock.Unlocked)
            {
                throw new ArgumentOutOfRangeException(nameof(lockLevel), "Lock is below locking boundary");
            }
            else if (lockLevel == Lock.Exclusive_Intent)
            {
                throw new ArgumentException(nameof(lockLevel), "Lock Level Exclusive Intent is not valid if a LockManager is not passed to the helper");
            }
            mgr = new LockManager(target)
            {
                ExclusiveLockTimeout = ExclusiveTimeout,
                ExclusiveIntentAcquisitionTimeout = ExclusiveIntentTimeout
            };            
            if (!mgr.Acquire(lockLevel))
                throw new LockManagerException("Unable to acquire lock");

        }
        /// <summary>
        /// Check if the lock level is current <see cref="Lock.Exclusive"/>, and if that exclusive has been acquired on this thread.
        /// </summary>
        public bool IsExclusiveSynched
        {
            get { return mgr.CheckExclusiveSynched(); }
        }
        /// <summary>
        /// Waits up to <paramref name="timeout"/> milliseconds to acquire and release a lock. 
        /// <para>Returns false if timeout was reached.</para>
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns>True if timeout was not used.</returns>
        public bool Wait(int timeout) => mgr.Wait(timeout);
        /// <summary>
        /// Attempts to transition the lock to another lock level. Returns true if the lock is at the target level at the end of the call.
        /// <para>Note: If there's no timeout, this will always either return true or throw an exception.</para>
        /// </summary>
        /// <param name="transitionTarget"></param>
        /// <returns></returns>
        public bool Transition(Lock transitionTarget) => mgr.TransitionLock(transitionTarget);
        /// <summary>
        /// Sets approximate timeout for acquiring an exclusive lock, in seconds.
        /// </summary>
        public int ExlusiveLockTimeout
        {
            get
            {
                return mgr.ExclusiveLockTimeout;
            }
            set
            {
                mgr.ExclusiveLockTimeout = value;
            }
        }
        /// <summary>
        /// Sets deadline for acquiring exclusive intent, in seconds.
        /// </summary>
        public int ExclusiveIntentAcquisitionTimeout
        {
            get { return mgr.ExclusiveIntentAcquisitionTimeout; }
            set { mgr.ExclusiveIntentAcquisitionTimeout = value; }
        }
        /// <summary>
        /// Attempts to release any held lock.
        /// </summary>
        public void Release()
        {
            if (mgr.HasLock)
                mgr.Release();
        }
        /// <summary>
        /// Gets the lock level of the underlying LockManager
        /// </summary>
        public Lock LevelLevel => mgr.LockLevel;
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
                    if (mgr.LockLevel > Lock.Unlocked)
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
