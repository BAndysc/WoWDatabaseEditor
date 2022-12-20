// Original code by https://github.com/Thenarden/nmpq 
// Apache-2.0 License
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using WDE.MPQ.Parsing;

namespace WDE.MPQ
{
    public partial class MpqArchive
    {
        private static MpqUserDataHeader? ParseUserDataHeader(BinaryReader reader)
        {
            var header = new MpqUserDataHeader();

            var magicString = new string(reader.ReadChars(3));
            var userDataIndicator = reader.ReadByte();

            if (magicString != "MPQ" || (userDataIndicator != 0x1a && userDataIndicator != 0x1b))
                return null;

            // 0x1a as the last byte of the magic number indicates that there is no user data section
            if (userDataIndicator == 0x1a)
            {
                header.HasUserData = false;

                header.ArchiveOffset = 0;
                header.UserDataReservedSize = 0;
            }

            // 0x1b as the last byte of the magic number indicates that there IS a user data section
            //	we have to skip over it to get to the archive header
            if (userDataIndicator == 0x1b)
            {
                header.HasUserData = true;

                header.UserDataReservedSize = reader.ReadInt32();
                header.ArchiveOffset = reader.ReadInt32();

                header.UserDataSize = reader.ReadInt32();
                header.UserData = reader.ReadBytes(header.UserDataSize);
            }

            return header;
        }

        private void ParseArchiveHeader()
        {
            SeekToArchiveOffset(0);

            ArchiveHeader = _reader.ReadStruct<ArchiveHeader>();

            if (!ArchiveHeader.IsMagicValid)
                throw new MpqParsingException("Invalid MPQ header, this is probably not an MPQ archive. (Invalid magic)");

            if ((ArchiveHeader.FormatVersion == 0 && ArchiveHeader.HeaderSize != 0x20)
                || (ArchiveHeader.FormatVersion == 1 && ArchiveHeader.HeaderSize != 0x2c)
                || (ArchiveHeader.FormatVersion == 3 && ArchiveHeader.HeaderSize != 0xD0))
            {
                throw new MpqParsingException(
                    string.Format(
                        "Unexpected header size for specified MPQ format. (FormatVersion: {0}, HeaderSize: 0x{1:X2})",
                        ArchiveHeader.FormatVersion, ArchiveHeader.HeaderSize));
            }

            if (ArchiveHeader.FormatVersion != 0 && ArchiveHeader.FormatVersion != 1 && ArchiveHeader.FormatVersion != 3)
                throw new MpqParsingException("Invalid MPQ format. Must be '0', '1' or '3'.");

            // Currently ExtendedBlockTableOffset is not used. Format 1+ only
            if (ArchiveHeader.FormatVersion > 0 && ArchiveHeader.ExtendedBlockTableOffset > 0)
                throw new MpqParsingException("MPQ uses extended table, this is currently not supported, report this to the developers.");
        }

        private IEnumerable<T> ReadTableEntries<T>(string name, uint tableOffset, uint numberOfEntries)
        {
            SeekToArchiveOffset(tableOffset);

            var entrySize = Marshal.SizeOf(typeof (T));
            var data = _reader.ReadBytes((int)(entrySize*numberOfEntries));
            var key = Crypto.Hash(name, HashType.TableKey);

            Crypto.DecryptInPlace(data, key);

            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            var addr = handle.AddrOfPinnedObject();

            for (var i = 0; i < numberOfEntries; i++)
            {
                var entryOffset = i*entrySize;
                var entryPointer = addr + entryOffset;

                yield return (T) Marshal.PtrToStructure(entryPointer, typeof (T))!;
            }

            handle.Free();
        }

        private BlockTableEntry? FindBlockTableEntry(string path)
        {
            var hashA = Crypto.Hash(path, HashType.FilePathA);
            var hashB = Crypto.Hash(path, HashType.FilePathB);

            var entry = HashTable.FindEntry(hashA, hashB);

            if (entry == null)
                return null;

            return BlockTable[(int)entry.Value.FileBlockIndex];
        }

        public int? GetFileSize(string path)
        {
            var blockEntry = FindBlockTableEntry(path);

            if (blockEntry == null)
                return null;

            return (int)blockEntry.Value.FileSize;
        }

        // todos: support more decompression algorithms?
        public int? ReadFile(byte[] buffer, int size, string path)
        {
            if (path == null) throw new ArgumentNullException("path");

            var blockEntry = FindBlockTableEntry(path);

            if (blockEntry == null)
                return null;

            if (!blockEntry.Value.IsFile)
                throw new NotSupportedException("Non-file blocks are not currently supported by Nmpq.");

            if (blockEntry.Value.IsEncrypted)
                throw new NotSupportedException("Encrypted files are not currently supported by Nmpq.");

            if (blockEntry.Value.IsImploded)
                throw new NotSupportedException("Imploded files are not currently supported by Nmpq.");

            SeekToArchiveOffset(blockEntry.Value.BlockOffset);
            
            if (!blockEntry.Value.IsFileSingleUnit)
            {
                return ReadMultiUnitFile(buffer, blockEntry.Value);
            }

            // file is only compressed if the block size is smaller than the file size.
            //	per docs at (http://wiki.devklog.net/index.php?title=MPQ_format_specification)
            var compressed = blockEntry.Value.IsCompressed && blockEntry.Value.BlockSize < blockEntry.Value.FileSize;

            if (!compressed)
            {
                return _reader.Read(buffer, 0, (int)blockEntry.Value.BlockSize);
            }

            // first byte of each compressed block is a set of flags indicating which 
            //	compression algorithm(s) to use
            var compressionFlags = (CompressionFlags) _reader.ReadByte();

            // compression flags don't count toward the data size, but does toward the block size
            var dataSize = (int)(blockEntry.Value.BlockSize - 1);
            var compressedData = ArrayPool<byte>.Shared.Rent((int)dataSize);
            _reader.Read(compressedData, 0, (int)dataSize);
            int decompressedLength;

            if (compressionFlags == CompressionFlags.Bzip2)
            {
                decompressedLength = Compression.BZip2Decompress(compressedData, 0,  dataSize, buffer);
            }
            else if (compressionFlags == CompressionFlags.Deflated)
            {
                decompressedLength = Compression.Deflate(compressedData, 0, dataSize, buffer);
            }
            else
                throw new NotSupportedException("Currenlty only Bzip2 and Deflate compression is supported by Nmpq. Compression flags: " + compressionFlags);

            ArrayPool<byte>.Shared.Return(compressedData);
            return decompressedLength;
        }

        private int ReadMultiUnitFile(byte[] buffer, BlockTableEntry blockEntry)
        {
            var sectorCount = blockEntry.FileSize/SectorSize + 1;

            //if (blockEntry.FileSize%SectorSize > 0)
            //sectorCount++;

            var hasCRC = (blockEntry.Flags & BlockFlags.HasChecksums) != 0;
            if (hasCRC)
                sectorCount++;
            
            var sectorTable = new int[sectorCount + 1];

            for (var i = 0; i < sectorTable.Length; i++)
            {
                sectorTable[i] = _reader.ReadInt32();
            }

            var resultPosition = 0;

            long left = blockEntry.FileSize;
            for (var i = 0; i < sectorCount - (hasCRC ? 1 : 0); i++)
            {
                var position = sectorTable[i];
                var length = sectorTable[i + 1] - position;

                SeekToArchiveOffset(position + blockEntry.BlockOffset);
                var tempBuffer = ArrayPool<byte>.Shared.Rent(length);
                var decompressedBuffer = tempBuffer;
                int decompressedLength = length;
                var sectorData = _reader.Read(tempBuffer, 0, length);
                Debug.Assert(sectorData == length);

                if (blockEntry.IsCompressed && left > length && decompressedLength != SectorSize)
                {
                    var compressionFlags = (CompressionFlags) tempBuffer[0];
                    
                    if (compressionFlags == CompressionFlags.Bzip2)
                    {
                        decompressedBuffer = Compression.BZip2Decompress(tempBuffer, 1, length - 1);
                        decompressedLength = decompressedBuffer.Length;
                        Array.Copy(decompressedBuffer, 0, buffer, resultPosition, decompressedLength);
                    }
                    else if (compressionFlags == CompressionFlags.Deflated)
                    {
                        decompressedLength = Compression.DeflateTo(tempBuffer, 1, length - 1, buffer.AsSpan(resultPosition));
                    }
                    else
                    {
                        throw new NotSupportedException(
                            "Currenlty only Bzip2 and Deflate compression is supported by Nmpq. Compression flags: " + compressionFlags);
                    }
                }
                else
                {
                    Array.Copy(decompressedBuffer, 0, buffer, resultPosition, decompressedLength);
                }

                ArrayPool<byte>.Shared.Return(tempBuffer);
                resultPosition += decompressedLength;
                left -= decompressedLength;
            }

            Debug.Assert(left == 0);
            return (int)blockEntry.FileSize;
        }

        private List<string> ParseListfile()
        {
            var listfileSize = GetFileSize("(listfile)");
            if (!listfileSize.HasValue)
                return new List<string>();

            var listfile = new byte[listfileSize.Value];
            ReadFile(listfile, -1, "(listfile)");
            
            var contents = Encoding.UTF8.GetString(listfile);
            var entries = contents.Split(new[] {';', '\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            return entries.ToList();
        }
    }
}