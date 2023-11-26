#nullable enable

using System;

namespace Utilities
{

    [Serializable]
    public class TaskResultException : Exception
    {
        public TaskResultException(string message) : base(message)
        {
        }
    }

    public readonly struct TaskResult<TResult, TError>
    {
        public readonly TResult? Result;
        public readonly TError? Error;

        public bool IsSucceed => Result != null;
        public bool IsFailed => Error != null;

        TaskResult(TResult result)
        {
            Result = result;
            Error = default;
        }

        TaskResult(TError error)
        {
            Result = default;
            Error = error;
        }

        public static implicit operator TaskResult<TResult, TError>(TResult result) => new(result);
        public static implicit operator TaskResult<TResult, TError>(TError error) => new(error);

        public static implicit operator TResult(TaskResult<TResult, TError> taskResult) => taskResult.Result ?? throw new TaskResultException($"Task has failed");
        public static implicit operator TError(TaskResult<TResult, TError> taskResult) => taskResult.Error ?? throw new TaskResultException($"Task has succeed");
    }
}