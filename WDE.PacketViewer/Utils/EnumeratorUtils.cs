using System.Collections.Generic;

namespace WDE.PacketViewer.Utils;

public static class EnumeratorUtils
{
    public static IEnumerable<int> GetFindEnumerator(int start, int count, int direction, bool wrap)
    {
        for (int i = start + direction; i >= 0 && i < count; i += direction)
            yield return i;

        if (wrap)
        {
            if (direction > 0)
            {
                for (int i = 0; i < start; ++i)
                    yield return i;
            }
            else
            {
                for (int i = count - 1; i > start; --i)
                    yield return i;
            }
        }
    }

    /// <summary>
    /// The same as above, but unrolled into a struct for no allocations
    /// </summary>
    public struct FindEnumerator
    {
        private readonly int start;
        private readonly int count;
        private readonly int direction;
        private readonly bool wrap;
        private int currentIndex;
        private bool initialRun;
        private bool isWrapping;

        public FindEnumerator(int start, int count, int direction, bool wrap)
        {
            this.start = start;
            this.count = count;
            this.direction = direction;
            this.wrap = wrap;
            currentIndex = start + direction;
            initialRun = true;
            isWrapping = false;
        }

        public bool Next(out int index)
        {
            if (initialRun)
            {
                if (currentIndex >= 0 && currentIndex < count)
                {
                    index = currentIndex;
                    currentIndex += direction;
                    return true;
                }

                initialRun = false;
            }

            if (wrap && !isWrapping)
            {
                isWrapping = true;

                if (direction > 0)
                {
                    currentIndex = 0;
                }
                else
                {
                    currentIndex = count - 1;
                }
            }

            if (isWrapping)
            {
                if ((direction > 0 && currentIndex < start) || (direction < 0 && currentIndex > start))
                {
                    index = currentIndex;
                    currentIndex += direction;
                    return true;
                }
            }

            index = -1;
            return false;
        }
    }
}