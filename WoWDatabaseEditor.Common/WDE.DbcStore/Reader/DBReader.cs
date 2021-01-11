using WDBXEditor.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.IO;
using System.Text;
using WDBXEditor.Reader.FileTypes;
using WDBXEditor.Common;
using static WDBXEditor.Common.Constants;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;

namespace WDBXEditor.Reader
{
	public class DBReader
	{
		public string ErrorMessage { get; set; }

		private List<Tuple<int, short>> OffsetMap = new List<Tuple<int, short>>();
		private string FileName;

		#region Read Methods
		private DBHeader ExtractHeader(BinaryReader dbReader)
		{
			DBHeader header = null;
			string signature = dbReader.ReadString(4);

			if (string.IsNullOrWhiteSpace(signature))
				return null;

			if (signature[0] != 'W')
				signature = signature.Reverse().ToString();

			switch (signature)
			{
				case "WDBC":
					header = new WDBC();
					break;
				case "WDB2":
				case "WCH2":
					header = new WDB2();
					break;
				case "WDB5":
					header = new WDB5();
					break;
				case "WDB6":
					header = new WDB6();
					break;
				case "WCH5":
					header = new WCH5(FileName);
					break;
				case "WCH7":
					header = new WCH7(FileName);
					break;
				case "WCH8":
					header = new WCH8(FileName);
					break;
				case "WDC1":
					header = new WDC1();
					break;
				case "WDC2":
					header = new WDC2();
					break;
				case "WMOB":
				case "WGOB":
				case "WQST":
				case "WIDB":
				case "WNDB":
				case "WITX":
				case "WNPC":
				case "WPTX":
				case "WRDN":
					header = new WDB();
					break;
				case "HTFX":
					header = new HTFX();
					break;
			}

			header?.ReadHeader(ref dbReader, signature);
			return header;
		}

		public DBEntry Read(MemoryStream stream, string dbFile)
		{
			FileName = dbFile;
			stream.Position = 0;

			using (var dbReader = new BinaryReader(stream, Encoding.UTF8))
			{
				DBHeader header = ExtractHeader(dbReader);
				long pos = dbReader.BaseStream.Position;

				//No header - must be invalid
				if (header == null)
					throw new Exception("Unknown file type.");

				if (header.CheckRecordSize && header.RecordSize == 0)
					throw new Exception("File contains no records.");
				if (header.CheckRecordCount && header.RecordCount == 0)
					throw new Exception("File contains no records.");

				DBEntry entry = new DBEntry(header, dbFile);
				if (header.CheckTableStructure && entry.TableStructure == null)
					throw new Exception("Definition missing.");

				if (header is WDC1 wdc1)
				{
					Dictionary<int, string> StringTable = wdc1.ReadStringTable(dbReader);
					wdc1.LoadDefinitionSizes(entry);

					//Read the data
					using (MemoryStream ms = new MemoryStream(header.ReadData(dbReader, pos)))
					using (BinaryReader dataReader = new BinaryReader(ms, Encoding.UTF8))
					{
						wdc1.AddRelationshipColumn(entry);
						//wdc1.SetColumnMinMaxValues(entry);
						ReadIntoTable(ref entry, dataReader, StringTable);
					}

					stream.Dispose();
					return entry;
				}
				else if (header.IsTypeOf<WDBC>() || header.IsTypeOf<WDB2>())
				{
					long stringTableStart = dbReader.BaseStream.Position += header.RecordCount * header.RecordSize;
					Dictionary<int, string> StringTable = new StringTable().Read(dbReader, stringTableStart); //Get stringtable
					dbReader.Scrub(pos);

					ReadIntoTable(ref entry, dbReader, StringTable); //Read data

					stream.Dispose();
					return entry;
				}
				else if (header.IsTypeOf<WDB5>() || header.IsTypeOf<WCH5>() || header.IsTypeOf<WDB6>())
				{
					int CopyTableSize = header.CopyTableSize; //Only WDB5 has a copy table
					uint CommonDataTableSize = header.CommonDataTableSize; //Only WDB6 has a CommonDataTable

					//StringTable - only if applicable
					long copyTablePos = dbReader.BaseStream.Length - CommonDataTableSize - CopyTableSize;
					long indexTablePos = copyTablePos - (header.HasIndexTable ? header.RecordCount * 4 : 0);
					long wch7TablePos = indexTablePos - (header.UnknownWCH7 * 4);
					long stringTableStart = wch7TablePos - header.StringBlockSize;
					Dictionary<int, string> StringTable = new Dictionary<int, string>();
					if (!header.HasOffsetTable) //Stringtable is only present if there isn't an offset map
					{
						dbReader.Scrub(stringTableStart);
						StringTable = new StringTable().Read(dbReader, stringTableStart, stringTableStart + header.StringBlockSize);
						dbReader.Scrub(pos);
					}

					//Read the data
					using (MemoryStream ms = new MemoryStream(header.ReadData(dbReader, pos)))
					using (BinaryReader dataReader = new BinaryReader(ms, Encoding.UTF8))
					{
						entry.UpdateColumnTypes();
						ReadIntoTable(ref entry, dataReader, StringTable);
					}

					//Cleanup
					header.OffsetLengths = null;

					stream.Dispose();
					return entry;
				}
				else if (header.IsTypeOf<WDB>())
				{
					WDB wdb = (WDB)header;
					using (MemoryStream ms = new MemoryStream(wdb.ReadData(dbReader)))
					using (BinaryReader dataReader = new BinaryReader(ms, Encoding.UTF8))
					{
						ReadIntoTable(ref entry, dataReader, new Dictionary<int, string>());
					}

					stream.Dispose();
					return entry;
				}
				else if (header.IsTypeOf<HTFX>())
				{
					//Load data when needed later
					stream.Dispose();
					return entry;
				}
				else
				{
					stream.Dispose();
					throw new Exception($"Invalid filetype.");
				}
			}
		}

		public DBEntry Read(string dbFile)
		{
			return Read(new MemoryStream(File.ReadAllBytes(dbFile)), dbFile);
		}

		public void ReadIntoTable(ref DBEntry entry, BinaryReader dbReader, Dictionary<int, string> StringTable)
		{
			if (entry.Header.RecordCount == 0)
				return;

			TypeCode[] columnTypes = entry.Data.Columns.Cast<DataColumn>().Select(x => Type.GetTypeCode(x.DataType)).ToArray();
			//int[] padding = entry.GetPadding();

			FieldStructureEntry[] bits = entry.GetBits();
			int recordcount = Math.Max(entry.Header.OffsetLengths.Length, (int)entry.Header.RecordCount);

			uint recordsize = entry.Header.RecordSize + (uint)(entry.Header.HasIndexTable ? 4 : 0);
			if (entry.Header.InternalRecordSize > 0)
				recordsize = entry.Header.InternalRecordSize;

			entry.Data.BeginLoadData();

			for (uint i = 0; i < recordcount; i++)
			{
				//Offset map has variable record lengths
				if (entry.Header.IsTypeOf<HTFX>() || entry.Header.HasOffsetTable)
					recordsize = (uint)entry.Header.OffsetLengths[i];

				//Store start position
				long offset = dbReader.BaseStream.Position;

				//Create row and add data
				var row = entry.Data.NewRow();
				for (int j = 0; j < entry.Data.Columns.Count; j++)
				{
					if (entry.Data.Columns[j].ExtendedProperties.ContainsKey(AUTO_GENERATED))
					{
						row.SetField(entry.Data.Columns[j], entry.Data.Rows.Count + 1);
						continue;
					}

					switch (columnTypes[j])
					{
						case TypeCode.Boolean:
							row.SetField(entry.Data.Columns[j], dbReader.ReadBoolean());
							break;
						case TypeCode.SByte:
							row.SetField(entry.Data.Columns[j], dbReader.ReadSByte());
							break;
						case TypeCode.Byte:
							row.SetField(entry.Data.Columns[j], dbReader.ReadByte());
							break;
						case TypeCode.Int16:
							row.SetField(entry.Data.Columns[j], dbReader.ReadInt16());
							break;
						case TypeCode.UInt16:
							row.SetField(entry.Data.Columns[j], dbReader.ReadUInt16());
							break;
						case TypeCode.Int32:
							row.SetField(entry.Data.Columns[j], dbReader.ReadInt32(bits[j]));
							break;
						case TypeCode.UInt32:
							row.SetField(entry.Data.Columns[j], dbReader.ReadUInt32(bits[j]));
							break;
						case TypeCode.Int64:
							row.SetField(entry.Data.Columns[j], dbReader.ReadInt64(bits[j]));
							break;
						case TypeCode.UInt64:
							row.SetField(entry.Data.Columns[j], dbReader.ReadUInt64(bits[j]));
							break;
						case TypeCode.Single:
							row.SetField(entry.Data.Columns[j], dbReader.ReadSingle());
							break;
						case TypeCode.String:
							if (entry.Header.IsTypeOf<WDB>() || entry.Header.IsTypeOf<HTFX>() || entry.Header.HasOffsetTable)
							{
								row.SetField(entry.Data.Columns[j], dbReader.ReadStringNull());
							}
							else
							{
								int stindex = entry.Header.GetStringOffset(dbReader, j, i);
								if (StringTable.ContainsKey(stindex))
								{
									row.SetField(entry.Data.Columns[j], StringTable[stindex]);
								}
								else
								{
									row.SetField(entry.Data.Columns[j], "String not found");
									ErrorMessage = "Strings not found in string table";
								}
							}
							break;
						default:
							throw new Exception($"Unknown field type at column {i}.");
					}

					//dbReader.BaseStream.Position += padding[j];
				}

				entry.Data.Rows.Add(row);

				//Scrub to the end of the record
				if (dbReader.BaseStream.Position - offset < recordsize)
					dbReader.BaseStream.Position += (recordsize - (dbReader.BaseStream.Position - offset));
				else if (dbReader.BaseStream.Position - offset > recordsize)
					throw new Exception("Definition exceeds record size");
			}

			//entry.Header.Clear();
			entry.Data.EndLoadData();
		}
		#endregion
        
	}
}
