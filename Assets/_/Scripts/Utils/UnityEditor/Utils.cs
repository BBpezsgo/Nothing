using System;
using System.Collections;
using System.Threading;

namespace Utilities.Editor
{
    public static class CoroutineUtils
    {
        public static IEnumerator Task(Func<bool> isDone)
        {
            while (!isDone.Invoke())
            { yield return null; }
        }

        public static IEnumerator Task(ITask task)
        {
            while (task != null && !task.IsDone)
            { yield return null; }
        }
    }

    public interface ITask
    {
        public bool IsDone { get; }
    }

    public delegate void ParameterizedThreadStartSafe<T>(T obj);

    public class ThreadTask : ITask
    {
        readonly Thread Thread;
        public bool IsDone
        {
            get
            {
                if (Thread.IsAlive) return false;
                return true;
            }
        }

        public ThreadTask(Thread thread)
        {
            Thread = thread;
        }

        public static ThreadTask Start(ThreadStart task)
        {
            Thread thread = new(task);
            thread.Start();
            return new ThreadTask(thread);
        }

        public static ThreadTask Start<T>(ParameterizedThreadStartSafe<T> task, T parameter)
        {
            Thread thread = new(new ParameterizedThreadStart((param) => task.Invoke((T)param)));
            thread.Start(parameter);
            return new ThreadTask(thread);
        }

        public static ThreadTask Start(ThreadStart task, string name)
        {
            Thread thread = new(task) { Name = name };
            thread.Start();
            return new ThreadTask(thread);
        }

        public static ThreadTask Start<T>(ParameterizedThreadStartSafe<T> task, T parameter, string name)
        {
            Thread thread = new(new ParameterizedThreadStart((param) => task.Invoke((T)param))) { Name = name };
            thread.Start(parameter);
            return new ThreadTask(thread);
        }
    }
}