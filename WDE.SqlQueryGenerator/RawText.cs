namespace WDE.SqlQueryGenerator
{
    internal class RawText : IRawText
    {
        private readonly string text;

        public RawText(string text)
        {
            this.text = text;
        }
        
        public override string ToString()
        {
            return text;
        }
    }
}