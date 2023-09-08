namespace SPB.Graphics.Exceptions
{
    public class ContextException : Exception
    {
        public ContextException()
        {
        }

        public ContextException(string message)
            : base(message)
        {
        }

        public ContextException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}