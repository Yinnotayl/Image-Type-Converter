using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ImageConverterDemo
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<ImageItem> Items { get; } = new ObservableCollection<ImageItem>();
        public string[] AvailableFormats { get; } = new[] { "png", "jpeg", "bmp", "gif", "tiff" };

        public MainWindow()
        {
            InitializeComponent();
            ItemsList.ItemsSource = Items;
            PreItemsList.ItemsSource = Items;
            DataContext = this;
        }

        #region Drag & Drop / Select
        private void DropArea_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Copy;
        }

        private void DropArea_DragLeave(object sender, DragEventArgs e) { }

        private void DropArea_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                AddFiles(files);
            }
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff"
            };
            if (dlg.ShowDialog() == true) AddFiles(dlg.FileNames);
        }

        private void AddFiles(string[] files)
        {
            foreach (var f in files)
            {
                if (!File.Exists(f)) continue;
                if (Items.Any(i => string.Equals(i.SourcePath, f, StringComparison.OrdinalIgnoreCase))) continue;

                try
                {
                    var thumb = CreateThumbnail(f, 200);
                    var item = new ImageItem
                    {
                        SourcePath = f,
                        FileName = Path.GetFileName(f),
                        OriginalFormat = Path.GetExtension(f).TrimStart('.').ToLower(),
                        NewFormat = "png",
                        Thumb = thumb
                    };
                    Items.Add(item);
                }
                catch { /* ignore invalid images */ }
            }
            FilesCount.Text = $"{Items.Count} file(s) ready";
        }
        #endregion

        #region Convert & layout change
        private async void BtnConvert_Click(object sender, RoutedEventArgs e)
        {
            if (!Items.Any()) { MessageBox.Show("Add images first."); return; }

            // Reveal right column (squish left implicitly)
            RightColumn.Width = new GridLength(500);

            // Start conversions concurrently but limited concurrency (e.g., 4)
            var tasks = Items.Select(i => ConvertItemAsync(i)).ToArray();
            await Task.WhenAll(tasks);
        }

        private async Task ConvertItemAsync(ImageItem item)
        {
            if (item.IsConverting || item.IsConverted) return;
            item.IsConverting = true;
            UpdateProgress(item, 0);

            try
            {
                // Step 1: load image (simulate progress)
                UpdateProgress(item, 10);
                await Task.Delay(120);

                // decode into BitmapFrame
                BitmapFrame frame;
                using (var fs = File.OpenRead(item.SourcePath))
                {
                    var decoder = BitmapDecoder.Create(fs, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    frame = decoder.Frames[0];
                }
                UpdateProgress(item, 50);

                // Step 2: choose encoder
                BitmapEncoder encoder = GetEncoder(item.NewFormat) ?? throw new Exception("Unsupported format");
                encoder.Frames.Add(frame);
                UpdateProgress(item, 80);
                await Task.Delay(120);

                // Step 3: save to temp file
                var outExt = "." + item.NewFormat;
                var outPath = Path.Combine(Path.GetTempPath(), $"imgconv_{Guid.NewGuid()}{outExt}");
                using (var ms = new MemoryStream())
                {
                    encoder.Save(ms);
                    File.WriteAllBytes(outPath, ms.ToArray());
                }

                item.ConvertedPath = outPath;
                UpdateProgress(item, 100);
                item.IsConverted = true;
            }
            catch (Exception)
            {
                item.ProgressText = "Error";
                item.IsConverted = false;
            }
            finally
            {
                item.IsConverting = false;
            }
        }

        private void UpdateProgress(ImageItem item, int value)
        {
            item.Progress = value;
            item.ProgressText = value >= 100 ? "Completed" : $"{value}%";
        }
        #endregion

        #region Helpers
        private BitmapImage CreateThumbnail(string path, int maxSize)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path);
            bmp.DecodePixelWidth = maxSize;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

        private BitmapEncoder GetEncoder(string format)
        {
            switch ((format ?? "").ToLower())
            {
                case "png": return new PngBitmapEncoder();
                case "jpeg":
                case "jpg": return new JpegBitmapEncoder() { QualityLevel = 90 };
                case "bmp": return new BmpBitmapEncoder();
                case "gif": return new GifBitmapEncoder();
                case "tiff": return new TiffBitmapEncoder();
                default: return null;
            }
        }
        #endregion

        #region Downloads
        private void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ImageItem item)
            {
                if (!item.IsConverted || string.IsNullOrEmpty(item.ConvertedPath) || !File.Exists(item.ConvertedPath))
                {
                    MessageBox.Show("File not yet converted.");
                    return;
                }

                var sfd = new SaveFileDialog
                {
                    FileName = item.FileNameWithoutExt + "." + item.NewFormat,
                    Filter = $"{item.NewFormat.ToUpper()}|*." + item.NewFormat
                };
                if (sfd.ShowDialog() == true)
                {
                    File.Copy(item.ConvertedPath, sfd.FileName, true);
                    MessageBox.Show("Saved.");
                }
            }
        }

        private void BtnDownloadAll_Click(object sender, RoutedEventArgs e)
        {
            var converted = Items.Where(i => i.IsConverted && File.Exists(i.ConvertedPath)).ToList();
            if (!converted.Any()) { MessageBox.Show("No converted files yet."); return; }

            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            var winFormResult = dlg.ShowDialog();
            if (winFormResult == System.Windows.Forms.DialogResult.OK)
            {
                var folder = dlg.SelectedPath;
                foreach (var it in converted)
                {
                    var dest = Path.Combine(folder, it.FileNameWithoutExt + "." + it.NewFormat);
                    File.Copy(it.ConvertedPath, dest, true);
                }
                MessageBox.Show("All files copied to: " + folder);
            }
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class ImageItem : INotifyPropertyChanged
    {
        public string SourcePath { get; set; }
        public string FileName { get; set; }
        public string OriginalFormat { get; set; }
        public BitmapImage Thumb { get; set; }

        private string newFormat;
        public string NewFormat { get => newFormat; set { if (newFormat != value) { newFormat = value; OnProp(nameof(NewFormat)); } } }

        private int progress;
        public int Progress { get => progress; set { if (progress != value) { progress = value; OnProp(nameof(Progress)); } } }

        private string progressText;
        public string ProgressText { get => progressText; set { if (progressText != value) { progressText = value; OnProp(nameof(ProgressText)); } } }

        private bool isConverting;
        public bool IsConverting { get => isConverting; set { if (isConverting != value) { isConverting = value; OnProp(nameof(IsConverting)); } } }

        private bool isConverted;
        public bool IsConverted { get => isConverted; set { if (isConverted != value) { isConverted = value; OnProp(nameof(IsConverted)); } } }

        private string convertedPath;
        public string ConvertedPath { get => convertedPath; set { if (convertedPath != value) { convertedPath = value; OnProp(nameof(ConvertedPath)); } } }

        public string FileNameWithoutExt => Path.GetFileNameWithoutExtension(FileName);

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnProp(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
