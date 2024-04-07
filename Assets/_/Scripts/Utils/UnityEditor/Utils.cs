using System;
using System.Collections;
using System.Threading;

namespace Utilities.Editor
{
    struct ProgressAuto : IDisposable
    {
        public int Id { get; }
        bool _isFinished;

        public readonly UnityEditor.Progress.Status Status => UnityEditor.Progress.GetStatus(Id);

        public readonly int CurrentStep => UnityEditor.Progress.GetCurrentStep(Id);

        public readonly string Description
        {
            get => UnityEditor.Progress.GetDescription(Id);
            set => UnityEditor.Progress.SetDescription(Id, value);
        }

        public readonly int Priority
        {
            get => UnityEditor.Progress.GetPriority(Id);
            set => UnityEditor.Progress.SetPriority(Id, value);
        }

        public readonly long RemainingTime
        {
            get => UnityEditor.Progress.GetRemainingTime(Id);
            set => UnityEditor.Progress.SetRemainingTime(Id, value);
        }

        public readonly string StepLabel
        {
            get => UnityEditor.Progress.GetStepLabel(Id);
            set => UnityEditor.Progress.SetStepLabel(Id, value);
        }

        public readonly UnityEditor.Progress.TimeDisplayMode TimeDisplayMode
        {
            get => UnityEditor.Progress.GetTimeDisplayMode(Id);
            set => UnityEditor.Progress.SetTimeDisplayMode(Id, value);
        }

        public readonly long EndDateTime => UnityEditor.Progress.GetEndDateTime(Id);
        public readonly string Name => UnityEditor.Progress.GetName(Id);
        public readonly UnityEditor.Progress.Options Options => UnityEditor.Progress.GetOptions(Id);
        public readonly int ParentId => UnityEditor.Progress.GetParentId(Id);
        public readonly float Progress => UnityEditor.Progress.GetProgress(Id);
        public readonly long StartDateTime => UnityEditor.Progress.GetStartDateTime(Id);
        public readonly int TotalSteps => UnityEditor.Progress.GetTotalSteps(Id);
        public readonly long UpdateDateTime => UnityEditor.Progress.GetUpdateDateTime(Id);

        public ProgressAuto(string name, string description = null, UnityEditor.Progress.Options options = UnityEditor.Progress.Options.None, int parentId = -1)
        {
            Id = UnityEditor.Progress.Start(name, description, options, parentId);
            _isFinished = false;
        }

        public readonly void ClearRemainingTime() => UnityEditor.Progress.ClearRemainingTime(Id);
        public readonly bool Cancel() => UnityEditor.Progress.Cancel(Id);
        public readonly bool Pause() => UnityEditor.Progress.Pause(Id);
        public readonly bool Resume() => UnityEditor.Progress.Resume(Id);
        public readonly void Report(float progress) => UnityEditor.Progress.Report(Id, progress);
        public readonly void Report(float progress, string description) => UnityEditor.Progress.Report(Id, progress, description);
        public readonly void Report(int currentStep, int totalSteps) => UnityEditor.Progress.Report(Id, currentStep, totalSteps);
        public readonly void Report(int currentStep, int totalSteps, string description) => UnityEditor.Progress.Report(Id, currentStep, totalSteps, description);

        public void Dispose()
        {
            if (_isFinished) return;
            UnityEditor.Progress.Status status = Status;
            if (status == UnityEditor.Progress.Status.Running)
            { status = UnityEditor.Progress.Status.Canceled; }
            UnityEditor.Progress.Finish(Id, status);
            _isFinished = true;
        }

        public void Finish(UnityEditor.Progress.Status status)
        {
            if (_isFinished) return;
            UnityEditor.Progress.Finish(Id, status);
            _isFinished = true;
        }
    }

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