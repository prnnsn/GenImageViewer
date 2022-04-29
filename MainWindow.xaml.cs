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
    public static class Extensions
    {
        public static BitmapSource ToWpfBitmap(this Bitmap bitmap)
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
    }
    public class Ratio
    {
        public double X, Y;
        public Ratio()
        {
            X = 1;
            Y = 1;
        }
    }

    public partial class MainWindow : Window
    {
        private static MainWindow mainWindow;
        private List<Control> mappedImageControls;
        private System.Windows.Size TGASize;
        private System.Windows.Size ImageSize;
        private Ratio ImageToTGA;


        public GameResource GameResources;
        public bool IsGrid = false;

        private void lstTGA_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBoxItem selectedItem = (ListBoxItem)lstTGA.SelectedItem;
            if (selectedItem != null)
            {
                SelectTGAItem(lstTGA.SelectedIndex);
            }
        }
        private void CalcRatioImageToTGA()
        {
            ImageSize = new System.Windows.Size()
            {
                Width = imgTGA.ActualWidth,
                Height = imgTGA.ActualHeight
            };

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

            ImageToTGA = new Ratio()
            {
                X = ImageSize.Width / TGASize.Width,
                Y = ImageSize.Height / TGASize.Height
            };
        }
        private void SelectTGAItem(int index)
        {
            infoTGACount.Text = "1";
            GameResource.TGAFile tgaFile = GameResources.TGAFiles[index];

            Bitmap bitmap = tgaFile.GetBitmap();

            imgTGA.Source = bitmap.ToWpfBitmap();

            TGASize = new System.Windows.Size()
            {
                Width = bitmap.Width,
                Height = bitmap.Height
            };

            CalcRatioImageToTGA();

            lstImages.Items.Clear();
            grdButtons.Children.Clear();

            mappedImageControls = new List<Control>();

            for (int i = 0; i < tgaFile.MappedImages.Count; i++)
            {
                AddMapedImageControls(tgaFile.MappedImages[i]);
            }
        }

        private void AddMapedImageControls(GameResource.MappedImage mappedImage)
        {
            Control control = new Control()
            {
                Margin = GetMappedImageThickness(mappedImage),
                ToolTip =
                    $"{(mappedImage.ParentMappedFile.BIGResource != null ? $"BIG FIle = {mappedImage.ParentMappedFile.BIGResource.BIGRFile.FileName}\r\n" : "")}" +
                    $"INI = {mappedImage.ParentMappedFile.Name}\r\n" +
                    $"\r\n" +
                    $"MappedImage {mappedImage.Name}\r\n" +
                    $"  Texture = {mappedImage.Texture}\r\n" +
                    $"  TextureWidth = {mappedImage.TextureSize.Width}\r\n" +
                    $"  TextureHeight = {mappedImage.TextureSize.Height}\r\n" +
                    $"  {mappedImage.Coords.ToString()}\r\n" +
                    $"  Status = {mappedImage.Status}\r\n" +
                    $"End"
            };
            ToolTipService.SetShowDuration(control, 60000);
            ToolTipService.SetPlacement(control, System.Windows.Controls.Primitives.PlacementMode.Top);

            ChangeMappedImageControlStyle(control);

            ListBoxItem listBoxItem = new ListBoxItem()
            {
                Content = mappedImage.Name,
                ToolTip =
                $"{(mappedImage.ParentMappedFile.BIGResource != null ? $"BIG FIle = {mappedImage.ParentMappedFile.BIGResource.BIGRFile.FileName}\r\n" : "")}" +
                    $"INI = {mappedImage.ParentMappedFile.Name}\r\n" +
                    $"\r\n" +
                    $"MappedImage {mappedImage.Name}\r\n" +
                    $"  Texture = {mappedImage.Texture}\r\n" +
                    $"  TextureWidth = {mappedImage.TextureSize.Width}\r\n" +
                    $"  TextureHeight = {mappedImage.TextureSize.Height}\r\n" +
                    $"  {mappedImage.Coords.ToString()}\r\n" +
                    $"  Status = {mappedImage.Status}\r\n" +
                    $"End"
            };
            ToolTipService.SetShowDuration(listBoxItem, 60000);
            ToolTipService.SetPlacement(listBoxItem, System.Windows.Controls.Primitives.PlacementMode.Top);
            listBoxItem.MouseEnter += delegate
            {
                control.Focus();
            };
            listBoxItem.MouseLeave += delegate
            {
                listBoxItem.Focus();
                bool f = control.IsFocused;
            };
            control.MouseEnter += delegate
            {
                lstImages.ScrollIntoView(listBoxItem);
                lstImages.SelectedItem = listBoxItem;
            };
            mappedImageControls.Add(control);
            grdButtons.Children.Add(control);
            lstImages.Items.Add(listBoxItem);
        }
        private Thickness GetMappedImageThickness(GameResource.MappedImage mappedImage)
        {
            Ratio tgaToMapped = new Ratio();
            if (TGASize.Width != mappedImage.TextureSize.Width)
            {
                tgaToMapped.X = TGASize.Width / (mappedImage.Coords.Right - mappedImage.Coords.Left);
            }
            if (TGASize.Height != mappedImage.TextureSize.Height)
            {
                tgaToMapped.Y = TGASize.Height / (mappedImage.Coords.Bottom - mappedImage.Coords.Top);
            }

            return new Thickness(
                        (double)mappedImage.Coords.Left * tgaToMapped.X * ImageToTGA.X,
                        (double)mappedImage.Coords.Top * tgaToMapped.Y * ImageToTGA.Y,
                        (double)(mappedImage.TextureSize.Width - mappedImage.Coords.Right) * tgaToMapped.X * ImageToTGA.X,
                        (double)(mappedImage.TextureSize.Height - mappedImage.Coords.Bottom) * tgaToMapped.Y * ImageToTGA.Y);
        }
        private void ChangeMappedImageControlStyle(Control control)
        {
            if (IsGrid)
                SetGridMappedImageControlStyle(control);
            else
                SetNormalMappedImageControlStyle(control);
        }
        private void SetGridMappedImageControlStyle(Control control) => control.Template = (ControlTemplate)mainWindow.FindResource("Control_Grid");
        private void SetNormalMappedImageControlStyle(Control control) => control.Template = (ControlTemplate)mainWindow.FindResource("Control_Normal");
        private void ResizeMappedImageControls()
        {
            for (int i = 0; i < mappedImageControls.Count; i++)
            {
                mappedImageControls[i].Margin = GetMappedImageThickness(GameResources.MappedImages[i]);
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            IsGrid = true;
            if (mappedImageControls != null)
                for (int i = 0; i < mappedImageControls.Count; i++)
                    SetGridMappedImageControlStyle(mappedImageControls[i]);
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            IsGrid = false;
            if (mappedImageControls != null)
                for (int i = 0; i < mappedImageControls.Count; i++)
                    SetNormalMappedImageControlStyle(mappedImageControls[i]);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void miSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();

            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (string.IsNullOrEmpty(dialog.SelectedPath))
                return;

            SelectGameFolder(dialog.SelectedPath);
        }

        public void SelectGameFolder(string folder)
        {
            lstImages.Items.Clear();
            imgTGA.Source = null;
            grdButtons.Children.Clear();
            lstTGA.Items.Clear();
            
            infoTGACount.Text = "0";         

            GameResources.Load(folder);

            stbSelectedFolder.Content = GameResources.MainFolder;
            infoTotalTGACount.Text = (GameResources.TGAFiles.Count).ToString();
            infoTotalINI.Text = GameResources.MappedFiles.Count.ToString();
            infoTotalMappedImages.Text = GameResources.MappedImages.Count.ToString();

            AddTGAItems();
        }

        private void AddTGAItems()
        {
            for (int i = 0; i < GameResources.TGAFiles.Count; i++)
            {
                lstTGA.Items.Add(CreateTGAListBoxItem(GameResources.TGAFiles[i]));
            }
        }

        private ListBoxItem CreateTGAListBoxItem(GameResource.TGAFile tgaFile)
        {
            ListBoxItem listBoxItem = new ListBoxItem()
            {
                Content = tgaFile.Name,
                ToolTip =
                        $@"{(tgaFile.BIGResource != null ? $@"BIG File = {tgaFile.BIGResource.BIGRFile.FileName}" + "\r\n" : "")}" +
                        $@"{(string)tgaFile.TGALocation}\{tgaFile.Name}"
            };
            ToolTipService.SetShowDuration(listBoxItem, 60000);
            ToolTipService.SetPlacement(listBoxItem, System.Windows.Controls.Primitives.PlacementMode.Top);
            return listBoxItem;
        }

        public MainWindow()
        {
            InitializeComponent();

            mainWindow = this;
            GameResources = new GameResource();
        }

        private void imgTGA_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (mappedImageControls != null)
            {
                ImageSize = e.NewSize;
                CalcRatioImageToTGA();
                ResizeMappedImageControls();
            }
        }
    }
}
