using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rrs.TaskShim
{
    public static class TaskExtensions
    {
        public static Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            var timeoutCancellationTokenSource = new CancellationTokenSource();

            return Task.Factory.ContinueWhenAny(new[] { task, Tap.Delay(timeout, timeoutCancellationTokenSource.Token) }, completedTask =>
            {
                if (completedTask == task)
                {
                    timeoutCancellationTokenSource.Cancel();
                    return task;  // Very important in order to propagate exceptions
                }
                else
                {
                    throw new TimeoutException("The operation has timed out.");
                }
            }).Unwrap();

        }

        public static Task TimeoutAfter(this Task task, TimeSpan timeout)
        {
            var timeoutCancellationTokenSource = new CancellationTokenSource();

            return Task.Factory.ContinueWhenAny(new[] { task, Tap.Delay(timeout, timeoutCancellationTokenSource.Token) }, completedTask =>
            {
                if (completedTask == task)
                {
                    timeoutCancellationTokenSource.Cancel();
                    return task;  // Very important in order to propagate exceptions
                }
                else
                {
                    throw new TimeoutException("The operation has timed out.");
                }
            }).Unwrap();
        }

        public static void DoNotAwait(this Task _) { }
    }
}
