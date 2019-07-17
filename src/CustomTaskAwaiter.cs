using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AwaitableTest {
    public class CustomTaskAwaiter : INotifyCompletion {
        private bool _isCompleted;
        private Task<int> _task;
        public CustomTaskAwaiter (Task<int> task) {
            this._task = task;
        }

        public bool IsCompleted {
            get => _isCompleted;
            set => _isCompleted = value;
        }

        public void OnCompleted (Action continuation) {
            Console.WriteLine ($"Begin Invoke continuation action on completed,thread id is {Thread.CurrentThread.ManagedThreadId}");
            continuation?.Invoke ();
            Console.WriteLine ($"End Invoke");
        }

        public int GetResult () {
            return this._task.GetAwaiter ().GetResult ();
        } 
    }

    public class CustomeTask{
        public CustomTaskAwaiter GetAwaiter(){
            return new CustomTaskAwaiter(Task.Run(()=>{
                Console.WriteLine ($"Return result,thread id is {Thread.CurrentThread.ManagedThreadId}");
                return Task.FromResult(100);
            }));
        }
    }
}