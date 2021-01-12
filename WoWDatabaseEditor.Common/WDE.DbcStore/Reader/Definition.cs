using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using static WDBXEditor.Common.Constants;

namespace WDBXEditor.Storage
{
    [Serializable]
    public class Definition
    {
        [XmlIgnore]
        private bool loading;

        [XmlElement("Table")]
        public HashSet<Table> Tables { get; set; } = new();

        [XmlIgnore]
        public int Build { get; set; }

        public bool LoadDefinition(string path)
        {
            if (loading)
                return true;

            try
            {
                XmlSerializer deser = new(typeof(Definition));
                using (FileStream fs = new FileStream(path, FileMode.Open))
                {
                    Definition def = (Definition) deser.Deserialize(fs);
                    var newtables = def.Tables.Where(x => Tables.Count(y => x.Build == y.Build && x.Name == y.Name) == 0).ToList();
                    newtables.ForEach(x => x.Load());
                    Tables.UnionWith(newtables.Where(x => x.Key != null));
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool SaveDefinitions()
        {
            string ValidFilename(string b)
            {
                return string.Join("_", b.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.') +
                       ".xml";
            }

            try
            {
                loading = true;

                var builds = Tables.OrderBy(x => x.Name).GroupBy(x => x.Build).ToList();
                Tables.Clear();
                foreach (var build in builds)
                {
                    Definition def = new()
                    {
                        Build = build.Key,
                        Tables = new HashSet<Table>(build)
                    };

                    XmlSerializer ser = new(typeof(Definition));
                    using (FileStream fs = new FileStream(Path.Combine(DefinitionDir, ValidFilename(BuildText(build.Key))),
                        FileMode.Create))
                    {
                        ser.Serialize(fs, def);
                    }
                }

                loading = false;
                return true;
            }
            catch (Exception)
            {
                loading = false;
                return false;
            }
        }
    }

    [Serializable]
    public class Table
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public int Build { get; set; }

        [XmlElement("Field")]
        public List<Field> Fields { get; set; }

        [XmlIgnore]
        public Field Key { get; private set; }

        [XmlIgnore]
        public bool Changed { get; set; } = false;

        [XmlIgnore]
        public string BuildText { get; private set; }

        public void Load()
        {
            Key = Fields.FirstOrDefault(x => x.IsIndex);
            BuildText = BuildText(Build);
        }
    }

    [Serializable]
    public class Field
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Type { get; set; }

        [XmlAttribute]
        [DefaultValue(1)]
        public int ArraySize { get; set; } = 1;

        [XmlAttribute]
        [DefaultValue(false)]
        public bool IsIndex { get; set; }

        [XmlAttribute]
        [DefaultValue(false)]
        public bool AutoGenerate { get; set; }

        [XmlAttribute]
        [DefaultValue("")]
        public string DefaultValue { get; set; } = "";

        [XmlAttribute]
        [DefaultValue("")]
        public string ColumnNames { get; set; } = "";

        [XmlIgnore]
        public string InternalName { get; set; }
    }
}