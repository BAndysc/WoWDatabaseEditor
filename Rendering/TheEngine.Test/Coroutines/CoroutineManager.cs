using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TheEngine.Coroutines;

namespace TheEngine.Test.Coroutines
{
    public class CoroutineManagerTest
    {
        private CoroutineManager cm = null!;
        
        [SetUp]
        public void Setup()
        {
            cm = new();
        }
        
        [Test]
        public void SimpleTest()
        {
            int state = 0;

            IEnumerator CoroutineA()
            {
                state = 1;
                yield return null;
                state = 2;
            }
            
            Start(CoroutineA);
            Assert.AreEqual(1, state);
            cm.Step();
            Assert.AreEqual(2, state);
            Assert.AreEqual(0, cm.PendingCoroutines);
            cm.Step();
            Assert.AreEqual(2, state);
        }
        
        
        [Test]
        public void TwoCoroutinesTest()
        {
            int state1 = 0;
            int state2 = 0;

            IEnumerator CoroutineA()
            {
                state1 = 1;
                yield return null;
                state1 = 2;
            }
            IEnumerator CoroutineB()
            {
                state2 = 1;
                yield return null;
                state2 = 2;
            }
            
            Start(CoroutineA);
            Start(CoroutineB);
            Assert.AreEqual(1, state1);
            Assert.AreEqual(1, state2);
            cm.Step();
            Assert.AreEqual(2, state1);
            Assert.AreEqual(2, state2);
            Assert.AreEqual(0, cm.PendingCoroutines);
            cm.Step();
            Assert.AreEqual(2, state1);
            Assert.AreEqual(2, state2);
        }

        [Test]
        public void NestedCoroutineTest()
        {
            int state1 = 0;
            int state2 = 0;

            IEnumerator CoroutineB()
            {
                state2 = 1;
                yield return null;
                state2 = 2;
                yield return null;
            }
            
            IEnumerator CoroutineA()
            {
                state1 = 1;
                yield return CoroutineB();
                state1 = 2;
            }
            
            Start(CoroutineA);
            Assert.AreEqual(1, state1);
            Assert.AreEqual(1, state2);
            Assert.AreEqual(2, cm.PendingCoroutines);
            cm.Step();
            Assert.AreEqual(1, state1);
            Assert.AreEqual(2, state2);
            Assert.AreEqual(2, cm.PendingCoroutines);
            cm.Step();
            Assert.AreEqual(1, state1);
            Assert.AreEqual(2, state2);
            Assert.AreEqual(1, cm.PendingCoroutines);
            cm.Step();
            Assert.AreEqual(2, state1);
            Assert.AreEqual(2, state2);
            Assert.AreEqual(0, cm.PendingCoroutines);
        }


        [Test]
        public void DoubleNestedCoroutineTest()
        {
            int state1 = 0;
            int state2 = 0;
            int state3 = 0;

            IEnumerator CoroutineC()
            {
                state3 = 1;
                yield return null;
            }
            
            IEnumerator CoroutineB()
            {
                yield return CoroutineC();
                state2 = 1;
            }
            
            IEnumerator CoroutineA()
            {
                state1 = 1;
                yield return CoroutineB();
                state1 = 2;
            }
            
            Start(CoroutineA);
            Assert.AreEqual(1, state1);
            Assert.AreEqual(0, state2);
            Assert.AreEqual(1, state3);
            cm.Step();
            Assert.AreEqual(1, state1);
            Assert.AreEqual(0, state2);
            Assert.AreEqual(1, state3);
            cm.Step();
            Assert.AreEqual(1, state1);
            Assert.AreEqual(1, state2);
            Assert.AreEqual(1, state3);
            cm.Step();
            Assert.AreEqual(2, state1);
            Assert.AreEqual(1, state2);
            Assert.AreEqual(1, state3);
        }
        
        [Test]
        public void TaskTest()
        {
            int state = 0;

            TaskCompletionSource tcs = new TaskCompletionSource();
            IEnumerator CoroutineA()
            {
                state = 1;
                yield return new WaitForTask(tcs.Task);
                state = 2;
            }
            
            Start(CoroutineA);
            Assert.AreEqual(1, state);
            cm.Step();
            cm.Step();
            cm.Step();
            cm.Step();
            cm.Step();
            Assert.AreEqual(1, state);
            tcs.SetResult();
            Thread.Sleep(300);
            cm.Step();
            Assert.AreEqual(2, state);
            Assert.AreEqual(0, cm.PendingCoroutines);
        }
        
        [Test]
        public void TaskWithResultTest()
        {
            int state = 0;

            async Task<int> Fun()
            {
                await Task.Delay(100);
                return 5;
            }
            IEnumerator CoroutineA()
            {
                state = 1;
                var t = Fun();
                yield return new WaitForTask(t);
                Assert.IsTrue(t.IsCompletedSuccessfully);
                state = t.Result;
            }
            
            Start(CoroutineA);
            Assert.AreEqual(1, state);
            cm.Step();
            Assert.AreEqual(1, state);
            Thread.Sleep(300);
            cm.Step();
            Assert.AreEqual(5, state);
            Assert.AreEqual(0, cm.PendingCoroutines);
        }
        
        [Test]
        public void TaskWaitOnCompletedTest()
        {
            int state = 0;

            TaskCompletionSource tcs = new TaskCompletionSource();
            tcs.SetResult();
            IEnumerator CoroutineA()
            {
                state = 1;
                yield return new WaitForTask(tcs.Task);
                state = 2;
            }
            
            Start(CoroutineA);
            Assert.AreEqual(2, state);
            Assert.AreEqual(0, cm.PendingCoroutines);
        }
        
        [Test]
        public void NestedCoroutinesRunInstantly()
        {
            int state1 = 0;
            int state2 = 0;
            int state3 = 0;

            IEnumerator CoroutineC()
            {
                state3 = 1;
                yield break;
            }

            IEnumerator CoroutineB()
            {
                yield return CoroutineC();
                state2 = 1;
            }
            
            IEnumerator CoroutineA()
            {
                yield return CoroutineB();
                state1 = 1;
            }
            
            Start(CoroutineA);
            Assert.AreEqual(1, state1);
            Assert.AreEqual(1, state2);
            Assert.AreEqual(1, state3);
            Assert.AreEqual(0, cm.PendingCoroutines);
        }
        
        [Test]
        public void NestedException()
        {
            int state1 = 0;

            IEnumerator CoroutineC()
            {
                throw new Exception();
            }

            IEnumerator CoroutineB()
            {
                yield return CoroutineC();
            }
            
            IEnumerator CoroutineA()
            {
                yield return CoroutineB();
                state1 = 1;
            }

            Start(CoroutineA);
            Assert.AreEqual(0, state1);
        }
        
        [Test]
        public void NestedExceptionIgnore()
        {
            int state1 = 0;

            IEnumerator CoroutineC()
            {
                throw new Exception();
            }

            IEnumerator CoroutineB()
            {
                yield return CoroutineC();
            }
            
            IEnumerator CoroutineA()
            {
                yield return CoroutineB().ContinueOnException();
                state1 = 1;
            }

            Start(CoroutineA);
            Assert.AreEqual(1, state1);
        }
        
        private void Start(Func<IEnumerator> a)
        {
            cm.Start(a());
        }
    }
}