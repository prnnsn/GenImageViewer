using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GenImageViewer
{
    public class BigEntry
    {
        public string Name => _Name;
        private string _Name;
        public uint Length => _Length;
        private uint _Length;
        public uint Offset;
        public BigEntry(string Name, uint Length, uint Offset)
        {
            this._Name = Name;
            this.Offset = Offset;
            this._Length = Length;
        }
    }
    public class BigArchive
    {
        public class EntryForAdding
        {
            public readonly string FileName;
            public readonly string EntryName;
            public readonly uint Offset;
            public readonly uint Length;
            public enum EAddingType { File, Entry };
            public readonly EAddingType AddingType;
            /// <summary>
            /// Объект для добавления является файл
            /// </summary>
            /// <param name="FileName">Путь к файлу</param>
            /// <param name="EntryName">Название файла внутри бига</param>
            public EntryForAdding(string FileName, string EntryName)
            {
                AddingType = EAddingType.File;
                this.FileName = FileName;
                this.EntryName = EntryName;
                this.Length = (uint)new FileInfo(FileName).Length;
            }
            /// <summary>
            /// Объект для добавления является файл из другого BIG архива
            /// </summary>
            /// <param name="BigName">Название бига</param>
            /// <param name="entry">Данные записи для добавления</param>
            /// <param name="EntryName">Название файла внутри конечного BIG</param>
            public EntryForAdding(string BigName, BigEntry entry, string EntryName)
            {
                AddingType = EAddingType.Entry;
                this.FileName = BigName;
                this.EntryName = EntryName;
                this.Offset = entry.Offset;
                this.Length = entry.Length;
            }
        }
        private class BigWriter : BinaryWriter
        {
            public BigWriter(Stream stream) : base(stream, Encoding.ASCII) { }
            /// <summary>
            /// Записываем строку
            /// </summary>
            /// <param name="s">Строка</param>
            public void WriteString(string s)
            {
                for (int i = 0; i < s.Length; i++)
                    this.Write(s[i]);
            }
            /// <summary>
            /// Записываем нулевой байт
            /// </summary>
            public void WriteNullTerminatedByte() => this.Write(false);
            /// <summary>
            /// Записываем определенное количество нулевых байтов
            /// </summary>
            /// <param name="count">Количество нулевых байтов</param>
            public void WriteNullTerminatedBytes(int count)
            {
                for (int i = count - 1; i >= 0; i--) this.Write(false);
            }
            /// <summary>
            /// Записываем uint число (4 байта)
            /// </summary>
            /// <param name="value">Число</param>
            public void WriteUint(uint value)
            {
                byte[] bytes = BitConverter.GetBytes(value).Reverse().ToArray();
                for (int i = 0; i < bytes.Length; i++)
                    if (bytes[i] == 0)
                        this.Write(false);
                    else
                        this.Write(bytes[i]);
            }
            /// <summary>
            /// Записываем uint число (4 байта)
            /// </summary>
            /// <param name="value">Число</param>
            /// <param name="IsReversingBytes">Обратный порядок байт</param>
            public void WriteUint(uint value, bool IsReversingBytes)
            {
                byte[] bytes;
                if (!IsReversingBytes)
                    bytes = BitConverter.GetBytes(value).Reverse().ToArray();
                else
                    bytes = BitConverter.GetBytes(value);
                for (int i = 0; i < bytes.Length; i++)
                    if (bytes[i] == 0)
                        this.Write(false);
                    else
                        this.Write(bytes[i]);
            }
        }
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
            /// <summary>
            /// Считываем uint число
            /// </summary>
            /// <returns></returns>
            public uint ReadUint() => BitConverter.ToUInt32(this.ReadBytes(4).Reverse().ToArray(), 0);
            /// <summary>
            /// Считываем uint число
            /// </summary>
            /// <param name="IsReversed">Инвертирована ли запись числа</param>
            /// <returns></returns>
            public uint ReadUint(bool IsReversed)
            {
                if (IsReversed)
                    return BitConverter.ToUInt32(this.ReadBytes(4), 0);
                else
                    return BitConverter.ToUInt32(this.ReadBytes(4).Reverse().ToArray(), 0);
            }
        }
        public enum EWorkMode
        {
            /// <summary>
            /// Открыть для чтения
            /// </summary>
            Open,
            /// <summary>
            /// Пересоздавать
            /// </summary>
            Create,
            /// <summary>
            /// Добавить файлы
            /// </summary>
            Update
        }
        public EWorkMode SaveMode
        {
            get
            {
                return _SaveMode;
            }
            set
            {
                if (_SaveMode == value)
                    return;
                else
                    if (IsBusy)
                {
                    return;//Ошибку
                }
                else
                {
                    _SaveMode = value;
                }
            }
        }

        private EWorkMode _SaveMode;
        public List<BigEntry> Entries => _Entries;
        private List<BigEntry> _Entries;
        public string Name => _Name;
        private string _Name;
        public uint EntriesCount => _Entries == null ? 0 : (uint)_Entries.Count;
        public uint FirstOffset => _FirstOffset;
        private uint _FirstOffset = 0;
        public uint Length => _Length;
        private uint _Length = 0;
        public bool IsBusy => _IsBusy;
        private bool _IsBusy = false;
        public bool IsExpiremental => _IsExpiremental;
        private bool _IsExpiremental = false;
        public List<EntryForAdding> EntriesForAdding => _EntriesForAdding;
        private List<EntryForAdding> _EntriesForAdding;
        private BigReader bigReader;
        private BigWriter bigWriter;
        public void AddForPack(EntryForAdding EntryForAdding)
        {
            _EntriesForAdding.Add(EntryForAdding);
            //_FirstOffset += (uint)EntryForAdding.EntryName.Length + 9;
            //_Length += EntryForAdding.Length + (uint)EntryForAdding.EntryName.Length + 9;
        }

        public void AddForPack(List<EntryForAdding> EntriesForAdding)
        {
            _EntriesForAdding.AddRange(EntriesForAdding);

        }

        /// <summary>
        /// Попытка установить новый сейв мод
        /// </summary>
        /// <param name="SaveMode"></param>
        /// <returns>Возвращает результат попытки, если false - архив занят</returns>
        public bool TrySetSaveMode(EWorkMode SaveMode)
        {
            if (_IsBusy) return false;
            this._SaveMode = SaveMode;
            return true;
        }
        public void Open(EWorkMode SaveMode, string BigName)
        {
            if (_IsBusy) ;//Сделать исключение
            _IsBusy = true;
            _Name = BigName;
            _SaveMode = SaveMode;
            _EntriesForAdding = new List<EntryForAdding>();
            _Entries = new List<BigEntry>();
            switch (SaveMode)
            {
            case EWorkMode.Create: return;
            case EWorkMode.Open:
            case EWorkMode.Update:
                {
                    bigReader = new BigReader(new FileStream(this._Name, FileMode.Open, FileAccess.Read));
                    bigReader.ReadBytes(4);
                    _Length = bigReader.ReadUint(true);
                    uint entryCount = bigReader.ReadUint();
                    _FirstOffset = bigReader.ReadUint();
                    int headerLength = 16 + 4 + 4;
                    for (uint i = 0; i < entryCount; i++)
                    {
                        uint offset = bigReader.ReadUint();
                        uint length = bigReader.ReadUint();
                        string name = bigReader.ReadString(Encoding.ASCII.GetChars(new byte[] { 0 })[0]);
                        headerLength += 4 + 4 + name.Length + 1;
                        _Entries.Add(new BigEntry(name, length, offset));
                        OpenProgressEvent?.BeginInvoke(entryCount, i + 1, null, null);
                    }
                    bigReader.Close();
                    bigReader.Dispose();
                    _IsExpiremental = headerLength - 1 != _FirstOffset;
                }
                return;
            }
            _IsBusy = false;
        }
        public delegate void OpenProgressEventHandler(uint count, uint progress);
        public event OpenProgressEventHandler OpenProgressEvent;
        public void StartPacking()
        {
            if (_IsBusy) ;//Сделать исключение
            if (_SaveMode == EWorkMode.Open) ;//Тоже исключение
            _IsBusy = true;
            switch (SaveMode)
            {
            case EWorkMode.Create:
                {
                    bigWriter = new BigWriter(new FileStream(this._Name + ".tmp", FileMode.Create, FileAccess.Write));
                    _Length = 0;
                    _FirstOffset = 16 + 4 + 4 - 1; //16 начальных байта, L225, 4 нулевых
                    for (int i = 0; i < _EntriesForAdding.Count; i++)
                    {
                        _FirstOffset += (uint)_EntriesForAdding[i].EntryName.Length + 9;
                        _Length += _EntriesForAdding[i].Length;
                    }
                    _Length += _FirstOffset + 1;
                    bigWriter.WriteString("BIGF");
                    bigWriter.WriteUint(_Length, true);
                    bigWriter.WriteUint((uint)_EntriesForAdding.Count);
                    bigWriter.WriteUint(_FirstOffset);
                    uint currentOffset = _FirstOffset + 1;
                    _Entries = new List<BigEntry>();
                    for (int i = 0; i < _EntriesForAdding.Count; i++)
                    {
                        bigWriter.WriteUint(currentOffset);
                        bigWriter.WriteUint(_EntriesForAdding[i].Length);
                        bigWriter.WriteString(_EntriesForAdding[i].EntryName);
                        bigWriter.WriteNullTerminatedByte();
                        _Entries.Add(new BigEntry(_EntriesForAdding[i].EntryName, _EntriesForAdding[i].Length, currentOffset));
                        currentOffset += _EntriesForAdding[i].Length;
                    }
                    bigWriter.WriteString("L225");
                    bigWriter.WriteNullTerminatedBytes(4);
                    for (int i = 0; i < _EntriesForAdding.Count; i++)
                    {
                        switch (_EntriesForAdding[i].AddingType)
                        {
                        case EntryForAdding.EAddingType.File:
                            {
                                using (var reader = new BinaryReader(new FileStream(_EntriesForAdding[i].FileName, FileMode.Open, FileAccess.Read), Encoding.ASCII))
                                    reader.BaseStream.CopyTo(bigWriter.BaseStream);
                            }
                            continue;
                        case EntryForAdding.EAddingType.Entry:
                            {
                                using (var reader = new BinaryReader(new FileStream(_EntriesForAdding[i].FileName, FileMode.Open, FileAccess.Read), Encoding.ASCII))
                                {
                                    reader.BaseStream.Seek(_EntriesForAdding[i].Offset, SeekOrigin.Begin);
                                    bigWriter.Write(reader.ReadBytes((int)_EntriesForAdding[i].Length));
                                }
                            }
                            continue;
                        }
                    }
                    bigWriter.Close();
                    bigWriter.Dispose();
                    File.Move(Name + ".tmp", Name);
                }
                break;
            case EWorkMode.Open:
            case EWorkMode.Update:
                {
                    bigWriter = new BigWriter(new FileStream(this._Name + ".tmp", FileMode.Create, FileAccess.Write));
                    _Length = 0;
                    _FirstOffset = 16 + 4 + 4 - 1; //16 начальных байта, L225, 4 нулевых
                    for (int i = 0; i < _EntriesForAdding.Count; i++)
                    {
                        _FirstOffset += (uint)_EntriesForAdding[i].EntryName.Length + 9;
                        _Length += _EntriesForAdding[i].Length;
                    }
                    for (int i = 0; i < _Entries.Count; i++)
                    {
                        _FirstOffset += (uint)_Entries[i].Name.Length + 9;
                        _Length += _Entries[i].Length;
                    }
                    _Length += _FirstOffset + 1;
                    bigWriter.WriteString("BIGF");
                    bigWriter.WriteUint(_Length, true);
                    bigWriter.WriteUint((uint)(_EntriesForAdding.Count + _Entries.Count));
                    bigWriter.WriteUint(_FirstOffset);
                    uint currentOffset = _FirstOffset + 1;
                    for (int i = 0; i < _Entries.Count; i++)
                    {
                        bigWriter.WriteUint(currentOffset);
                        bigWriter.WriteUint(_Entries[i].Length);
                        bigWriter.WriteString(_Entries[i].Name);
                        bigWriter.WriteNullTerminatedByte();
                        currentOffset += _Entries[i].Length;
                    }
                    var newEntries = new List<BigEntry>();
                    for (int i = 0; i < _EntriesForAdding.Count; i++)
                    {
                        bigWriter.WriteUint(currentOffset);
                        bigWriter.WriteUint(_EntriesForAdding[i].Length);
                        bigWriter.WriteString(_EntriesForAdding[i].EntryName);
                        bigWriter.WriteNullTerminatedByte();
                        newEntries.Add(new BigEntry(_EntriesForAdding[i].EntryName, _EntriesForAdding[i].Length, currentOffset));
                        currentOffset += _EntriesForAdding[i].Length;
                    }
                    bigWriter.WriteString("L225");
                    bigWriter.WriteNullTerminatedBytes(4);
                    for (int i = 0; i < _Entries.Count; i++)
                    {
                        using (var reader = new BinaryReader(new FileStream(this.Name, FileMode.Open, FileAccess.Read), Encoding.ASCII))
                        {
                            reader.BaseStream.Seek(_Entries[i].Offset, SeekOrigin.Begin);
                            bigWriter.Write(reader.ReadBytes((int)_Entries[i].Length));
                        }
                    }
                    _Entries.AddRange(newEntries);
                    for (int i = 0; i < _EntriesForAdding.Count; i++)
                    {
                        switch (_EntriesForAdding[i].AddingType)
                        {
                        case EntryForAdding.EAddingType.File:
                            {
                                using (var reader = new BinaryReader(new FileStream(_EntriesForAdding[i].FileName, FileMode.Open, FileAccess.Read), Encoding.ASCII))
                                    reader.BaseStream.CopyTo(bigWriter.BaseStream);
                            }
                            continue;
                        case EntryForAdding.EAddingType.Entry:
                            {
                                using (var reader = new BinaryReader(new FileStream(_EntriesForAdding[i].FileName, FileMode.Open, FileAccess.Read), Encoding.ASCII))
                                {
                                    reader.BaseStream.Seek(_EntriesForAdding[i].Offset, SeekOrigin.Begin);
                                    bigWriter.Write(reader.ReadBytes((int)_EntriesForAdding[i].Length));
                                }
                            }
                            continue;
                        }
                    }
                    bigWriter.Close();
                    bigWriter.Dispose();
                    //File.Move(Name + ".tmp", Name);
                }
                break;
            }
        }
    }
}
