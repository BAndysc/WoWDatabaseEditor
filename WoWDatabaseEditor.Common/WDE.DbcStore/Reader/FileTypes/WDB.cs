using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDBXEditor.Common;
using WDBXEditor.Storage;

namespace WDBXEditor.Reader.FileTypes
{
	public class WDB : DBHeader
	{
		public new string Locale { get; set; }
		public int RecordVersion { get; set; }
		public int CacheVersion { get; set; }
		public int Build { get; set; }
		public override bool CheckRecordCount => false;

		public override void ReadHeader(ref BinaryReader dbReader, string signature)
		{
			this.Signature = signature;
			Build = dbReader.ReadInt32();

			if (Build >= 4500) // 1.6.0
				Locale = dbReader.ReadString(4).Reverse().ToString();

			RecordSize = dbReader.ReadUInt32();
			RecordVersion = dbReader.ReadInt32();
			
			if (Build >= 9506) // 3.0.8
				CacheVersion = dbReader.ReadInt32();
		}

		public byte[] ReadData(BinaryReader dbReader)
		{
			List<byte> data = new List<byte>();

			//Stored as Index, Size then Data
			while (dbReader.BaseStream.Position != dbReader.BaseStream.Length)
			{
				int index = dbReader.ReadInt32();
				if (index == 0 && dbReader.BaseStream.Position == dbReader.BaseStream.Length)
					break;

				int size = dbReader.ReadInt32();
				if (index == 0 && size == 0 && dbReader.BaseStream.Position == dbReader.BaseStream.Length)
					break;

				data.AddRange(BitConverter.GetBytes(index));
				data.AddRange(dbReader.ReadBytes(size));

				RecordCount++;
			}

			return data.ToArray();
		}
	}
}
