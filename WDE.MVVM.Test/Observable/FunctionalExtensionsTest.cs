using System;
using NSubstitute;
using NUnit.Framework;
using WDE.MVVM.Observable;

namespace WDE.MVVM.Test.Observable
{
    public class FunctionalExtensionsTest
    {
        [Test]
        public void TestSelect()
        {
            ReactiveProperty<int> property = new ReactiveProperty<int>(5);

            var observer = Substitute.For<IObserver<int>>();
            var doubleValue = property.Select(v => v * 2);
            
            Assert.AreEqual(0, property.SubscriptionsCount);

            using (doubleValue.Subscribe(observer))
            {
                Assert.AreEqual(1, property.SubscriptionsCount);
                observer.Received(1).OnNext(10);
                observer.ClearReceivedCalls();

                property.Value = 4;
                observer.Received(1).OnNext(8);
            }
        }
        
        [Test]
        public void TestNot()
        {
            ReactiveProperty<bool> property = new ReactiveProperty<bool>(false);

            var observer = Substitute.For<IObserver<bool>>();
            var not = property.Not();
            Assert.AreEqual(0, property.SubscriptionsCount);
            
            using (not.Subscribe(observer))
            {
                observer.Received(1).OnNext(true);
                observer.ClearReceivedCalls();

                property.Value = true;
                observer.Received(1).OnNext(false);
            }
        }
    }
}