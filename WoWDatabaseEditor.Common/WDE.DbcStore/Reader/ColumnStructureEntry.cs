using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WDBXEditor.Common.Constants;

namespace WDBXEditor.Reader
{
	public class ColumnStructureEntry
	{
		public ushort RecordOffset { get; set; }
		public ushort Size { get; set; }
		public uint AdditionalDataSize { get; set; }
		public CompressionType CompressionType { get; set; }
		public int BitOffset { get; set; }  // used as common data column for Sparse
		public int BitWidth { get; set; }
		public int Cardinality { get; set; } // flags for Immediate, &1: Signed


		public List<byte[]> PalletValues { get; set; }
		public Dictionary<int, byte[]> SparseValues { get; set; }
		public int ArraySize { get; set; } = 1;
	}
}
