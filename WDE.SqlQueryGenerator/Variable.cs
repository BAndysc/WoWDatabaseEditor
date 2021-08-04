namespace WDE.SqlQueryGenerator
{
    internal class Variable : IVariable
    {
        private readonly string name;

        public Variable(string name)
        {
            this.name = name;
        }
        
        public override string ToString()
        {
            return "@" + name;
        }
    }
}