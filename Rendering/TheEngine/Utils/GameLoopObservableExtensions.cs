namespace TheEngine.Utils;

public static class GameLoopObservableExtensions
{
    public static IObservable<T> ObserveOnGameLoop<T>(this IObservable<T> observable)
    {
        if (SynchronizationContext.Current is not TheEngineSynchronizationContext sc)
        {
            throw new Exception(
                "You can only call ObserveOnGameLoop in a game loop, otherwise we don't know what the engine is.");
        }

        return new GameLoopObservable<T>(observable, sc);
    }

    private sealed class GameLoopObservable<T> : IObservable<T>
    {
        private readonly IObservable<T> _source;
        private readonly TheEngineSynchronizationContext _sc;

        public GameLoopObservable(IObservable<T> source, TheEngineSynchronizationContext sc)
        {
            _source = source;
            _sc = sc;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var gameLoopObserver = new GameLoopObserver<T>(observer, _sc);
            // Must attach upstream after creation to avoid missing synchronous emissions
            gameLoopObserver.SetSubscription(_source.Subscribe(gameLoopObserver));
            return gameLoopObserver;
        }
    }

    private sealed class GameLoopObserver<T> : IObserver<T>, IDisposable
    {
        // Cache the delegate statically to avoid a delegate allocation per subscription
        private static readonly SendOrPostCallback _drainCallback = Drain;

        private readonly IObserver<T> _downstream;
        private readonly TheEngineSynchronizationContext _sc;

        // A standard Queue inside a lock is faster and allocates less than ConcurrentQueue
        // in its steady state, preventing segment node allocations.
        private readonly Queue<T> _queue = new Queue<T>();

        private Exception _error;
        private bool _completed;
        private IDisposable _upstream;

        private int _isScheduled;
        private int _isDisposed;

        public GameLoopObserver(IObserver<T> downstream, TheEngineSynchronizationContext sc)
        {
            _downstream = downstream;
            _sc = sc;
        }

        public void SetSubscription(IDisposable upstream)
        {
            _upstream = upstream;
            // Handle edge case where subscription is immediately disposed synchronously
            if (Volatile.Read(ref _isDisposed) == 1)
            {
                _upstream.Dispose();
            }
        }

        public void OnNext(T value)
        {
            if (Volatile.Read(ref _isDisposed) == 1) return;

            bool needsSchedule = false;
            lock (_queue)
            {
                if (_isDisposed == 1) return;

                _queue.Enqueue(value);

                if (_isScheduled == 0)
                {
                    _isScheduled = 1;
                    needsSchedule = true;
                }
            }

            // Post outside the lock to prevent deadlocks with complex SynchronizationContexts
            if (needsSchedule)
            {
                _sc.Post(_drainCallback, this);
            }
        }

        public void OnError(Exception error)
        {
            if (Volatile.Read(ref _isDisposed) == 1) return;

            bool needsSchedule = false;
            lock (_queue)
            {
                if (_isDisposed == 1) return;

                _error = error;

                if (_isScheduled == 0)
                {
                    _isScheduled = 1;
                    needsSchedule = true;
                }
            }

            if (needsSchedule)
            {
                _sc.Post(_drainCallback, this);
            }
        }

        public void OnCompleted()
        {
            if (Volatile.Read(ref _isDisposed) == 1) return;

            bool needsSchedule = false;
            lock (_queue)
            {
                if (_isDisposed == 1) return;

                _completed = true;

                if (_isScheduled == 0)
                {
                    _isScheduled = 1;
                    needsSchedule = true;
                }
            }

            if (needsSchedule)
            {
                _sc.Post(_drainCallback, this);
            }
        }

        private static void Drain(object state)
        {
            var self = (GameLoopObserver<T>)state;
            self.DrainLoop();
        }

        private void DrainLoop()
        {
            while (true)
            {
                T item = default;
                bool hasItem = false;
                Exception err = null;
                bool comp = false;

                lock (_queue)
                {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP
                    hasItem = _queue.TryDequeue(out item);
#else
                    if (_queue.Count > 0)
                    {
                        item = _queue.Dequeue();
                        hasItem = true;
                    }
#endif
                    if (!hasItem)
                    {
                        err = _error;
                        comp = _completed;

                        // If the queue is empty and the sequence is still active, reset scheduling flag.
                        if (err == null && !comp)
                        {
                            _isScheduled = 0;
                            return; // Wait for the next OnNext to schedule a new Drain
                        }
                    }
                }

                // Process callbacks outside the lock to prevent deadlocking the engine
                if (hasItem)
                {
                    _downstream.OnNext(item);
                }
                else if (err != null)
                {
                    _downstream.OnError(err);
                    Dispose();
                    return;
                }
                else if (comp)
                {
                    _downstream.OnCompleted();
                    Dispose();
                    return;
                }
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
            {
                _upstream?.Dispose();

                // Clear lingering items to avoid memory leaks of cached reference types
                lock (_queue)
                {
                    _queue.Clear();
                }
            }
        }
    }
}