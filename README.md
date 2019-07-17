# 重新认识 async/await

提起.Net中的 async/await，相信很多.neter 第一反应都会是异步编程，其本质是语法糖，但继续追查下去，既然是语法糖，那么经过编译之后，真正的代码是什么样的，如何执行的？带着这些疑问，通过网上资料的查询，可以了解到编译之后，是通过实现 IAsyncStateMachine 的一个状态机来实现的，博客园里大神Jeffcky 已经说得很清楚了，传送门： https://www.cnblogs.com/CreateMyself/p/5983208.html

上述知识对我们理解 async/await 非常重要，但不是本文讨论的侧重点，触发笔者写这篇文章的初衷是：

####是否只有Task可以被await，await就一定是异步执行吗？
答案当然不是，google了一圈后发现，当一个类可以被await，必须满足以下条件：
>它必须包含 GetAwaiter() 方法（实例方法或者<u>扩展方法</u>） 
>GetAwaiter() 返回awatier实例，并且这个实例包含如下条件：
>>- 必须实现 INotifyCompletion 或者 ICriticalNotifyCompletion 接口
>>- 必须包含 IsCompleted 公共属性
>>- 必须包含 GetResult() 方法，返回void或者其他返回值

上述条件中INotifyCompletion 接口信息如下：

``` csharp
    //
    // 摘要:
    //     Represents an operation that schedules continuations when it completes.
    public interface INotifyCompletion
    {
        //
        // 摘要:
        //     Schedules the continuation action that&#39;s invoked when the instance completes.
        //
        // 参数:
        //   continuation:
        //     The action to invoke when the operation completes.
        //
        // 异常:
        //   T:System.ArgumentNullException:
        //     The continuation argument is null (Nothing in Visual Basic).
        void OnCompleted(Action continuation);
    }
```
重点上述对于参数 continuation 的解释：委托在操作完成之后调用。此处遗留一个问题：在谁的操作完成之后调用，是怎么调用的？

先把上述问题放一边，我们来自己写一个可以被await的类，并且观察前后执行的顺序以及是否存在线程切换：

```csharp
 public class Program {
        static async Task Main (string[] args) {
            Console.WriteLine ($"Begin awati,thread id is {Thread.CurrentThread.ManagedThreadId}");
            int result = await new CustomAwaitable ();
            Console.WriteLine ($"End await，result is {result},thread id is {Thread.CurrentThread.ManagedThreadId}");
            await Task.Delay (Timeout.Infinite);
        }
    }

    public class CustomAwaitable : INotifyCompletion {
        public void OnCompleted (Action continuation) {
            Console.WriteLine ($"Invoke continuation action on completed,thread id is {Thread.CurrentThread.ManagedThreadId}");
            continuation?.Invoke ();
        }

        public int GetResult () {
            Console.WriteLine ($"Get result,thread id is {Thread.CurrentThread.ManagedThreadId}");
            return 100;
        }

        public bool IsCompleted { get; set; }

        public CustomAwaitable GetAwaiter(){
            return this;
        }
    }
```
上述代码中，CustomAwaitable满足了可被await的所有条件，并且正常通过编译，运行后发现结果如下：

```
PS D:\git\awaitable\src> dotnet run
Begin main,thread id is 1
Get awatier,thread id is 1
Begin Invoke continuation action on completed,thread id is 1
Get result,thread id is 1
End main，result is 100,thread id is 1
End Invoke
```

>根据上述日志，可以看出：
>1. **执行前后线程并未发生切换，所以当我们不假思索的回答 await/async 就是异步编程时，至少是一个不太严谨的答案**
>
>2. 最后执行日志 "End Invoke" 表明：continuation action 这个委托，根据上述调用日志顺序可以大致理解为：编译器将await之后的代码封装为这个 action，在实例完成后调用OnCompleted方法执行了await 之后的代码（注：实际情况比较复杂，如果有多行await，会转换为一个状态机，具体参看文章开头给出的连接）。


了解了上述知识之后，那么我们常规所说的await异步编程又是怎么回事呢？先来看Task部分源码：
https://referencesource.microsoft.com/#mscorlib/system/threading/Tasks/Task.cs,045a746eb48cbaa9

``` csharp
  public struct TaskAwaiter : ICriticalNotifyCompletion
    {
        private readonly Task m_task;
        internal TaskAwaiter(Task task)
        {
            Contract.Requires(task != null, "Constructing an awaiter requires a task to await.");
            m_task = task;
        }

        public void GetResult()
        {
            ValidateEnd(m_task);
        }

        ....
    }
        
```

首先TaskAwaiter 必须接收一个Task参数，并且当调用 GetResult() 时，会等待Task完成并返回结果：

``` csharp
        internal static void ValidateEnd(Task task)
        {
            if (task.IsWaitNotificationEnabledOrNotRanToCompletion)
            {
                HandleNonSuccessAndDebuggerNotification(task);
            }
        }

        private static void HandleNonSuccessAndDebuggerNotification(Task task)
        {
            if (!task.IsCompleted)
            {
                bool taskCompleted = task.InternalWait(Timeout.Infinite, default(CancellationToken));
                Contract.Assert(taskCompleted, "With an infinite timeout, the task should have always completed.");
            }
 
            task.NotifyDebuggerOfWaitCompletionIfNecessary();
 
            // And throw an exception if the task is faulted or canceled.
            if (!task.IsRanToCompletion) ThrowForNonSuccess(task);
        }
```
上述if条件中，如果Task状态为未完成，则会一直等待该任务完成后进行回调后续操作。

