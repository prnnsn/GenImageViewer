using System.IO;
using System.Windows;

namespace GenImageViewer
{
    public partial class windowSaveTGAEx : Window
    {
        /// <summary>
        /// Placement architecture for result files
        /// </summary>
        private enum EResultStructure
        {
            /// <summary>
            /// Save files by they extensions to the relevant folder (.ini in INI folder and itc.)
            /// </summary>
            Extension,
            /// <summary>
            /// Save filed by they game location (if source .tga located in "Art\Textures" then result will be create in relevant folder 
            /// (and its work for archivied files in .big, but will be unpacked)
            /// </summary>
            Game
        }

        /// <summary>
        /// Way for save tga files
        /// </summary>
        private enum ETGAResult
        {
            /// <summary>
            /// Save tga file "as is" - one file
            /// </summary>
            Union,
            /// <summary>
            /// Save cuted tga files by MappedImages. 
            /// If choice this option, then MappedImages will be resized (to zero margins, and original size)
            /// and tga files get new name - MappedImage name, also texture name changed
            /// </summary>
            Separated
        }

        /// <summary>
        /// Way for save ini file/s with MappedImage/s
        /// </summary>
        private enum EMappedImageResult
        {
            /// <summary>
            /// Save MappedImages in one .ini (Result.INI)
            /// </summary>
            Union,
            /// <summary>
            /// Save MappedImages in separated .ini (1 MappedImage = 1 ini file, ini name = MappedImage name)
            /// </summary>
            SeparatedImages,
            /// <summary>
            /// Save MappedImages in separated .ini, but save source ini names (1 ini file can contain several MappImages)
            /// </summary>
            SeparatedAsIs
        }

        private GameResource.TGAFile _tgaFile;

        private EResultStructure _resultStructure = EResultStructure.Game;
        private ETGAResult _tgaResult = ETGAResult.Union;
        private EMappedImageResult _mappedImageResult = EMappedImageResult.Union;

        public windowSaveTGAEx(GameResource.TGAFile tgaFile)
        {
            InitializeComponent();

            _tgaFile = tgaFile;
        }

        private void SaveResult(string resultDir)
        {
            string tgaDir = "";
            string mappedDir = "";
            switch (_resultStructure)
            {
                case EResultStructure.Extension:
                    {
                        tgaDir = $"{resultDir}\\TGA";
                        mappedDir = $"{resultDir}\\INI";
                    }
                    break;
                case EResultStructure.Game:
                    {
                        tgaDir = $"{resultDir}\\{(string)_tgaFile.TGALocation}";
                        mappedDir = $"{resultDir}\\Data\\INI\\MappedImages";
                    }
                    break;
            }

            if (!Directory.Exists(tgaDir))
                Directory.CreateDirectory(tgaDir);

            if (!Directory.Exists(mappedDir))
                Directory.CreateDirectory(mappedDir);

            switch (_mappedImageResult)
            {
                case EMappedImageResult.Union: SaveMappedImage_Union(_tgaResult, mappedDir, tgaDir); break;
                case EMappedImageResult.SeparatedImages: SaveMappedImage_Separated(_tgaResult, mappedDir, tgaDir); break;
                case EMappedImageResult.SeparatedAsIs: SaveMappedImage_SeparatedAsIs(_tgaResult, mappedDir, tgaDir); break;
            }
        }

        private void SaveMappedImage_Union(ETGAResult tgaResult, string mappedDir, string tgaDir)
        {
            switch (tgaResult)
            {
                case ETGAResult.Union:
                    {
                        for (int i = 0; i < _tgaFile.MappedImages.Count; i++)
                        {
                            GameResource.MappedImage.SaveMappedCode(_tgaFile.MappedImages[i], $"{mappedDir}\\Result.INI", false);
                        }
                        _tgaFile.Save($"{tgaDir}\\{_tgaFile.Name}");
                    }
                    break;
                case ETGAResult.Separated:
                    {
                        GameResource.MappedImage mappedImage;
                        for (int i = 0; i < _tgaFile.MappedImages.Count; i++)
                        {
                            mappedImage = (GameResource.MappedImage)_tgaFile.MappedImages[i].Clone();
                            mappedImage.Texture = $"{mappedImage.Name}.tga";
                            GameResource.MappedImage.SaveMappAndTGA(mappedImage, $"{tgaDir}\\{mappedImage.Texture}", $"{mappedDir}\\Result.INI", false);
                        }
                    }
                    break;
            }
        }
        private void SaveMappedImage_Separated(ETGAResult tgaResult, string mappedDir, string tgaDir)
        {
            switch (tgaResult)
            {
                case ETGAResult.Union:
                    {
                        for (int i = 0; i < _tgaFile.MappedImages.Count; i++)
                        {
                            GameResource.MappedImage.SaveMappedCode(_tgaFile.MappedImages[i], $"{mappedDir}\\{_tgaFile.MappedImages[i].Name}.INI", true);
                        }
                        _tgaFile.Save($"{tgaDir}\\{_tgaFile.Name}");
                    }
                    break;
                case ETGAResult.Separated:
                    {
                        GameResource.MappedImage mappedImage;
                        for (int i = 0; i < _tgaFile.MappedImages.Count; i++)
                        {
                            mappedImage = (GameResource.MappedImage)_tgaFile.MappedImages[i].Clone();
                            mappedImage.Texture = $"{mappedImage.Name}.tga";

                            GameResource.MappedImage.SaveMappAndTGA(mappedImage, $"{tgaDir}\\{mappedImage.Texture}", $"{mappedDir}\\{mappedImage.Name}.INI", true);
                        }
                    }
                    break;
            }

        }
        private void SaveMappedImage_SeparatedAsIs(ETGAResult tgaResult, string mappedDir, string tgaDir)
        {
            switch (tgaResult)
            {
                case ETGAResult.Union:
                    {
                        for (int i = 0; i < _tgaFile.MappedImages.Count; i++)
                        {
                            string dir = System.IO.Path.GetDirectoryName($"{mappedDir}\\{_tgaFile.MappedImages[i].ParentMappedFile.Name}");
                            if (!Directory.Exists(dir))
                                Directory.CreateDirectory(dir);
                            GameResource.MappedImage.SaveMappedCode(_tgaFile.MappedImages[i], $"{mappedDir}\\{_tgaFile.MappedImages[i].ParentMappedFile.Name}", false);
                        }
                        _tgaFile.Save($"{tgaDir}\\{_tgaFile.Name}");
                    }
                    break;
                case ETGAResult.Separated:
                    {
                        GameResource.MappedImage mappedImage;
                        for (int i = 0; i < _tgaFile.MappedImages.Count; i++)
                        {
                            mappedImage = (GameResource.MappedImage)_tgaFile.MappedImages[i].Clone();
                            mappedImage.Texture = $"{mappedImage.Name}.tga";
                            string dir = System.IO.Path.GetDirectoryName($"{mappedDir}\\{mappedImage.ParentMappedFile.Name}");
                            if (!Directory.Exists(dir))
                                Directory.CreateDirectory(dir);
                            GameResource.MappedImage.SaveMappAndTGA(mappedImage, $"{tgaDir}\\{mappedImage.Texture}", $"{mappedDir}\\{mappedImage.ParentMappedFile.Name}", false);
                        }
                    }
                    break;
            }

        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new WK.Libraries.BetterFolderBrowserNS.BetterFolderBrowser
            {
                Title = "Выберете папку для сохранения",
                Multiselect = false
            })
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (string.IsNullOrEmpty(dialog.SelectedFolder))
                    return;

                SaveResult(dialog.SelectedPath);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void rbStructureEx_Checked(object sender, RoutedEventArgs e) => _resultStructure = EResultStructure.Extension;

        private void rbStructureGame_Checked(object sender, RoutedEventArgs e) => _resultStructure = EResultStructure.Game;

        private void rbTGAUnion_Checked(object sender, RoutedEventArgs e) => _tgaResult = ETGAResult.Union;

        private void rbTGASeparated(object sender, RoutedEventArgs e) => _tgaResult = ETGAResult.Separated;

        private void rbImageUnion_Checked(object sender, RoutedEventArgs e) => _mappedImageResult = EMappedImageResult.Union;

        private void rbImageSeparatedImages_Checked(object sender, RoutedEventArgs e) => _mappedImageResult = EMappedImageResult.SeparatedImages;

        private void rbImageSeparatedAsIs_Checked(object sender, RoutedEventArgs e) => _mappedImageResult = EMappedImageResult.SeparatedAsIs;
    }
}