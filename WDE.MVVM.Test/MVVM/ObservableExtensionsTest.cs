using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using WDE.MVVM.Observable;

namespace WDE.MVVM.Test.MVVM
{
    public class ObservableExtensionsTest
    {
        [Test]
        public void TestSubscribeDelegate()
        {
            ReactiveProperty<int> property = new ReactiveProperty<int>(1);

            int receivedValue = 0;

            using (property.SubscribeAction(v => receivedValue = v))
            {
                Assert.AreEqual(1, receivedValue);
                property.Value = 2;
                Assert.AreEqual(2, receivedValue);                
            }
            
            Assert.AreEqual(0, property.SubscriptionsCount);
        }

        [Test]
        public void TestSubscribeINotify()
        {
            TestObjectNotifyPropertyChanged obj = new() {Number = 3};

            int receivedValue = -1;

            using (obj.Subscribe(o => o.Number, v => { receivedValue = v; }))
            {
                Assert.AreEqual(1, obj.EventHandlers);
                Assert.AreEqual(3, receivedValue);

                obj.Number = 2;
                
                Assert.AreEqual(2, receivedValue);
            }
            
            Assert.AreEqual(0, obj.EventHandlers);
        }
    }
}