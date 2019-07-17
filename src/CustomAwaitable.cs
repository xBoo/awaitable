using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace AwaitableTest {
    public class CustomAwaitable : INotifyCompletion {
        private bool isCompleted;
        public bool IsCompleted {
            get => isCompleted;
            set => isCompleted = value;
        }

        public void OnCompleted (Action continuation) {
            Console.WriteLine ($"Begin Invoke continuation action on completed,thread id is {Thread.CurrentThread.ManagedThreadId}");
            continuation?.Invoke ();
            Console.WriteLine ($"End Invoke");
        }

        public int GetResult () {
            Console.WriteLine ($"Get result,thread id is {Thread.CurrentThread.ManagedThreadId}");
            return 100;
        }

        public CustomAwaitable GetAwaiter () {
            Console.WriteLine ($"Get awatier,thread id is {Thread.CurrentThread.ManagedThreadId}");
            return this;
        }
    }
}