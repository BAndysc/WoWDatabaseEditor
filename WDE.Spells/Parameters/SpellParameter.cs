using WDE.Common.Parameters;

namespace WDE.Spells.Parameters
{
    internal class SpellParameter : ParameterNumbered
    {
        public SpellParameter(IParameter<long> dbc, IParameter<long> db)
        {
            Items = new();
            if (dbc.Items != null)
            {
                foreach (var i in dbc.Items)
                    Items[i.Key] = i.Value;
            }
            
            if (db.Items != null)
            {
                foreach (var i in db.Items)
                    Items[i.Key] = i.Value;
            }
        }
    }
}