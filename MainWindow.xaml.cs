using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Size = System.Windows.Size;

namespace GenImageViewer
{
    public static class Extensions
    {
        /// <summary>
        /// Get BitmapSource for WPF image
        /// </summary>
        /// <param name="tgaFile">TGA file from GameResource.TGAFile</param>
        /// <returns>WPF BitmapSource</returns>
        public static BitmapSource ToWpfBitmapSource(this GameResource.TGAFile tgaFile)
        {
            var bitmap = tgaFile.GetBitmap();

            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

            var bitmapData = bitmap.LockBits(
                rect,
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);

            try
            {
                var size = (rect.Width * rect.Height) * 4;

                return BitmapSource.Create(
                    bitmap.Width,
                    bitmap.Height,
                    bitmap.HorizontalResolution,
                    bitmap.VerticalResolution,
                    System.Windows.Media.PixelFormats.Bgra32,
                    null,
                    bitmapData.Scan0,
                    size,
                    bitmapData.Stride);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }

        /// <summary>
        /// Get ratio from this size to size in parameters
        /// </summary>
        /// <param name="src">Current size</param>
        /// <param name="size">Size to which is will be calculated ratio</param>
        /// <returns>Ratio</returns>
        public static Ratio GetRatioTo(this Size src, Size size)
        {
            return new Ratio(src.Width / size.Width, src.Height / size.Height);
        }
    }

    /// <summary>
    /// Ratio for two image sizes
    /// </summary>
    public class Ratio
    {
        public double k_Width, k_Height;
        public Ratio()
        {
            k_Width = 1;
            k_Height = 1;
        }
        public Ratio(double k_width, double k_height)
        {
            this.k_Width = k_width;
            this.k_Height = k_height;
        }
    }

    public partial class MainWindow : Window
    {
        private static MainWindow mainWindow;
        private List<Control> mappedImageControls;
        /// <summary>
        /// Current TGA size
        /// </summary>
        private Size TGASize;
        /// <summary>
        /// Current image size on window
        /// </summary>
        private Size ImageSize;
        /// <summary>
        /// Ratio image to tga (need for correctly draw)
        /// </summary>
        private Ratio ImageToTGA;

        public GameResource GameResources;
        public bool GridHighlighting = false;

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
            ImageSize = new Size(grdButtons.ActualWidth, grdButtons.ActualHeight);

            ImageToTGA = ImageSize.GetRatioTo(TGASize);
        }
        private void CalcRatioImageToTGA(Size size)
        {
            ImageSize = size;
            ImageToTGA = ImageSize.GetRatioTo(TGASize);
        }
        private void SelectTGAItem(int index)
        {
            infoTGACount.Text = "1";
            GameResource.TGAFile tgaFile = GameResources.TGAFiles[index];

            imgTGA.Source = tgaFile.ToWpfBitmapSource();

            TGASize = new Size()
            {
                Width = tgaFile.Width,
                Height = tgaFile.Height
            };

            CalcRatioImageToTGA();

            lstImages.Items.Clear();
            grdButtons.Children.Clear();

            mappedImageControls = new List<Control>();

            for (int i = 0; i < tgaFile.MappedImages.Count; i++)
            {
                AddMapedImageControls(tgaFile.MappedImages[i]);
            }

            infoMappedImages.Text = tgaFile.MappedImages.Count.ToString();
            GC.Collect();
        }

        private void AddMapedImageControls(GameResource.MappedImage mappedImage)
        {
            Control control = new Control()
            {
                //Margin = GetMappedImageThickness(mappedImage),
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

            bool mouseEnter = false;
            listBoxItem.ToolTipOpening += delegate (object o, ToolTipEventArgs e)
            {
                e.Handled = !mouseEnter;
            };

            listBoxItem.MouseEnter += delegate
            {
                mouseEnter = true;
                lstImages.SelectedItem = listBoxItem;
                control.Focus();
            };
            listBoxItem.MouseLeave += delegate
            {
                mouseEnter = false;
                listBoxItem.Focus();
                bool f = control.IsFocused;
            };
            control.MouseEnter += delegate
            {
                lstImages.ScrollIntoView(listBoxItem);
                lstImages.SelectedItem = listBoxItem;
            };
            control.MouseLeave += delegate
            {
                mouseEnter = false;
                listBoxItem.Focus();
                bool f = control.IsFocused;
            };

            ContextMenu contextMenu = new ContextMenu();
            {
                MenuItem menuItemSaveAs = new MenuItem() { Header = "Сохранить как.." };
                {
                    MenuItem menuItemSaveAsTGA = new MenuItem() { Header = "TGA" };
                    menuItemSaveAsTGA.Click += delegate
                    {
                        string name = SaveFile(".tga");
                        if (!string.IsNullOrEmpty(name))
                        {
                            mappedImage.Save(name);
                        }
                    };
                    //
                    menuItemSaveAs.Items.Add(menuItemSaveAsTGA);
                }
                MenuItem menuItemCopyCode = new MenuItem() { Header = "Копировать код" };
                menuItemCopyCode.Click += delegate
                {
                    mappedImage.CopyMappCodeToClipboard();
                };
                //
                contextMenu.Items.Add(menuItemSaveAs);
                contextMenu.Items.Add(menuItemCopyCode);
            }
            control.Tag = mappedImage;
            control.ContextMenu = contextMenu;
            listBoxItem.ContextMenu = contextMenu;

            mappedImageControls.Add(control);
            grdButtons.Children.Add(control);
            lstImages.Items.Add(listBoxItem);

            ResizeMappedImageControl(mappedImage, control);
        }
        private void ChangeMappedImageControlStyle(Control control)
        {
            if (GridHighlighting)
                SetGridMappedImageControlStyle(control);
            else
                SetNormalMappedImageControlStyle(control);
        }
        private void SetGridMappedImageControlStyle(Control control) => control.Template = (ControlTemplate)mainWindow.FindResource("Control_Grid");
        private void SetNormalMappedImageControlStyle(Control control) => control.Template = (ControlTemplate)mainWindow.FindResource("Control_Normal");
        private void ResizeMappedImageControl(GameResource.MappedImage mappedImage, Control control)
        {
            Ratio tgaToMapped = new Ratio();
            if (TGASize.Width != mappedImage.TextureSize.Width)
            {
                tgaToMapped.k_Width = TGASize.Width / mappedImage.TextureSize.Width;
            }
            if (TGASize.Height != mappedImage.TextureSize.Height)
            {
                tgaToMapped.k_Height = TGASize.Height / mappedImage.TextureSize.Height;
            }
            control.Width = (mappedImage.Coords.Right - mappedImage.Coords.Left) * tgaToMapped.k_Width * ImageToTGA.k_Width;
            control.Height = (mappedImage.Coords.Bottom - mappedImage.Coords.Top) * tgaToMapped.k_Height * ImageToTGA.k_Height;

            double left = mappedImage.Coords.Left * tgaToMapped.k_Width * ImageToTGA.k_Width;
            if (left < 3)
            {
                left = 3;
            }
            if (left + control.Width + 3 > ImageSize.Width)
            {
                double w = ImageSize.Width - left - 3;
                if (w < 0) w = 0;
                control.Width = w;
            }

            double top = mappedImage.Coords.Top * tgaToMapped.k_Height * ImageToTGA.k_Height;
            if (top < 3)
            {
                top = 3;
            }
            if (top + control.Height + 3 > ImageSize.Height)
            {
                double h = ImageSize.Height - top - 3;
                if (h < 0) h = 0;
                control.Height = h;
            }

            Canvas.SetLeft(control, left);
            Canvas.SetTop(control, top);
        }
        private void ResizeMappedImageControls()
        {
            for (int i = 0; i < mappedImageControls.Count; i++)
            {
                ResizeMappedImageControl(mappedImageControls[i].Tag as GameResource.MappedImage, mappedImageControls[i]);
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            GridHighlighting = true;
            if (mappedImageControls != null)
                for (int i = 0; i < mappedImageControls.Count; i++)
                    SetGridMappedImageControlStyle(mappedImageControls[i]);
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            GridHighlighting = false;
            if (mappedImageControls != null)
                for (int i = 0; i < mappedImageControls.Count; i++)
                    SetNormalMappedImageControlStyle(mappedImageControls[i]);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void miSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new WK.Libraries.BetterFolderBrowserNS.BetterFolderBrowser
            {
                Title = "Выберете папку с ресурсами",
                Multiselect = false
            })
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (string.IsNullOrEmpty(dialog.SelectedFolder))
                    return;

                SelectGameFolder(dialog.SelectedPath);
            }
        }

        public void SelectGameFolder(string folder)
        {
            lstImages.Items.Clear();
            imgTGA.Source = null;
            grdButtons.Children.Clear();
            lstTGA.Items.Clear();

            cmbSearch.Items.Clear();

            infoTGACount.Text = "0";

            GameResources.Load(folder);

            stbSelectedFolder.Content = GameResources.MainFolder;
            infoTotalTGACount.Text = (GameResources.TGAFiles.Count).ToString();
            infoTotalINI.Text = GameResources.MappedFiles.Count.ToString();
            infoTotalMappedImages.Text = GameResources.MappedImages.Count.ToString();

            AddTGAItems();
            AddItemsToSearchCmb();
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
            // I am forced to use this dirt method because otherwise i get memory leaks in ToolTip image
            // StackPanel for ToolTip content
            StackPanel GetToolTipContent()
            {
                StackPanel stackPanel = new StackPanel()
                {
                    Orientation = Orientation.Vertical
                };
                Border border1 = new Border()
                {
                    BorderThickness = new Thickness(1),
                    BorderBrush = System.Windows.Media.Brushes.LightGray,
                    Padding = new Thickness(3),
                    Margin = new Thickness(3)
                };
                TextBlock tb = new TextBlock()
                {
                    Text =
                    $@"{(tgaFile.BIGResource != null ? $@"BIG File = {tgaFile.BIGResource.BIGRFile.FileName}" + "\r\n" : "")}" +
                            $@"{tgaFile.TGALocation}\{tgaFile.Name}" + "\r\n" + "\r\n" +
                            $@"Width = {tgaFile.Width}" + "\r\n" +
                            $@"Height = {tgaFile.Height}" + "\r\n" + "\r\n" +
                            $@"MappedImages = {tgaFile.MappedImages.Count}"
                };
                border1.Child = tb;

                Border border2 = new Border()
                {
                    BorderThickness = new Thickness(1),
                    BorderBrush = System.Windows.Media.Brushes.LightGray,
                    Padding = new Thickness(3),
                    Margin = new Thickness(3)
                };
                Border border2_1 = new Border()
                {
                    BorderThickness = new Thickness(3),
                    BorderBrush = System.Windows.Media.Brushes.LightGray,
                    Width = 256,
                    Height = 256
                };
                System.Windows.Controls.Image img = new System.Windows.Controls.Image();
                border2_1.Child = img;
                border2.Child = border2_1;
                img.Source = tgaFile.ToWpfBitmapSource();

                stackPanel.Children.Add(border1);
                stackPanel.Children.Add(border2);

                return stackPanel;
            }

            ToolTip toolTip = new ToolTip();
            {
                toolTip.Opened += delegate
                {
                    toolTip.Content = GetToolTipContent();
                };
                toolTip.Closed += delegate
                {
                    toolTip.Content = null;
                    GC.Collect(); // For avoid unnecessary memory leaks (comment up)
                };
            }

            ListBoxItem listBoxItem = new ListBoxItem()
            {
                Content = tgaFile.Name,
                ToolTip = toolTip
            };

            // Fix for open ToolTIp only when mouse enter on item
            bool mouseEnter = false;
            listBoxItem.MouseEnter += delegate
            {
                mouseEnter = true;
            };
            listBoxItem.MouseLeave += delegate
            {
                mouseEnter = false;
            };
            listBoxItem.ToolTipOpening += delegate (object o, ToolTipEventArgs e)
            {
                e.Handled = !mouseEnter;
            };

            ToolTipService.SetShowDuration(listBoxItem, 60000);
            ToolTipService.SetPlacement(listBoxItem, System.Windows.Controls.Primitives.PlacementMode.Top);

            ContextMenu contextMenu = new ContextMenu();
            {
                MenuItem menuItemSaveAs = new MenuItem() { Header = "Сохранить как.." };
                {
                    MenuItem menuItemSaveAsTGA = new MenuItem() { Header = "TGA" };
                    menuItemSaveAsTGA.Click += delegate
                    {
                        string name = SaveFile(".tga");
                        if (!string.IsNullOrEmpty(name))
                        {
                            tgaFile.Save(name);
                        }
                    };

                    MenuItem menuItemSaveAsEx = new MenuItem() { Header = "Расширенное" };
                    menuItemSaveAsEx.Click += delegate
                    {
                        windowSaveTGAEx windowSaveTGAEx = new windowSaveTGAEx(tgaFile);
                        windowSaveTGAEx.ShowDialog();
                    };
                    //
                    menuItemSaveAs.Items.Add(menuItemSaveAsTGA);
                    menuItemSaveAs.Items.Add(menuItemSaveAsEx);
                }
                //
                contextMenu.Items.Add(menuItemSaveAs);
            }

            listBoxItem.ContextMenu = contextMenu;
            return listBoxItem;
        }

        private void AddItemsToSearchCmb()
        {
            List<ComboBoxItem> comboBoxItems = new List<ComboBoxItem>();

            foreach (GameResource.TGAFile tgaFile in GameResources.TGAFiles)
            {
                ComboBoxItem item = new ComboBoxItem()
                {
                    Content = tgaFile.Name,
                    Tag = tgaFile
                };
                item.PreviewMouseLeftButtonUp += delegate
                {
                    SelectItemTGA(tgaFile);
                };
                comboBoxItems.Add(item);
            }

            foreach (GameResource.MappedImage mappedImage in GameResources.MappedImages)
            {
                ComboBoxItem item = new ComboBoxItem()
                {
                    Content = mappedImage.Name,
                    Tag = mappedImage
                };
                item.PreviewMouseLeftButtonUp += delegate
                {
                    SelectItemMappedImage(mappedImage);
                };
                comboBoxItems.Add(item);
            }

            int Sort(ComboBoxItem item1, ComboBoxItem item2)
            {
                if (item1.Content == null && item2.Content == null) return 0;
                else if (item2.Content == null) return -1;
                else if (item1.Content == null) return 1;
                else return (item1.Content as string).CompareTo((string)item2.Content);
            }

            comboBoxItems.Sort(Sort);

            for (int i = 0; i < comboBoxItems.Count; i++)
                cmbSearch.Items.Add(comboBoxItems[i]);
        }

        private void SelectItemTGA(GameResource.TGAFile tgaFile)
        {
            for (int i = 0; i < lstTGA.Items.Count; i++)
                if (tgaFile.Name == (string)(lstTGA.Items[i] as ListBoxItem).Content)
                {
                    lstTGA.SelectedIndex = i;
                    return;
                }
        }
        private void SelectItemMappedImage(GameResource.MappedImage mappedImage)
        {
            SelectItemTGA(mappedImage.TGAFile);

            for (int i = 0; i < lstImages.Items.Count; i++)
                if (mappedImage.Name == (mappedImageControls[i].Tag as GameResource.MappedImage).Name)
                {
                    mappedImageControls[i].Focus();
                    lstImages.SelectedIndex = i;
                    return;
                }
        }
        private string SaveFile(string extension)
        {
            using (System.Windows.Forms.SaveFileDialog fileFialog = new System.Windows.Forms.SaveFileDialog())
            {
                if (fileFialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string s = Path.GetExtension(fileFialog.FileName).ToLower();
                    if (string.IsNullOrEmpty(s))
                        return fileFialog.FileName + extension;
                    else
                        return fileFialog.FileName.Remove(fileFialog.FileName.Length - s.Length, 4) + extension;
                }
                else
                    return "";
            }
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
                if (e.NewSize.Width != 0 && e.NewSize.Height != 0)
                {
                    CalcRatioImageToTGA(e.NewSize);
                    ResizeMappedImageControls();
                }
        }

        private void miSearch_Click(object sender, RoutedEventArgs e)
        {
            cmbSearch.SelectedIndex = -1;
            sbSearchBar.Visibility = Visibility.Visible;
            cmbSearch.Focus();
        }

        private void cmbSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            cmbSearch.IsDropDownOpen = true;
        }

        private void cmbSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            cmbSearch.IsDropDownOpen = false;
        }

        private void btnCloseSearch_Click(object sender, RoutedEventArgs e)
        {
            sbSearchBar.Visibility = Visibility.Collapsed;
        }

        private void cmbSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ComboBoxItem selectedItem = cmbSearch.SelectedItem as ComboBoxItem;
            if (selectedItem != null && e.Key == System.Windows.Input.Key.Enter)
            {
                cmbSearch.IsEditable = false;
                cmbSearch.IsEditable = true;

                Type tagType = selectedItem.Tag.GetType();
                if (tagType == typeof(GameResource.TGAFile))
                {
                    SelectItemTGA(selectedItem.Tag as GameResource.TGAFile);
                }
                else if (tagType == typeof(GameResource.MappedImage))
                {
                    SelectItemMappedImage(selectedItem.Tag as GameResource.MappedImage);
                }

            }
        }
    }
}
