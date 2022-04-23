using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace GenImageViewer
{
    public class BIGFile
    {
        public string FileName;
    }
    public class BIGResource
    {
        public BIGFile BIGRFile;
        public int Offset, Lenght;
        public string Name;
    }
    public class MappedCoordinates : ICloneable
    {
        public int Left, Top, Right, Bottom;

        public object Clone() => MemberwiseClone();
    }

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
    public class MappedImage
    {
        public string Name;
        public string Texture;
        public int TextureWidth;
        public int TextureHeight;
        public MappedCoordinates Coords;
        public string Status;
        public MappedFile ParentMappedFile;
        public TGAFile TGAFile;

        public void CopyMappCodeToClipboard()
        {
            string s =
                $"MappedImage {Name}\r\n" +
                $"  Texture = {Texture}\r\n" +
                $"  TextureWidth = {TextureWidth}\r\n" +
                $"  TextureHeight = {TextureHeight}\r\n" +
                $"  Coords = " +
                $"Left:{Coords.Left} " +
                $"Top:{Coords.Top} " +
                $"Right:{Coords.Right} " +
                $"Bottom:{Coords.Bottom}\r\n" +
                $"  Status = {Status}\r\n" +
                $"End";
            Clipboard.SetText(s);
        }
        public void Save(string fileName, string location)
        {
            Bitmap bitmap;
            if (TGAFile.BIGResource != null)
            {
                using (FileStream fs = new FileStream($@"{ResourceManager.MainFolder}\{TGAFile.BIGResource.BIGRFile.FileName}", FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    br.BaseStream.Position = TGAFile.BIGResource.Offset;
                    byte[] bytes = br.ReadBytes(TGAFile.BIGResource.Lenght);
                    using (MemoryStream ms = new MemoryStream(bytes))
                    {
                        bitmap = TGALib.TargaImage.LoadTargaImage(ms);
                    }
                }
            }
            else
            {
                bitmap = TGALib.TargaImage.LoadTargaImage($@"{ResourceManager.MainFolder}\{location}\{TGAFile.Name}");
            }

            int height = TextureHeight;
            int width = TextureWidth;
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
    public enum ETGALocation
    {
        Other,
        ArtTextures,
        DataEnglishArtTextures
    }
    public static class TGALocationExtensions
    {
        public static string GetLocation(this ETGALocation tgaLocation)
        {
            switch (tgaLocation)
            {
            case ETGALocation.ArtTextures: return @"Art\Textures";
            case ETGALocation.DataEnglishArtTextures: return @"Data\English\Art\Textures";
            case ETGALocation.Other:
            default: return "";
            }
        }
    }

    public class TGAFile
    {
        public string Name;
        public BIGResource BIGResource;
        public List<MappedImage> MappedImages;

        public Bitmap GetBitmap(string mainFolder, string location)
        {
            if (BIGResource != null)
            {
                using (FileStream fs = new FileStream($@"{mainFolder}\{BIGResource.BIGRFile.FileName}", FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    br.BaseStream.Position = BIGResource.Offset;
                    byte[] bytes = br.ReadBytes(BIGResource.Lenght);
                    using (MemoryStream ms = new MemoryStream(bytes))
                    {
                        return TGALib.TargaImage.LoadTargaImage(ms);
                    }
                }
            }
            else
            {
                return TGALib.TargaImage.LoadTargaImage($@"{mainFolder}\{location}\{Name}");
            }
        }
    }
}
