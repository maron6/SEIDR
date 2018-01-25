using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEIDR.ThreadManaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace SEIDR.Test
{
    [TestClass]
    public class ThreadManagingTest
    {
        LockManager l1 = new LockManager("Test");
        LockManager l2 = new LockManager("Test");
        LockManager l3 = new LockManager("Test");
        LockManager l4 = new LockManager("TestSeparate");
        LockManager l5 = new LockManager("Test");
        [TestMethod]
        public void TestBlock()
        {
            bool T2First = false;
            l2.Acquire(Lock.Exclusive);
            Task t = new Task(() =>
            {
                Assert.IsFalse(T2First);
                l1.Acquire(Lock.Exclusive);
                Assert.IsTrue(T2First);
                l1.Release();
            });
            t.Start();
            Task t2 = new Task(() =>
            {
                T2First = true;
                l2.Release();
            });
            t2.Start();
        }
        [TestMethod]
        public void TestShare()
        {
            System.Diagnostics.Debug.WriteLine("Current ThreadID: " + Thread.CurrentThread.ManagedThreadId);
            l5.Acquire(Lock.Exclusive_Intent);
            l4.Acquire(Lock.Exclusive);// should not interfere.            
            Thread t = new Thread(() =>
            {
                System.Diagnostics.Debug.WriteLine("Entered, t1.");
                System.Diagnostics.Debug.WriteLine("Current ThreadID: " + Thread.CurrentThread.ManagedThreadId);                
                //Assert.AreEqual(0, pointer);                                
                l1.Acquire(Lock.Shared);

                //Assert.AreEqual(3, pointer);
                System.Diagnostics.Debug.WriteLine("T1, Lock Acquired. Release and grab exclusive");
                l1.Release();
                System.Diagnostics.Debug.WriteLine("T1, wait for exclusive");
                l1.Acquire(Lock.Exclusive);
                //Assert.AreEqual(4, pointer);
                //pointer++;
                System.Diagnostics.Debug.WriteLine("Release l1, t1");
                l1.Release();
                System.Diagnostics.Debug.WriteLine("Exit, t1");
            });
            t.IsBackground = true;
            Task t0 = new Task(() =>
            {
                System.Diagnostics.Debug.WriteLine("Entered, t0. t3 Acquire exclusive");
                System.Diagnostics.Debug.WriteLine("Current ThreadID: " + Thread.CurrentThread.ManagedThreadId);
                l3.Acquire(Lock.Exclusive);
                Thread.Sleep(10_000);
                l3.Release();
            });
            
            t0.Start();
            l5.Release();
            System.Threading.Thread.Sleep(1000);
            t.Start();
            System.Threading.Thread.Sleep(1000);
            //Assert.AreEqual(1, pointer);
            Thread t2 = new Thread(() =>
            {
                //Assert.AreEqual(1, pointer);
                //pointer++;
                System.Diagnostics.Debug.WriteLine("Entered, t2");
                System.Diagnostics.Debug.WriteLine("Current ThreadID: " + Thread.CurrentThread.ManagedThreadId);
                l2.Acquire(Lock.Shared);                
                System.Diagnostics.Debug.WriteLine("Lock Acquired t2");
                System.Diagnostics.Debug.WriteLine("Wait ten seconds, then release.");
                System.Threading.Thread.Sleep(10 * 1000);                
                l2.Release();
                System.Diagnostics.Debug.WriteLine("Exit t2");
            });
            t2.IsBackground = true;
            t2.Start();
            System.Diagnostics.Debug.WriteLine("T2 started");
            //Assert.AreEqual(2, pointer);
            l5.Acquire(Lock.Exclusive_Intent);
            System.Diagnostics.Debug.WriteLine("Tasks started, l3 released. Sleep 15 seconds and release l4");
            System.Threading.Thread.Sleep(15 * 1000);
            System.Diagnostics.Debug.WriteLine("release l4");
            //Assert.AreEqual(5, pointer);
            l5.Acquire(Lock.Exclusive); //Do NOT release/acquire from different threads. Need to add a safety there..
            l4.Release();
            while (l5.MyLock < Lock.Exclusive)
            {
                System.Diagnostics.Debug.WriteLine("l5 waiting for exclusive. Sleep...");
                System.Threading.Thread.Sleep(5 * 1000);                
            }
            l5.Release();
            System.Diagnostics.Debug.WriteLine("l5 released");

            /*
             Output:
                 Current ThreadID: 11
                IntentHolder: LockID 
                LockID 5 - acquired exclusive intent for null IntentTargetHolder
                IntentHolder: LockID 
                LockID 4 - acquired exclusive intent for null IntentTargetHolder
                LockID 4 - acquired exclusive lock.
                Entered, t0. t3 Acquire exclusive
                Current ThreadID: 9
                IntentHolder: LockID 5
                
                Exclusive_Intent Lock released for target 'TEST'. LockID: 5
                LockID 3 - acquired exclusive intent for null IntentTargetHolder
                LockID 3 - acquired exclusive lock.
                Entered, t1.
                Current ThreadID: 14
                T2 started
                IntentHolder: LockID 3
                LockID 5 - waiting for IntentHolder 3
                Entered, t2
                Current ThreadID: 16
                
                LockID 1 Share Lock acquired for 'TEST'.
                Exclusive Lock released for target 'TEST'. LockID: 3
                LockID 5 - acquired exclusive intent for null IntentTargetHolder
                Tasks started, l3 released. Sleep 15 seconds and release l4
                T1, Lock Acquired. Release and grab exclusive
                LockID 2 Share Lock acquired for 'TEST'.
                Lock Acquired t2
                
                Wait ten seconds, then release.
                Share released for target 'TEST' on LockID 1. Remaining sharecount: 1
                T1, wait for exclusive
                IntentHolder: LockID 5
                Share released for target 'TEST' on LockID 2. Remaining sharecount: 0
                Exit t2
                
                
                release l4
                IntentHolder: LockID 5
                LockID 5 - owns Exclusive intent. Remove Expiration.
                LockID 5 - acquired exclusive lock.
                Exclusive Lock released for target 'TESTSEPARATE'. LockID: 4
                Exclusive Lock released for target 'TEST'. LockID: 5
                l5 released
                LockID 1 - acquired exclusive intent for null IntentTargetHolder
                LockID 1 - acquired exclusive lock.
                Release l1, t1
                
                Exclusive Lock released for target 'TEST'. LockID: 1
                Exit, t1
             
             
             */

        }
    }
}
