// Original code by https://github.com/Thenarden/nmpq 
// Apache-2.0 License
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WDE.Common.MPQ;
using WDE.MPQ.Parsing;

namespace WDE.MPQ
{
    public interface IMpqArchiveDetails
    {
        ArchiveHeader ArchiveHeader { get; }
        IMpqHashTable HashTable { get; }
        IList<BlockTableEntry> BlockTable { get; }

        int SectorSize { get; }
    }
    public interface IMpqHashTable
    {
        HashTableEntry? FindEntry(ulong hashA, ulong hashB);
    }

    public partial class MpqArchive : IMpqArchive, IMpqArchiveDetails
    {
        public MpqLibrary Library => MpqLibrary.Managed;

        private readonly string path;

        public IMpqArchiveDetails Details => this;

        public int SectorSize { get; private set; }

        public IMpqUserDataHeader UserDataHeader { get; private set; }

        public ArchiveHeader ArchiveHeader { get; private set; }
        public IMpqHashTable HashTable { get; private set; }
        public IList<BlockTableEntry> BlockTable { get; private set; }

        public IList<string> KnownFiles => _knownFiles ?? (_knownFiles = ParseListfile().AsReadOnly());

        private BinaryReader _reader;
        private bool _cleanupStreamOnDispose;
        private IList<string>? _knownFiles;

        public IMpqArchive Clone()
        {
            return new MpqArchive(this);
        }

        protected MpqArchive(MpqArchive archive)
        {
            this.path = archive.path;
            
            _reader = new BinaryReader(File.OpenRead(path), Encoding.UTF8);
            _cleanupStreamOnDispose = archive._cleanupStreamOnDispose;

            UserDataHeader = archive.UserDataHeader;
            SectorSize = archive.SectorSize;
            BlockTable = archive.BlockTable;
            ArchiveHeader = archive.ArchiveHeader;
            HashTable = archive.HashTable;
        }
            
        protected MpqArchive(string path, Stream stream, bool cleanupStreamOnDispose)
        {
            this.path = path;
            
            _reader = new BinaryReader(stream, Encoding.UTF8);
            _cleanupStreamOnDispose = cleanupStreamOnDispose;

            var header = ParseUserDataHeader(_reader);

            UserDataHeader = header ?? throw new MpqParsingException("Invalid MPQ header. This is probably not an MPQ archive.");
            
            ParseArchiveHeader();

            SectorSize = 512 << ArchiveHeader.SectorSizeShift;

            var hashTableEntries = ReadTableEntries<HashTableEntry>("(hash table)", ArchiveHeader.HashTableOffset,
                ArchiveHeader.HashTableEntryCount);
            var blockTableEntries = ReadTableEntries<BlockTableEntry>("(block table)", ArchiveHeader.BlockTableOffset,
                ArchiveHeader.BlockTableEntryCount);

            BlockTable = blockTableEntries.ToList().AsReadOnly();
            HashTable = new MpqHashTable(hashTableEntries);
        }
        
        public static IMpqUserDataHeader? ParseUserDataHeader(string path)
        {
            if (path == null) throw new ArgumentNullException("path");

            using (var stream = File.OpenRead(path))
                return ParseUserDataHeader(stream);
        }

        public static IMpqUserDataHeader? ParseUserDataHeader(byte[] data)
        {
            if (data == null) throw new ArgumentNullException("data");

            using (var stream = new MemoryStream(data))
                return ParseUserDataHeader(stream);
        }

        public static IMpqUserDataHeader? ParseUserDataHeader(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            using (var reader = new BinaryReader(stream))
                return ParseUserDataHeader(reader);
        }

        public static IMpqArchive Open(string path)
        {
            if (path == null) throw new ArgumentNullException("path");

            var archive = new MpqArchive(path, File.OpenRead(path), true);
            return archive;
        }

        public static IMpqArchive Open(byte[] data)
        {
            if (data == null) throw new ArgumentNullException("data");

            var archive = new MpqArchive(null!, new MemoryStream(data), true);
            return archive;
        }

        public static IMpqArchive Open(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            var archive = new MpqArchive(null!, stream, false);
            return archive;
        }
        
        // seeks to a stream posistion relative to the start of the archive
        private void SeekToArchiveOffset(long offset)
        {
            _reader!.BaseStream.Seek(UserDataHeader.ArchiveOffset + offset, SeekOrigin.Begin);
        }

        public void Dispose()
        {
            if (_cleanupStreamOnDispose)
            {
                if (_reader != null)
                {
                    _reader.Dispose();
                    _reader = null!;
                }
            }
        }
    }
}