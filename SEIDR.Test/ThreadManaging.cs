using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEIDR.ThreadManaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            int pointer = 0;
            l3.Acquire(Lock.Exclusive);
            l4.Acquire(Lock.Exclusive);// should not interfere.
            Task t = new Task(() =>
            {
                System.Diagnostics.Debug.WriteLine("Entered, t1");
                Assert.AreEqual(0, pointer);                
                pointer++;
                l1.Acquire(Lock.Shared);
                pointer++;
                Assert.AreEqual(3, pointer);
                System.Diagnostics.Debug.WriteLine("T1, Lock Acquired. Release and grab exclusive");
                l1.Release();
                System.Diagnostics.Debug.WriteLine("T1, wait for exclusive");
                l1.Acquire(Lock.Exclusive);
                Assert.AreEqual(4, pointer);
                pointer++;
                System.Diagnostics.Debug.WriteLine("Exit, t1");
                l1.Release();
            });
            t.Start();
            System.Threading.Thread.Sleep(1000);
            Assert.AreEqual(1, pointer);
            Task t2 = new Task(() =>
            {
                Assert.AreEqual(1, pointer);
                pointer++;
                System.Diagnostics.Debug.WriteLine("Entered, t2");
                l2.Acquire(Lock.Shared);
                Assert.AreEqual(3, pointer);                
                System.Diagnostics.Debug.WriteLine("Lock Acquired t2");
                System.Diagnostics.Debug.WriteLine("Wait ten seconds, then release.");
                System.Threading.Thread.Sleep(10 * 1000);
                pointer++;
                l2.Release();
                System.Diagnostics.Debug.WriteLine("Exit t2");
            });
            t2.Start();
            System.Threading.Thread.Sleep(1000); //give time for pointer to be updated.. Could also just lock the object.
            System.Diagnostics.Debug.WriteLine("T2 started");
            Assert.AreEqual(2, pointer);
            l3.Release();
            System.Diagnostics.Debug.WriteLine("Tasks started, l3 released. Sleep 15 seconds and release l4");
            System.Threading.Thread.Sleep(15 * 1000);
            System.Diagnostics.Debug.WriteLine("release l4");
            Assert.AreEqual(5, pointer);
            l4.Release();
        }
    }
}
