using System.Collections;

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
            if (state.Coroutine.MoveNext())
            {
                var cur = state.Coroutine.Current;
                if (cur == null)
                {
                    return true;
                }
                else if (cur is IEnumerator nested)
                {
                    state.Active = false;
                    if (!StartNested(nested, state))
                        return CoroutineStep(state);
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
            for (int i = coroutines.Count - 1; i >= 0; --i)
            {
                if (coroutines[i].Active)
                {
                    if (!CoroutineStep(coroutines[i]))
                        coroutines.RemoveAt(i);
                }
            }
        }
    }
}