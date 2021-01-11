using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDBXEditor.Reader
{
	public class RelationShipData
	{
		public uint Records;
		public uint MinId;
		public uint MaxId;
		public Dictionary<uint, byte[]> Entries; // index, id
	}
}
