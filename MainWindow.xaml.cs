using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace GenImageViewer
{
    public static class GameResources
    {
        public static string MainFolder => _mainFolder;
        private static string _mainFolder;
        public static List<BIGFile> BIGFiles = new List<BIGFile>();
        public static List<MappedImage> MappedImages = new List<MappedImage>();
        public static List<MappedFile> MappedFiles = new List<MappedFile>();

        private static TGALocation _tgaLocation_Art = new TGALocation(@"Art\Textures");
        private static TGALocation _tgaLocation_Data = new TGALocation(@"Data\English\Art\Textures");

        public static List<TGAFile> TGAFiles;

        private static List<MappedFile> _resourceMappedFiles;
        private static List<MappedImage> _resourceMappedImages;

        private static List<TGAFile> _resourceTGAFiles_Art;
        private static List<TGAFile> _resourceTGAFiles_Data;

        public static void Load(string mainFolder)
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
                            TextureHeight = _resourceMappedImages[i].TextureHeight,
                            TextureWidth = _resourceMappedImages[i].TextureWidth,
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
                        TextureHeight = _resourceMappedImages[i].TextureHeight,
                        TextureWidth = _resourceMappedImages[i].TextureWidth,
                        ParentMappedFile = mappedFile
                    };

                    mappedFile.MappedImages.Add(mappedImage);
                    MappedImages.Add(mappedImage);
                    tgaFile.MappedImages.Add(mappedImage);

                }
            }

        }

        //---BIG Loading---//
        private static void LoadFromBIGs()
        {
            BIGFiles = Directory.GetFiles(MainFolder, "*.big", SearchOption.TopDirectoryOnly).Reverse().Select(x => new BIGFile() { FileName = x }).ToList();

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
                        if (bigArchive.Entries[i].Name.StartsWith(@"Data\English") && CheckStrings(bigArchive.Entries[i].Name, @"Art\Textures", 13))
                            AddDataTGAFromBIG(bigArchive, bIGFile, i);
                        else if (bigArchive.Entries[i].Name.StartsWith(@"Art\Textures"))
                            AddArtTGAFromBIG(bigArchive, bIGFile, i);
                    }
                }
            }
        }
        private static void AddMappedFileFromBIG(BigArchive bigArchive, BIGFile bIGFile, int entryIndex)
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
        private static void AddArtTGAFromBIG(BigArchive bigArchive, BIGFile bIGFile, int entryIndex)
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
        private static void AddDataTGAFromBIG(BigArchive bigArchive, BIGFile bIGFile, int entryIndex)
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
        private static void LoadFromDirectory()
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
        private static void AddMappedFileFromDir(string fileName)
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
        private static void AddArtTGAFromDir(string fileName)
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
        private static void AddDataTGAFromDir(string fileName)
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
        private static void GetResourceMappedImages()
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
                            _resourceMappedImages[m].TextureWidth = mappedImage.TextureWidth;
                            _resourceMappedImages[m].TextureHeight = mappedImage.TextureHeight;
                            _resourceMappedImages[m].Coords = mappedImage.Coords;
                            _resourceMappedImages[m].Status = mappedImage.Status;

                            _resourceMappedImages[m].ParentMappedFile = _resourceMappedFiles[i];
                            goto close;
                        }
                    _resourceMappedImages.Add(new MappedImage()
                    {
                        Name = mappedImage.Name,
                        Texture = mappedImage.Texture,
                        TextureWidth = mappedImage.TextureWidth,
                        TextureHeight = mappedImage.TextureHeight,
                        Coords = mappedImage.Coords,
                        Status = mappedImage.Status,
                        ParentMappedFile = _resourceMappedFiles[i]
                    });
                    continue;
                close:;
                }
            }
        }
        private static List<string> GetMappedImageLines(MappedFile mappedFile)
        {
            if (mappedFile.BIGResource == null)
                return GetMappedImageLinesFromFile($@"{MainFolder}\Data\INI\MappedImages\{mappedFile.Name}");
            else
                return GetMappedImageLinesFromBIGRes(mappedFile.BIGResource);
        }
        private static List<string> GetMappedImageLinesFromFile(string fileName)
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
        private static List<string> GetMappedImageLinesFromBIGRes(BIGResource bigResource)
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
                mappedImage.TextureWidth = int.Parse(lines[index + 2].Substring(lines[index + 2].LastIndexOf('=') + 1).TrimStart());
                mappedImage.TextureHeight = int.Parse(lines[index + 3].Substring(lines[index + 3].LastIndexOf('=') + 1).TrimStart());

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
        /// <summary>
        /// Получить MappedFile если он существует, иначе null
        /// </summary>
        /// <param name="name">Название искомого MappedFile</param>
        /// <returns>MappedFile или null</returns>
        private static MappedFile GetResourceMappedFile(string name)
        {
            for (int k = 0; k < _resourceMappedFiles.Count; k++)
                if (_resourceMappedFiles[k].Name == name)
                    return _resourceMappedFiles[k];
            return null;
        }

        private static List<TGAFile> CheckResourceTGA(string name, out int index)
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
        private static TGAFile CheckTGA(string name)
        {
            for (int i = 0; i < TGAFiles.Count; i++)
                if (TGAFiles[i].Name == name) return TGAFiles[i];
            return null;
        }
    }
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            mainWindow = this;
        }
        private static MainWindow mainWindow;
        public static class MappedImageViewModel
        {
            public static bool IsGridView
            {
                get => isGridView;

                set
                {
                    if (value != isGridView)
                    {
                        isGridView = value;
                        if (MappedImageViews != null && MappedImageViews.Count > 0)
                        {
                            for (int i = 0; i < MappedImageViews.Count; i++)
                            {
                                MappedImageViews[i].ChangeStyle(isGridView);
                            }
                        }
                    }
                }
            }
            private static bool isGridView = false;
            private class Ratio
            {
                public double X, Y;
                public Ratio()
                {
                    X = 1;
                    Y = 1;
                }
            }
            private static ListBox ParentListBox;
            private static Grid ParentGrid_Image;
            private static Grid ParentGrid_Buttons;
            private static System.Windows.Controls.Image ParentImage;

            private static Ratio ImageToTGA;

            private static System.Windows.Size TGASize;
            private static System.Windows.Size ImageSize;

            public static void InitParentsViews(ListBox parentListbox, Grid parentGrid_image, Grid parentGrid_buttons, System.Windows.Controls.Image parentImage)
            {
                if (ParentListBox != null)
                    ParentListBox.Items.Clear();
                ParentListBox = parentListbox;
                ParentGrid_Image = parentGrid_image;
                if (ParentGrid_Buttons != null)
                    ParentGrid_Buttons.Children.Clear();
                ParentGrid_Buttons = parentGrid_buttons;
                if (ParentImage != null)
                    ParentImage.Source = null;
                ParentImage = parentImage;

                ImageToTGA = new Ratio();
                TGASize = new System.Windows.Size();
                ImageSize = new System.Windows.Size();
                if (MappedImageViews != null)
                    MappedImageViews.Clear();
                MappedImageViews = new List<MappedImageView>();
            }

            public static void LoadImage(TGAFile tgaFile)
            {
                LoadTGAToView(tgaFile);

                ReCalcImageSizeRatio();

                ParentListBox.Items.Clear();
                ParentGrid_Buttons.Children.Clear();
                MappedImageViews.Clear();

                for (int i = 0; i < tgaFile.MappedImages.Count; i++)
                {
                    MappedImageViews.Add(new MappedImageView(tgaFile.MappedImages[i]));
                }
            }
            public static void ResizeViews()
            {
                if (MappedImageViews != null && MappedImageViews.Count > 0)
                {
                    ReCalcImageSizeRatio();
                    for (int i = 0; i < MappedImageViews.Count; i++)
                    {
                        MappedImageViews[i].Resize();
                    }
                }
            }
            private static void LoadTGAToView(TGAFile tgaFile)
            {
                Bitmap bitmap = tgaFile.GetBitmap(GameResources.MainFolder);

                ParentImage.Source = ToWpfBitmap(bitmap);
                ParentImage.Visibility = Visibility.Visible;

                TGASize.Width = (double)bitmap.Width;
                TGASize.Height = (double)bitmap.Height;
            }
            private static void ReCalcImageSizeRatio()
            {
                ImageSize.Width = ParentGrid_Image.ActualWidth;
                ImageSize.Height = ParentGrid_Image.ActualHeight;

                if (TGASize.Width > TGASize.Height)
                {
                    double k = TGASize.Width / TGASize.Height;
                    ImageSize.Height = ImageSize.Width / k;
                }
                else
                {
                    double k = TGASize.Height / TGASize.Width;
                    ImageSize.Width = ImageSize.Height / k;
                }

                ImageToTGA.X = ImageSize.Width / TGASize.Width; //imgTGA.ActualWidth
                ImageToTGA.Y = ImageSize.Height / TGASize.Height; //imgTGA.ActualHeight
            }

            public class MappedImageView
            {
                public ListBoxItem ListBoxItem;
                public Control Button;
                public MappedImage MappedImage;

                private Ratio tgaToMapped;

                public MappedImageView(MappedImage mappedImage)
                {
                    MappedImage = mappedImage;
                    CalcMappRatio();
                    Thickness thickness = GetThickness();
                    Button = new Control()
                    {
                        Margin = thickness,
                        ToolTip =
                            $"{(MappedImage.ParentMappedFile.BIGResource != null ? $"BIG FIle = {MappedImage.ParentMappedFile.BIGResource.BIGRFile.FileName}\r\n" : "")}" +
                            $"INI = {MappedImage.ParentMappedFile.Name}\r\n" +
                            $"\r\n" +
                            $"MappedImage {MappedImage.Name}\r\n" +
                            $"  Texture = {MappedImage.Texture}\r\n" +
                            $"  TextureWidth = {MappedImage.TextureWidth}\r\n" +
                            $"  TextureHeight = {MappedImage.TextureHeight}\r\n" +
                            $"  Coords = " +
                                $"Left:{MappedImage.Coords.Left} " +
                                $"Top:{MappedImage.Coords.Top} " +
                                $"Right:{MappedImage.Coords.Right} " +
                                $"Bottom:{MappedImage.Coords.Bottom}\r\n" +
                            $"  Status = {MappedImage.Status}\r\n" +
                            $"End"
                    };
                    ToolTipService.SetShowDuration(Button, 60000);
                    ToolTipService.SetPlacement(Button, System.Windows.Controls.Primitives.PlacementMode.Top);

                    ChangeStyle(isGridView);

                    ListBoxItem = new ListBoxItem()
                    {
                        Content = MappedImage.Name,
                        ToolTip =
                        $"{(MappedImage.ParentMappedFile.BIGResource != null ? $"BIG FIle = {MappedImage.ParentMappedFile.BIGResource.BIGRFile.FileName}\r\n" : "")}" +
                            $"INI = {MappedImage.ParentMappedFile.Name}\r\n" +
                            $"\r\n" +
                            $"MappedImage {MappedImage.Name}\r\n" +
                            $"  Texture = {MappedImage.Texture}\r\n" +
                            $"  TextureWidth = {MappedImage.TextureWidth}\r\n" +
                            $"  TextureHeight = {MappedImage.TextureHeight}\r\n" +
                            $"  Coords = " +
                                $"Left:{MappedImage.Coords.Left} " +
                                $"Top:{MappedImage.Coords.Top} " +
                                $"Right:{MappedImage.Coords.Right} " +
                                $"Bottom:{MappedImage.Coords.Bottom}\r\n" +
                            $"  Status = {MappedImage.Status}\r\n" +
                            $"End"
                    };
                    ToolTipService.SetShowDuration(ListBoxItem, 60000);
                    ToolTipService.SetPlacement(ListBoxItem, System.Windows.Controls.Primitives.PlacementMode.Top);
                    ListBoxItem.MouseEnter += delegate
                    {
                        Button.Focus();
                    };
                    ListBoxItem.MouseLeave += delegate
                    {
                        ListBoxItem.Focus();
                        bool f = Button.IsFocused;
                    };
                    Button.MouseEnter += delegate
                    {
                        ParentListBox.ScrollIntoView(ListBoxItem);
                        ParentListBox.SelectedItem = ListBoxItem;
                    };
                    ParentGrid_Buttons.Children.Add(Button);
                    ParentListBox.Items.Add(ListBoxItem);
                }
                public void ChangeStyle(bool isGridView)
                {
                    if (isGridView)
                        Button.Template = (ControlTemplate)mainWindow.FindResource("Control_Grid");
                    else
                        Button.Template = (ControlTemplate)mainWindow.FindResource("Control_Normal");
                }
                public void Resize()
                {
                    CalcMappRatio();
                    Thickness thickness = GetThickness();
                    Button.Margin = thickness;
                }
                public void CalcMappRatio()
                {
                    tgaToMapped = new Ratio();
                    if (TGASize.Width != MappedImage.TextureWidth)
                    {
                        tgaToMapped.X = TGASize.Width / (MappedImage.Coords.Right - MappedImage.Coords.Left);
                    }
                    if (TGASize.Height != MappedImage.TextureHeight)
                    {
                        tgaToMapped.Y = TGASize.Height / (MappedImage.Coords.Bottom - MappedImage.Coords.Top);
                    }
                }
                private Thickness GetThickness()
                {
                    return new Thickness(
                                (double)MappedImage.Coords.Left * tgaToMapped.X * ImageToTGA.X,
                                (double)MappedImage.Coords.Top * tgaToMapped.Y * ImageToTGA.Y,
                                (double)(MappedImage.TextureWidth - MappedImage.Coords.Right) * tgaToMapped.X * ImageToTGA.X,
                                (double)(MappedImage.TextureHeight - MappedImage.Coords.Bottom) * tgaToMapped.Y * ImageToTGA.Y);
                }
            }

            public static List<MappedImageView> MappedImageViews;
        }

        public static class TGAViewModel
        {
            private static ListBox ParentListBox;
            public static void InitParentsViews(ListBox parentListBox)
            {
                if (ParentListBox != null)
                    ParentListBox.Items.Clear();
                ParentListBox = parentListBox;
                if (TGAViews != null)
                    TGAViews.Clear();
                else
                    TGAViews = new List<TGAView>();
            }

            public class TGAView
            {
                public ListBoxItem ListBoxItem;
                public TGAFile TGAFile;

                public TGAView(TGAFile tgaFile)
                {
                    TGAFile = tgaFile;
                    ListBoxItem = new ListBoxItem() { Content = TGAFile.Name };
                    ListBoxItem.ToolTip =
                        $@"{(TGAFile.BIGResource != null ? $@"BIG File = {TGAFile.BIGResource.BIGRFile.FileName}" + "\r\n" : "")}" +
                        $@"{(string)tgaFile.TGALocation}\{TGAFile.Name}";
                    ListBoxItem.Tag = this;
                    ToolTipService.SetShowDuration(ListBoxItem, 60000);
                    ToolTipService.SetPlacement(ListBoxItem, System.Windows.Controls.Primitives.PlacementMode.Top);
                    ParentListBox.Items.Add(ListBoxItem);
                }
            }

            public static List<TGAView> TGAViews;
        }
        public void AddTGAToView()
        {
            TGAViewModel.InitParentsViews(lstTGA);

            for (int i = 0; i < GameResources.TGAFiles.Count; i++)
            {
                TGAViewModel.TGAViews.Add(new TGAViewModel.TGAView(GameResources.TGAFiles[i]));
            }

        }

        public static BitmapSource ToWpfBitmap(Bitmap bitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Bmp);

                stream.Position = 0;
                BitmapImage result = new BitmapImage();
                result.BeginInit();
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = stream;
                result.EndInit();
                result.Freeze();
                return result;
            }
        }
        private void lstTGA_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBoxItem selectedItem = (ListBoxItem)lstTGA.SelectedItem;
            if (selectedItem != null)
            {
                TGAViewModel.TGAView tgaView = selectedItem.Tag as TGAViewModel.TGAView;
                TGAFile tgaFile = tgaView.TGAFile;

                MappedImageViewModel.LoadImage(tgaFile);

                infoTGACount.Text = "1";
                infoMappedImages.Text = MappedImageViewModel.MappedImageViews.Count.ToString();
            }

        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            MappedImageViewModel.IsGridView = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            MappedImageViewModel.IsGridView = false;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MappedImageViewModel.ResizeViews();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();

            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (string.IsNullOrEmpty(dialog.SelectedPath))
                return;
            infoTGACount.Text = "0";

            GameResources.Load(dialog.SelectedPath);

            stbSelectedFolder.Content = GameResources.MainFolder;
            infoTotalTGACount.Text = (GameResources.TGAFiles.Count).ToString();
            infoTotalINI.Text = GameResources.MappedFiles.Count.ToString();
            infoTotalMappedImages.Text = GameResources.MappedImages.Count.ToString();

            AddTGAToView();
            MappedImageViewModel.InitParentsViews(lstImages, grdImage, grdButtons, imgTGA);
        }
    }
}
