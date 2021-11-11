using System.Collections;

namespace TheEngine.Coroutines
{
    public static class EnumeratorExtensions
    {
        public static ContinueOnExceptionEnumerator ContinueOnException(this IEnumerator enumerator)
        {
            return new ContinueOnExceptionEnumerator() { Enumerator = enumerator };
        }
    }

    public struct ContinueOnExceptionEnumerator
    {
        public IEnumerator Enumerator;
    }
}