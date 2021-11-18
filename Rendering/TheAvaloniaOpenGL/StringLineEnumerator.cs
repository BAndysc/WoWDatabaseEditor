namespace TheAvaloniaOpenGL
{
    internal struct StringLineEnumerator
    {
        private readonly string str;
        private int start;
        private int pos;
        private int prevPos;

        public StringLineEnumerator(string str)
        {
            this.str = str;
            pos = -1;
            start = 0;
            prevPos = -1;
        }

        public ReadOnlySpan<char> Current
        {
            get
            {
                var lastIsLineFeed = str[pos - 1] == '\r';
                return str.AsSpan(start, pos - start - (lastIsLineFeed ? 1 : 0));
            }
        }

        public ReadOnlySpan<char> Rest => str.AsSpan(pos + 1);

        public bool MoveNext()
        {
            pos = str.IndexOf('\n', pos + 1);
            if (pos == -1)
                return false;

            start = prevPos + 1;
            prevPos = pos;
            return true;
        }
    }
}