namespace SEIDR.ThreadManaging
{
    using System;
    using System.Collections.Generic;

    public enum Lock
    {
        /// <summary>
        /// Mostly the same effect as unlocked. Differentiated for intent.
        /// <para>
        /// NoLock is for accepting dirty reads, incomplete changes to objects.
        /// </para>
        /// <remarks>
        /// <para>NOTE: A NoLock is still considered a lock and will throw an Exception if you 
        /// </para>possess a NoLock and try to acquire a new lock.
        /// </remarks>
        /// </summary>
        NoLock = -1,
        /// <summary>
        /// Default lock value - No lock acquired
        /// </summary>
        Unlocked = -2,
        /// <summary>
        /// Shared allows many threads to look at the same object without having to worry about it being updated.
        /// </summary>
        Shared = 0,
        /// <summary>
        /// Exclusive intent will eventually be the same as an exclusive lock but will wait longer for any share locks to finish
        /// </summary>
        Exclusive_Intent = 4,
        /// <summary>
        /// Exclusive locks are for writing/updating the values of an object
        /// <para>
        /// Will wait until all share level locks are released before actually obtaining the lock
        /// </para>
        /// </summary>
        Exclusive = 5

        ///// <summary>
        ///// Allow reading even if another object has a lock. 
        ///// </summary>
        //Read_Uncommitted = 0,
        ///// <summary>
        ///// Allow setting values in an object without worrying about the lock
        ///// </summary>
        //Write_Uncommitted = 1,
        //Read_Committed = 10,

        //Read_Exclusive = 99,
        //Write_Intended = 100,
        //Write_Exclusive = 101
    }            
    /// <summary>
    /// Creates an object for managing different levels of locking.
    /// <para>
    /// This is only useful for multi threading and each thread really needs to have its own lock manager(s).
    /// </para>
    /// </summary>
    public class LockManager
    {        
        /// <summary>
        /// A lock below this value is considered 'Share' 
        /// <para>
        /// A lock at or above the value is considered exclusive
        /// </para>
        /// </summary>
        public const int ShareBoundary = 4;
        /// <summary>
        /// A lock below this value is considered 'unlocked' and should mainly be used for consistency or if the lock might be changed.
        /// <para>
        /// E.g. if the lock is in a variable, and the level of locking depends on other factors.
        /// </para>
        /// </summary>
        public const int LockBoundary = 0;
        /// <summary>
        /// The identifier for the default lock manager target
        /// </summary>
        public const string DefaultTarget = "DEFAULT";
        public bool HasLock
        {
            get { return _MyLock >= LockBoundary; }
        }
        static object _lock; //For locking manager settings        
        Lock _MyLock;
        //static SortedList<Lock, int> lockList ;
        static uint _IDCounter;
        /// <summary>
        /// A unique ID for your LockManager object
        /// </summary>
        public readonly uint LockID;
        static Dictionary<string, uint?> _IntentHolder;
        static Dictionary<string, uint?> _ExclusiveHolder; //Up to one exclusive at a time. Many shares
        static Dictionary<string, uint> _ShareCount; //For checking the number of shares on a lock target at a time
        static Dictionary<string, object> _LockTargets; //For actual lock management
        static LockManager()
        {
            //_IntentFlag = false;
            //_ExclusiveHolder = null;
            _IntentHolder = new Dictionary<string, uint?>();
            _ExclusiveHolder = new Dictionary<string, uint?>();
            _ShareCount = new Dictionary<string, uint>();
            _LockTargets = new Dictionary<string, object>();
            _lock = new object();            
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
                if(_ShareCount[_myTarget] >0)
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
        /// If the new value is unlocked, nothing will be done.
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
                if (_MyLock > Lock.Unlocked || value == Lock.Unlocked)
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
        public string Target
        {
            get
            {
                return _myTarget;
            }
        }
        public uint TimeOut { get; set; } = 0;
        /// <summary>
        /// Constructor.
        /// <para>Used for sharing or getting exclusive locks on sections based on other sections using the same target.</para>
        /// </summary>
        /// <param name="TARGET">Case insensitive</param>
        public LockManager(string TARGET = DefaultTarget, uint timeout = 0)
        {
            if (string.IsNullOrWhiteSpace(TARGET))
                throw new ArgumentNullException("TARGET");
            _MyLock = Lock.Unlocked;
            TimeOut = timeout;
            lock (_lock)
            {
                LockID = _IDCounter++;
                _myTarget = TARGET.ToUpper();
                if (!_LockTargets.ContainsKey(_myTarget))
                {
                    _LockTargets.Add(_myTarget, new object());
                    _IntentHolder.Add(_myTarget, null);
                    _ExclusiveHolder.Add(_myTarget, null);
                    _ShareCount.Add(_myTarget, 0);
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
        public void Release()
        {
            if (_MyLock == Lock.Unlocked)
            {
                throw new LockManagerException("Tried to release a lock but no lock exists.");
            }
            int xl = (int)_MyLock;
            //if ((int)_MyLock < LockBoundary)
            //{
            //    //Set to unlocked in case that's checked for anything
            //    _MyLock = Lock.Unlocked;
            //    return; //Mainly so that people can use the same code and
            //}
            if (xl >= LockBoundary)
            {
                if (xl < ShareBoundary)
                {
                    lock (_LockTargets[_myTarget])
                    {
                        _ShareCount[_myTarget]--;
                    }
                }
                if (xl >= ShareBoundary)
                {
                    lock (_LockTargets[_myTarget])
                    {
                        _ExclusiveHolder[_myTarget] = null;
                        _IntentHolder[_myTarget] = null;
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
            if (level == Lock.Unlocked)
            {
                Release();
                return;
            }
            if (_MyLock >= LockBoundary)
            {
                throw new LockManagerException("Tried to Acquire a new lock on a manager that already holds a lock.");
            }
            #endregion

            _MyLock = level;
            bool matched = false;
            DateTime start = DateTime.Now;

            #region NOLOCK
            if (level < LockBoundary)
            {   
                //Already know that _MyLock is < LockBoundary or an exception would have been thrown             
                return; //NoLock doesn't need to ACTUALLY register or do anything. 
                //This is mainly handled so that a Lock can be variable or in case the level of locking might be changed
                //later.
            }
            #endregion
            #region SHARE LOCKING
            if ((int)level < ShareBoundary)
            {
                while (true)
                {                    
                    lock (_LockTargets[_myTarget])
                    {
                        if(_ExclusiveHolder[_myTarget] == null)
                        {
                            _ShareCount[_myTarget]++;                            
                            return;
                        }
                    }
                    if (TimeOut > 0 && start.AddSeconds(TimeOut) > DateTime.Now)
                        throw new TimeoutException("Acquiring lock - " + DateTime.Now.Subtract(start).TotalSeconds);

                    System.Threading.Thread.Sleep(500);                    
                }
                /*
                while (_ExclusiveHolder[_myTarget] != null) { }
                lock (_LockTargets[_myTarget])
                {
                    _ShareCount[_myTarget]++;
                }                
                return;*/
            }
            #endregion
            #region EXCLUSIVE LOCKING
            //Get access to the intent flag for the target
            while (_IntentHolder[_myTarget] != LockID)
            {
                lock (_LockTargets[_myTarget])
                {
                    if (_IntentHolder[_myTarget] == null)
                    {                    
                        _IntentHolder[_myTarget] = LockID;
                        matched = true;
                    }
                }
                if (!matched)
                {
                    if (TimeOut > 0 && start.AddSeconds(TimeOut) > DateTime.Now)
                        throw new TimeoutException("Acquiring lock - " + DateTime.Now.Subtract(start).TotalSeconds);
                    System.Threading.Thread.Sleep(500);
                }
            }
            matched = false;
            //An intent lock will wait until the share locks are at 0 before trying to acquire exclusive lock
            //while ((int) level == ShareBoundary && _ShareCount[_myTarget] > 0) { } //#Update: Handle this in the exclusive loop's condition check
            while (LockID != _ExclusiveHolder[_myTarget])
            {
                //_LockValue >= ShareBoundary && //Doesn't really matter... Only really care about the exclusive holder for the target
                lock (_LockTargets[_myTarget])
                {
                    if (_ExclusiveHolder[_myTarget] == null && ((int)level > ShareBoundary || _ShareCount[_myTarget] == 0))
                    {                    
                        _ExclusiveHolder[_myTarget] = LockID;
                        matched = true;
                    }
                }
                if (!matched)
                {
                    if (TimeOut > 0 && start.AddSeconds(TimeOut) > DateTime.Now)
                        throw new TimeoutException("Acquiring lock - " + DateTime.Now.Subtract(start).TotalSeconds);
                    System.Threading.Thread.Sleep(500);
                }
            }
            //while (_ShareCount[_myTarget] > 0) { } //Not necessary after expanding the lock blocks
            //AddLock(level);
            return;
            #endregion            
        }
    }
    
}
