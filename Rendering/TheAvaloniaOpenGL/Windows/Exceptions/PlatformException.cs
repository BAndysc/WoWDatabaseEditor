namespace SPB.Platform.Exceptions
{
    public class PlatformException : Exception
    {
        public PlatformException()
        {
        }

        public PlatformException(string message)
            : base(message)
        {
        }

        public PlatformException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
