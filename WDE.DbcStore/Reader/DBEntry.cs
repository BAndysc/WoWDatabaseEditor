using WDBXEditor.Common;
using WDBXEditor.Reader;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using static WDBXEditor.Common.Constants;
using WDBXEditor.Reader.FileTypes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.MemoryMappedFiles;
using System.Security.AccessControl;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

namespace WDBXEditor.Storage
{
	public class DBEntry : IDisposable
	{
		public DBHeader Header { get; private set; }
		public DataTable Data { get; set; }
		public bool Changed { get; set; } = false;
		public string FilePath { get; private set; }
		public string FileName => Path.GetFileName(this.FilePath);
		public string SavePath { get; set; }
		public Table TableStructure => Header.TableStructure;

		public string Key { get; private set; }
		public int Build { get; private set; }
		public string BuildName { get; private set; }
		public string Tag { get; private set; }


		private int min = -1;
		private int max = -1;
		private IEnumerable<int> unqiueRowIndices;
		private IEnumerable<int> primaryKeys;


		public DBEntry(DBHeader header, string filepath)
		{
			this.Header = header;
			this.FilePath = filepath;
			this.SavePath = filepath;
			this.Header.TableStructure = Database.Definitions.Tables.FirstOrDefault(x =>
										  x.Name.Equals(Path.GetFileNameWithoutExtension(filepath), IGNORECASE) &&
										  x.Build == Database.BuildNumber);

			LoadDefinition();
		}


		/// <summary>
		/// Converts the XML definition to an empty DataTable
		/// </summary>
		public void LoadDefinition()
		{
			if (TableStructure == null)
				return;

			Build = TableStructure.Build;
			Key = TableStructure.Key.Name;
			BuildName = BuildText(Build);
			Tag = Guid.NewGuid().ToString();

			//Column name check
			if (TableStructure.Fields.GroupBy(x => x.Name).Any(y => y.Count() > 1))
			{
				MessageBox.Show($"Duplicate column names for {FileName} - {Build} definition");
				return;
			}

			LoadTableStructure();
		}

		public void LoadTableStructure()
		{
			Data = new DataTable() { TableName = Tag, CaseSensitive = false, RemotingFormat = SerializationFormat.Binary };

			var LocalizationCount = (Build <= (int)ExpansionFinalBuild.Classic ? 9 : 17); //Pre TBC had 9 locales

			foreach (var col in TableStructure.Fields)
			{
				Queue<TextWowEnum> languages = new Queue<TextWowEnum>(Enum.GetValues(typeof(TextWowEnum)).Cast<TextWowEnum>());
				string[] columnsNames = col.ColumnNames.Split(',');

				for (int i = 0; i < col.ArraySize; i++)
				{
					string columnName = col.Name;

					if (col.ArraySize > 1)
					{
						if (columnsNames.Length >= (i + 1) && !string.IsNullOrWhiteSpace(columnsNames[i]))
							columnName = columnsNames[i];
						else
							columnName += "_" + (i + 1);
					}

					col.InternalName = columnName;

					switch (col.Type.ToLower())
					{
						case "sbyte":
							Data.Columns.Add(columnName, typeof(sbyte));
							Data.Columns[columnName].DefaultValue = 0;
							break;
						case "byte":
							Data.Columns.Add(columnName, typeof(byte));
							Data.Columns[columnName].DefaultValue = 0;
							break;
						case "int32":
						case "int":
							Data.Columns.Add(columnName, typeof(int));
							Data.Columns[columnName].DefaultValue = 0;
							break;
						case "uint32":
						case "uint":
							Data.Columns.Add(columnName, typeof(uint));
							Data.Columns[columnName].DefaultValue = 0;
							break;
						case "int64":
						case "long":
							Data.Columns.Add(columnName, typeof(long));
							Data.Columns[columnName].DefaultValue = 0;
							break;
						case "uint64":
						case "ulong":
							Data.Columns.Add(columnName, typeof(ulong));
							Data.Columns[columnName].DefaultValue = 0;
							break;
						case "single":
						case "float":
							Data.Columns.Add(columnName, typeof(float));
							Data.Columns[columnName].DefaultValue = 0;
							break;
						case "boolean":
						case "bool":
							Data.Columns.Add(columnName, typeof(bool));
							Data.Columns[columnName].DefaultValue = 0;
							break;
						case "string":
							Data.Columns.Add(columnName, typeof(string));
							Data.Columns[columnName].DefaultValue = string.Empty;
							break;
						case "int16":
						case "short":
							Data.Columns.Add(columnName, typeof(short));
							Data.Columns[columnName].DefaultValue = 0;
							break;
						case "uint16":
						case "ushort":
							Data.Columns.Add(columnName, typeof(ushort));
							Data.Columns[columnName].DefaultValue = 0;
							break;
						case "loc":
							//Special case for localized strings, build up all locales and add string mask
							for (int x = 0; x < LocalizationCount; x++)
							{
								if (x == LocalizationCount - 1)
								{
									Data.Columns.Add(col.Name + "_Mask", typeof(uint)); //Last column is a mask
									Data.Columns[col.Name + "_Mask"].AllowDBNull = false;
									Data.Columns[col.Name + "_Mask"].DefaultValue = 0;
								}
								else
								{
									columnName = col.Name + "_" + languages.Dequeue().ToString(); //X columns for local strings
									Data.Columns.Add(columnName, typeof(string));
									Data.Columns[columnName].AllowDBNull = false;
									Data.Columns[columnName].DefaultValue = string.Empty;
								}
							}
							break;
						default:
							throw new Exception($"Unknown field type {col.Type} for {col.Name}.");
					}

					//AutoGenerated Id for CharBaseInfo
					if (col.AutoGenerate)
					{
						Data.Columns[0].ExtendedProperties.Add(AUTO_GENERATED, true);
						Header.AutoGeneratedColumns++;
					}

					Data.Columns[columnName].AllowDBNull = false;
				}
			}

			//Setup the Primary Key
			Data.Columns[Key].DefaultValue = null; //Clear default value
			Data.PrimaryKey = new DataColumn[] { Data.Columns[Key] };
			Data.Columns[Key].AutoIncrement = true;
			Data.Columns[Key].Unique = true;
		}
        
		public void Attach()
		{
			if (Data != null && Data.Rows.Count > 0)
				return;

			using (FileStream fs = new FileStream(Path.Combine(TEMP_FOLDER, Tag + ".cache"), FileMode.Open))
			using (var mmf = MemoryMappedFile.CreateFromFile(fs, Tag, fs.Length, MemoryMappedFileAccess.ReadWrite, null, HandleInheritability.None, false))
			using (var stream = mmf.CreateViewStream(0, fs.Length, MemoryMappedFileAccess.Read))
			{
				var formatter = new BinaryFormatter();
				Data = (DataTable)formatter.Deserialize(stream);
			}
		}


		/// <summary>
		/// Checks if the file is of Name and Expansion
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="expansion"></param>
		/// <returns></returns>
		/// 
		public bool IsFileOf(string filename, Expansion expansion)
		{
			return TableStructure.Name.Equals(filename, IGNORECASE) && IsBuild(Build, expansion);
		}

		public bool IsFileOf(string filename)
		{
			return TableStructure.Name.Equals(filename, IGNORECASE);
		}


		/// <summary>
		/// Generates a Bit map for all columns as the Blizzard one combines array columns
		/// </summary>
		/// <returns></returns>
		public FieldStructureEntry[] GetBits()
		{
			if (!Header.IsTypeOf<WDB5>())
				return new FieldStructureEntry[Data.Columns.Count];

			List<FieldStructureEntry> bits = new List<FieldStructureEntry>();
			if (Header is WDC1 header)
			{
				var fields = header.ColumnMeta;
				for (int i = 0; i < fields.Count; i++)
				{
					short bitcount = (short)(Header.FieldStructure[i].BitCount == 64 ? Header.FieldStructure[i].BitCount : 0); // force bitcounts
					for (int x = 0; x < fields[i].ArraySize; x++)
						bits.Add(new FieldStructureEntry(bitcount, 0));
				}
			}
			else
			{
				var fields = Header.FieldStructure;
				for (int i = 0; i < TableStructure.Fields.Count; i++)
				{
					Field f = TableStructure.Fields[i];
					for (int x = 0; x < f.ArraySize; x++)
						bits.Add(new FieldStructureEntry((fields[i]?.Bits ?? 0), 0, (fields[i]?.CommonDataType ?? 0xFF)));
				}
			}

			return bits.ToArray();
		}

		public int[] GetPadding()
		{
			int[] padding = new int[Data.Columns.Count];

			Dictionary<Type, int> bytecounts = new Dictionary<Type, int>()
			{
				{ typeof(byte), 1 },
				{ typeof(short), 2 },
				{ typeof(ushort), 2 },
			};

			if (Header is WDC1 header)
			{

				int c = 0;

				foreach (var field in header.ColumnMeta)
				{
					Type type = Data.Columns[c].DataType;
					bool isneeded = field.CompressionType >= CompressionType.Sparse;

					if (bytecounts.ContainsKey(type) && isneeded)
					{
						for (int x = 0; x < field.ArraySize; x++)
							padding[c++] = 4 - bytecounts[type];
					}
					else
					{
						c += field.ArraySize;
					}
				}
			}

			return padding;
		}

		public void UpdateColumnTypes()
		{
			if (!Header.IsTypeOf<WDB6>())
				return;

			var fields = ((WDB6)Header).FieldStructure;
			int c = 0;
			for (int i = 0; i < TableStructure.Fields.Count; i++)
			{
				int arraySize = TableStructure.Fields[i].ArraySize;

				if (!fields[i].CommonDataColumn)
				{
					c += arraySize;
					continue;
				}

				Type columnType;
				switch (fields[i].CommonDataType)
				{
					case 0:
						columnType = typeof(string);
						break;
					case 1:
						columnType = typeof(ushort);
						break;
					case 2:
						columnType = typeof(byte);
						break;
					case 3:
						columnType = typeof(float);
						break;
					case 4:
						columnType = typeof(int);
						break;
					default:
						c += arraySize;
						continue;
				}

				for (int x = 0; x < arraySize; x++)
				{
					Data.Columns[c].DataType = columnType;
					c++;
				}
			}
		}
        
		/// <summary>
		/// Gets the Min and Max ids
		/// </summary>
		/// <returns></returns>
		public Tuple<int, int> MinMax()
		{
			if (min == -1 || max == -1)
			{
				min = int.MaxValue;
				max = int.MinValue;
				foreach (DataRow dr in Data.Rows)
				{
					int val = dr.Field<int>(Key);
					min = Math.Min(min, val);
					max = Math.Max(max, val);
				}
			}

			return new Tuple<int, int>(min, max);
		}

		/// <summary>
		/// Gets a list of Ids
		/// </summary>
		/// <returns></returns>
		public IEnumerable<int> GetPrimaryKeys()
		{
			if (primaryKeys == null)
				primaryKeys = Data.AsEnumerable().Select(x => x.Field<int>(Key));

			return primaryKeys;
		}
        
		public void Dispose()
		{
			this.Data?.Dispose();
			this.Data = null;
		}
	}
}
