using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDBXEditor.Storage;
using static WDBXEditor.Common.Constants;

namespace WDBXEditor.Reader.FileTypes
{
	public class WDC1 : WDB6
	{
		public int PackedDataOffset;
		public uint RelationshipCount;
		public int OffsetTableOffset;
		public int IndexSize;
		public int ColumnMetadataSize;
		public int SparseDataSize;
		public int PalletDataSize;
		public int RelationshipDataSize;

		public List<ColumnStructureEntry> ColumnMeta;
		public RelationShipData RelationShipData;
		//public Dictionary<int, MinMax> MinMaxValues;

		protected int[] columnSizes;
		protected byte[] recordData;

		#region Read
		public override void ReadHeader(ref BinaryReader dbReader, string signature)
		{
			ReadBaseHeader(ref dbReader, signature);

			TableHash = dbReader.ReadUInt32();
			LayoutHash = dbReader.ReadInt32();
			MinId = dbReader.ReadInt32();
			MaxId = dbReader.ReadInt32();
			Locale = dbReader.ReadInt32();
			CopyTableSize = dbReader.ReadInt32();
			Flags = (HeaderFlags)dbReader.ReadUInt16();
			IdIndex = dbReader.ReadUInt16();
			TotalFieldSize = dbReader.ReadUInt32();

			PackedDataOffset = dbReader.ReadInt32();
			RelationshipCount = dbReader.ReadUInt32();
			OffsetTableOffset = dbReader.ReadInt32();
			IndexSize = dbReader.ReadInt32();
			ColumnMetadataSize = dbReader.ReadInt32();
			SparseDataSize = dbReader.ReadInt32();
			PalletDataSize = dbReader.ReadInt32();
			RelationshipDataSize = dbReader.ReadInt32();

			//Gather field structures
			FieldStructure = new List<FieldStructureEntry>();
			for (int i = 0; i < FieldCount; i++)
			{
				var field = new FieldStructureEntry(dbReader.ReadInt16(), dbReader.ReadUInt16());
				FieldStructure.Add(field);
			}

			recordData = dbReader.ReadBytes((int)(RecordCount * RecordSize));
			Array.Resize(ref recordData, recordData.Length + 8);

			Flags &= ~HeaderFlags.RelationshipData; // appears to be obsolete now
		}

		public new Dictionary<int, byte[]> ReadOffsetData(BinaryReader dbReader, long pos)
		{
			Dictionary<int, byte[]> CopyTable = new Dictionary<int, byte[]>();
			List<Tuple<int, short>> offsetmap = new List<Tuple<int, short>>();
			Dictionary<int, OffsetDuplicate> firstindex = new Dictionary<int, OffsetDuplicate>();
			Dictionary<int, int> OffsetDuplicates = new Dictionary<int, int>();
			Dictionary<int, List<int>> Copies = new Dictionary<int, List<int>>();

			int[] m_indexes = null;

			// OffsetTable
			if (HasOffsetTable && OffsetTableOffset > 0)
			{
				dbReader.BaseStream.Position = OffsetTableOffset;
				for (int i = 0; i < (MaxId - MinId + 1); i++)
				{
					int offset = dbReader.ReadInt32();
					short length = dbReader.ReadInt16();

					if (offset == 0 || length == 0)
						continue;

					// special case, may contain duplicates in the offset map that we don't want
					if (CopyTableSize == 0)
					{
						if (!firstindex.ContainsKey(offset))
						{
							firstindex.Add(offset, new OffsetDuplicate(offsetmap.Count, firstindex.Count));
						}
						else
						{
							OffsetDuplicates.Add(MinId + i, firstindex[offset].VisibleIndex);
							continue;
						}
					}

					offsetmap.Add(new Tuple<int, short>(offset, length));
				}
			}

			// IndexTable
			if (HasIndexTable)
			{
				m_indexes = new int[RecordCount];
				for (int i = 0; i < RecordCount; i++)
					m_indexes[i] = dbReader.ReadInt32();
			}

			// Copytable
			if (CopyTableSize > 0)
			{
				long end = dbReader.BaseStream.Position + CopyTableSize;
				while (dbReader.BaseStream.Position < end)
				{
					int id = dbReader.ReadInt32();
					int idcopy = dbReader.ReadInt32();

					if (!Copies.ContainsKey(idcopy))
						Copies.Add(idcopy, new List<int>());

					Copies[idcopy].Add(id);
				}
			}

			// ColumnMeta
			ColumnMeta = new List<ColumnStructureEntry>();
			for (int i = 0; i < FieldCount; i++)
			{
				var column = new ColumnStructureEntry()
				{
					RecordOffset = dbReader.ReadUInt16(),
					Size = dbReader.ReadUInt16(),
					AdditionalDataSize = dbReader.ReadUInt32(), // size of pallet / sparse values
					CompressionType = (CompressionType)dbReader.ReadUInt32(),
					BitOffset = dbReader.ReadInt32(),
					BitWidth = dbReader.ReadInt32(),
					Cardinality = dbReader.ReadInt32()
				};

				// preload arraysizes
				if (column.CompressionType == CompressionType.None)
					column.ArraySize = Math.Max(column.Size / FieldStructure[i].BitCount, 1);
				else if (column.CompressionType == CompressionType.PalletArray)
					column.ArraySize = Math.Max(column.Cardinality, 1);

				ColumnMeta.Add(column);
			}

			// Pallet values
			for (int i = 0; i < ColumnMeta.Count; i++)
			{
				if (ColumnMeta[i].CompressionType == CompressionType.Pallet || ColumnMeta[i].CompressionType == CompressionType.PalletArray)
				{
					int elements = (int)ColumnMeta[i].AdditionalDataSize / 4;
					int cardinality = Math.Max(ColumnMeta[i].Cardinality, 1);

					ColumnMeta[i].PalletValues = new List<byte[]>();
					for (int j = 0; j < elements / cardinality; j++)
						ColumnMeta[i].PalletValues.Add(dbReader.ReadBytes(cardinality * 4));
				}
			}

			// Sparse values
			for (int i = 0; i < ColumnMeta.Count; i++)
			{
				if (ColumnMeta[i].CompressionType == CompressionType.Sparse)
				{
					ColumnMeta[i].SparseValues = new Dictionary<int, byte[]>();
					for (int j = 0; j < ColumnMeta[i].AdditionalDataSize / 8; j++)
						ColumnMeta[i].SparseValues[dbReader.ReadInt32()] = dbReader.ReadBytes(4);
				}
			}

			// Relationships
			if (RelationshipDataSize > 0)
			{
				RelationShipData = new RelationShipData()
				{
					Records = dbReader.ReadUInt32(),
					MinId = dbReader.ReadUInt32(),
					MaxId = dbReader.ReadUInt32(),
					Entries = new Dictionary<uint, byte[]>()
				};

				for (int i = 0; i < RelationShipData.Records; i++)
				{
					byte[] foreignKey = dbReader.ReadBytes(4);
					uint index = dbReader.ReadUInt32();
					// has duplicates just like the copy table does... why?
					if (!RelationShipData.Entries.ContainsKey(index))
						RelationShipData.Entries.Add(index, foreignKey);
				}

				FieldStructure.Add(new FieldStructureEntry(0, 0));
				ColumnMeta.Add(new ColumnStructureEntry());
			}

			// Record Data
			BitStream bitStream = new BitStream(recordData);
			for (int i = 0; i < RecordCount; i++)
			{
				int id = 0;

				if (HasOffsetTable && HasIndexTable)
				{
					id = m_indexes[CopyTable.Count];
					var map = offsetmap[i];

					if (CopyTableSize == 0 && firstindex[map.Item1].HiddenIndex != i) //Ignore duplicates
						continue;

					dbReader.BaseStream.Position = map.Item1;

					byte[] data = dbReader.ReadBytes(map.Item2);

					IEnumerable<byte> recordbytes = BitConverter.GetBytes(id).Concat(data);

					// append relationship id
					if (RelationShipData != null)
					{
						// seen cases of missing indicies 
						if (RelationShipData.Entries.TryGetValue((uint)i, out byte[] foreignData))
							recordbytes = recordbytes.Concat(foreignData);
						else
							recordbytes = recordbytes.Concat(new byte[4]);
					}

					CopyTable.Add(id, recordbytes.ToArray());

					if (Copies.ContainsKey(id))
					{
						foreach (int copy in Copies[id])
							CopyTable.Add(copy, BitConverter.GetBytes(copy).Concat(data).ToArray());
					}
				}
				else
				{
					bitStream.Seek(i * RecordSize, 0);
					int idOffset = 0;

					List<byte> data = new List<byte>();

					if (HasIndexTable)
					{
						id = m_indexes[i];
						data.AddRange(BitConverter.GetBytes(id));
					}

					int c = HasIndexTable ? 1 : 0;
					for (int f = 0; f < FieldCount; f++)
					{
						int bitOffset = ColumnMeta[f].BitOffset;
						int bitWidth = ColumnMeta[f].BitWidth;
						int cardinality = ColumnMeta[f].Cardinality;
						uint palletIndex;
						int take = columnSizes[c] * ColumnMeta[f].ArraySize;

						switch (ColumnMeta[f].CompressionType)
						{
							case CompressionType.None:
								int bitSize = FieldStructure[f].BitCount;
								if (!HasIndexTable && f == IdIndex)
								{
									idOffset = data.Count;
									id = bitStream.ReadInt32(bitSize); // always read Ids as ints
									data.AddRange(BitConverter.GetBytes(id));
								}
								else
								{
									data.AddRange(bitStream.ReadBytes(bitSize * ColumnMeta[f].ArraySize, false, take));
								}
								break;

							case CompressionType.Immediate:
							case CompressionType.SignedImmediate:
								if (!HasIndexTable && f == IdIndex)
								{
									idOffset = data.Count;
									id = bitStream.ReadInt32(bitWidth); // always read Ids as ints
									data.AddRange(BitConverter.GetBytes(id));
								}
								else
								{
									data.AddRange(bitStream.ReadBytes(bitWidth, false, take));
								}
								break;

							case CompressionType.Sparse:
								if (ColumnMeta[f].SparseValues.TryGetValue(id, out byte[] valBytes))
									data.AddRange(valBytes.Take(take));
								else
									data.AddRange(BitConverter.GetBytes(ColumnMeta[f].BitOffset).Take(take));
								break;

							case CompressionType.Pallet:
							case CompressionType.PalletArray:
								palletIndex = bitStream.ReadUInt32(bitWidth);
								data.AddRange(ColumnMeta[f].PalletValues[(int)palletIndex].Take(take));
								break;

							default:
								throw new Exception($"Unknown compression {ColumnMeta[f].CompressionType}");

						}

						c += ColumnMeta[f].ArraySize;
					}

					// append relationship id
					if (RelationShipData != null)
					{
						// seen cases of missing indicies 
						if (RelationShipData.Entries.TryGetValue((uint)i, out byte[] foreignData))
							data.AddRange(foreignData);
						else
							data.AddRange(new byte[4]);
					}

					CopyTable.Add(id, data.ToArray());

					if (Copies.ContainsKey(id))
					{
						foreach (int copy in Copies[id])
						{
							byte[] newrecord = CopyTable[id].ToArray();
							Buffer.BlockCopy(BitConverter.GetBytes(copy), 0, newrecord, idOffset, 4);
							CopyTable.Add(copy, newrecord);
						}
					}
				}
			}

			if (HasIndexTable)
			{
				FieldStructure.Insert(0, new FieldStructureEntry(0, 0));
				ColumnMeta.Insert(0, new ColumnStructureEntry());
			}

			offsetmap.Clear();
			firstindex.Clear();
			OffsetDuplicates.Clear();
			Copies.Clear();
			Array.Resize(ref recordData, 0);
			bitStream.Dispose();
			ColumnMeta.ForEach(x => { x.PalletValues?.Clear(); x.SparseValues?.Clear(); });

			InternalRecordSize = (uint)CopyTable.First().Value.Length;

			if (CopyTableSize > 0)
				CopyTable = CopyTable.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

			return CopyTable;
		}

		public override byte[] ReadData(BinaryReader dbReader, long pos)
		{
			Dictionary<int, byte[]> CopyTable = ReadOffsetData(dbReader, pos);
			OffsetLengths = CopyTable.Select(x => x.Value.Length).ToArray();
			return CopyTable.Values.SelectMany(x => x).ToArray();
		}

		public virtual Dictionary<int, string> ReadStringTable(BinaryReader dbReader)
		{
			long pos = dbReader.BaseStream.Position;
			return new StringTable().Read(dbReader, pos, pos + StringBlockSize);
		}


		public void LoadDefinitionSizes(DBEntry entry)
		{
			if (HasOffsetTable)
				return;

			Dictionary<TypeCode, int> typeLookup = new Dictionary<TypeCode, int>()
			{
				{ TypeCode.Byte, 1 },
				{ TypeCode.SByte, 1 },
				{ TypeCode.UInt16, 2 },
				{ TypeCode.Int16, 2 },
				{ TypeCode.Int32, 4 },
				{ TypeCode.UInt32, 4 },
				{ TypeCode.Int64, 8 },
				{ TypeCode.UInt64, 8 },
				{ TypeCode.String, 4 },
				{ TypeCode.Single, 4 }
			};
			columnSizes = entry.Data.Columns.Cast<DataColumn>().Select(x => typeLookup[Type.GetTypeCode(x.DataType)]).ToArray();
		}

		public void AddRelationshipColumn(DBEntry entry)
		{
			if (RelationShipData == null)
				return;

			if (!entry.Data.Columns.Cast<DataColumn>().Any(x => x.ExtendedProperties.ContainsKey("RELATIONSHIP")))
			{
				DataColumn dataColumn = new DataColumn("RelationshipData", typeof(uint));
				dataColumn.ExtendedProperties.Add("RELATIONSHIP", true);
				entry.Data.Columns.Add(dataColumn);
			}
		}
		#endregion
        
		protected void RemoveBitLimits()
		{
			if (HasOffsetTable)
				return;

			int c = HasIndexTable ? 1 : 0;
			int cm = ColumnMeta.Count - (RelationShipData != null ? 1 : 0);

			var skipType = new HashSet<CompressionType>(new[] { CompressionType.None, CompressionType.Sparse });

			for (int i = c; i < cm; i++)
			{
				var col = ColumnMeta[i];
				var type = col.CompressionType;
				int oldsize = col.BitWidth;
				ushort newsize = (ushort)(columnSizes[c] * 8);

				c += col.ArraySize;

				if (skipType.Contains(col.CompressionType) || newsize == oldsize)
					continue;

				col.BitWidth = col.Size = newsize;

				for (int x = i + 1; x < cm; x++)
				{
					if (skipType.Contains(ColumnMeta[x].CompressionType))
						continue;

					ColumnMeta[x].RecordOffset += (ushort)(newsize - oldsize);
					ColumnMeta[x].BitOffset = ColumnMeta[x].RecordOffset - (PackedDataOffset * 8);
				}
			}

			RecordSize = (uint)((ColumnMeta.Sum(x => x.Size) + 7) / 8);
		}
	}

	public class MinMax
	{
		public object MinVal;
		public object MaxVal;
		public bool Signed;
		public bool IsSingle;
	}
}
