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
    public partial class MainWindow : Window
    {
        private static MainWindow mainWindow;
        public GameResource GameResources;
        public MainWindow()
        {
            InitializeComponent();

            mainWindow = this;
            GameResources = new GameResource();
        }

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

            public static void LoadImage(GameResource.TGAFile tgaFile)
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
            private static void LoadTGAToView(GameResource.TGAFile tgaFile)
            {
                Bitmap bitmap = tgaFile.GetBitmap();

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
                public GameResource.MappedImage MappedImage;

                private Ratio tgaToMapped;

                public MappedImageView(GameResource.MappedImage mappedImage)
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
                            $"  TextureWidth = {MappedImage.TextureSize.Width}\r\n" +
                            $"  TextureHeight = {MappedImage.TextureSize.Height}\r\n" +
                            $"  {MappedImage.Coords.ToString()}\r\n" +
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
                            $"  TextureWidth = {MappedImage.TextureSize.Width}\r\n" +
                            $"  TextureHeight = {MappedImage.TextureSize.Height}\r\n" +
                            $"  {MappedImage.Coords.ToString()}\r\n" +
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
                    if (TGASize.Width != MappedImage.TextureSize.Width)
                    {
                        tgaToMapped.X = TGASize.Width / (MappedImage.Coords.Right - MappedImage.Coords.Left);
                    }
                    if (TGASize.Height != MappedImage.TextureSize.Height)
                    {
                        tgaToMapped.Y = TGASize.Height / (MappedImage.Coords.Bottom - MappedImage.Coords.Top);
                    }
                }
                private Thickness GetThickness()
                {
                    return new Thickness(
                                (double)MappedImage.Coords.Left * tgaToMapped.X * ImageToTGA.X,
                                (double)MappedImage.Coords.Top * tgaToMapped.Y * ImageToTGA.Y,
                                (double)(MappedImage.TextureSize.Width - MappedImage.Coords.Right) * tgaToMapped.X * ImageToTGA.X,
                                (double)(MappedImage.TextureSize.Height - MappedImage.Coords.Bottom) * tgaToMapped.Y * ImageToTGA.Y);
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
                public GameResource.TGAFile TGAFile;

                public TGAView(GameResource.TGAFile tgaFile)
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
                GameResource.TGAFile tgaFile = tgaView.TGAFile;

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
