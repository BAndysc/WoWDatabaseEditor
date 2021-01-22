using System;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using WDE.MVVM.Observable;

namespace WDE.MVVM.Test.MVVM
{
    public class ObservableBaseTest
    {
        [Test]
        public void TestLinkObservable()
        {
            ReactiveProperty<int> property = new(4);
            ObservableObjectLinkObservable o = new(property);
            
            Assert.AreEqual(4, o.Number);

            property.Value = 2;
            
            Assert.AreEqual(2, o.Number);
            
            o.Dispose();
            
            Assert.AreEqual(0, property.SubscriptionsCount);
        }
        
        [Test]
        public void TestLinkNotifyProperty()
        {
            TestObjectNotifyPropertyChanged source = new() {Number = 4};
            ObservableObjectLinkNotifyPropertyChanged o = new(source);
            
            Assert.AreEqual(4, o.Number);

            source.Number = 2;
            
            Assert.AreEqual(2, o.Number);
            
            o.Dispose();
            
            Assert.AreEqual(0, source.EventHandlers);
        }
        
        
        [Test]
        public void TestWatch()
        {
            TestObjectNotifyPropertyChanged source = new() {Number = 4};
            ObservableObjectWatchNotifyPropertyChanged o = new(source);

            int calledPropertyChanged = 0;
            o.PropertyChanged += (_, _) => calledPropertyChanged++;
            
            source.Number = 2;
            source.Number = 3;
            source.Number = 4;

            Assert.AreEqual(3, calledPropertyChanged);
        }
        
        [ExcludeFromCodeCoverage]
        private class ObservableObjectLinkObservable : ObservableBase
        {
            public int Number { get; private set; }

            public ObservableObjectLinkObservable(IObservable<int> observable)
            {
                Link(observable, () => Number);
            }
        }
        
        [ExcludeFromCodeCoverage]
        private class ObservableObjectLinkNotifyPropertyChanged : ObservableBase
        {
            public int Number { get; private set; }

            public ObservableObjectLinkNotifyPropertyChanged(TestObjectNotifyPropertyChanged other)
            {
                Link(other, o => o.Number, () => Number);
            }
        }
        
        [ExcludeFromCodeCoverage]
        private class ObservableObjectWatchNotifyPropertyChanged : ObservableBase
        {
            public int Number { get; private set; }
            public ObservableObjectWatchNotifyPropertyChanged(TestObjectNotifyPropertyChanged other)
            {
                Watch(other, o => o.Number, nameof(Number));
            }
        }
    }
}