// using System;
// using Infrastructure.EventAggregator.API.Events;
// using Infrastructure.EventAggregator.Internal.Core; // Assuming internal classes are accessible
// using Xunit;
//
// namespace Infrastructure.EventAggregator.Tests // Adjust namespace accordingly
// {
//     public class PriorityEventQueueTests
//     {
//         // Simple helper to create wrappers easily
//         private PrioritizedEventWrapper CreateWrapper(string data, EventPriority priority)
//         {
//             object eventData = priority switch
//             {
//                 EventPriority.Low => new PrioritizedTestEvent(data, EventPriority.Low),
//                 EventPriority.Normal => new PrioritizedTestEvent(data, EventPriority.Normal),
//                 EventPriority.High => new PrioritizedTestEvent(data, EventPriority.High),
//                 EventPriority.Critical => new PrioritizedTestEvent(data, EventPriority.Critical),
//                 _ => new TestEvent(data) // Fallback for simplicity
//             };
//             return new PrioritizedEventWrapper(eventData, eventData.GetType(), priority);
//         }
//
//         [Fact]
//         public void IsEmpty_WhenNew_ReturnsTrue()
//         {
//             // Arrange
//             var queue = new PriorityEventQueue();
//
//             // Act & Assert
//             Assert.True(queue.IsEmpty);
//         }
//
//         [Fact]
//         public void IsEmpty_AfterEnqueue_ReturnsFalse()
//         {
//             // Arrange
//             var queue = new PriorityEventQueue();
//             queue.Enqueue(CreateWrapper("Test", EventPriority.Normal));
//
//             // Act & Assert
//             Assert.False(queue.IsEmpty);
//         }
//
//         [Fact]
//         public void IsEmpty_AfterEnqueueAndDequeue_ReturnsTrue()
//         {
//             // Arrange
//             var queue = new PriorityEventQueue();
//             queue.Enqueue(CreateWrapper("Test", EventPriority.Normal));
//             queue.Dequeue();
//
//             // Act & Assert
//             Assert.True(queue.IsEmpty);
//         }
//
//         [Fact]
//         public void Dequeue_FromEmptyQueue_ReturnsNull()
//         {
//             // Arrange
//             var queue = new PriorityEventQueue();
//
//             // Act
//             var result = queue.Dequeue();
//
//             // Assert
//             Assert.Null(result);
//         }
//
//         [Fact]
//         public void Dequeue_SingleItem_ReturnsItemAndMakesQueueEmpty()
//         {
//             // Arrange
//             var queue = new PriorityEventQueue();
//             var wrapper = CreateWrapper("Test", EventPriority.Normal);
//             queue.Enqueue(wrapper);
//
//             // Act
//             var result = queue.Dequeue();
//
//             // Assert
//             Assert.Same(wrapper, result);
//             Assert.True(queue.IsEmpty);
//         }
//
//         [Fact]
//         public void Dequeue_MultipleItemsWithDifferentPriorities_ReturnsHighestPriorityFirst()
//         {
//             // Arrange
//             var queue = new PriorityEventQueue();
//             var low = CreateWrapper("L", EventPriority.Low);
//             var normal = CreateWrapper("N", EventPriority.Normal);
//             var high = CreateWrapper("H", EventPriority.High);
//             var critical = CreateWrapper("C", EventPriority.Critical);
//
//             queue.Enqueue(low);
//             queue.Enqueue(high);
//             queue.Enqueue(critical);
//             queue.Enqueue(normal);
//
//             // Act & Assert
//             Assert.Same(critical, queue.Dequeue());
//             Assert.Same(high, queue.Dequeue());
//             Assert.Same(normal, queue.Dequeue());
//             Assert.Same(low, queue.Dequeue());
//             Assert.True(queue.IsEmpty);
//         }
//
//         [Fact]
//         public void Dequeue_MultipleItemsWithSamePriority_ReturnsInFIFOOrder()
//         {
//             // Arrange
//             var queue = new PriorityEventQueue();
//             var high1 = CreateWrapper("H1", EventPriority.High);
//             var high2 = CreateWrapper("H2", EventPriority.High);
//             var normal1 = CreateWrapper("N1", EventPriority.Normal);
//
//             queue.Enqueue(normal1);
//             queue.Enqueue(high1); // Enqueue H1 first
//             queue.Enqueue(high2); // Enqueue H2 second
//
//
//             // Act & Assert
//             Assert.Same(high1, queue.Dequeue()); // Dequeue H1 first
//             Assert.Same(high2, queue.Dequeue()); // Dequeue H2 second
//             Assert.Same(normal1, queue.Dequeue());
//             Assert.True(queue.IsEmpty);
//         }
//
//         [Fact]
//         public void Peek_MultipleItems_ReturnsHighestPriorityWithoutRemoving()
//         {
//             // Arrange
//             var queue = new PriorityEventQueue();
//             var low = CreateWrapper("L", EventPriority.Low);
//             var high = CreateWrapper("H", EventPriority.High);
//
//             queue.Enqueue(low);
//             queue.Enqueue(high);
//
//             // Act
//             var peek1 = queue.Peek();
//             var peek2 = queue.Peek(); // Peek again
//             bool wasEmpty = queue.IsEmpty; // Check state after peeking
//             var dequeued = queue.Dequeue();
//             var nextPeek = queue.Peek();
//
//             // Assert
//             Assert.Same(high, peek1);
//             Assert.Same(high, peek2); // Peeking is idempotent
//             Assert.False(wasEmpty); // Peeking didn't empty it
//             Assert.Same(high, dequeued); // Dequeue gets the same item
//             Assert.Same(low, nextPeek); // Next item is now low pri
//         }
//
//         [Fact]
//         public void Clear_RemovesAllItems()
//         {
//             // Arrange
//            var queue = new PriorityEventQueue();
//            queue.Enqueue(CreateWrapper("L", EventPriority.Low));
//            queue.Enqueue(CreateWrapper("H", EventPriority.High));
//            Assert.False(queue.IsEmpty); // Verify not empty beforehand
//
//            // Act
//            queue.Clear();
//
//            // Assert
//            Assert.True(queue.IsEmpty);
//            Assert.Null(queue.Dequeue()); // Verify dequeue returns null now
//         }
//     }
// }