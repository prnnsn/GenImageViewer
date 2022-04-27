using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using static GenImageViewer.GameResource;

namespace GenImageViewer
{
    public class GameResource
    {
        /// <summary>
        /// BIG file is main flie type for packing game resources
        /// </summary>
        public class BIGFile
        {
            private GameResource _gameResource;
            public GameResource GameResource => _gameResource;
            public string FileName;
            public BIGFile(GameResource gameResource)
            {
                _gameResource = gameResource;
            }
        }
        /// <summary>
        /// If file (TGA or MappedFile) located in BIG file then its not null
        /// </summary>
        public class BIGResource
        {
            public BIGFile BIGRFile;
            public int Offset, Lenght;
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
            public static int Sort(MappedFile file1, MappedFile file2)
            {
                if (file2.Name == null && file1.Name == null) return 0;
                else if (file2.Name == null) return -1;
                else if (file1.Name == null) return 1;
                else return file2.Name.CompareTo(file1.Name);
            }
            public string Name;
            public BIGResource BIGResource;
            public List<MappedImage> MappedImages;
        }
        /// <summary>
        /// Data of mapped image in TGA from MappedFile (MappedImages ini file)
        /// </summary>
        public class MappedImage
        {
            public string Name;
            public string Texture;
            public MappedTextureSize TextureSize;
            public MappedCoordinates Coords;
            public string Status;
            public MappedFile ParentMappedFile;
            public TGAFile TGAFile;

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
            /// Save image from TGA by MappedImage variables
            /// </summary>
            /// <param name="fileName">Name and path to save</param>
            public void Save(string fileName)
            {
                Bitmap bitmap;
                if (TGAFile.BIGResource != null)
                {
                    using (FileStream fs = new FileStream($@"{TGAFile.BIGResource.BIGRFile.GameResource.MainFolder}\{TGAFile.BIGResource.BIGRFile.FileName}", FileMode.Open, FileAccess.Read))
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        br.BaseStream.Position = TGAFile.BIGResource.Offset;
                        byte[] bytes = br.ReadBytes(TGAFile.BIGResource.Lenght);
                        using (MemoryStream ms = new MemoryStream(bytes))
                        {
                            bitmap = TGA.FromBytes(ms.ToArray()).ToBitmap();
                        }
                    }
                }
                else
                {
                    bitmap = TGA.FromFile($@"{TGAFile.BIGResource.BIGRFile.GameResource.MainFolder}\{(string)TGAFile.TGALocation}\{TGAFile.Name}").ToBitmap();
                }

                int height = TextureSize.Height;
                int width = TextureSize.Width;
                MappedCoordinates coords = (MappedCoordinates)Coords.Clone();

                if (height != bitmap.Height)
                {
                    double k = (double)bitmap.Height / (double)height;
                    coords.Top = (int)Math.Round((double)coords.Top * k, MidpointRounding.AwayFromZero);
                    coords.Bottom = (int)Math.Round((double)coords.Bottom * k, MidpointRounding.AwayFromZero);
                    if (coords.Bottom > bitmap.Height) coords.Bottom = bitmap.Height;
                }
                height = coords.Bottom - coords.Top;

                if (width != bitmap.Width)
                {
                    double k = (double)bitmap.Width / (double)width;
                    coords.Left = (int)Math.Round((double)coords.Left * k, MidpointRounding.AwayFromZero);
                    coords.Right = (int)Math.Round((double)coords.Right * k, MidpointRounding.AwayFromZero);
                    if (coords.Right > bitmap.Width) coords.Right = bitmap.Width;
                }
                width = coords.Right - coords.Left;

                Bitmap cameo = new Bitmap(width, height);
                for (int y = 0; y < cameo.Height; y++)
                    for (int x = 0; x < cameo.Width; x++)
                        cameo.SetPixel(x, y, bitmap.GetPixel(x + coords.Left, y + coords.Top));

                bitmap.Dispose();

                if (File.Exists(fileName))
                    File.Delete(fileName);
                TGA tGA = TGA.FromBitmap(cameo, false, false);
                tGA.Save(fileName);
                cameo.Dispose();
            }
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
            public TGALocation TGALocation;
            public string Name;
            public BIGResource BIGResource;
            public List<MappedImage> MappedImages;
            /// <summary>
            /// Get bitmap from TGA image
            /// </summary>
            /// <returns>System.Drawing.Bitmap</returns>
            public Bitmap GetBitmap()
            {
                if (BIGResource != null)
                {
                    using (FileStream fs = new FileStream($@"{BIGResource.BIGRFile.GameResource.MainFolder}\{BIGResource.BIGRFile.FileName}", FileMode.Open, FileAccess.Read))
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        br.BaseStream.Position = BIGResource.Offset;
                        byte[] bytes = br.ReadBytes(BIGResource.Lenght);
                        using (MemoryStream ms = new MemoryStream(bytes))
                        {
                            return TGA.FromBytes(ms.ToArray()).ToBitmap();
                        }
                    }
                }
                else
                {
                    return TGA.FromFile($@"{BIGResource.BIGRFile.GameResource.MainFolder}\{(string)TGALocation}\{Name}").ToBitmap();
                }
            }
            public void Save(string fileName)
            {
                if (BIGResource != null)
                {
                    using (FileStream fsOpen = new FileStream($@"{BIGResource.BIGRFile.GameResource.MainFolder}\{BIGResource.BIGRFile.FileName}", FileMode.Open, FileAccess.Read))
                    using (BinaryReader br = new BinaryReader(fsOpen))
                    {
                        br.BaseStream.Position = BIGResource.Offset;
                        using (FileStream fsWrite = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                        using (StreamWriter sw = new StreamWriter(fsWrite))
                            sw.Write(br.ReadBytes(BIGResource.Lenght));
                    }
                }
                else
                {
                    File.Copy($@"{BIGResource.BIGRFile.GameResource.MainFolder}\{(string)TGALocation}\{Name}", fileName, true);
                }
            }
        }

        private string _mainFolder;
        private List<BIGFile> _bigFiles = new List<BIGFile>();
        private List<MappedFile> _mappedFiles = new List<MappedFile>();
        private List<MappedImage> _mappedImages = new List<MappedImage>();

        private List<TGAFile> _tgaFiles = new List<TGAFile>();
        private TGALocation _tgaLocation_Art = new TGALocation(@"Art\Textures");
        private TGALocation _tgaLocation_Data = new TGALocation(@"Data\English\Art\Textures");

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
                    TGAFile tgaFile = CheckTGA(_resourceMappedImages[i].Texture);
                    if (tgaFile != null)
                    {
                        MappedFile mappedFile;

                        for (int k = 0; k < MappedFiles.Count; k++)
                            if (_resourceMappedImages[i].ParentMappedFile.Name == MappedFiles[k].Name)
                            {
                                mappedFile = MappedFiles[k];
                                goto close;
                            }
                        mappedFile = new MappedFile()
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
                    TGAFile tgaFile = new TGAFile()
                    {
                        TGALocation = resourceTGAFiles[index].TGALocation,
                        BIGResource = resourceTGAFiles[index].BIGResource,
                        MappedImages = new List<MappedImage>(),
                        Name = resourceTGAFiles[index].Name
                    };
                    TGAFiles.Add(tgaFile);
                    resourceTGAFiles.RemoveAt(index);

                    MappedFile mappedFile;

                    for (int k = 0; k < MappedFiles.Count; k++)
                        if (_resourceMappedImages[i].ParentMappedFile.Name == MappedFiles[k].Name)
                        {
                            mappedFile = MappedFiles[k];
                            goto close;
                        }
                    mappedFile = new MappedFile()
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
            _bigFiles = Directory.GetFiles(MainFolder, "*.big", SearchOption.TopDirectoryOnly).Reverse().Select(x => new BIGFile(this) { FileName = x }).ToList();

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
                        if (bigArchive.Entries[i].Name.StartsWith(@"Data\English") && bigArchive.Entries[i].Name.CheckStrings(@"Art\Textures", 13))
                            AddDataTGAFromBIG(bigArchive, bIGFile, i);
                        else if (bigArchive.Entries[i].Name.StartsWith(@"Art\Textures"))
                            AddArtTGAFromBIG(bigArchive, bIGFile, i);
                    }
                }
            }
        }
        private void AddMappedFileFromBIG(BigArchive bigArchive, BIGFile bIGFile, int entryIndex)
        {
            string fileName = bigArchive.Entries[entryIndex].Name.Remove(0, 21 + 1); // (Data\INI\MappedImages) = 21
            MappedFile resourceMappedFile = GetResourceMappedFile(fileName);
            if (resourceMappedFile == null)
            {
                _resourceMappedFiles.Add(new MappedFile()
                {
                    BIGResource = new BIGResource()
                    {
                        BIGRFile = bIGFile,
                        Lenght = (int)bigArchive.Entries[entryIndex].Length,
                        Offset = (int)bigArchive.Entries[entryIndex].Offset,
                        Name = bigArchive.Entries[entryIndex].Name
                    },
                    Name = fileName
                });
            }
            else
            {
                resourceMappedFile.BIGResource = new BIGResource()
                {
                    BIGRFile = bIGFile,
                    Lenght = (int)bigArchive.Entries[entryIndex].Length,
                    Offset = (int)bigArchive.Entries[entryIndex].Offset,
                    Name = bigArchive.Entries[entryIndex].Name
                };
            }
        }
        private void AddArtTGAFromBIG(BigArchive bigArchive, BIGFile bIGFile, int entryIndex)
        {
            string name = System.IO.Path.GetFileName(bigArchive.Entries[entryIndex].Name);
            for (int k = 0; k < _resourceTGAFiles_Data.Count; k++)
                if (_resourceTGAFiles_Data[k].Name == name)
                    goto close;
            for (int k = 0; k < _resourceTGAFiles_Art.Count; k++)
                if (_resourceTGAFiles_Art[k].Name == name)
                {
                    _resourceTGAFiles_Art[k].BIGResource.BIGRFile = bIGFile;
                    _resourceTGAFiles_Art[k].BIGResource.Lenght = (int)bigArchive.Entries[entryIndex].Length;
                    _resourceTGAFiles_Art[k].BIGResource.Offset = (int)bigArchive.Entries[entryIndex].Offset;
                    goto close;
                }
            _resourceTGAFiles_Art.Add(new TGAFile()
            {
                TGALocation = _tgaLocation_Art,
                Name = System.IO.Path.GetFileName(bigArchive.Entries[entryIndex].Name),
                BIGResource = new BIGResource()
                {
                    BIGRFile = bIGFile,
                    Lenght = (int)bigArchive.Entries[entryIndex].Length,
                    Offset = (int)bigArchive.Entries[entryIndex].Offset,
                    Name = bigArchive.Entries[entryIndex].Name
                }
            });
        close:;
        }
        private void AddDataTGAFromBIG(BigArchive bigArchive, BIGFile bIGFile, int entryIndex)
        {
            string name = System.IO.Path.GetFileName(bigArchive.Entries[entryIndex].Name);
            for (int k = 0; k < _resourceTGAFiles_Data.Count; k++)
                if (_resourceTGAFiles_Data[k].Name == name)
                {
                    _resourceTGAFiles_Data[k].BIGResource.BIGRFile = bIGFile;
                    _resourceTGAFiles_Data[k].BIGResource.Lenght = (int)bigArchive.Entries[entryIndex].Length;
                    _resourceTGAFiles_Data[k].BIGResource.Offset = (int)bigArchive.Entries[entryIndex].Offset;
                    goto close;
                }
            _resourceTGAFiles_Data.Add(new TGAFile()
            {
                TGALocation = _tgaLocation_Data,
                Name = name,
                BIGResource = new BIGResource()
                {
                    BIGRFile = bIGFile,
                    Lenght = (int)bigArchive.Entries[entryIndex].Length,
                    Offset = (int)bigArchive.Entries[entryIndex].Offset,
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
                _resourceMappedFiles.Add(new MappedFile()
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
            _resourceTGAFiles_Art.Add(new TGAFile()
            {
                TGALocation = _tgaLocation_Art,
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
            _resourceTGAFiles_Data.Add(new TGAFile()
            {
                TGALocation = _tgaLocation_Data,
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
                List<string> lines = GetMappedImageLines(_resourceMappedFiles[i]);

                for (int n = 0; n < lines.Count; n += 7)
                {
                    MappedImage mappedImage = GetMappedImageFromLines(lines, n);
                    if (mappedImage == null) continue;

                    for (int m = 0; m < _resourceMappedImages.Count; m++)
                        if (_resourceMappedImages[m].Name == mappedImage.Name)
                        {
                            _resourceMappedImages[m].Texture = mappedImage.Texture;
                            _resourceMappedImages[m].TextureSize = mappedImage.TextureSize;
                            _resourceMappedImages[m].Coords = mappedImage.Coords;
                            _resourceMappedImages[m].Status = mappedImage.Status;

                            _resourceMappedImages[m].ParentMappedFile = _resourceMappedFiles[i];
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
                    continue;
                close:;
                }
            }
        }
        private List<string> GetMappedImageLines(MappedFile mappedFile)
        {
            if (mappedFile.BIGResource == null)
                return GetMappedImageLinesFromFile($@"{MainFolder}\Data\INI\MappedImages\{mappedFile.Name}");
            else
                return GetMappedImageLinesFromBIGRes(mappedFile.BIGResource);
        }
        private List<string> GetMappedImageLinesFromFile(string fileName)
        {
            List<string> lines = new List<string>();
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
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
        private List<string> GetMappedImageLinesFromBIGRes(BIGResource bigResource)
        {
            List<string> lines = new List<string>();
            string s;
            using (FileStream fs = new FileStream($@"{MainFolder}\{bigResource.BIGRFile.FileName}", FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                br.BaseStream.Position = bigResource.Offset;
                s = Encoding.ASCII.GetString(br.ReadBytes(bigResource.Lenght));
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
        private static MappedImage GetMappedImageFromLines(List<string> lines, int index)
        {
            MappedImage mappedImage = new MappedImage();
            try
            {
                mappedImage.Name = lines[index].Substring(lines[index].LastIndexOf(' ') + 1);
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
        private TGAFile CheckTGA(string name)
        {
            for (int i = 0; i < TGAFiles.Count; i++)
                if (TGAFiles[i].Name == name) return TGAFiles[i];
            return null;
        }
    }
    public static class GameResourceExtensions
    {
        /// <summary>
        /// Аналог для StartWith с указанием первой позиции
        /// </summary>
        /// <param name="src"></param>
        /// <param name="value"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static bool CheckStrings(this string src, string value, int pos)
        {
            if (value.Length + pos > src.Length) return false;
            for (int i = pos; i < value.Length; i++)
                if (src[i] != value[i])
                    return false;
            return true;
        }
    }
}
