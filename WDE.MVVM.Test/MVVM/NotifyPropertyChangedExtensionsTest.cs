using System;
using NSubstitute;
using NUnit.Framework;

namespace WDE.MVVM.Test.MVVM
{
    public class NotifyPropertyChangedExtensionsTest
    {
        [Test]
        public void TestSimpleToObservable()
        {
            TestObjectNotifyPropertyChanged obj = new TestObjectNotifyPropertyChanged() {Number = 2};
            
            var number = obj.ToObservable(o => o.Number);
            
            var numberObserver = Substitute.For<IObserver<int>>();

            using (number.Subscribe(numberObserver))
            {
                numberObserver.Received(1).OnNext(2);
                obj.Number = 3;
                numberObserver.Received(1).OnNext(3);
            }
            
            numberObserver.ReceivedWithAnyArgs(2).OnNext(default);
        }

        [Test]
        public void TestToObservableCallsWhenNeeded()
        {
            TestObjectNotifyPropertyChanged obj = new TestObjectNotifyPropertyChanged() {Number = 2, StringValue = "abc"};
            
            var number = obj.ToObservable(o => o.Number);
            
            var numberObserver = Substitute.For<IObserver<int>>();

            using (number.Subscribe(numberObserver))
            {
                numberObserver.Received(1).OnNext(2);
                obj.StringValue = "edf";
            }
            
            numberObserver.ReceivedWithAnyArgs(1).OnNext(default);
        }

        [Test]
        public void TestToObservableDoesNotAddListener()
        {
            TestObjectNotifyPropertyChanged obj = new TestObjectNotifyPropertyChanged() {Number = 2, StringValue = "abc"};
            Assert.AreEqual(0, obj.EventHandlers);
            obj.ToObservable(o => o.Number);
            Assert.AreEqual(0, obj.EventHandlers);
        }
        
        [Test]
        public void TestDisposeRemovesListener()
        {
            TestObjectNotifyPropertyChanged obj = new TestObjectNotifyPropertyChanged() {Number = 2, StringValue = "abc"};
            Assert.AreEqual(0, obj.EventHandlers);
            var number = obj.ToObservable(o => o.Number);
            Assert.AreEqual(0, obj.EventHandlers);

            using (number.Subscribe(Substitute.For<IObserver<int>>()))
            {
                Assert.AreEqual(1, obj.EventHandlers);
            }
            
            Assert.AreEqual(0, obj.EventHandlers);
        }

        [Test]
        public void TestCannotObserveField()
        {
            TestObjectNotifyPropertyChanged obj = new();

            Assert.Throws<ArgumentException>(() => obj.ToObservable(o => o.Field));
        }
        
        [Test]
        public void TestCannotObserveAnyExpression()
        {
            TestObjectNotifyPropertyChanged obj = new();

            Assert.Throws<ArgumentException>(() => obj.ToObservable(o => o.Number + 1));
        }
    }
}