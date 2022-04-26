///.BIG File Specification
///
///.big files are an archive format that was used in many game titles created by EA Studios.
///
///Header
/// char[4] FourCC - Identifies the string as valid big archive. The string may either be “BIG4” or “BIGF”, depending on the version.
/// uint Size (LE) - The entire size of a big archive. Size of a single archive can not be greater than 2^32 bytes
/// uint NumEntries (BE) - Number of files that were packed into this archive
/// uint OffsetFirst (BE) - The offset inside the file to the first entry
///end Header
///
///List of entries [NumEntries]
/// uint EntryOffset (BE) - specified the start of this entry inside the file (in bytes)
/// uint EntrySize (BE) - the size of the specified entry
/// string EntryName - the name of this entry, read as a nullterminated string. The maximum length is limited ny the Windows MAX_PATH (which is 260)
///end List of entries
///
///string unkownString = L225
///byte nullTerminatedBytes[4]
///
///List of files [NumEntries] (Binary) - all files placed one after another without null terminated bytes
///
///Source - OpenSAGE project https://github.com/OpenSAGE/OpenSAGE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GenImageViewer
{
    /// <summary>
    /// Entry in BIG file
    /// </summary>
    public class BigEntry
    {
        public string Name => _name;
        private string _name;
        public uint Length => _length;
        private uint _length;
        public uint Offset => _offset;
        private uint _offset;
        public BigEntry(string name, uint length, uint offset)
        {
            this._name = name;
            this._offset = offset;
            this._length = length;
        }
    }
    public class BigArchive
    {
        /// <summary>
        /// Custom reader for BIG files
        /// </summary>
        private class BigReader : BinaryReader
        {
            public BigReader(Stream stream) : base(stream, Encoding.ASCII) { }
            /// <summary>
            /// Read string
            /// </summary>
            /// <param name="charCount">Char count</param>
            /// <returns>Return string</returns>
            public string ReadString(int charCount)
            {
                StringBuilder stringBuilder = new StringBuilder(charCount);
                for (int i = 0; i < charCount; i++)
                    stringBuilder[i] = this.ReadChar();
                return stringBuilder.ToString();
            }
            /// <summary>
            /// Read string
            /// </summary>
            /// <param name="ch">Null terminated char</param>
            /// <returns></returns>
            public string ReadString(char ch)
            {
                StringBuilder stringBuilder = new StringBuilder();
                for (; ; )
                {
                    //Проверку на конец потока
                    char c = this.ReadChar();
                    if (c == ch)
                        return stringBuilder.ToString();
                    else
                        stringBuilder.Append(c);
                }
            }
            public uint ReadUintBigEndian() => BitConverter.ToUInt32(this.ReadBytes(4).Reverse().ToArray(), 0);
            public uint ReadUintLittleEndian() => BitConverter.ToUInt32(this.ReadBytes(4), 0);
        }
        /// <summary>
        /// Mode for working with BIG file
        /// </summary>
        public enum EOpenMode
        {
            /// <summary>
            /// Open for read
            /// </summary>
            Open
        }
        private EOpenMode _openMode;
        public List<BigEntry> Entries => _entries;
        private List<BigEntry> _entries;
        public string Name => _name;
        private string _name;
        public uint EntriesCount => _entries == null ? 0 : (uint)_entries.Count;
        public uint FirstOffset => _firstOffset;
        private uint _firstOffset = 0;
        public uint Length => _length;
        private uint _length = 0;
        private BigReader bigReader;
        public void Open(EOpenMode openMode, string bigName)
        {
            _name = bigName;
            _openMode = openMode;
            _entries = new List<BigEntry>();
            switch (openMode)
            {
            case EOpenMode.Open:
                {
                    bigReader = new BigReader(new FileStream(this._name, FileMode.Open, FileAccess.Read));
                    bigReader.ReadBytes(4);
                    _length = bigReader.ReadUintLittleEndian();
                    uint entryCount = bigReader.ReadUintBigEndian();
                    _firstOffset = bigReader.ReadUintBigEndian();
                    int headerLength = 16 + 4 + 4;
                    for (uint i = 0; i < entryCount; i++)
                    {
                        uint offset = bigReader.ReadUintBigEndian();
                        uint length = bigReader.ReadUintBigEndian();
                        string name = bigReader.ReadString(Encoding.ASCII.GetChars(new byte[] { 0 })[0]);
                        headerLength += 4 + 4 + name.Length + 1;
                        _entries.Add(new BigEntry(name, length, offset));
                        OpenProgressEvent?.BeginInvoke(entryCount, i + 1, null, null);
                    }
                    bigReader.Close();
                    bigReader.Dispose();
                }
                return;
            }
        }
        /// <summary>
        /// Call an event every entry was readed
        /// </summary>
        /// <param name="count">Total entries</param>
        /// <param name="progress">Readed entries</param>
        public delegate void OpenProgressEventHandler(uint count, uint progress);
        public event OpenProgressEventHandler OpenProgressEvent;
    }
}
