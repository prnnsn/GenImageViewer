using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace GenImageViewer
{
    public class GameResource
    {
        /// <summary>
        /// BIG file is main flie type for packing game resources
        /// </summary>
        public class BIGFile
        {
            public string FileName;
        }
        /// <summary>
        /// If file (TGA or MappedFile) located in BIG file then its not null
        /// </summary>
        public class BIGResource
        {
            public BIGFile BIGRFile;
            public uint Offset, Lenght;
            public string Name;
        }
        /// <summary>
        /// MappedImage coordinates on TGA file
        /// </summary>
        public class MappedCoordinates : ICloneable
        {
            public int Left, Top, Right, Bottom;
            public override string ToString() =>
                $"  Coords = " +
                    $"Left:{Left} " +
                    $"Top:{Top} " +
                    $"Right:{Right} " +
                    $"Bottom:{Bottom}";

            public object Clone() => MemberwiseClone();
        }
        /// <summary>
        /// It's just a canvas size for release MappedCoordinates
        /// If TGA size is different than texture size, then need calculate ratio factor for every side
        /// </summary>
        public class MappedTextureSize : ICloneable
        {
            public int Width, Height;
            public object Clone() => MemberwiseClone();
        }
        /// <summary>
        /// INI file with list of MappedImages
        /// </summary>
        public class MappedFile
        {
            private interface IFileLocation
            {
                List<string> GetMappedImageLines(MappedFile mappedFile);
            }

            private class Mapp_InFile : IFileLocation
            {
                public List<string> GetMappedImageLines(MappedFile mappedFile)
                {
                    List<string> lines = new List<string>();
                    using (FileStream fs = new FileStream($@"{mappedFile.GameResource.MainFolder}\Data\INI\MappedImages\{mappedFile.Name}", FileMode.Open, FileAccess.Read))
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        while (!sr.EndOfStream)
                        {
                            string s = sr.ReadLine();
                            if (!string.IsNullOrEmpty(s))
                            {
                                int pos = s.IndexOf(';');
                                if (pos != -1)
                                    s = s.Substring(0, pos);
                                s = s.Trim();
                                if (s.Length != 0)
                                    lines.Add(s);
                            }
                        }
                    }
                    return lines;
                }
            }
            private class Mapp_InBIG : IFileLocation
            {
                public List<string> GetMappedImageLines(MappedFile mappedFile)
                {
                    List<string> lines = new List<string>();
                    string s;
                    using (FileStream fs = new FileStream($@"{mappedFile.GameResource.MainFolder}\{mappedFile.BIGResource.BIGRFile.FileName}", FileMode.Open, FileAccess.Read))
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        br.BaseStream.Position = mappedFile.BIGResource.Offset;
                        s = Encoding.ASCII.GetString(br.ReadBytes((int)mappedFile.BIGResource.Lenght));
                    }
                    string[] tempLines = s.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                    for (int k = 0; k < tempLines.Length; k++)
                    {
                        s = tempLines[k];
                        if (!string.IsNullOrEmpty(s))
                        {
                            int pos = s.IndexOf(';');
                            if (pos != -1)
                                s = s.Substring(0, pos);
                            s = s.Trim();
                            if (s.Length != 0)
                                lines.Add(s);
                        }
                    }
                    return lines;
                }
            }

            private IFileLocation _fileLocation;
            private GameResource _gameResource;
            private BIGResource _bigResource;

            public GameResource GameResource => _gameResource;
            public static int Sort(MappedFile file1, MappedFile file2)
            {
                if (file2.Name == null && file1.Name == null) return 0;
                else if (file2.Name == null) return -1;
                else if (file1.Name == null) return 1;
                else return file2.Name.CompareTo(file1.Name);
            }
            public string Name;
            public BIGResource BIGResource
            {
                get => _bigResource;
                set
                {
                    if (value == null)
                        _fileLocation = new Mapp_InFile();
                    else
                        _fileLocation = new Mapp_InBIG();

                    _bigResource = value;
                }
            }
            public List<MappedImage> MappedImages;
            public MappedFile(GameResource gameResource)
            {
                _gameResource = gameResource;
            }
            public List<MappedImage> GetMappedImagesFromFile()
            {
                List<string> lines = _fileLocation.GetMappedImageLines(this);
                List<MappedImage> mappedImages = new List<MappedImage>();

                for (int n = 0; n < lines.Count; n++)
                {
                    MappedImage mappedImage = GetMappedImageFromLines(lines, n);
                    if (mappedImage == null)
                        continue;
                    else n += 6;
                    mappedImages.Add(mappedImage);
                }

                return mappedImages;
            }
            private static MappedImage GetMappedImageFromLines(List<string> lines, int index)
            {
                MappedImage mappedImage = new MappedImage();
                try
                {
                    if (!lines[index].StartsWith("MappedImage", StringComparison.OrdinalIgnoreCase)) return null;
                    {
                        string s = lines[index].Trim();
                        StringBuilder sb = new StringBuilder();
                        for (int i = 12; i < s.Length; i++)
                            if (Char.IsLetterOrDigit(s[i]) || (s[i] == '_') || (s[i] == '-'))
                            {
                                sb.Append(s[i]);
                            }
                            else
                            {
                                break;
                            }
                        mappedImage.Name = sb.ToString();
                        if (string.IsNullOrEmpty(mappedImage.Name.ToString())) return null;
                    }
                    mappedImage.Texture = lines[index + 1].Substring(lines[index + 1].LastIndexOf('=') + 1).TrimStart();
                    mappedImage.TextureSize = new MappedTextureSize()
                    {
                        Width = int.Parse(lines[index + 2].Substring(lines[index + 2].LastIndexOf('=') + 1).TrimStart()),
                        Height = int.Parse(lines[index + 3].Substring(lines[index + 3].LastIndexOf('=') + 1).TrimStart())
                    };

                    string[] tempLines = lines[index + 4].Split(':').Select(x => x.TrimStart()).ToArray();
                    mappedImage.Coords = new MappedCoordinates()
                    {
                        Left = int.Parse(tempLines[1].Substring(0, tempLines[1].IndexOf(' '))),
                        Top = int.Parse(tempLines[2].Substring(0, tempLines[2].IndexOf(' '))),
                        Right = int.Parse(tempLines[3].Substring(0, tempLines[3].IndexOf(' '))),
                        Bottom = int.Parse(tempLines[4])
                    };
                    mappedImage.Status = lines[index + 5].Substring(lines[index + 5].LastIndexOf('=') + 1).TrimStart();
                }
                catch
                {
                    return null;
                }
                return mappedImage;
            }
        }
        /// <summary>
        /// Data of mapped image in TGA from MappedFile (MappedImages ini file)
        /// </summary>
        public class MappedImage : ICloneable
        {
            public string Name;
            public string Texture;
            public MappedTextureSize TextureSize;
            public MappedCoordinates Coords;
            public string Status;
            public MappedFile ParentMappedFile;
            public TGAFile TGAFile;

            /// <summary>
            /// Get MappedImage code by MappedImage object
            /// </summary>
            /// <param name="mappedImage">MappedImage object</param>
            /// <returns>MappedImage code (string)</returns>
            public static string GetMappedCode(MappedImage mappedImage)
            {
                return
                    $"MappedImage {mappedImage.Name}\r\n" +
                    $"  Texture {mappedImage.Texture}\r\n" +
                    $"  TextureWidth = {mappedImage.TextureSize.Width}\r\n" +
                    $"  TextureHeight = {mappedImage.TextureSize.Height}\r\n" +
                    $"  Coords = " +
                            $"Left: {mappedImage.Coords.Left} " +
                            $"Top: {mappedImage.Coords.Top} " +
                            $"Right: {mappedImage.Coords.Right} " +
                            $"Bottom: {mappedImage.Coords.Bottom}\r\n" +
                    $"  Status = {mappedImage.Status}\r\n" +
                    $"End";
            }
            /// <summary>
            /// Save MappedImage code to file (for example, INI)
            /// </summary>
            /// <param name="mappedImage">MappedImage object</param>
            /// <param name="name">Name for save</param>
            /// <param name="rewrite">If rewrite = true, then create new file with code, else add code in exist file</param>
            public static void SaveMappedCode(MappedImage mappedImage, string name, bool rewrite)
            {
                if (rewrite)
                {
                    File.WriteAllText(name, GetMappedCode(mappedImage));
                }
                else
                {
                    using (var stream = File.AppendText(name))
                    {
                        stream.WriteLine(GetMappedCode(mappedImage));
                        stream.WriteLine();
                    }
                }
            }

            public void CopyMappCodeToClipboard()
            {
                string s =
                    $"MappedImage {Name}\r\n" +
                    $"  Texture = {Texture}\r\n" +
                    $"  TextureWidth = {TextureSize.Width}\r\n" +
                    $"  TextureHeight = {TextureSize.Height}\r\n" +
                    $"  {Coords.ToString()}\r\n" +
                    $"  Status = {Status}\r\n" +
                    $"End";
                Clipboard.SetText(s);
            }
            /// <summary>
            /// Get cuted MappedImage (sizes) to TGA file sizes
            /// </summary>
            /// <param name="tgaWidth"></param>
            /// <param name="tgaHeight"></param>
            /// <returns></returns>
            public MappedImage GetCutedMappByTGASize()
            {
                int height = TextureSize.Height;
                int width = TextureSize.Width;
                MappedCoordinates coords = (MappedCoordinates)Coords.Clone();

                if (height != TGAFile.Height)
                {
                    double k = (double)TGAFile.Height / (double)height;
                    coords.Top = (int)Math.Round((double)coords.Top * k, MidpointRounding.AwayFromZero);
                    coords.Bottom = (int)Math.Round((double)coords.Bottom * k, MidpointRounding.AwayFromZero);
                    if (coords.Bottom > TGAFile.Height) coords.Bottom = TGAFile.Height;
                }
                height = coords.Bottom - coords.Top;

                if (width != TGAFile.Width)
                {
                    double k = (double)TGAFile.Width / (double)width;
                    coords.Left = (int)Math.Round((double)coords.Left * k, MidpointRounding.AwayFromZero);
                    coords.Right = (int)Math.Round((double)coords.Right * k, MidpointRounding.AwayFromZero);
                    if (coords.Right > TGAFile.Width) coords.Right = TGAFile.Width;
                }
                width = coords.Right - coords.Left;

                return new MappedImage()
                {
                    TextureSize = new MappedTextureSize()
                    {
                        Width = width,
                        Height = height
                    },
                    Coords = coords
                };
            }
            /// <summary>
            /// Save image from TGA by MappedImage variables
            /// </summary>
            /// <param name="fileName">Name and path to save</param>
            public void Save(string fileName)
            {
                Bitmap bitmap = TGAFile.GetBitmap();

                MappedImage mappedImage = GetCutedMappByTGASize();

                Bitmap cameo = new Bitmap(mappedImage.TextureSize.Width, mappedImage.TextureSize.Height);
                for (int y = 0; y < cameo.Height; y++)
                    for (int x = 0; x < cameo.Width; x++)
                        cameo.SetPixel(x, y, bitmap.GetPixel(x + mappedImage.Coords.Left, y + mappedImage.Coords.Top));

                bitmap.Dispose();

                if (File.Exists(fileName))
                    File.Delete(fileName);
                TGA tGA = TGA.FromBitmap(cameo, false, false);
                tGA.Save(fileName);
                cameo.Dispose();
            }
            /// <summary>
            /// Save image and mapp code from TGA by MappedImage variables
            /// </summary>
            /// <param name="mappedImageSource">Source mapped image</param>
            /// <param name="tgaFileName">Name and path to save tga file</param>
            /// <param name="iniFileName">Name and path to save ini file (mapp code)</param>
            /// <param name="rewriteIni">If true, then create new file with code, else add code in exist file</param>
            public static void SaveMappAndTGA(MappedImage mappedImageSource, string tgaFileName, string iniFileName, bool rewriteIni)
            {
                Bitmap bitmap = mappedImageSource.TGAFile.GetBitmap();

                MappedImage mappedImage = mappedImageSource.GetCutedMappByTGASize();
                mappedImage.Name = mappedImageSource.Name;
                mappedImage.Texture = mappedImageSource.Texture;
                mappedImage.Status = mappedImageSource.Status;

                Bitmap cameo = new Bitmap(mappedImage.TextureSize.Width, mappedImage.TextureSize.Height);
                for (int y = 0; y < cameo.Height; y++)
                    for (int x = 0; x < cameo.Width; x++)
                        cameo.SetPixel(x, y, bitmap.GetPixel(x + mappedImage.Coords.Left, y + mappedImage.Coords.Top));

                bitmap.Dispose();

                if (File.Exists(tgaFileName))
                    File.Delete(tgaFileName);
                TGA tGA = TGA.FromBitmap(cameo, false, false);
                tGA.Save(tgaFileName);

                mappedImage.Coords = new MappedCoordinates()
                {
                    Top = 0,
                    Left = 0,
                    Right = cameo.Width,
                    Bottom = cameo.Height
                };

                cameo.Dispose();

                SaveMappedCode(mappedImage, iniFileName, rewriteIni);
            }

            public object Clone() => MemberwiseClone();
        }
        /// <summary>
        /// TGA Location in game resources (Art\Textrures and itc)
        /// </summary>
        public class TGALocation
        {
            public TGALocation(string location) => _location = location;
            private string _location;
            public static explicit operator string(TGALocation tgaLocation) => tgaLocation._location;
        }
        /// <summary>
        /// Main file type for game imeges
        /// </summary>
        public class TGAFile
        {
            private static string _artTextures = @"Art\Textures";
            private static string _dataEnglishArtTextures = @"Data\English\Art\Textures";
            private interface ITGALocation
            {
                string GetLocation();
                ETGALocation ETGALocation();
            }
            private class TGA_ArtTextures : ITGALocation
            {
                public ETGALocation ETGALocation() => TGAFile.ETGALocation.ArtTextures;

                public string GetLocation() => _artTextures;
            }
            private class TGA_DataEnglishArtTextures : ITGALocation
            {
                public ETGALocation ETGALocation() => TGAFile.ETGALocation.DataEnglishArtTextures;
                public string GetLocation() => _dataEnglishArtTextures;
            }

            private interface IFileLocation
            {
                Bitmap GetBitmap(TGAFile tgaFile);
                void Save(TGAFile tgaFile, string fileName);
                void InitSize(TGAFile tgaFile);
            }
            private class TGA_InFile : IFileLocation
            {
                public Bitmap GetBitmap(TGAFile tgaFile)
                {
                    return TGA.FromFile($@"{tgaFile.GameResource.MainFolder}\{tgaFile.TGALocation}\{tgaFile.Name}").ToBitmap();
                }

                public void InitSize(TGAFile tgaFile)
                {
                    byte[] buffer;
                    using (FileStream fs = new FileStream($@"{tgaFile.GameResource.MainFolder}\{tgaFile.TGALocation}\{tgaFile.Name}", FileMode.Open, FileAccess.Read))
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        br.BaseStream.Position = 12;
                        buffer = br.ReadBytes(2);
                        tgaFile.Width = BitConverter.ToInt32(new byte[] { buffer[0], buffer[1], 0, 0 }, 0);

                        buffer = br.ReadBytes(2);
                        tgaFile.Height = BitConverter.ToInt32(new byte[] { buffer[0], buffer[1], 0, 0 }, 0);
                    }
                }

                public void Save(TGAFile tgaFile, string fileName)
                {
                    File.Copy($@"{tgaFile.GameResource.MainFolder}\{tgaFile.TGALocation}\{tgaFile.Name}", fileName, true);
                }
            }
            private class TGA_InBIG : IFileLocation
            {
                public Bitmap GetBitmap(TGAFile tgaFile)
                {
                    using (FileStream fs = new FileStream($@"{tgaFile.GameResource.MainFolder}\{tgaFile.BIGResource.BIGRFile.FileName}", FileMode.Open, FileAccess.Read))
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        br.BaseStream.Position = tgaFile.BIGResource.Offset;
                        byte[] bytes = br.ReadBytes((int)tgaFile.BIGResource.Lenght);
                        using (MemoryStream ms = new MemoryStream(bytes))
                        {
                            return TGA.FromBytes(ms.ToArray()).ToBitmap();
                        }
                    }
                }

                public void InitSize(TGAFile tgaFile)
                {
                    byte[] buffer;
                    using (FileStream fs = new FileStream($@"{tgaFile.GameResource.MainFolder}\{tgaFile.BIGResource.BIGRFile.FileName}", FileMode.Open, FileAccess.Read))
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        br.BaseStream.Position = tgaFile.BIGResource.Offset + 12;
                        buffer = br.ReadBytes(2);
                        tgaFile.Width = BitConverter.ToInt32(new byte[] { buffer[0], buffer[1], 0, 0 }, 0);

                        buffer = br.ReadBytes(2);
                        tgaFile.Height = BitConverter.ToInt32(new byte[] { buffer[0], buffer[1], 0, 0 }, 0);
                    }
                }

                public void Save(TGAFile tgaFile, string fileName)
                {
                    using (FileStream fsOpen = new FileStream($@"{tgaFile.GameResource.MainFolder}\{tgaFile.BIGResource.BIGRFile.FileName}", FileMode.Open, FileAccess.Read))
                    using (BinaryReader br = new BinaryReader(fsOpen))
                    {
                        br.BaseStream.Position = tgaFile.BIGResource.Offset;
                        using (FileStream fsWrite = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                        using (BinaryWriter sw = new BinaryWriter(fsWrite))
                            sw.Write(br.ReadBytes((int)tgaFile.BIGResource.Lenght));
                    }
                }
            }

            private ITGALocation _tgaLocation;
            private IFileLocation _fileLocation;
            private GameResource _gameResource;
            private BIGResource _bigResource;

            public enum ETGALocation
            {
                ArtTextures,
                DataEnglishArtTextures
            }

            public GameResource GameResource => _gameResource;
            public ETGALocation Location
            {
                get => _tgaLocation.ETGALocation();
                set
                {
                    switch (value)
                    {
                        case ETGALocation.ArtTextures: _tgaLocation = new TGA_ArtTextures(); break;
                        case ETGALocation.DataEnglishArtTextures: _tgaLocation = new TGA_DataEnglishArtTextures(); break;
                    }
                }
            }
            public string TGALocation => _tgaLocation.GetLocation();
            public string Name;
            public int Width;
            public int Height;
            public BIGResource BIGResource
            {
                get => _bigResource;
                set
                {
                    if (value == null)
                        _fileLocation = new TGA_InFile();
                    else
                        _fileLocation = new TGA_InBIG();
                    _bigResource = value;
                }
            }
            public List<MappedImage> MappedImages;
            public TGAFile(GameResource gameResource)
            {
                _gameResource = gameResource;
            }
            /// <summary>
            /// Get bitmap from TGA image
            /// </summary>
            /// <returns>System.Drawing.Bitmap</returns>
            public Bitmap GetBitmap() => _fileLocation.GetBitmap(this);
            public void Save(string fileName) => _fileLocation.Save(this, fileName);
            /// <summary>
            /// Init TGA size from existing tga file
            /// </summary>
            public void InitSize() => _fileLocation.InitSize(this);
        }

        private string _mainFolder;
        private List<BIGFile> _bigFiles = new List<BIGFile>();
        private List<MappedFile> _mappedFiles = new List<MappedFile>();
        private List<MappedImage> _mappedImages = new List<MappedImage>();

        private List<TGAFile> _tgaFiles = new List<TGAFile>();

        private List<MappedFile> _resourceMappedFiles;
        private List<MappedImage> _resourceMappedImages;

        private List<TGAFile> _resourceTGAFiles_Art;
        private List<TGAFile> _resourceTGAFiles_Data;

        public string MainFolder => _mainFolder;

        public List<BIGFile> BIGFiles { get => _bigFiles; }
        public List<MappedFile> MappedFiles { get => _mappedFiles; }
        public List<MappedImage> MappedImages { get => _mappedImages; }
        public List<TGAFile> TGAFiles { get => _tgaFiles; }

        public void Load(string mainFolder)
        {
            _mainFolder = mainFolder;

            _resourceMappedFiles = new List<MappedFile>();
            _resourceTGAFiles_Art = new List<TGAFile>();
            _resourceTGAFiles_Data = new List<TGAFile>();
            LoadFromBIGs();
            LoadFromDirectory();

            GetResourceMappedImages();

            //Delete useless resources, make links (pointers) with resources
            MappedImages.Clear();
            MappedFiles.Clear();
            TGAFiles.Clear();

            for (int i = 0; i < _resourceMappedImages.Count; i++)
            {
                int index;
                List<TGAFile> resourceTGAFiles = CheckResourceTGA(_resourceMappedImages[i].Texture, out index);

                if (index == -1)
                {
                    TGAFile tgaFile = TGAFiles.FindTGA(_resourceMappedImages[i].Texture);
                    if (tgaFile != null)
                    {
                        MappedFile mappedFile;

                        for (int k = 0; k < MappedFiles.Count; k++)
                            if (_resourceMappedImages[i].ParentMappedFile.Name == MappedFiles[k].Name)
                            {
                                mappedFile = MappedFiles[k];
                                goto close;
                            }
                        mappedFile = new MappedFile(this)
                        {
                            BIGResource = _resourceMappedImages[i].ParentMappedFile.BIGResource,
                            MappedImages = new List<MappedImage>(),
                            Name = _resourceMappedImages[i].ParentMappedFile.Name
                        };
                        MappedFiles.Add(mappedFile);
                    close:;

                        MappedImage mappedImage = new MappedImage()
                        {
                            TGAFile = tgaFile,
                            Coords = _resourceMappedImages[i].Coords,
                            Name = _resourceMappedImages[i].Name,
                            Status = _resourceMappedImages[i].Status,
                            Texture = _resourceMappedImages[i].Texture,
                            TextureSize = _resourceMappedImages[i].TextureSize,
                            ParentMappedFile = mappedFile
                        };

                        mappedFile.MappedImages.Add(mappedImage);
                        MappedImages.Add(mappedImage);
                        tgaFile.MappedImages.Add(mappedImage);
                    }
                    else continue;
                }
                else
                {
                    TGAFile tgaFile = new TGAFile(this)
                    {
                        Location = resourceTGAFiles[index].Location,
                        BIGResource = resourceTGAFiles[index].BIGResource,
                        MappedImages = new List<MappedImage>(),
                        Name = resourceTGAFiles[index].Name
                    };
                    tgaFile.InitSize();
                    TGAFiles.Add(tgaFile);
                    resourceTGAFiles.RemoveAt(index);

                    MappedFile mappedFile;

                    for (int k = 0; k < MappedFiles.Count; k++)
                        if (_resourceMappedImages[i].ParentMappedFile.Name == MappedFiles[k].Name)
                        {
                            mappedFile = MappedFiles[k];
                            goto close;
                        }
                    mappedFile = new MappedFile(this)
                    {
                        BIGResource = _resourceMappedImages[i].ParentMappedFile.BIGResource,
                        MappedImages = new List<MappedImage>(),
                        Name = _resourceMappedImages[i].ParentMappedFile.Name
                    };
                    MappedFiles.Add(mappedFile);
                close:;

                    MappedImage mappedImage = new MappedImage()
                    {
                        TGAFile = tgaFile,
                        Coords = _resourceMappedImages[i].Coords,
                        Name = _resourceMappedImages[i].Name,
                        Status = _resourceMappedImages[i].Status,
                        Texture = _resourceMappedImages[i].Texture,
                        TextureSize = _resourceMappedImages[i].TextureSize,
                        ParentMappedFile = mappedFile
                    };

                    mappedFile.MappedImages.Add(mappedImage);
                    MappedImages.Add(mappedImage);
                    tgaFile.MappedImages.Add(mappedImage);

                }
            }
        }

        //---BIG Loading---//
        private void LoadFromBIGs()
        {
            _bigFiles = Directory.GetFiles(MainFolder, "*.big", SearchOption.TopDirectoryOnly).Reverse().Select(x => new BIGFile() { FileName = x }).ToList();

            foreach (BIGFile bIGFile in BIGFiles)
            {
                BigArchive bigArchive = new BigArchive();
                bigArchive.Open(BigArchive.EOpenMode.Open, bIGFile.FileName);
                bIGFile.FileName = bIGFile.FileName.Substring(MainFolder.Length + 1);

                for (int i = bigArchive.Entries.Count - 1; i > -1; i--)
                {
                    string fileExtension = System.IO.Path.GetExtension(bigArchive.Entries[i].Name).ToLower();
                    if (fileExtension == ".ini" && bigArchive.Entries[i].Name.StartsWith(@"Data\INI\MappedImages"))
                        AddMappedFileFromBIG(bigArchive, bIGFile, i);
                    else if (fileExtension == ".tga")
                    {
                        if (bigArchive.Entries[i].Name.StartsWith(@"Data\English") && bigArchive.Entries[i].Name.StartWith(@"Art\Textures", 13))
                            AddDataTGAFromBIG(bigArchive, bIGFile, i);
                        else if (bigArchive.Entries[i].Name.StartsWith(@"Art\Textures"))
                            AddArtTGAFromBIG(bigArchive, bIGFile, i);
                    }
                }
            }
        }
        private void AddMappedFileFromBIG(BigArchive bigArchive, BIGFile bigFile, int entryIndex)
        {
            string fileName = bigArchive.Entries[entryIndex].Name.Remove(0, 21 + 1); // (Data\INI\MappedImages) = 21
            MappedFile resourceMappedFile = GetResourceMappedFile(fileName);
            if (resourceMappedFile == null)
            {
                _resourceMappedFiles.Add(new MappedFile(this)
                {
                    BIGResource = new BIGResource()
                    {
                        BIGRFile = bigFile,
                        Lenght = bigArchive.Entries[entryIndex].Length,
                        Offset = bigArchive.Entries[entryIndex].Offset,
                        Name = bigArchive.Entries[entryIndex].Name
                    },
                    Name = fileName
                });
            }
            else
            {
                resourceMappedFile.BIGResource = new BIGResource()
                {
                    BIGRFile = bigFile,
                    Lenght = bigArchive.Entries[entryIndex].Length,
                    Offset = bigArchive.Entries[entryIndex].Offset,
                    Name = bigArchive.Entries[entryIndex].Name
                };
            }
        }
        private void AddArtTGAFromBIG(BigArchive bigArchive, BIGFile bigFile, int entryIndex)
        {
            string name = System.IO.Path.GetFileName(bigArchive.Entries[entryIndex].Name);
            for (int k = 0; k < _resourceTGAFiles_Data.Count; k++)
                if (_resourceTGAFiles_Data[k].Name == name)
                    goto close;
            for (int k = 0; k < _resourceTGAFiles_Art.Count; k++)
                if (_resourceTGAFiles_Art[k].Name == name)
                {
                    _resourceTGAFiles_Art[k].BIGResource.BIGRFile = bigFile;
                    _resourceTGAFiles_Art[k].BIGResource.Lenght = bigArchive.Entries[entryIndex].Length;
                    _resourceTGAFiles_Art[k].BIGResource.Offset = bigArchive.Entries[entryIndex].Offset;
                    goto close;
                }
            _resourceTGAFiles_Art.Add(new TGAFile(this)
            {
                Location = TGAFile.ETGALocation.ArtTextures,
                Name = System.IO.Path.GetFileName(bigArchive.Entries[entryIndex].Name),
                BIGResource = new BIGResource()
                {
                    BIGRFile = bigFile,
                    Lenght = bigArchive.Entries[entryIndex].Length,
                    Offset = bigArchive.Entries[entryIndex].Offset,
                    Name = bigArchive.Entries[entryIndex].Name
                }
            });
        close:;
        }
        private void AddDataTGAFromBIG(BigArchive bigArchive, BIGFile bigFile, int entryIndex)
        {
            string name = System.IO.Path.GetFileName(bigArchive.Entries[entryIndex].Name);
            for (int k = 0; k < _resourceTGAFiles_Data.Count; k++)
                if (_resourceTGAFiles_Data[k].Name == name)
                {
                    _resourceTGAFiles_Data[k].BIGResource.BIGRFile = bigFile;
                    _resourceTGAFiles_Data[k].BIGResource.Lenght = bigArchive.Entries[entryIndex].Length;
                    _resourceTGAFiles_Data[k].BIGResource.Offset = bigArchive.Entries[entryIndex].Offset;
                    goto close;
                }
            _resourceTGAFiles_Data.Add(new TGAFile(this)
            {
                Location = TGAFile.ETGALocation.DataEnglishArtTextures,
                Name = name,
                BIGResource = new BIGResource()
                {
                    BIGRFile = bigFile,
                    Lenght = bigArchive.Entries[entryIndex].Length,
                    Offset = bigArchive.Entries[entryIndex].Offset,
                    Name = bigArchive.Entries[entryIndex].Name
                }
            });

            for (int k = 0; k < _resourceTGAFiles_Art.Count; k++)
                if (_resourceTGAFiles_Art[k].Name == name)
                {
                    _resourceTGAFiles_Art.RemoveAt(k);
                    break;
                }
            close:;
        }
        //---BIG Loading---//

        //---Directory Loading---//
        private void LoadFromDirectory()
        {
            List<string> files;
            if (Directory.Exists($@"{MainFolder}\Data\INI\MappedImages"))
            {
                files = Directory.GetFiles($@"{MainFolder}\Data\INI\MappedImages", "*.ini", SearchOption.AllDirectories).Select(x => x.Remove(0, 21 + MainFolder.Length + 2)).ToList();
                for (int i = files.Count - 1; i > -1; i--)
                    AddMappedFileFromDir(files[i]);
                _resourceMappedFiles.Sort(MappedFile.Sort);
            }

            if (Directory.Exists($@"{MainFolder}\Data\English\Art\Textures"))
            {
                files = Directory.GetFiles($@"{MainFolder}\Data\English\Art\Textures", "*.tga", SearchOption.AllDirectories).Select(x => x.Remove(0, 25 + MainFolder.Length)).ToList();
                for (int i = files.Count - 1; i > -1; i--)
                    AddDataTGAFromDir(files[i]);
            }

            if (Directory.Exists($@"{MainFolder}\Art\Textures"))
            {
                files = Directory.GetFiles($@"{MainFolder}\Art\Textures", "*.tga", SearchOption.AllDirectories).Select(x => x.Remove(0, 12 + MainFolder.Length)).ToList();
                for (int i = files.Count - 1; i > -1; i--)
                    AddArtTGAFromDir(files[i]);
            }
        }
        private void AddMappedFileFromDir(string fileName)
        {
            MappedFile resourceMappedFile = GetResourceMappedFile(fileName);
            if (resourceMappedFile == null)
            {
                _resourceMappedFiles.Add(new MappedFile(this)
                {
                    BIGResource = null,
                    Name = fileName
                });
            }
            else
            {
                resourceMappedFile.BIGResource = null;
            }
        }
        private void AddArtTGAFromDir(string fileName)
        {
            string name = System.IO.Path.GetFileName(fileName);
            for (int k = 0; k < _resourceTGAFiles_Data.Count; k++)
                if (_resourceTGAFiles_Data[k].Name == name)
                    goto close;
            for (int k = 0; k < _resourceTGAFiles_Art.Count; k++)
                if (_resourceTGAFiles_Art[k].Name == name)
                {
                    _resourceTGAFiles_Art[k].BIGResource = null;
                    goto close;
                }
            _resourceTGAFiles_Art.Add(new TGAFile(this)
            {
                Location = TGAFile.ETGALocation.ArtTextures,
                Name = name,
                BIGResource = null
            });
        close:;
        }
        private void AddDataTGAFromDir(string fileName)
        {
            string name = System.IO.Path.GetFileName(fileName);
            for (int k = 0; k < _resourceTGAFiles_Data.Count; k++)
                if (_resourceTGAFiles_Data[k].Name == name)
                {
                    _resourceTGAFiles_Data[k].BIGResource = null;
                    goto close;
                }
            _resourceTGAFiles_Data.Add(new TGAFile(this)
            {
                Location = TGAFile.ETGALocation.DataEnglishArtTextures,
                Name = name,
                BIGResource = null
            });

            for (int k = 0; k < _resourceTGAFiles_Art.Count; k++)
                if (_resourceTGAFiles_Art[k].Name == name)
                {
                    _resourceTGAFiles_Art.RemoveAt(k);
                    break;
                }
            close:;
        }
        //---Directory Loading---//

        //---Get _resourceMappedImages---//
        private void GetResourceMappedImages()
        {
            _resourceMappedImages = new List<MappedImage>();
            for (int i = 0; i < _resourceMappedFiles.Count; i++)
            {
                List<MappedImage> mappedImages = _resourceMappedFiles[i].GetMappedImagesFromFile();

                foreach (MappedImage mappedImage in mappedImages)
                {
                    for (int k = 0; k < _resourceMappedImages.Count; k++)
                        if (_resourceMappedImages[k].Name == mappedImage.Name)
                        {
                            _resourceMappedImages[k].Texture = mappedImage.Texture;
                            _resourceMappedImages[k].TextureSize = mappedImage.TextureSize;
                            _resourceMappedImages[k].Coords = mappedImage.Coords;
                            _resourceMappedImages[k].Status = mappedImage.Status;

                            _resourceMappedImages[k].ParentMappedFile = _resourceMappedFiles[i];
                            goto close;
                        }
                    _resourceMappedImages.Add(new MappedImage()
                    {
                        Name = mappedImage.Name,
                        Texture = mappedImage.Texture,
                        TextureSize = mappedImage.TextureSize,
                        Coords = mappedImage.Coords,
                        Status = mappedImage.Status,
                        ParentMappedFile = _resourceMappedFiles[i]
                    });
                close:;
                }
            }
        }

        //---Get _resourceMappedImages---//

        /// <summary>
        /// Получить MappedFile если он существует, иначе null
        /// </summary>
        /// <param name="name">Название искомого MappedFile</param>
        /// <returns>MappedFile или null</returns>
        private MappedFile GetResourceMappedFile(string name)
        {
            for (int k = 0; k < _resourceMappedFiles.Count; k++)
                if (_resourceMappedFiles[k].Name == name)
                    return _resourceMappedFiles[k];
            return null;
        }

        private List<TGAFile> CheckResourceTGA(string name, out int index)
        {
            for (int i = 0; i < _resourceTGAFiles_Data.Count; i++)
                if (_resourceTGAFiles_Data[i].Name == name)
                {
                    index = i;
                    return _resourceTGAFiles_Data;
                }

            for (int i = 0; i < _resourceTGAFiles_Art.Count; i++)
                if (_resourceTGAFiles_Art[i].Name == name)
                {
                    index = i;
                    return _resourceTGAFiles_Art;
                }
            index = -1;
            return null;
        }
    }
    public static class GameResourceExtensions
    {
        /// <summary>
        /// Extension for StartWith with startPos
        /// </summary>
        /// <param name="src">Source</param>
        /// <param name="value">Value for find</param>
        /// <param name="startPos">Start pos in source</param>
        /// <returns></returns>
        public static bool StartWith(this string src, string value, int startPos)
        {
            if (value.Length + startPos > src.Length) return false;
            for (int i = startPos; i < value.Length; i++)
                if (src[i] != value[i])
                    return false;
            return true;
        }

        /// <summary>
        /// Find TGAFile
        /// </summary>
        /// <param name="tgaFiles"></param>
        /// <param name="name">Name for search</param>
        /// <returns>GameResource.TGAFile</returns>
        public static GameResource.TGAFile FindTGA(this List<GameResource.TGAFile> tgaFiles, string name)
        {
            for (int i = 0; i < tgaFiles.Count; i++)
                if (tgaFiles[i].Name == name) return tgaFiles[i];
            return null;
        }
    }
}
