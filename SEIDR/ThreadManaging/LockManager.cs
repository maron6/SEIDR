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
    public sealed class LockManager: IDisposable
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
        volatile Lock _MyLock;
        //static SortedList<Lock, int> lockList ;
        /// <summary>
        /// Identifier for Lock Management
        /// </summary>
        public int LockManagementID { get; private set; }
        
        //Note: logic using the dictionaries are not atomic, so still need to use locking
        static ConcurrentDictionary<string, int?> _IntentHolder;        
        static ConcurrentDictionary<string, uint> _ShareCount; //For checking the number of shares on a lock target at a time
        static ConcurrentDictionary<string, object> _LockTargets; //For actual lock management        
        static ConcurrentDictionary<string, object> _IntentTargets; //Intent lock management.
        static ConcurrentDictionary<string, DateTime> _IntentExpiration;
        static LockManager()
        {
            //_IntentFlag = false;
            //_ExclusiveHolder = null;
            _IntentHolder = new ConcurrentDictionary<string, int?>();            
            _ShareCount = new ConcurrentDictionary<string, uint>();
            _LockTargets = new ConcurrentDictionary<string, object>();
            _IntentTargets = new ConcurrentDictionary<string, object>();
            _IntentExpiration = new ConcurrentDictionary<string, DateTime>();
            _lock = new object();                        
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
        public Lock LockLevel
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
        /// <summary>
        /// Timeout for acquiring an exclusive lock
        /// </summary>
        public int ExclusiveLockTimeout { get; set; } = -1;
        /// <summary>
        /// Sets an approximate deadline for acquiring ExclusiveIntent.
        /// </summary>
        public int ExclusiveIntentAcquisitionTimeout { get; set; } = -1;
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
        /// Constructor. Note: This does  not overlap with locking by the <see cref="TimedLockManager"/>
        /// <para>Used for sharing or getting exclusive locks on sections based on other sections using the same target.</para>
        /// </summary>
        /// <param name="TARGET">Case insensitive</param>        
        public LockManager(string TARGET = DefaultTarget)
        {
            if (string.IsNullOrWhiteSpace(TARGET))
                throw new ArgumentNullException(nameof(TARGET));
            _MyLock = Lock.Unlocked;
            lock (_lock)
            {                
                LockManagementID = Thread.CurrentThread.ManagedThreadId; //Replace reclaimedLockIDList.
                //Set LockID again whenever acquiring. If doing transition or acquire and we have a lock, should throw an exception or wait if not matched.
                //potential issues with tracking mutliple lock managers on same thread with same target? I think it should be okay.

                _myTarget = TARGET.ToUpper();
                if (!_LockTargets.ContainsKey(_myTarget))
                {
                    _LockTargets.TryAdd(_myTarget, new object());
                    _IntentTargets.TryAdd(_myTarget, new object());
                    _IntentHolder.TryAdd(_myTarget, null);                    
                    _ShareCount.TryAdd(_myTarget, 0);
                }
            }
        }
        

        #region lock release

        /// <summary>
        /// Release your lock. Useful even with nolock in the event that you MIGHT need to change to using a lock later on.
        /// <para> 
        /// Also useful even with no lock if you're using a variable to decide the lock level based on other factors
        /// </para>
        /// <remarks>
        /// Will throw an exception if you try to release an already unlocked LockManager.
        /// </remarks>
        /// </summary>
        public void Release()
        {

            object target;
            object intent;
            lock (_lock)
            {
                target = _LockTargets[_myTarget];
                intent = _IntentTargets[_myTarget];
            }

            if (disposedValue)
                throw new InvalidOperationException("LockManager has been disposed already.");
            else if (_MyLock == Lock.Unlocked)
                throw new LockManagerException("Tried to release a lock but no lock exists.");
            else
            {
                int calling = Thread.CurrentThread.ManagedThreadId;
                if (LockManagementID != calling)
                {
                    throw new LockManagerSynchronizationException(calling, LockManagementID);
                }
                if (_MyLock == Lock.Exclusive && !Monitor.IsEntered(target))
                    throw new SynchronizationLockException("Tried to release a lock from a different thread from what acquired the lock.");
                //second if shouldn't really be a concern after adding first one...
            }
            release(target, intent);
        }
        private void release()
        {
            object target;
            object intent;
            lock (_lock)
            {
                target = _LockTargets[_myTarget];
                intent = _IntentTargets[_myTarget];
            }
            release(target, intent);
            //Only needed if this can affect the calling method. I would imagine it can as part of compiling code, but would need research to confirm..
            Thread.MemoryBarrier();
        }
        private void exclusiveRelease(object target, object intent)
        {            
            DateTime d;
            try
            {
                if(Monitor.IsEntered(target))
                    Monitor.PulseAll(target); //exclusive, pulse all. Note: Needs to be done while we have the lock.
                while (Monitor.IsEntered(target))
                {
                    //This can end up entered more times than expected for some reason, but that shouldn't hurt since it's the same Thread                    
                    Monitor.Exit(target);
                }
                System.Diagnostics.Debug.WriteLine(DebugName + " - " + _MyLock.ToString() + " Lock released for target '" + _myTarget + "'.");
            }
            finally
            {
                lock (intent)
                {
                    if (_IntentHolder[_myTarget] == LockManagementID)
                    {
                        if (_IntentExpiration.ContainsKey(_myTarget))
                            _IntentExpiration.TryRemove(_myTarget, out d);
                        _IntentHolder[_myTarget] = null;   
                        Monitor.PulseAll(intent); //intent changed, pulse all                     
                    }
                }
            }
        }
        private void release( object target,  object intent)
        {
            if (_MyLock < LockBoundary)
            {
                _MyLock = Lock.Unlocked;
                return;
            }

            if (_MyLock < ShareBoundary)
            {
                lock (target)
                {
                    uint shares = --_ShareCount[_myTarget];
                    System.Diagnostics.Debug.WriteLine(DebugName + " - Share released for target '" + _myTarget + "'. Remaining sharecount: " + shares);
                    Monitor.PulseAll(target);
                    //pulsing here is for exclusives waiting for sharecount to go to 0. 
                    //Need to pulse all in case there are exclusive intents trying to check for access to the exclusive flag, though                    
                }
            }                
            else if (_MyLock >= ShareBoundary)
            {
                //Wouldn't want to move the lock forward early, but should be okay to move it down to Unlocked a little early here.                
                exclusiveRelease(target, intent);                
            }
            _MyLock = Lock.Unlocked;            
        }
        #endregion

        /// <summary>
        /// Block until no other threads are trying to acquire locks.
        /// </summary>
        public void Wait()
        {

            object target;
            object intent;
            lock (_lock)
            {
                target = _LockTargets[_myTarget];
                intent = _IntentTargets[_myTarget];
            }
            lock (target)
            {
                lock (intent) { }
            }
        }
        bool CheckLock(object target, bool wait)
        {            
            if (Monitor.IsEntered(target))
            {
                return true;
            }
            else if (wait)
            {
                lock (target) { }                
            }            
            return false;
        }
        string DebugName
        {
            get
            {
                return "LockManager ID " + LockManagementID;
            }
        }
        /// <summary>
        /// Checks if the LockManger has an exclusive lock on this thread.
        /// </summary>
        /// <returns></returns>
        public bool CheckExclusiveSynched()
        {
            if (_MyLock != Lock.Exclusive)
                return false;
            object target;
            lock(_lock)
                target = _LockTargets[_myTarget];
            return Monitor.IsEntered(target);
        }
        /// <summary>
        /// Block until no other threads are trying to acquire locks.
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns>True if this method returns without reaching the timeout.</returns>
        public bool Wait(int timeout)
        {
            object target;
            lock(_lock)
                target = _LockTargets[_myTarget];
            if (Monitor.IsEntered(target))
            {
                return true; //Already in the lock, don't need to try
            }            
            bool taken = false;
            try
            {
                if (timeout <= 0)
                    Monitor.Enter(target, ref taken);
                else
                    Monitor.TryEnter(target, timeout, ref taken);
                return taken;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (taken)
                {
                    Monitor.Exit(target);                    
                }                
            }
        }
        /// <summary>
        /// Block until no other threads are trying to acquire a lock on the target.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="timeout">Timeout to wait. If less than or equal to 0, will be ignored.</param>
        /// <returns>False if the timeout is reached, or if the target is not used by the LockManager functionality.</returns>
        public static bool Wait(string target, int timeout = 0)
        {
            object lockTarget;
            lock (_lock)
            {
                if (!_LockTargets.ContainsKey(target))
                    return false;
                lockTarget = _LockTargets[target];
            }
            if (Monitor.IsEntered(lockTarget))
            {
                return true; //Already in the lock, don't need to try
            }
            bool taken = false;
            try
            {
                if (timeout <= 0)
                    Monitor.Enter(lockTarget, ref taken);
                else
                    Monitor.TryEnter(lockTarget, timeout, ref taken);
                return taken;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (taken)
                {
                    Monitor.Exit(lockTarget);
                }
            }
        }
        /// <summary>
        /// Maximum number of seconds that exclusive intent can be maintained without grabbing the actual exclusive lock.
        /// <para>(5 minutes)</para>
        /// </summary>
        public const int EXCLUSIVE_INTENT_EXPIRATION_SECONDS = 300;
        const string ALREADY_DISPOSED = "LockManager has had Dispose called already.";
        /// <summary>
        /// Determines whether the current lock needs to be released before calling <see cref="Acquire(Lock, bool, bool)"/> in safe mode.
        /// </summary>
        /// <param name="level">Lock level to be transitioned to.</param>
        /// <returns>Result of <see cref="Acquire(Lock, bool, bool)"/>, or true if the Lock is already at the desired level</returns>
        public bool TransitionLock(Lock level)
        {
            if (disposedValue)
            {
                throw new InvalidOperationException(ALREADY_DISPOSED);
            }
            if (_MyLock == level)
                return true;
            int calling = Thread.CurrentThread.ManagedThreadId;
            if (_MyLock >= LockBoundary && calling != LockManagementID)
            {
                //Note: it might make sense in some situations to allow, but because of monitor logic, this would cause issues for exclusive levels.
                throw new LockManagerSynchronizationException(calling, LockManagementID); 
            }
            else
                LockManagementID = calling; //Might be okay for shared....?



            object target;
            object intent;
            lock (_lock)
            {
                target = _LockTargets[_myTarget];
                intent = _IntentTargets[_myTarget];
            }
            

            if (_MyLock >= LockBoundary && level.In(Lock.Exclusive_Intent, Lock.Shared_Exclusive_Intent)) //Targeting intent.
                release(target, intent);                        

            /*
             Unlocked -> Anything
             NoLock -> Anything
             Shared -> Unlocked, NoLock: just release. Exclusive_Intent, with release.
                        Exclusive: handled in acquire
             Exclusive_Intent -> Unlocked, noLock: just release
                                   Shared: handle in acquire
                                   Exclusive: handle in acquire
             Exclusive -> Unlocked, noLock: just release
                            Shared: handle in acquire
                            Exclusive_intent: release first

             */
            return _acquire(level, target, intent, safe: true, timeoutSafe: true);
        }

        private bool _acquire(Lock level, object target, object intentTarget, bool safe, bool timeoutSafe)
        {
            if (level == Lock.Unlocked)
            {
                release(target, intentTarget);
                return true;
            }

            int calling = Thread.CurrentThread.ManagedThreadId;
            if(_MyLock == Lock.Unlocked)
            {
                LockManagementID = calling;
            }
            else if (LockManagementID != calling)
            {
                if(safe)
                {
                    System.Diagnostics.Debug.WriteLine("Unexpected LockID");
                    return false;
                }
                throw new LockManagerSynchronizationException(calling, LockManagementID);
            }

            const int LOCK_WAIT = 500;
            const int MONITOR_WAIT_TIMEOUT = 300000; //5 minutes


            if (Interlocked.Exchange(ref acquiring, 1) == 1)
            {
                if (safe)
                {
                    System.Diagnostics.Debug.WriteLine(DebugName + " - already acquiring. Return false.");
                    return false;
                }
                throw new LockManagerException("Tried to Acquire a new lock, but the manager is already trying to acquire a lock.");
            }

            Thread.MemoryBarrier(); //Only needed if this can affect the calling method. I would imagine it can as part of compiling code, but would need research to confirm..

            #region NOLOCK
            if (level < LockBoundary)
            {
                _MyLock = Lock.NoLock;
                System.Diagnostics.Debug.WriteLine(DebugName + " - NoLock.");
                //Already know that _MyLock is < LockBoundary or an exception would have been thrown/false returned.           
                return true;
                //NoLock doesn't need to ACTUALLY register or do anything. 
                //Still have the memory barrier from above, which might help by inserting a memory barrier into the compiled code..Would also need to research to confirm

                //This is mainly handled so that a Lock level can be variable
            }
            #endregion
            #region SHARE LOCKING
            if (level < ShareBoundary)
            {
                //Note that this is skipped for Exclusive/Exclusive intent (level is above ShareBoundary)                
                bool waitForIntent = false;
                lock (intentTarget)
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
                if (waitForIntent && !Thread.Yield())
                    Thread.Sleep(LOCK_WAIT);
                //Note: It's okay to have shared while exclusive intent is active. Just not while exclusive is active.
                //Exclusive is accounted for by the lock statement, since it will hold onto this lock
                lock (target)
                {
                    System.Diagnostics.Debug.WriteLine(DebugName + " - Share Lock acquired for '" + _myTarget + "'.");
                    _ShareCount[_myTarget]++;                   
                    if (Interlocked.Exchange(ref acquiring, 0) < 0)
                    {
                        release(target, intentTarget);
                        throw new LockManagerException("Attempting to acquire lock, but LockManager Dispose is being called");
                    }
                }
                if (_MyLock >= ShareBoundary)
                    exclusiveRelease(target, intentTarget); //do this after setting ShareCount, since we're going to lose the lock. 
                _MyLock = Lock.Shared;
                if (_MyLock == Lock.Shared_Exclusive_Intent)
                {
                    if (!GetExclusiveIntent(level, target, intentTarget, MONITOR_WAIT_TIMEOUT, timeoutSafe))
                    {
                        release(target, intentTarget);
                        return false;
                    }
                    _MyLock = Lock.Shared_Exclusive_Intent;
                }
                return true;
            }
            #endregion
            bool shareRelease = _MyLock >= LockBoundary && _MyLock < ShareBoundary;                
            if (shareRelease)
            {
                lock(target)
                {
                    _ShareCount[_myTarget]--; //Can't call release, because _MyLock is exclusive Intent at this point.                    
                    Monitor.PulseAll(target);              
                }
            }
            if (!GetExclusiveIntent(level, target, intentTarget, MONITOR_WAIT_TIMEOUT, timeoutSafe))
                return false;
            if (level < Lock.Exclusive)
                return true;
            #region EXCLUSIVE LOCKING       
            //Exclusive, once we're at this section, exclusive intent does not have an expiration.
            //lock (target)
            bool locked = false;
            lock (target)
            {
                while (_ShareCount[_myTarget] != 0)
                {
                    System.Diagnostics.Debug.WriteLine(DebugName + " - Check Share Locks for '" + _myTarget + "'. Count: " + _ShareCount[_myTarget]);
                    //Release lock and wait for a share or exclusive to pulse, up to ~5 minutes and then checking again.
                    Monitor.Wait(target, MONITOR_WAIT_TIMEOUT);
                    if (acquiring < 0)
                        throw new LockManagerException("Attempting to acquire lock, but LockManager Dispose is being called");
                }
#pragma warning disable 420
                /*Exceptions: argument null or exclusiveLocked is already true. Not worried about either of those.
                https://msdn.microsoft.com/en-us/library/dd289498(v=vs.110).aspx
                Key points from this page:
                 - It is legal for the same thread to invoke Enter more than once without it blocking; however, an equal number of Exit calls must be invoked before other threads waiting on the object will unblock.
                 - If this method returns without throwing an exception, the variable specified for the lockTaken parameter is always true, and there is no need to test it.
                */
                if (ExclusiveLockTimeout <= 0)
                    Monitor.Enter(target, ref locked);
                else
                    Monitor.TryEnter(target, ExclusiveLockTimeout * 1000, ref locked);
#pragma warning restore 420
                _MyLock = Lock.Exclusive;
                if (Interlocked.Exchange(ref acquiring, 0) < 0)
                {
                    release(target, intentTarget);
                    throw new LockManagerException("Attempting to acquire lock, but LockManager Dispose is being called");

                }
                if (!locked)
                {
                    Monitor.PulseAll(target);
                    release(target, intentTarget);
                    System.Diagnostics.Debug.WriteLine(DebugName + " - Exclusive Lock acquisition failed");
                    if (!timeoutSafe)
                        throw new TimeoutException("Timed out attempting to acquire exclusive lock on LockManager ID " + LockManagementID);
                }
                System.Diagnostics.Debug.WriteLineIf(locked, DebugName + " - acquired exclusive lock for '" + _myTarget + "'.");
                return locked;
            }


            #endregion
        }
        /// <summary>
        /// Mark exclusive intent in concurrentDictionary.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="target"></param>
        /// <param name="intentTarget"></param>
        /// <param name="MONITOR_WAIT_TIMEOUT"></param>
        /// <param name="timeoutSafe"></param>
        /// <returns></returns>
        bool GetExclusiveIntent(Lock level, object target, object intentTarget, int MONITOR_WAIT_TIMEOUT, bool timeoutSafe)
        {
            DateTime? deadline = null;
            if (ExclusiveIntentAcquisitionTimeout >= 0)
                deadline = DateTime.Now.AddSeconds(ExclusiveIntentAcquisitionTimeout);
            
            //Get access to the intent flag for the target
            DateTime exp;
            int? intentTargetHolder;
            lock (intentTarget)
            {
                _IntentHolder.TryGetValue(_myTarget, out intentTargetHolder);
                System.Diagnostics.Debug.WriteLine(DebugName + " - Check IntentHolder. Holder LockID: " + intentTargetHolder.ToString());
                if (intentTargetHolder == LockManagementID && _IntentExpiration.ContainsKey(_myTarget))
                {
                    //If we still have the intent holder and it's set to expire, remove the expiration time.
                    _IntentExpiration.TryRemove(_myTarget, out exp);
                    System.Diagnostics.Debug.WriteLine(DebugName + " - owns Exclusive intent. Remove Expiration.");
                }
                while (intentTargetHolder != LockManagementID)
                {
                    _IntentHolder.TryGetValue(_myTarget, out intentTargetHolder);
                    if (acquiring < 0)
                        throw new LockManagerException("Attempting to acquire lock, but LockManager Dispose is being called");
                    if (_IntentExpiration.TryGetValue(_myTarget, out exp))
                    {
                        if (exp < DateTime.Now)
                        {
                            //If expired, then the first LockManager to get here with the lock on target gets to take Intent
                            _IntentExpiration.TryRemove(_myTarget, out exp);
                            if (!_IntentHolder.TryUpdate(_myTarget, LockManagementID, intentTargetHolder))
                                continue;

                            _IntentHolder[_myTarget] = LockManagementID;
                            //Remove intent. Note that expiration is only set when grabbing exclusive intent but not exclusive.
                            //Removed when grabbing exclusive     
                            _MyLock = Lock.Exclusive_Intent;
                            if (Interlocked.Exchange(ref acquiring, 0) < 0)
                            {
                                release(target, intentTarget);
                                throw new LockManagerException("Attempting to acquire lock, but LockManager Dispose is being called");
                            }
                            System.Diagnostics.Debug.WriteLine(DebugName + " - acquired exclusive intent via Expiration for '" + _myTarget + "'.");
                            if (level < Lock.Exclusive)
                            {
                                _IntentExpiration.TryAdd(_myTarget, DateTime.Now.AddSeconds(EXCLUSIVE_INTENT_EXPIRATION_SECONDS));                                
                            }                            
                            return true;
                        }
                    }
                    else if (intentTargetHolder == null)
                    {
                        if (!_IntentHolder.TryUpdate(_myTarget, LockManagementID, intentTargetHolder))
                            continue;

                        _MyLock = Lock.Exclusive_Intent;
                        if (Interlocked.Exchange(ref acquiring, 0) < 0)
                        {
                            release(target, intentTarget);
                            throw new LockManagerException("Attempting to acquire lock, but LockManager Dispose is being called");
                        }
                        System.Diagnostics.Debug.WriteLine(DebugName + " - acquired exclusive intent for null IntentTargetHolder, for target '" + _myTarget + "'.");
                        if (level < Lock.Exclusive)
                        {
                            _IntentExpiration.TryAdd(_myTarget, DateTime.Now.AddSeconds(EXCLUSIVE_INTENT_EXPIRATION_SECONDS));                            
                        }
                        return true;
                    }
                    else
                        System.Diagnostics.Debug.WriteLine(DebugName + " - waiting for IntentHolder " + intentTargetHolder.ToString());
                    if (acquiring < 0)
                        throw new LockManagerException("Attempting to acquire lock, but LockManager Dispose is being called");
                    if (deadline.HasValue && deadline.Value > DateTime.Now)
                    {
                        System.Diagnostics.Debug.WriteLine(DebugName + " - Deadline exceeded for acquiring exclusive intent.");
                        if(!timeoutSafe)
                            throw new TimeoutException("Deadline exceeded for acquiring exclusive intent.");
                        return false;
                    }
                    Monitor.Wait(intentTarget, MONITOR_WAIT_TIMEOUT);//Note: need to be entered in Monitor/lock block
                }
            }
            return true; //while loop skipped, already had intent
        }

        /// <summary>
        /// Try to acquire a lock.
        /// <para>
        /// An exception will be thrown if you try to acquire a lock on a lock manager that already has a lock, unless moving to a higher lock level.
        /// </para>
        /// <remarks>
        /// If you try to acquire Lock.Unlocked, this will simply Release any lock the lock manager might have.
        /// </remarks>
        /// </summary>
        /// <param name="level"></param>
        /// <param name="safe">Returns immediately instead of throwing LockManagerExceptions if Parameter validation fails, or if the LockManager instance is already trying to acquire a lock.
        /// <para>Does not stop errors thrown as a result of another thread calling dispose on the LockManager while it's trying to dispose.</para></param>
        /// <param name="timeoutSafe">If true, returns false instead of throwing TimeOutExceptions for Exclusive and Exclusive Intent</param>
        /// <returns>True if lock is successfully acquired.</returns>
        public bool Acquire(Lock level,  bool safe = false, bool timeoutSafe = true)
        {
            if(level == _MyLock)
            {
                System.Diagnostics.Debug.WriteLine(DebugName + $" - attempting to gain lock '{level}', but manager is already at this level. Return false.");
                return false;
            }
            object target;
            object intent;
            lock (_lock)
            {
                target = _LockTargets[_myTarget];
                intent = _IntentTargets[_myTarget];
            }
            #region Parameter Validation
            if (disposedValue)
            {
                throw new InvalidOperationException(ALREADY_DISPOSED);
            }
            if (level == Lock.Unlocked)
            {
                release(target, intent);
                return true;
            }
            if (Monitor.IsEntered(target))
            {
                if (safe)
                {
                    System.Diagnostics.Debug.WriteLineIf(acquiring == 0, DebugName + " - already has exclusive Lock. Return false.");
                    System.Diagnostics.Debug.WriteLineIf(acquiring > 0, DebugName + " - already attempting to acquire a lock from another thread. Return false.");
                    return false;
                }
                if(acquiring == 0)
                    throw new LockManagerException("Tried to acquire a new lock on a manager that already has an exclusive lock.");
                throw new LockManagerException("Trying to acquire a lock, but this LockManager is already attempting to acquire a lock from another thread.");
            }
            else if(_MyLock == Lock.Exclusive)
            {
                System.Diagnostics.Debug.WriteLine(DebugName + " - Current lock level is 'Exclusive', but not in the monitor. Lock to wait for the other thread using lock manager to finish.");
                //lock (target) {  } 
                CheckLock(target, true);
            }
            if (_MyLock >= LockBoundary && level < _MyLock) //Allowed to call again when going to a higher lock.
            {
                if (safe)
                {
                    System.Diagnostics.Debug.WriteLine($"{DebugName} - cannot acquire lock {level.ToString()}, already at lock level {_MyLock}. Return false.");
                    return false;
                }
                throw new LockManagerException("Tried to Acquire a new lock on a manager that already holds a lock.");
            }
            #endregion

            return _acquire(level, target, intent, safe, timeoutSafe);
            
        }

        #region IDisposable Support
        volatile private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {                    
                    Interlocked.Exchange(ref acquiring, -1);                
                    release();                    
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }
        
        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~LockManager()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion

    }
    
}
