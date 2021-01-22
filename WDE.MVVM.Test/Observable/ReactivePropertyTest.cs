using System;
using NSubstitute;
using NUnit.Framework;
using WDE.MVVM.Observable;

namespace WDE.MVVM.Test.Observable
{
    public class ReactivePropertyTest
    {
        [Test]
        public void TestSimpleValueHolder()
        {
            ReactiveProperty<int> property = new ReactiveProperty<int>(5);
            
            Assert.AreEqual(5, property.Value);

            property.Value = 3;
            
            Assert.AreEqual(3, property.Value);
        }
        
        [Test]
        public void TestSubscribe()
        {
            ReactiveProperty<int> property = new ReactiveProperty<int>(5);

            var observer = Substitute.For<IObserver<int>>();

            Assert.AreEqual(0, property.SubscriptionsCount);

            using (property.Subscribe(observer))
            {
                Assert.AreEqual(1, property.SubscriptionsCount);
                observer.Received(1).OnNext(5);
                
                observer.ClearReceivedCalls();

                property.Value = 4;
                
                observer.Received(1).OnNext(4);
            }
            
            Assert.AreEqual(0, property.SubscriptionsCount);
        }

        [Test]
        public void TestSetSameValue()
        {
            ReactiveProperty<int> property = new ReactiveProperty<int>(5);
            var observer = Substitute.For<IObserver<int>>();
            using (property.Subscribe(observer))
            {
                observer.Received(1).OnNext(5);
                observer.ClearReceivedCalls();

                property.Value = 5;
                
                observer.ReceivedWithAnyArgs(0).OnNext(default);
            }
        }
        
        [Test]
        public void TestDisposeProperty()
        {
            ReactiveProperty<int> property = new ReactiveProperty<int>(5);
            var observer = Substitute.For<IObserver<int>>();
            
            using (property.Subscribe(observer))
            {
                Assert.AreEqual(1, property.SubscriptionsCount);
                property.Dispose();
                Assert.AreEqual(0, property.SubscriptionsCount);
            }
            Assert.AreEqual(0, property.SubscriptionsCount);

            var dummyDispose = property.Subscribe(observer);
            Assert.AreEqual(0, property.SubscriptionsCount);
            dummyDispose.Dispose();
        }
        
        [Test]
        public void TestDisposeOnReceive()
        {
            ReactiveProperty<int> property = new ReactiveProperty<int>(5);
            IDisposable[] disposable = new IDisposable[1];
            var observer = Substitute.For<IObserver<int>>();
            observer.When(x => x.OnNext(2)).Do(_ =>  disposable[0].Dispose());

            disposable[0] = property.Subscribe(observer);
            Assert.AreEqual(1, property.SubscriptionsCount);
            property.Value = 2;
            
            Assert.AreEqual(0, property.SubscriptionsCount);
        }
    }
}