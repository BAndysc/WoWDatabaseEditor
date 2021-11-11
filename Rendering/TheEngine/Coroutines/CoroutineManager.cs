using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TheEngine.Coroutines
{
    public class WaitForTask
    {
        public readonly Task Task;

        public WaitForTask(Task task)
        {
            Task = task;
        }
    }
    
    public class CoroutineManager
    {
        private List<CoroutineState> coroutines = new();
        
        private class CoroutineState
        {
            public IEnumerator Coroutine;
            public bool Active;
            public CoroutineState? Parent;
            public Exception? NestedException;
            public bool IgnoreNestedExceptions;
        }

        public int PendingCoroutines => coroutines.Count;

        public void Start(IEnumerator coroutine)
        {
            var state = new CoroutineState()
            {
                Coroutine = coroutine,
                Active = true,
                Parent = null
            };
            if (CoroutineStep(state))
                coroutines.Add(state);
        }

        private bool StartNested(IEnumerator coroutine, CoroutineState parent)
        {
            var state = new CoroutineState()
            {
                Coroutine = coroutine,
                Active = true,
                Parent = parent
            };
            if (CoroutineStep(state))
            {
                coroutines.Add(state);
                return true;
            }
            return false;
        }

        private bool CoroutineStep(CoroutineState state)
        {
            bool hasContinuation = false;
            try
            {
                hasContinuation = state.Coroutine.MoveNext();
            }
            catch (Exception e)
            {
                if (state.Parent != null && !state.Parent.IgnoreNestedExceptions)
                {
                    state.Parent.NestedException = e;
                    state.Parent = null; // <-- so that it doesn't get activated
                }
                Console.WriteLine("Exception while running a continuation: ");
                Console.WriteLine(e);
            }
            if (hasContinuation)
            {
                var cur = state.Coroutine.Current;
                if (cur == null)
                {
                    return true;
                }
                else if (cur is ContinueOnExceptionEnumerator continueOnException)
                {
                    state.Active = false;
                    state.IgnoreNestedExceptions = true;
                    if (!StartNested(continueOnException.Enumerator, state) && state.NestedException == null)
                    {
                        state.Active = true;
                        return CoroutineStep(state);
                    }
                }
                else if (cur is IEnumerator nested)
                {
                    state.Active = false;
                    if (!StartNested(nested, state) && state.NestedException == null)
                    {
                        state.Active = true;
                        return CoroutineStep(state);
                    }
                }
                else if (cur is WaitForTask waitForTask)
                {
                    state.Active = false;
                    waitForTask.Task.ContinueWith(t =>
                    {
                        state.Active = true;
                    });
                } 
                else if (cur is Task t)
                {
                    state.Active = false;
                    t.ContinueWith(_ =>
                    {
                        state.Active = true;
                    });
                } else
                    throw new Exception("You can only yield null (For the next frame) or WaitForTask or another IEnumerator");

                return true;
            }
            else
            {
                if (state.Parent != null)
                    state.Parent.Active = true;
                return false;
            }
        }

        public void Step()
        {
            var active = 0;
            var inactive = 0;
            for (int i = coroutines.Count - 1; i >= 0; --i)
            {
                if (coroutines[i].Active)
                {
                    active++;
                    if (!CoroutineStep(coroutines[i]))
                        coroutines.RemoveAt(i);
                }
                else if (coroutines[i].NestedException != null)
                {
                    if (coroutines[i].Parent != null && !coroutines[i].Parent.IgnoreNestedExceptions)
                        coroutines[i].Parent.NestedException = coroutines[i].NestedException; // propagate
                    coroutines.RemoveAt(i);
                }
                else
                    inactive++;
            }
        }
    }
}