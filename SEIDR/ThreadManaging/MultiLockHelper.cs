using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.ThreadManaging
{
    /// <summary>
    /// Aid for helping with multiple locks concurrently, when the individual locks are managed by <see cref="LockHelper{RT}"/> instances.
    /// </summary>
    /// <typeparam name="RT"></typeparam>
    public sealed class MultiLockHelper<RT> : MultiLockHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetAliasList">A list of aliases used in combination with the TypeName for determining target. Used with <see cref="LockHelper{RT}.CheckTarget(string)"/></param>
        /// <param name="lockTarget"></param>
        /// <param name="exclusiveTimeout"></param>
        /// <param name="ExclusiveIntentTimeout"></param>
        public MultiLockHelper(IEnumerable<string> targetAliasList,
            Lock lockTarget = Lock.NoLock, int exclusiveTimeout = 0, int ExclusiveIntentTimeout = 0)
            :base(
                 lockTarget,
                 targetAliasList.TransformEach(a => LockHelper<RT>.CheckTarget(a)),
                 exclusiveTimeout: exclusiveTimeout, 
                 ExclusiveIntentTimeout: ExclusiveIntentTimeout                 
                 )
            {
            }
    }
    /// <summary>
    /// Aid for helping with multiple locks concurrently. 
    /// <para>E.g., on a main thread which manages a list of child threads and needs to lock for a target from each thread
    /// </para>
    /// </summary>
    public class MultiLockHelper: IDisposable
    {
        List<LockHelper> lockList = new List<LockHelper>();
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Helper wrapper for concurrently managing multiple locks that should share target access level.
        /// <para>Note: will throw an argument exception if targets are not unique</para>
        /// <para>Will intialize as NoLock (effectively unlocked).</para>
        /// </summary>
        /// <param name="targetList"></param>
        /// <param name="exclusiveTimeout"></param>
        /// <param name="ExclusiveIntentTimeout"></param>
        public MultiLockHelper(IEnumerable<string> targetList, int exclusiveTimeout = 0, int ExclusiveIntentTimeout = 0)
        {
            foreach (var t in targetList)
            {
                if (lockList.HasMinimumCount(l => l.Target == t, 1))
                    throw new ArgumentException("Duplicate targets", nameof(targetList));
                lockList.Add(new LockHelper(Lock.NoLock, t, exclusiveTimeout, ExclusiveIntentTimeout));
            }
        }
        /// <summary>
        /// Helper wrapper for concurrently managing multiple locks that should share target access level.
        /// <para>Note: will throw an argument exception if targets are not unique</para>
        /// </summary>
        /// <param name="targetLock"></param>
        /// <param name="targetList"></param>
        /// <param name="exclusiveTimeout"></param>
        /// <param name="ExclusiveIntentTimeout"></param>
        public MultiLockHelper(Lock targetLock, IEnumerable<string> targetList, int exclusiveTimeout = 0, int ExclusiveIntentTimeout = 0)
        {
            foreach (var t in targetList)
            {
                if(lockList.HasMinimumCount(l => l.Target == t, 1))                
                    throw new ArgumentException("Duplicate targets", nameof(targetList));
                
                lockList.Add(new LockHelper(targetLock, t, exclusiveTimeout, ExclusiveIntentTimeout));
            }
        }
        const string DISPOSED_MESSAGE = "MultiLock Helper instance has already been disposed.";
        /// <summary>
        /// Get the lock helper for the specified target
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public LockHelper this[string target]
        {
            get
            {
                if (disposedValue)
                    throw new InvalidOperationException(DISPOSED_MESSAGE);
                return lockList.FirstOrDefault(l => l.Target == target);
            }
        }
        public LockHelper this[int index]
        {
            get
            {
                if (disposedValue)
                    throw new InvalidOperationException(DISPOSED_MESSAGE);
                return lockList[index];
            }
        }
        /// <summary>
        /// Updates the timeLimit for acquiring <see cref="Lock.Exclusive"/>. Set to 0 to remove the limit
        /// </summary>
        /// <param name="newTimeout"></param>
        public void SetExclusiveTimeout(int newTimeout)
        {
            if (disposedValue)
                throw new InvalidOperationException(DISPOSED_MESSAGE);
            lockList.ForEach(l => l.ExlusiveLockTimeout = newTimeout);
        }
        /// <summary>
        /// Updates the timeLimit for acquiring <see cref="Lock.Exclusive_Intent"/>. Set to 0 to remove the limit
        /// </summary>
        /// <param name="newTimeout"></param>
        public void SetExclusiveIntentTimeout(int newTimeout)
        {
            if (disposedValue)
                throw new InvalidOperationException(DISPOSED_MESSAGE);
            lockList.ForEach(l => l.ExclusiveIntentAcquisitionTimeout = newTimeout);
        }
        /// <summary>
        /// Attempts to transition all locks.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="skipFail"></param>
        /// <returns>True if all locks were able to transition. False if any did not reach the indicated target level.</returns>
        public bool Transition(Lock target, bool skipFail = false)
        {
            if (disposedValue)
                throw new InvalidOperationException(DISPOSED_MESSAGE);
            bool ret = true;
            foreach(var l in lockList)
            {
                if (!l.Transition(target))
                {
                    if(skipFail)
                        return false;
                    ret = false;
                }
            }
            return ret;
        }
        /// <summary>
        /// Release all lockHelpers
        /// </summary>
        public void Release()
        {            
            //Should already be released, but should be safe to call again anyway.
            lockList.ForEach(l => l.Release());
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }
                lockList.ForEach(l => l.Dispose());
                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MultiLockHelper() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

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
