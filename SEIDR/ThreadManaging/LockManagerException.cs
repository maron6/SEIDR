namespace SEIDR.ThreadManaging
{
    using System;
    /// <summary>
    /// Lock Manager Exception. Thrown by the lock manager when there's an issue with a lock trying to be acquired.
    /// </summary>
    [Serializable]
    public class LockManagerException : Exception
    {         
        public LockManagerException() { }
        public LockManagerException(string message) : base(message) { }
        public LockManagerException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
