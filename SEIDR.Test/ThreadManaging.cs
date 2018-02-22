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
            Task t = new Task(() =>
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
            //t.IsBackground = true;
            Task t0 = new Task(() =>
            {
                System.Diagnostics.Debug.WriteLine("Entered, t0. t3 Acquire exclusive");
                System.Diagnostics.Debug.WriteLine("Current ThreadID: " + Thread.CurrentThread.ManagedThreadId);
                l3.Acquire(Lock.Exclusive);
                Thread.Sleep(10000);
                l3.Release();
            });
            
            t0.Start();
            l5.Release();
            Thread.Sleep(1000);
            t.Start();
            Thread.Sleep(1000);
            //Assert.AreEqual(1, pointer);
            var t2 = new Task(() =>
            {
                //Assert.AreEqual(1, pointer);
                //pointer++;
                System.Diagnostics.Debug.WriteLine("Entered, t2");
                System.Diagnostics.Debug.WriteLine("Current ThreadID: " + Thread.CurrentThread.ManagedThreadId);
                l2.Acquire(Lock.Shared);                
                System.Diagnostics.Debug.WriteLine("Lock Acquired t2");
                System.Diagnostics.Debug.WriteLine("Wait ten seconds, then release.");
                Thread.Sleep(10 * 1000);                
                l2.Release();
                using(var h =  new LockHelper(Lock.Shared, "Test"))
                {
                    h.Transition(Lock.Exclusive);
                    Thread.Sleep(10 * 1000);
                    h.Transition(Lock.Shared);
                    Thread.Sleep(10 * 1000);                    
                }
                System.Diagnostics.Debug.WriteLine("Exit t2");

            });
            t2.Start();
            System.Diagnostics.Debug.WriteLine("T2 started");
            //Assert.AreEqual(2, pointer);
            l5.Acquire(Lock.Exclusive_Intent);
            System.Diagnostics.Debug.WriteLine("Tasks started, l3 released. Sleep 15 seconds and release l4");
            Thread.Sleep(15 * 1000);
            System.Diagnostics.Debug.WriteLine("release l4");
            //Assert.AreEqual(5, pointer);
            l5.Acquire(Lock.Exclusive); //Do NOT release/acquire from different threads. Need to add a safety there..
            l4.Release();
            while (l5.LockLevel < Lock.Exclusive)
            {
                System.Diagnostics.Debug.WriteLine("l5 waiting for exclusive. Sleep...");
                Thread.Sleep(5 * 1000);                
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
        [TestMethod]
        public void MultiLockTest()
        {
            string[] targetList = new string[] {  "T2", "T1", "L1" };
            LockManager l1 = new LockManager("T1"); //6
            l1.Acquire(Lock.Exclusive);
            Thread t = new Thread(() =>
            {

                using (var m = new MultiLockHelper(Lock.Shared, targetList)) //7-9
                {
                    System.Diagnostics.Debug.WriteLine("Thread 1 - Entered MultiLockHelper!");
                    if (m.Transition(Lock.Exclusive_Intent))
                    {
                        System.Diagnostics.Debug.WriteLine("Thread 1 - Exclusive_Intent!");
                        if (m.Transition(Lock.Exclusive))
                            System.Diagnostics.Debug.WriteLine("Thread 1 - Exclusive!");
                    }
                    Thread.Sleep(10000);
                    System.Diagnostics.Debug.WriteLine("Thread 1 - Ending");
                }
                System.Diagnostics.Debug.WriteLine("Thread 1 - End.");
            })
            {
                IsBackground = true
            };
            t.Start();
            Thread.Sleep(5000);
            System.Diagnostics.Debug.WriteLine("Main - Wake");
            l1.TransitionLock(Lock.Shared);
            Thread t2 = new Thread(() =>
            {
                using (var m = new MultiLockHelper(Lock.Shared, targetList.Where(s => s.EndsWith("1")))) //10, 11
                {
                    System.Diagnostics.Debug.WriteLine("Thread 2 - Enterd MultiLockHelper");
                    Thread.Sleep(15000); 
                }
                System.Diagnostics.Debug.WriteLine("Thread 2 - Finish");
            })
            {
                IsBackground = true
            };
            t2.Start();
            Thread.Sleep(25000);
            Thread t3 = new Thread(() =>
            {
                System.Diagnostics.Debug.WriteLine("Thread 3 - Waiting for T2!");
                LockManager.Wait("T2");
                System.Diagnostics.Debug.WriteLine("Thread 3 - Finished wait!");                
            })
            {
                IsBackground = true
            };
            t3.Start();
            Thread.Sleep(15000);
            System.Diagnostics.Debug.WriteLine("Main - Release");
            l1.Release();
            System.Diagnostics.Debug.WriteLine("Main - Wait");
            Thread.Sleep(15000);
            LockManager.Wait("T2");
            LockManager.Wait("T1");
            LockManager.Wait("L1");
            Thread.Sleep(40000);
            System.Diagnostics.Debug.WriteLine("Main - Done");
            /*
             Output:
 
            LockManager ID 6 - Check IntentHolder. Holder LockID: 
            LockManager ID 6 - acquired exclusive intent for null IntentTargetHolder, for target 'T1'.
            LockManager ID 6 - acquired exclusive lock for 'T1'.
            LockManager ID 7 - Share Lock acquired for 'T2'.
            Main - Wake
            LockManager ID 6 - Share Lock acquired for 'T1'.
            LockManager ID 8 - Share Lock acquired for 'T1'.
            Exclusive Lock released for target 'T1'. LockID: 6
            LockManager ID 9 - Share Lock acquired for 'L1'.
            Thread 1 - Entered MultiLockHelper!
            Share released for target 'T2' on LockID 7. Remaining sharecount: 0
            LockManager ID 10 - Share Lock acquired for 'T1'.
            LockManager ID 11 - Share Lock acquired for 'L1'.
            
            Thread 2 - Enterd MultiLockHelper
            LockManager ID 7 - Check IntentHolder. Holder LockID: 
            LockManager ID 7 - acquired exclusive intent for null IntentTargetHolder, for target 'T2'.
            Share released for target 'T1' on LockID 8. Remaining sharecount: 2
            LockManager ID 8 - Check IntentHolder. Holder LockID: 
            LockManager ID 8 - acquired exclusive intent for null IntentTargetHolder, for target 'T1'.
            Share released for target 'L1' on LockID 9. Remaining sharecount: 1
            LockManager ID 9 - Check IntentHolder. Holder LockID: 
            LockManager ID 9 - acquired exclusive intent for null IntentTargetHolder, for target 'L1'.
            Thread 1 - Exclusive_Intent!

            LockManager ID 7 - Check IntentHolder. Holder LockID: 7
            LockManager ID 7 - owns Exclusive intent. Remove Expiration.
            LockManager ID 7 - acquired exclusive lock for 'T2'.
            LockManager ID 8 - Check IntentHolder. Holder LockID: 8
            LockManager ID 8 - owns Exclusive intent. Remove Expiration.
            LockManager ID 8 - Check Share Locks for 'T1'. Count: 2
            

            Share released for target 'T1' on LockID 10. Remaining sharecount: 1
            Share released for target 'L1' on LockID 11. Remaining sharecount: 0
            LockManager ID 8 - Check Share Locks for 'T1'. Count: 1
            Thread 2 - Finish
            

            Thread 3 - Waiting for T2!
            Main - Release
            Share released for target 'T1' on LockID 6. Remaining sharecount: 0
            Main - Wait
            LockManager ID 8 - acquired exclusive lock for 'T1'.
            LockManager ID 9 - Check IntentHolder. Holder LockID: 9
            LockManager ID 9 - owns Exclusive intent. Remove Expiration.
            LockManager ID 9 - acquired exclusive lock for 'L1'.
            Thread 1 - Exclusive!
            Thread 1 - Ending
            Thread 3 - Finished wait!
            

            Exclusive Lock released for target 'T2'. LockID: 7
            Exclusive Lock released for target 'T1'. LockID: 8
            Exclusive Lock released for target 'L1'. LockID: 9
            Thread 1 - End.
            

            Main - Done
             */
        }
    }
}
