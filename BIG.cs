using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GenImageViewer
{
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
        private class BigReader : BinaryReader
        {
            public BigReader(Stream stream) : base(stream, Encoding.ASCII) { }
            /// <summary>
            /// Считываем строку
            /// </summary>
            /// <param name="count">Количество байт</param>
            /// <returns>Возвращаем строку</returns>
            public string ReadString(int count)
            {
                StringBuilder stringBuilder = new StringBuilder(count);
                for (int i = 0; i < count; i++)
                    stringBuilder[i] = this.ReadChar();
                return stringBuilder.ToString();
            }
            /// <summary>
            /// Считываем строку
            /// </summary>
            /// <param name="ch">Крайний символ для окончания считывания</param>
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
        public enum EOpenMode
        {
            /// <summary>
            /// Открыть для чтения
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
        public delegate void OpenProgressEventHandler(uint count, uint progress);
        public event OpenProgressEventHandler OpenProgressEvent;
    }
}
