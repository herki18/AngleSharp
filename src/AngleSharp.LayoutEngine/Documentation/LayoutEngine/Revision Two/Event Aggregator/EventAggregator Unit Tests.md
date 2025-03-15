```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Common.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AngleSharp.Tests.Events
{
    [TestClass]
    public class EventAggregatorTests
    {
        #region Test Events

        public class TestEvent
        {
            public string Message { get; set; } = string.Empty;
        }

        public class TestEventWithPayload
        {
            public int Payload { get; set; }
        }

        public class DerivedTestEvent : TestEvent
        {
            public string AdditionalInfo { get; set; } = string.Empty;
        }

        #endregion

        [TestMethod]
        public void Subscribe_AndPublish_EventIsReceived()
        {
            // Arrange
            var eventAggregator = new EventAggregator();
            var received = false;
            var testMessage = "Test Message";
            
            eventAggregator.Subscribe<TestEvent>(e => {
                received = true;
                Assert.AreEqual(testMessage, e.Message);
            });
            
            // Act
            eventAggregator.Publish(new TestEvent { Message = testMessage });
            
            // Assert
            Assert.IsTrue(received, "Event was not received by subscriber");
        }

        [TestMethod]
        public void SubscribeWithFilter_WhenFilterMatches_EventIsReceived()
        {
            // Arrange
            var eventAggregator = new EventAggregator();
            var received = false;
            var testPayload = 42;
            
            eventAggregator.Subscribe<TestEventWithPayload>(
                e => e.Payload > 40,
                e => {
                    received = true;
                    Assert.AreEqual(testPayload, e.Payload);
                });
            
            // Act
            eventAggregator.Publish(new TestEventWithPayload { Payload = testPayload });
            
            // Assert
            Assert.IsTrue(received, "Event was not received by filtered subscriber");
        }
        
        [TestMethod]
        public void SubscribeWithFilter_WhenFilterDoesNotMatch_EventIsNotReceived()
        {
            // Arrange
            var eventAggregator = new EventAggregator();
            var received = false;
            
            eventAggregator.Subscribe<TestEventWithPayload>(
                e => e.Payload > 50,
                _ => received = true);
            
            // Act
            eventAggregator.Publish(new TestEventWithPayload { Payload = 42 });
            
            // Assert
            Assert.IsFalse(received, "Event was incorrectly received by filtered subscriber");
        }
        
        [TestMethod]
        public void Subscribe_WhenMultipleSubscribers_AllReceiveEvent()
        {
            // Arrange
            var eventAggregator = new EventAggregator();
            var subscriber1Received = false;
            var subscriber2Received = false;
            var testMessage = "Test Message";
            
            eventAggregator.Subscribe<TestEvent>(_ => subscriber1Received = true);
            eventAggregator.Subscribe<TestEvent>(_ => subscriber2Received = true);
            
            // Act
            eventAggregator.Publish(new TestEvent { Message = testMessage });
            
            // Assert
            Assert.IsTrue(subscriber1Received, "Event was not received by first subscriber");
            Assert.IsTrue(subscriber2Received, "Event was not received by second subscriber");
        }
        
        [TestMethod]
        public void Unsubscribe_SubscriberDoesNotReceiveEvents()
        {
            // Arrange
            var eventAggregator = new EventAggregator();
            var received = false;
            
            var token = eventAggregator.Subscribe<TestEvent>(_ => received = true);
            
            // Act
            token.Dispose(); // Unsubscribe
            eventAggregator.Publish(new TestEvent());
            
            // Assert
            Assert.IsFalse(received, "Event was incorrectly received after unsubscribing");
        }
        
        [TestMethod]
        public void ClearSubscriptions_NoSubscribersReceiveEvents()
        {
            // Arrange
            var eventAggregator = new EventAggregator();
            var subscriber1Received = false;
            var subscriber2Received = false;
            
            eventAggregator.Subscribe<TestEvent>(_ => subscriber1Received = true);
            eventAggregator.Subscribe<TestEventWithPayload>(_ => subscriber2Received = true);
            
            // Act
            eventAggregator.ClearSubscriptions<TestEvent>();
            eventAggregator.Publish(new TestEvent());
            eventAggregator.Publish(new TestEventWithPayload());
            
            // Assert
            Assert.IsFalse(subscriber1Received, "TestEvent was incorrectly received after clearing");
            Assert.IsTrue(subscriber2Received, "TestEventWithPayload should still be received");
        }
        
        [TestMethod]
        public void ClearAllSubscriptions_NoSubscribersReceiveEvents()
        {
            // Arrange
            var eventAggregator = new EventAggregator();
            var subscriber1Received = false;
            var subscriber2Received = false;
            
            eventAggregator.Subscribe<TestEvent>(_ => subscriber1Received = true);
            eventAggregator.Subscribe<TestEventWithPayload>(_ => subscriber2Received = true);
            
            // Act
            eventAggregator.ClearAllSubscriptions();
            eventAggregator.Publish(new TestEvent());
            eventAggregator.Publish(new TestEventWithPayload());
            
            // Assert
            Assert.IsFalse(subscriber1Received, "TestEvent was incorrectly received after clearing all");
            Assert.IsFalse(subscriber2Received, "TestEventWithPayload was incorrectly received after clearing all");
        }
        
        [TestMethod]
        public void ExceptionInHandler_DoesNotAffectOtherHandlers()
        {
            // Arrange
            var eventAggregator = new EventAggregator();
            var subscriber2Received = false;
            
            eventAggregator.Subscribe<TestEvent>(_ => throw new InvalidOperationException("Test exception"));
            eventAggregator.Subscribe<TestEvent>(_ => subscriber2Received = true);
            
            // Act & Assert
            // Should not throw, and second subscriber should still receive the event
            eventAggregator.Publish(new TestEvent());
            
            Assert.IsTrue(subscriber2Received, "Second subscriber did not receive event after first handler exception");
        }
        
        [TestMethod]
        public void ParallelPublish_ThreadSafety()
        {
            // Arrange
            var eventAggregator = new EventAggregator();
            var receiveCount = 0;
            const int publishCount = 100;
            
            eventAggregator.Subscribe<TestEventWithPayload>(_ => Interlocked.Increment(ref receiveCount));
            
            // Act
            Parallel.For(0, publishCount, i => {
                eventAggregator.Publish(new TestEventWithPayload { Payload = i });
            });
            
            // Assert
            Assert.AreEqual(publishCount, receiveCount, "Not all published events were received");
        }
        
        [TestMethod]
        public void ParallelSubscribeAndPublish_ThreadSafety()
        {
            // Arrange
            var eventAggregator = new EventAggregator();
            var receiveCount = 0;
            const int threadCount = 10;
            const int eventsPerThread = 10;
            var subscriptions = new List<ISubscriptionToken>();
            
            // Act
            var tasks = new Task[threadCount * 2]; // Half subscribe, half publish
            
            for (int i = 0; i < threadCount; i++)
            {
                var threadIndex = i; // Capture for lambda
                
                // Subscribe task
                tasks[i] = Task.Run(() => {
                    var token = eventAggregator.Subscribe<TestEventWithPayload>(e => {
                        if (e.Payload % threadCount == threadIndex)
                        {
                            Interlocked.Increment(ref receiveCount);
                        }
                    });
                    
                    lock (subscriptions)
                    {
                        subscriptions.Add(token);
                    }
                });
                
                // Publish task
                tasks[i + threadCount] = Task.Run(() => {
                    for (int j = 0; j < eventsPerThread; j++)
                    {
                        eventAggregator.Publish(new TestEventWithPayload { 
                            Payload = threadIndex * eventsPerThread + j 
                        });
                    }
                });
            }
            
            Task.WaitAll(tasks);
            
            // Assert
            Assert.AreEqual(threadCount * eventsPerThread, receiveCount, 
                "Expected each event to be received exactly once");
            
            // Cleanup
            foreach (var subscription in subscriptions)
            {
                subscription.Dispose();
            }
        }
        
        [TestMethod]
        public void SubscriptionToken_CanBeDisposedMultipleTimes()
        {
            // Arrange
            var eventAggregator = new EventAggregator();
            var received = false;
            
            var token = eventAggregator.Subscribe<TestEvent>(_ => received = true);
            
            // Act
            token.Dispose(); // Unsubscribe
            token.Dispose(); // Should not throw
            eventAggregator.Publish(new TestEvent());
            
            // Assert
            Assert.IsFalse(received, "Event was incorrectly received after unsubscribing");
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Publish_NullEvent_ThrowsArgumentNullException()
        {
            // Arrange
            var eventAggregator = new EventAggregator();
            
            // Act
            eventAggregator.Publish<TestEvent>(null!);
            
            // Assert: ExpectedException attribute handles the assertion
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Subscribe_NullHandler_ThrowsArgumentNullException()
        {
            // Arrange
            var eventAggregator = new EventAggregator();
            
            // Act
            eventAggregator.Subscribe<TestEvent>(null!);
            
            // Assert: ExpectedException attribute handles the assertion
        }
    }
}
```