namespace SEIDR.ThreadManaging
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Creates an object for managing different levels of locking.
    /// <para>
    /// This is only useful for multi threading and each thread really needs to have its own lock manager(s).
    /// </para>
    /// <para>Instances should NOT be created as static variables or shared across threads.</para>
    /// </summary>
    [Obsolete("Better to use the LockManager class.")]
    public sealed class TimedLockManager : IDisposable
    {
        /// <summary>
        /// A lock below this value is considered 'Share' 
        /// <para>
        /// A lock at or above the value is considered exclusive
        /// </para>
        /// </summary>
        public const Lock ShareBoundary = Lock.Exclusive_Intent; //4;
        /// <summary>
        /// A lock below this value is considered 'unlocked' and should mainly be used for consistency or if the lock might be changed.
        /// <para>
        /// E.g. if the lock is in a variable, and the level of locking depends on other factors.
        /// </para>
        /// </summary>
        public const Lock LockBoundary = Lock.Shared; //0;
        /// <summary>
        /// The identifier for the default lock manager target
        /// </summary>
        public const string DefaultTarget = "DEFAULT";
        /// <summary>
        /// Check if this LockManager is holding any lock 
        /// </summary>
        public bool HasLock
        {
            get { return _MyLock >= LockBoundary; }
        }
        volatile int acquiring = 0;
        static object _lock; //For locking manager settings        
        Lock _MyLock;
        //static SortedList<Lock, int> lockList ;
        static uint _IDCounter;
        /// <summary>
        /// A unique ID for your LockManager object
        /// </summary>
        public readonly uint LockID;
        static volatile List<uint> reclaimedLockIDList;
        //Note: logic using the dictionaries are not atomic, so still need to use locking
        static ConcurrentDictionary<string, uint?> _IntentHolder;
        static ConcurrentDictionary<string, uint?> _ExclusiveHolder; //Up to one exclusive at a time. Many shares
        static ConcurrentDictionary<string, uint> _ShareCount; //For checking the number of shares on a lock target at a time
        static ConcurrentDictionary<string, object> _LockTargets; //For actual lock management
        static ConcurrentDictionary<string, DateTime> _IntentExpiration;
        static TimedLockManager()
        {
            //_IntentFlag = false;
            //_ExclusiveHolder = null;
            _IntentHolder = new ConcurrentDictionary<string, uint?>();
            _ExclusiveHolder = new ConcurrentDictionary<string, uint?>();
            _ShareCount = new ConcurrentDictionary<string, uint>();
            _LockTargets = new ConcurrentDictionary<string, object>();
            _IntentExpiration = new ConcurrentDictionary<string, DateTime>();
            _lock = new object();
            reclaimedLockIDList = new List<uint>();
            //lockList = new SortedList<Lock, int>();
            //Use a loop in case more lock types are considered later on.
            //foreach (Lock l in Enum.GetValues(typeof(Lock)))
            //{
            //    if (l == Lock.Unlocked || l == Lock.NoLock)
            //        continue;
            //    lockList.Add(l, 0);
            //}
            _IDCounter = 0;
            /*
            _LockTargets.Add(DefaultTarget, new object());
            _IntentHolder.Add(DefaultTarget, null);
            _ExclusiveHolder.Add(DefaultTarget, null);
            _ShareCount.Add(DefaultTarget, 0);*/
        }
        /// <summary>
        /// Allows you to check the current lock level of your LockManager's target.
        /// <para>
        /// Could potentially be used in a loop if a thread is really low priority 
        /// </para><para>
        /// EX: while( LockManager.currentLockLevel >Lock.Unlocked) or something
        /// </para>
        /// </summary>
        public Lock currentTargetLockLevel
        {
            get
            {
                if (_ShareCount[_myTarget] > 0)
                    return Lock.Shared;
                if (_IntentHolder[_myTarget] != null)
                {
                    if (_ExclusiveHolder[_myTarget] != null)
                        return Lock.Exclusive;
                    return Lock.Exclusive_Intent;
                }
                return Lock.Unlocked;
            }
        }
        /// <summary>
        /// Gets the value of this LockManager's lock.
        /// <para>
        /// If setting, will also try to acquire the lock, unless the new value is unlocked. 
        /// </para><para>
        /// If the new value is unlocked, nothing will be done. (Call Release to unlock)
        /// </para><remarks>
        /// Setting the value will also do nothing if the LockManageralready holds a lock.
        /// </remarks>
        /// </summary>
        public Lock MyLock
        {
            get
            {
                return _MyLock;
            }
            set
            {
                if ((_MyLock > Lock.Unlocked || value == Lock.Unlocked)
                    && !(_MyLock == Lock.Exclusive_Intent && value == Lock.Exclusive)) //Exclusive_Intent -> Exclusive is allowed
                    return;
                Acquire(value);
            }
        }
        /*
         * TODO: Consider allowing target to be a parameter instead of a field? 
         * Maybe static method for add Target, 
         * and throw exception if target doesn't exist in acquire lock
        */
        string _myTarget;
        /// <summary>
        /// Readonly lock target.
        /// </summary>
        public string Target
        {
            get
            {
                return _myTarget;
            }
        }        
        /// <summary>
        /// Approximate # of maximum seconds that the lockmanager can take to try to acquire a lock
        /// </summary>
        public uint TimeOut { get; set; }
        /// <summary>
        /// Constructor. Note: This does not overlap with locking managed by <see cref="LockManager"/>
        /// <para>Used for sharing or getting exclusive locks on sections based on other sections using the same target.</para>
        /// </summary>
        /// <param name="TARGET">Case insensitive</param>
        /// <param name="timeout">If > 0, will throw an error if the lock has to wait that many seconds without obtaining target lock level</param>
        public TimedLockManager(string TARGET = DefaultTarget, uint timeout = 0)
        {
            if (string.IsNullOrWhiteSpace(TARGET))
                throw new ArgumentNullException(nameof(TARGET));
            _MyLock = Lock.Unlocked;
            TimeOut = timeout;
            lock (_lock)
            {
                if (reclaimedLockIDList.HasMinimumCount(1))
                {
                    LockID = reclaimedLockIDList[0];
                    reclaimedLockIDList.RemoveAt(0);
                }
                else
                    LockID = ++_IDCounter;


                _myTarget = TARGET.ToUpper();
                if (!_LockTargets.ContainsKey(_myTarget))
                {
                    _LockTargets.TryAdd(_myTarget, new object());
                    _IntentHolder.TryAdd(_myTarget, null);
                    _ExclusiveHolder.TryAdd(_myTarget, null);
                    _ShareCount.TryAdd(_myTarget, 0);
                }
            }
        }
        /// <summary>
        /// Release your lock. Useful even with nolock in the event that you MIGHT need to change to using a lock later on.
        /// <para> 
        /// Also useful even with no lock if you're using a variable to decide the lock level based on other factors
        /// </para>
        /// <remarks>
        /// Will throw an exception if you try to release an already unlocked LockManager.
        /// </remarks>
        /// </summary>
        public void Release() => Release(false);
        private void Release(bool safeMode)
        {
            if (!safeMode)
            {
                if (disposedValue)
                    throw new InvalidOperationException("LockManager has been disposed already.");
                else if (_MyLock == Lock.Unlocked)
                    throw new LockManagerException("Tried to release a lock but no lock exists.");
            }
            if (_MyLock == Lock.Unlocked)
            {
                return;
            }
            //Only needed if this can affect the calling method. I would imagine it can as part of compiling code, but would need research to confirm..
            Thread.MemoryBarrier();

            //if ((int)_MyLock < LockBoundary)
            //{
            //    //Set to unlocked in case that's checked for anything
            //    _MyLock = Lock.Unlocked;
            //    return; //Mainly so that people can use the same code and
            //}
            var target = _LockTargets[_myTarget];
            if (_MyLock >= LockBoundary)
            {
                if (_MyLock < ShareBoundary)
                {
                    lock (target)
                    {
                        _ShareCount[_myTarget]--;
                        Monitor.Pulse(target);
                        //pulsing here is for exclusives waiting for sharecount to go to 0. 
                        //Only need to signal to one on this target object -- if shareCount > 0, 
                        //there should not be any instances waiting for the share level, so we shouldn't need a pulse all
                    }
                }
                if (_MyLock >= ShareBoundary)
                {
                    DateTime d;
                    lock (target)
                    {
                        if (_ExclusiveHolder[_myTarget] == LockID)
                            _ExclusiveHolder[_myTarget] = null;
                        if (_IntentExpiration.ContainsKey(_myTarget))
                            _IntentExpiration.TryRemove(_myTarget, out d);
                        if (_IntentHolder[_myTarget] == LockID)
                            _IntentHolder[_myTarget] = null;
                        Monitor.PulseAll(target);
                    }
                }
            }
            _MyLock = Lock.Unlocked;
            return;
            //int value = (int)_MyLock;
            //lockList[_MyLock]--;
            //_MyLock = Lock.Unlocked;
            //if (value < _LockValue)
            //{
            //    return;//don't need to reset the current lock if the current lock is lower
            //}
            //Lock nextLock = Lock.Unlocked;
            //int count = lockList[nextLock];
            //foreach (var kv in lockList)
            //{
            //    if (kv.Value > count)
            //    {
            //        count = kv.Value;
            //        nextLock = kv.Key;
            //    }
            //}
            //_LockValue = (int)nextLock;

        }
        public void Wait()
        {            
            lock (_LockTargets[_myTarget]) { }            
        }
        /// <summary>
        /// Maximum number of seconds that exclusive intent can be maintained without grabbing the actual exclusive lock. (5 minutes)        
        /// </summary>
        public const int ExclusiveIntentExpirationTime = 300;
        /// <summary>
        /// Try to acquire a lock.
        /// <para>
        /// An exception will be thrown if you try to acquire a lock on a lock manager that already has a lock.
        /// </para>
        /// <remarks>
        /// If you try to acquire Lock.Unlocked, this will simply Release any lock the lock manager might have.
        /// </remarks>
        /// </summary>
        /// <param name="level"></param>
        public void Acquire(Lock level)
        {

            #region Validation
            if (disposedValue)
            {
                throw new InvalidOperationException("LockManager has been disposed already.");
            }
            if (level == Lock.Unlocked)
            {
                Release();
                return;
            }
            if (_MyLock >= LockBoundary
                && (_MyLock != Lock.Exclusive_Intent || level < Lock.Exclusive)) //ExclusiveIntent is allowed to call again for acquiring Exclusive.
            {
                throw new LockManagerException("Tried to Acquire a new lock on a manager that already holds a lock.");
            }
            if (Interlocked.Exchange(ref acquiring, 1) == 1)
            {
                throw new LockManagerException("Tried to Acquire a new lock, but the manager is already trying to acquire a lock.");
            }
            #endregion
            Thread.MemoryBarrier(); //Only needed if this can affect the calling method. I would imagine it can as part of compiling code, but would need research to confirm..
            //_MyLock = level;
            //bool matched = false;
            DateTime start = DateTime.Now;
            const int LOCK_WAIT = 500;
            const int MONITOR_WAIT_TIMEOUT = 60_000;
            object target = _LockTargets[_myTarget];

            #region NOLOCK
            if (level < LockBoundary)
            {
                _MyLock = Lock.NoLock;
                //Already know that _MyLock is < LockBoundary or an exception would have been thrown             
                return;

                //NoLock doesn't need to ACTUALLY register or do anything. 
                //Still have the memory barrier from above, which might help by inserting a memory barrier into the compiled code..Would also need to research to confirm

                //This is mainly handled so that a Lock can be variable or in case the level of locking might be changed
                //later.
            }
            #endregion
            #region SHARE LOCKING
            if (level < ShareBoundary)
            {
                //Note that this is skipped for Exclusive/Exclusive intent (level is above ShareBoundary)
                while (true)
                {
                    bool waitForIntent = false;
                    lock (target)
                    {
                        if (_IntentHolder[_myTarget] != null)
                        {
                            waitForIntent = true;
                            /*
                             * If there's an intent holder, wait a bit in case they're going to get an exclusive hold
                             * or in case it already has exclusive and is almost done working.
                             */
                        }
                    }
                    if (acquiring < 0)
                        throw new LockManagerException("Attempting to acquire lock, but LockManager Dispose is being called");
                    if (waitForIntent)
                        Thread.Sleep(LOCK_WAIT);
                    lock (target)
                    {
                        //ToDo: Remove exclusiveHolder check, just enter a semaphore, set _MyLock and increment share count
                        //If no exclusive holder, add to sharecount, even if there is a lock manager holding intent (_IntentHolder[_myTarget])
                        if (_ExclusiveHolder[_myTarget] == null)
                        {
                            _ShareCount[_myTarget]++;
                            _MyLock = Lock.Shared;
                            if (Interlocked.Exchange(ref acquiring, 0) < 0)
                            {
                                Release(true);
                                throw new LockManagerException("Attempting to acquire lock, but LockManager Dispose is being called");
                            }
                            return;
                        }
                        if (TimeOut == 0)
                            Monitor.Wait(target, MONITOR_WAIT_TIMEOUT);
                    }
                    if(TimeOut > 0)
                    {
                        Thread.Sleep(LOCK_WAIT); //replace with wait when TimeOut == 0
                        if (start.AddSeconds(TimeOut) > DateTime.Now)
                        {
                            if (Interlocked.Exchange(ref acquiring, 0) < 0) //So that we can try again later if the exception is caught, without needing to dispose the manager.
                                throw new LockManagerException("Attempting to acquire lock, but LockManager Dispose is being called");
                            throw new TimeoutException("Acquiring lock - " + DateTime.Now.Subtract(start).TotalSeconds);
                        }
                    }
                    if (acquiring < 0)
                        throw new LockManagerException("Attempting to acquire lock, but LockManager Dispose is being called");
                }
            }
            #endregion
            #region EXCLUSIVE LOCKING
            #region INTENT
            lock (target)
            {
                if (_IntentHolder[_myTarget] == LockID && _IntentExpiration.ContainsKey(_myTarget))
                {
                    DateTime d;
                    //If we still have the intent holder and it's set to expire, remove the expiration before the loop's test.
                    _IntentExpiration.TryRemove(_myTarget, out d);
                }

            }
            //Get access to the intent flag for the target
            while (true)//_IntentHolder[_myTarget] != LockID)
            {
                DateTime exp;
                lock (target)
                {
                    if (_IntentHolder[_myTarget] == LockID)
                        break;
                    if (_IntentExpiration.TryGetValue(_myTarget, out exp))
                    {
                        if (exp < DateTime.Now)
                        {
                            _IntentHolder[_myTarget] = null;
                            _IntentExpiration.TryRemove(_myTarget, out exp);
                            //Remove intent. Note that expiration is only set when grabbing exclusive intent but not exclusive.
                            //Removed when grabbing exclusive                            
                        }
                    }
                    if (_IntentHolder[_myTarget] == null)
                    {
                        _IntentHolder[_myTarget] = LockID;
                        //matched = true;
                        _MyLock = Lock.Exclusive_Intent;
                        if (level < Lock.Exclusive)
                        {
                            _IntentExpiration[_myTarget] = DateTime.Now.AddSeconds(ExclusiveIntentExpirationTime);
                            if (Interlocked.Exchange(ref acquiring, 0) < 0)
                            {
                                Release(true);
                                throw new LockManagerException("Attempting to acquire lock, but LockManager Dispose is being called");
                            }
                            return; //Stopped after getting the intent. Don't actually have access yet, just have it reserved
                        }
                        break;
                    }
                    if(TimeOut ==0)
                        Monitor.Wait(target, MONITOR_WAIT_TIMEOUT);
                }
                if (acquiring < 0)
                    throw new LockManagerException("Attempting to acquire lock, but LockManager Dispose is being called");
                Thread.Sleep(LOCK_WAIT);
                if (TimeOut > 0 && start.AddSeconds(TimeOut) > DateTime.Now)
                {
                    if (Interlocked.Exchange(ref acquiring, 0) < 0) //So that we can try again later if the exception is caught, without needing to dispose the manager.
                        throw new LockManagerException("Attempting to acquire lock, but LockManager Dispose is being called");
                    throw new TimeoutException("Acquiring lock - " + DateTime.Now.Subtract(start).TotalSeconds);
                }

            }
            #endregion            
            //An intent lock will wait until the share locks are at 0 before trying to acquire exclusive lock
            //while ((int) level == ShareBoundary && _ShareCount[_myTarget] > 0) { } //#Update: Handle this in the exclusive loop's condition check
            while (true)//LockID != _ExclusiveHolder[_myTarget])
            {
                lock (target)
                {
                    if (_ExclusiveHolder[_myTarget] == null && _ShareCount[_myTarget] == 0)
                    {
                        _ExclusiveHolder[_myTarget] = LockID;
                        _MyLock = Lock.Exclusive;
                        if (Interlocked.Exchange(ref acquiring, 0) < 0)
                        {
                            Release(true);
                            throw new LockManagerException("Attempting to acquire lock, but LockManager Dispose is being called");
                        }
                        return;
                    }
                    if (TimeOut == 0)
                        Monitor.Wait(target, MONITOR_WAIT_TIMEOUT);
                }
                if(TimeOut > 0)
                {
                    Thread.Sleep(LOCK_WAIT); //if timeout == 0, use monitor wait instead of sleep
                    if (start.AddSeconds(TimeOut) > DateTime.Now)
                    {
                        if (Interlocked.Exchange(ref acquiring, 0) < 0) //So that we can try again later if the exception is caught, without needing to dispose the manager.
                            throw new LockManagerException("Attempting to acquire lock, but LockManager Dispose is being called");
                        throw new TimeoutException("Acquiring lock - " + DateTime.Now.Subtract(start).TotalSeconds);
                    }
                }
                if (acquiring < 0)
                    throw new LockManagerException("Attempting to acquire lock, but LockManager Dispose is being called");
            }
            #endregion            
        }

        #region IDisposable Support
        volatile private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {

                }
                Interlocked.Exchange(ref acquiring, -1);
                Release(true);

                lock (_lock)
                {
                    reclaimedLockIDList.Add(LockID);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }
        
        ~TimedLockManager()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }

}
