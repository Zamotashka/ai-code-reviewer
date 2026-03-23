using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Win32;

namespace AiReviewerWPF
{
    public partial class MainWindow : Window
    {
        private GroqClient? _client;

        public MainWindow()
        {
            InitializeComponent();
            InitClient();
            SetActiveNav(BtnFile);
        }

        private void InitClient()
        {
            var key = ConfigManager.GetApiKey();
            if (!string.IsNullOrEmpty(key))
            {
                _client = new GroqClient(key);
                ApiStatus.Text = "✅ API ключ настроен";
            }
            else
            {
                ApiStatus.Text = "❌ API ключ не задан — зайди в Настройки";
                ShowPanel(PanelSettings);
                SetActiveNav(null);
            }
        }

        // ─── Навигация ───────────────────────────────────────────────────────

        private void SetActiveNav(Button? active)
        {
            var buttons = new[] { BtnFile, BtnFolder, BtnHistory };
            foreach (var btn in buttons)
            {
                btn.Foreground = new SolidColorBrush(Color.FromRgb(0x6c, 0x70, 0x86));
                btn.BorderBrush = Brushes.Transparent;
            }
            if (active != null)
            {
                active.Foreground = new SolidColorBrush(Color.FromRgb(0xcb, 0xa6, 0xf7));
                active.BorderBrush = new SolidColorBrush(Color.FromRgb(0xcb, 0xa6, 0xf7));
            }
        }

        private void ShowPanel(UIElement panel)
        {
            PanelFile.Visibility     = Visibility.Collapsed;
            PanelFolder.Visibility   = Visibility.Collapsed;
            PanelHistory.Visibility  = Visibility.Collapsed;
            PanelSettings.Visibility = Visibility.Collapsed;
            panel.Visibility = Visibility.Visible;
        }

        private void BtnFile_Click(object sender, RoutedEventArgs e)    { ShowPanel(PanelFile);    SetActiveNav(BtnFile);    }
        private void BtnFolder_Click(object sender, RoutedEventArgs e)  { ShowPanel(PanelFolder);  SetActiveNav(BtnFolder);  }
        private void BtnSettings_Click(object sender, RoutedEventArgs e){ ShowPanel(PanelSettings); SetActiveNav(null);       }

        private void BtnHistory_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(PanelHistory);
            SetActiveNav(BtnHistory);
            LoadHistory();
        }

        // ─── Проверка файла ──────────────────────────────────────────────────

        private void BrowseFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Код|*.cs;*.js;*.ts;*.py;*.java|Все файлы|*.*"
            };
            if (dialog.ShowDialog() == true)
                TxtFilePath.Text = dialog.FileName;
        }

        private async void AnalyzeFile_Click(object sender, RoutedEventArgs e)
        {
            if (_client == null) { ShowNoKeyError(); return; }
            if (string.IsNullOrWhiteSpace(TxtFilePath.Text)) return;
            if (!File.Exists(TxtFilePath.Text))
            {
                StatusText.Text = "❌ Файл не найден";
                return;
            }

            BtnAnalyzeFile.IsEnabled = false;
            StatusText.Text = "⏳ Анализирую...";
            OutputBox.Document.Blocks.Clear();
            ResetStats();

            try
            {
                var code     = await File.ReadAllTextAsync(TxtFilePath.Text);
                var fileName = Path.GetFileName(TxtFilePath.Text);
                var prompt   = PromptBuilder.Build(fileName, code);
                var review   = await _client.ReviewAsync(prompt);

                DisplayReview(OutputBox, fileName, review);
                UpdateStats(review);
                HistoryManager.Save(fileName, review);
                StatusText.Text = $"✅ Готово — {fileName}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"❌ Ошибка: {ex.Message}";
            }
            finally
            {
                BtnAnalyzeFile.IsEnabled = true;
            }
        }

        // ─── Проверка папки ──────────────────────────────────────────────────

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            // OpenFolderDialog доступен в .NET 8+ WPF
            var dialog = new OpenFolderDialog { Title = "Выбери папку проекта" };
            if (dialog.ShowDialog() == true)
                TxtFolderPath.Text = dialog.FolderName;
        }

        private async void AnalyzeFolder_Click(object sender, RoutedEventArgs e)
        {
            if (_client == null) { ShowNoKeyError(); return; }
            if (string.IsNullOrWhiteSpace(TxtFolderPath.Text)) return;
            if (!Directory.Exists(TxtFolderPath.Text))
            {
                FolderStatusText.Text = "❌ Папка не найдена";
                return;
            }

            var lang = (LangCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "cs";

           
            var dirInfo = new DirectoryInfo(TxtFolderPath.Text);
            var files   = new FileScanner().Scan(dirInfo, lang);

            if (files.Count == 0)
            {
                FolderStatusText.Text = $"❌ Файлы .{lang} не найдены";
                return;
            }

            BtnAnalyzeFolder.IsEnabled = false;
            FolderOutputBox.Document.Blocks.Clear();
            FolderProgress.Visibility = Visibility.Visible;
            FolderProgress.Maximum    = files.Count;
            FolderProgress.Value      = 0;

            try
            {
                for (int i = 0; i < files.Count; i++)
                {
                    var f = files[i];
                    FolderStatusText.Text = $"⏳ Анализирую {f.Name} ({i + 1}/{files.Count})...";

                    var code   = await File.ReadAllTextAsync(f.FullName);
                    var prompt = PromptBuilder.Build(f.Name, code);
                    var review = await _client.ReviewAsync(prompt);

                    DisplayReview(FolderOutputBox, f.Name, review);
                    HistoryManager.Save(f.Name, review);
                    FolderProgress.Value = i + 1;
                }
                FolderStatusText.Text = $"✅ Готово — проверено файлов: {files.Count}";
            }
            catch (Exception ex)
            {
                FolderStatusText.Text = $"❌ Ошибка: {ex.Message}";
            }
            finally
            {
                FolderProgress.Visibility  = Visibility.Collapsed;
                BtnAnalyzeFolder.IsEnabled = true;
            }
        }

        // ─── История ─────────────────────────────────────────────────────────

        private void LoadHistory()
        {
            HistoryList.Items.Clear();
            var entries = HistoryManager.Load();
            foreach (var entry in entries)
                HistoryList.Items.Add($"{entry.Date:dd.MM.yyyy HH:mm}  {entry.FileName}");

            if (entries.Count == 0)
                HistoryList.Items.Add("История пуста");
        }
        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Очистить всю историю проверок?", "AI Reviewer",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                HistoryManager.Clear();
                HistoryDetailBox.Document.Blocks.Clear();
                LoadHistory();
            }
        }

        private void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var idx     = HistoryList.SelectedIndex;
            var entries = HistoryManager.Load();
            if (idx < 0 || idx >= entries.Count) return;

            var entry = entries[idx];
            HistoryDetailBox.Document.Blocks.Clear();
            DisplayReview(HistoryDetailBox, entry.FileName, entry.Review);
        }

        // ─── Настройки ───────────────────────────────────────────────────────

        private void SaveApiKey_Click(object sender, RoutedEventArgs e)
        {
            var key = ApiKeyBox.Password;
            if (string.IsNullOrWhiteSpace(key)) return;

            ConfigManager.SaveApiKey(key);
            _client        = new GroqClient(key);
            ApiStatus.Text = "✅ API ключ сохранён";
            ApiKeyBox.Clear();
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private static void DisplayReview(RichTextBox box, string fileName, string review)
        {
            var doc = box.Document;

            var header = new Paragraph(new Run($"── {fileName} ──"))
            {
                Foreground = new SolidColorBrush(Color.FromRgb(0xcb, 0xa6, 0xf7)),
                FontWeight = FontWeights.Bold,
                Margin     = new Thickness(0, 8, 0, 4)
            };
            doc.Blocks.Add(header);

            foreach (var line in review.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var color = line.StartsWith("BUG")      ? Color.FromRgb(0xf3, 0x8b, 0xa8)
                          : line.StartsWith("WARNING")  ? Color.FromRgb(0xfa, 0xb3, 0x87)
                          : line.StartsWith("СОВЕТ")    ? Color.FromRgb(0x89, 0xb4, 0xfa)
                          : line.StartsWith("ХОРОШО")   ? Color.FromRgb(0xa6, 0xe3, 0xa1)
                          : Color.FromRgb(0xcd, 0xd6, 0xf4);

                doc.Blocks.Add(new Paragraph(new Run(line))
                {
                    Foreground = new SolidColorBrush(color),
                    Margin     = new Thickness(0, 1, 0, 1)
                });
            }

            box.ScrollToEnd();
        }

        private void UpdateStats(string review)
        {
            var lines = review.Split('\n');
            StatBugs.Text     = lines.Count(l => l.StartsWith("BUG")).ToString();
            StatWarnings.Text = lines.Count(l => l.StartsWith("WARNING")).ToString();
            StatTips.Text     = lines.Count(l => l.StartsWith("СОВЕТ") || l.StartsWith("ХОРОШО")).ToString();
        }

        private void ResetStats()
        {
            StatBugs.Text = StatWarnings.Text = StatTips.Text = "—";
        }

        private void ShowNoKeyError()
        {
            MessageBox.Show("API ключ не задан. Зайди в Настройки.", "AI Reviewer",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            ShowPanel(PanelSettings);
        }
    }
}
