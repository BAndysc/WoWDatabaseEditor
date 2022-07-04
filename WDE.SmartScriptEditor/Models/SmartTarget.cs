using System;
using System.Globalization;
using System.Linq;
using SmartFormat;
using SmartFormat.Core.Formatting;
using SmartFormat.Core.Parsing;
using WDE.Common.Parameters;
using WDE.Parameters.Models;
using WDE.SmartScriptEditor.Editor;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartTarget : SmartSource
    {
        public SmartTarget(int id, IEditorFeatures features) : base(id, features, true)
        {
        }

        public new SmartTarget Copy()
        {
            SmartTarget se = new(Id, features)
            {
                ReadableHint = ReadableHint,
                DescriptionRules = DescriptionRules,
                IsPosition = IsPosition
            };
            
            se.CopyParameters(this);
            
            for (var i = 0; i < Position!.Length; ++i)
                se.Position![i].Copy(Position[i]);
            
            se.Conditions = Conditions?.ToList();
            
            return se;
        }
    }
}