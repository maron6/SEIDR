using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEIDR.ThreadManaging
{
    public static class LockMangerExtensions
    {
        public static LockHelper GetLockHelper<RT>(this RT obj, Lock targetLock)
        {
            return new LockHelper<RT>(targetLock);
        }
        public static LockHelper GetLockHelper<RT>(this RT obj, Lock targetLock, string Alias)
        {
            return new LockHelper<RT>(targetLock, alias: Alias);
        }
        /// <summary>
        /// Blocks thread until there isn't a thread attempting to access the target, 
        /// </summary>
        /// <typeparam name="RT"></typeparam>
        /// <param name="obj">Used together with Alias (if provided) to determine the target, using <see cref="LockHelper{RT}.CheckTarget(string)"/></param>
        /// <param name="Alias"></param>
        /// <param name="timeout"></param>
        public static bool Wait<RT>(this RT obj, string Alias = null, int timeout = 0)
        {
            string target = LockHelper<RT>.CheckTarget(Alias);
            return LockManager.Wait(target, timeout: timeout);
        }
    }
}
