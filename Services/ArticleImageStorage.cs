using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Caupo.Services
{
    public static class ArticleImageStorage
    {
        public static string DataDirectory
        {
            get
            {
                string? path = AppDomain.CurrentDomain.GetData ("DataDirectory") as string;

                if(!string.IsNullOrWhiteSpace (path))
                    return path;

                return AppContext.BaseDirectory;
            }
        }

        public static string ArticlesFolder => Path.Combine (DataDirectory, "Images", "Articles");
        public static string ImportedFolder => Path.Combine (ArticlesFolder, "Imported");
        public static string DefaultArticlesFolder => Path.Combine (AppContext.BaseDirectory, "Images", "Articles", "Default");

        public static void Initialize()
        {
            Directory.CreateDirectory (ArticlesFolder);
            Directory.CreateDirectory (ImportedFolder);

            Debug.WriteLine ($"[IMAGES] DataDirectory = {DataDirectory}");
            Debug.WriteLine ($"[IMAGES] ArticlesFolder = {ArticlesFolder}");
            Debug.WriteLine ($"[IMAGES] DefaultArticlesFolder = {DefaultArticlesFolder}");

            CopyDefaultImages ();
        }

        public static bool IsImportedImage(string? imagePath)
        {
            if(string.IsNullOrWhiteSpace (imagePath))
                return false;

            try
            {
                string fullImagePath = Path.IsPathRooted (imagePath)
                    ? Path.GetFullPath (imagePath)
                    : Path.GetFullPath (Path.Combine (ArticlesFolder, imagePath));

                string importedFolder = Path.GetFullPath (ImportedFolder);
                string importedPrefix = importedFolder.TrimEnd (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

                return fullImagePath.StartsWith (importedPrefix, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public static (int Width, int Height) GetTargetSize(int articleType)
        {
            return articleType switch
            {
                0 => (400, 800),
                1 => (800, 400),
                _ => (800, 800)
            };
        }

        public static string ProcessAndSaveExternalImage(string sourcePath, Int32Rect cropPixels, int articleType)
        {
            if(string.IsNullOrWhiteSpace (sourcePath) || !File.Exists (sourcePath))
                throw new FileNotFoundException ("Izvorna slika nije pronađena.", sourcePath);

            Directory.CreateDirectory (ArticlesFolder);

            BitmapDecoder decoder;
            using(FileStream stream = File.OpenRead (sourcePath))
                decoder = BitmapDecoder.Create (stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

            BitmapSource source = decoder.Frames[0];
            Int32Rect safeCrop = NormalizeCrop (cropPixels, source.PixelWidth, source.PixelHeight);
            CroppedBitmap cropped = new (source, safeCrop);

            (int targetWidth, int targetHeight) = GetTargetSize (articleType);
            double scaleX = (double)targetWidth / cropped.PixelWidth;
            double scaleY = (double)targetHeight / cropped.PixelHeight;
            TransformedBitmap resized = new (cropped, new ScaleTransform (scaleX, scaleY));

            string baseName = SanitizeFileName (Path.GetFileNameWithoutExtension (sourcePath));
            string destinationPath = GetUniqueDestinationPath (baseName, ".png");

            PngBitmapEncoder encoder = new ();
            encoder.Frames.Add (BitmapFrame.Create (resized));

            using(FileStream output = File.Create (destinationPath))
                encoder.Save (output);

            Debug.WriteLine ($"[IMAGES] Obrađena slika: {sourcePath} -> {destinationPath} ({targetWidth}x{targetHeight})");
            return destinationPath;
        }

        private static Int32Rect NormalizeCrop(Int32Rect crop, int imageWidth, int imageHeight)
        {
            int x = Math.Max (0, Math.Min (crop.X, imageWidth - 1));
            int y = Math.Max (0, Math.Min (crop.Y, imageHeight - 1));
            int width = Math.Max (1, Math.Min (crop.Width, imageWidth - x));
            int height = Math.Max (1, Math.Min (crop.Height, imageHeight - y));
            return new Int32Rect (x, y, width, height);
        }

        private static string GetUniqueDestinationPath(string baseName, string extension)
        {
            string destinationPath = Path.Combine (ArticlesFolder, baseName + extension);

            if(!File.Exists (destinationPath))
                return destinationPath;

            int index = 1;
            do
            {
                destinationPath = Path.Combine (ArticlesFolder, $"{baseName}_{index}{extension}");
                index++;
            }
            while(File.Exists (destinationPath));

            return destinationPath;
        }

        private static string SanitizeFileName(string fileName)
        {
            foreach(char invalidChar in Path.GetInvalidFileNameChars ())
                fileName = fileName.Replace (invalidChar, '_');

            return string.IsNullOrWhiteSpace (fileName) ? "artikl" : fileName.Trim ();
        }

        private static void CopyDefaultImages()
        {
            if(!Directory.Exists (DefaultArticlesFolder))
            {
                Debug.WriteLine ("[IMAGES] Default biblioteka ne postoji.");
                return;
            }

            int copied = 0;
            int existing = 0;

            foreach(string sourcePath in Directory.EnumerateFiles (DefaultArticlesFolder))
            {
                string extension = Path.GetExtension (sourcePath);

                if(!IsSupportedImageExtension (extension))
                    continue;

                string fileName = Path.GetFileName (sourcePath);
                string destinationPath = Path.Combine (ArticlesFolder, fileName);

                if(File.Exists (destinationPath))
                {
                    existing++;
                    continue;
                }

                File.Copy (sourcePath, destinationPath, false);
                copied++;
            }

            Debug.WriteLine ($"[IMAGES] Kopirano novih slika: {copied}");
            Debug.WriteLine ($"[IMAGES] Već postoji: {existing}");
        }

        private static bool IsSupportedImageExtension(string extension)
        {
            return extension.Equals (".jpg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals (".jpeg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals (".png", StringComparison.OrdinalIgnoreCase);
        }
    }
}
