namespace SEIDR.ThreadManaging
{
    /// <summary>
    /// LockManager helper enum.
    /// </summary>
    public enum Lock
    {
        /// <summary>
        /// Mostly the same effect as unlocked. Differentiated for intent.
        /// <para>
        /// NoLock is for accepting that objects may be in an incomplete state.
        /// </para>
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
        /// Contribute to shareCount, but obtain exclusive intent
        /// </summary>        
        Shared_Exclusive_Intent = 3,
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
}
