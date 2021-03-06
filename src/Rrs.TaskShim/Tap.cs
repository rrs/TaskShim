﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Rrs.TaskShim
{
    public static class Tap
    {
        public static Task Run(Action a, CancellationToken token = default(CancellationToken))
        {
            return Task.Factory.StartNew(a, token, TaskCreationOptions.None, TaskScheduler.Default);
        }

        public static Task<T> Run<T>(Func<T> f, CancellationToken token = default(CancellationToken))
        {
            return Task.Factory.StartNew(f, token, TaskCreationOptions.None, TaskScheduler.Default);
        }

        public static Task Run(Func<Task> f, CancellationToken token = default(CancellationToken))
        {
            return Task.Factory.StartNew(f, token, TaskCreationOptions.None, TaskScheduler.Default).Unwrap();
        }

        public static Task<T> Run<T>(Func<Task<T>> f, CancellationToken token = default(CancellationToken))
        {
            return Task.Factory.StartNew(f, token, TaskCreationOptions.None, TaskScheduler.Default).Unwrap();
        }

        public static Task Delay(TimeSpan time)
        {
            return Delay((int)time.TotalMilliseconds);
        }

        public static Task Delay(TimeSpan time, CancellationToken token)
        {
            return Delay((int)time.TotalMilliseconds, token);
        }

        public static Task Delay(int milliseconds)
        {
            var tcs = new TaskCompletionSource<object>();
            Timer t = null;
            t = new Timer(_ =>
            {
                t.Dispose();
                tcs.SetResult(null);
            });

            t.Change(milliseconds, Timeout.Infinite);

            return tcs.Task;
        }

        public static Task Delay(int milliseconds, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<object>();
            Timer t = null;
            t = new Timer(_ =>
            {
                t.Dispose();
                tcs.TrySetResult(null);
            });

            token.Register(() =>
            {
                t.Dispose();
                tcs.TrySetCanceled();
            });

            t.Change(milliseconds, Timeout.Infinite);

            return tcs.Task;
        }

        public static Task<T> FromResult<T>(T value)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(value);
            return tcs.Task;
        }

        public static Task CompletedTask => CompletedTaskImpl();

        private static Task CompletedTaskImpl()
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetResult(null);
            return tcs.Task;
        }


        public static Task IterateTasks(IEnumerable<Task> asyncIterator)
        {
            if (asyncIterator == null) throw new ArgumentNullException(nameof(asyncIterator));

            var enumerator = asyncIterator.GetEnumerator();
            if (enumerator == null) throw new InvalidOperationException("Invalid enumerable - GetEnumerator returned null");

            var tcs = new TaskCompletionSource<object>();
            tcs.Task.ContinueWith(_ => enumerator.Dispose(), TaskContinuationOptions.ExecuteSynchronously);

            Action<Task> recursiveBody = null;
            recursiveBody = delegate {
                try
                {
                    if (enumerator.MoveNext()) enumerator.Current.ContinueWith(recursiveBody, TaskContinuationOptions.ExecuteSynchronously);
                    else tcs.TrySetResult(null);
                }
                catch (Exception exc) { tcs.TrySetException(exc); }
            };

            recursiveBody(null);
            return tcs.Task;
        }
    }
}
