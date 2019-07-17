using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AwaitableTest {
    class Program {
        static async Task Main (string[] args) {
            Console.WriteLine ($"Begin main,thread id is {Thread.CurrentThread.ManagedThreadId}");
            int result = await new CustomAwaitable ();
            Console.WriteLine ($"End main，result is {result},thread id is {Thread.CurrentThread.ManagedThreadId}");

            Console.WriteLine("********************************");
            await new CustomeTask();
            await Task.Delay (Timeout.Infinite);
        }
    }
}