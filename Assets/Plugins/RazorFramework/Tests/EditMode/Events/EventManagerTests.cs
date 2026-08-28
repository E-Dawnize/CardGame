using System;
using NUnit.Framework;

namespace RazorFramework.Events.Tests
{
    public sealed class EventManagerTests
    {
        private struct TestEvent
        {
            public int Value;
        }

        [Test]
        public void Subscribe_Publish_DeliversToHandler()
        {
            using var events = new EventManager();
            TestEvent? received = null;

            events.Subscribe<TestEvent>(e => received = e);
            events.Publish(new TestEvent { Value = 7 });

            Assert.That(received?.Value, Is.EqualTo(7));
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            using var events = new EventManager();
            var calls = 0;
            Action<TestEvent> handler = _ => calls++;

            events.Subscribe<TestEvent>(handler);
            events.Unsubscribe<TestEvent>(handler);
            events.Publish(new TestEvent());

            Assert.That(calls, Is.Zero);
        }

        [Test]
        public void Publish_WithNoSubscribers_DoesNotThrow()
        {
            using var events = new EventManager();

            Assert.DoesNotThrow(() => events.Publish(new TestEvent()));
        }

        [Test]
        public void Subscribe_NullHandler_Throws()
        {
            using var events = new EventManager();

            Assert.Throws<ArgumentNullException>(() => events.Subscribe<TestEvent>(null));
        }
    }
}
