using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Security.Policy;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;



namespace InstantDealingApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<BatchItem> BatchItems { get; set; } = new ObservableCollection<BatchItem>();
        private bool _isLaborManuallyEditedMulti = false;
        private bool _isSyncingPurityMulti = false;
        private bool _isCalculatingRattiMulti = false;
        private bool _isUpdatingGoldRateMulti = false;
        private double _rawBaseGoldRateMulti = 0;

        public MainWindow()
        {
            InitializeComponent();
            LoadWindowIcon();
        }

        private void LoadWindowIcon()
        {
            try
            {
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_icon.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = BitmapFrame.Create(new Uri(iconPath, UriKind.Absolute));
                }
                else
                {
                    Uri packUri = new Uri("pack://application:,,,/app_icon.ico", UriKind.RelativeOrAbsolute);
                    this.Icon = BitmapFrame.Create(packUri);
                }
            }
            catch
            {
                // Gracefully ignore if icon cannot be loaded
            }
        }

        private string GetCustomerJsonPath()
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "customer.json");
            if (!File.Exists(path))
            {
                try
                {
                    File.WriteAllText(path, "{\"Names\":[]}");
                }
                catch { }
            }
            return path;
        }

        private List<string> LoadCustomerNames()
        {
            try
            {
                string jsonPath = GetCustomerJsonPath();
                var jsonData = File.ReadAllText(jsonPath);
                dynamic? jsonObject = JsonConvert.DeserializeObject(jsonData);
                if (jsonObject?.Names != null)
                {
                    return jsonObject.Names.ToObject<List<string>>();
                }
            }
            catch { }
            return new List<string>();
        }

        private void cashTextbox_KeyDown(object sender, KeyEventArgs e)
        {
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var names = LoadCustomerNames();
            if (customersTest != null) customersTest.ItemsSource = names;
            if (customersMulti != null) customersMulti.ItemsSource = names;
            if (batchDataGrid != null) batchDataGrid.ItemsSource = BatchItems;
            LoadPrintSettings();
            LoadAppSettings();
            _allPrintRecords = LoadPrintHistory();
            ApplyHistoryFilters();
            mainTabControl.SelectedItem = testTab;
            updateTestCalculations();
            updateMultiTestCalculations();

            CheckAndHandleScreenshotArgs();
        }

        private void CheckAndHandleScreenshotArgs()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--screenshot" && i + 1 < args.Length)
                {
                    string outPath = args[i + 1];
                    string? historyOutPath = null;
                    string? receiptOutPath = null;
                    string? smallReceiptOutPath = null;
                    string? multiOutPath = null;
                    string? batchReceiptOutPath = null;
                    string? smallBatchReceiptOutPath = null;
                    string? settingsOutPath = null;

                    for (int j = 0; j < args.Length; j++)
                    {
                        if (args[j] == "--history" && j + 1 < args.Length) historyOutPath = args[j + 1];
                        if (args[j] == "--receipt" && j + 1 < args.Length) receiptOutPath = args[j + 1];
                        if (args[j] == "--small-receipt" && j + 1 < args.Length) smallReceiptOutPath = args[j + 1];
                        if (args[j] == "--multi" && j + 1 < args.Length) multiOutPath = args[j + 1];
                        if (args[j] == "--batch-receipt" && j + 1 < args.Length) batchReceiptOutPath = args[j + 1];
                        if (args[j] == "--small-batch-receipt" && j + 1 < args.Length) smallBatchReceiptOutPath = args[j + 1];
                        if (args[j] == "--settings" && j + 1 < args.Length) settingsOutPath = args[j + 1];
                    }
                    if (string.IsNullOrEmpty(historyOutPath) && i + 2 < args.Length && !args[i + 2].StartsWith("--"))
                    {
                        historyOutPath = args[i + 2];
                    }

                    Dispatcher.InvokeAsync(async () =>
                    {
                        // Set realistic sample data for Single Test Tab
                        if (goldRateTextboxTest != null) goldRateTextboxTest.Text = "285,000";
                        if (customersTest != null) customersTest.Text = "Ahsan Bhai";
                        if (radioGold != null) radioGold.IsChecked = true;
                        if (itemTypeDropdownTest != null)
                        {
                            foreach (ComboBoxItem it in itemTypeDropdownTest.Items)
                            {
                                if (it.Tag?.ToString() == "Chori" || it.Content?.ToString()?.Contains("Chori") == true)
                                {
                                    itemTypeDropdownTest.SelectedItem = it;
                                    break;
                                }
                            }
                        }
                        if (weightInputTextbox != null) weightInputTextbox.Text = "10.500";
                        if (takaTextbox != null) takaTextbox.Text = "0.500";
                        if (resultTextbox != null) resultTextbox.Text = "1.50";
                        if (doPrintWeight != null) doPrintWeight.IsChecked = true;
                        if (doPrintTaka != null) doPrintTaka.IsChecked = true;
                        if (doPrintTotalWeight != null) doPrintTotalWeight.IsChecked = true;
                        if (doPrintResult != null) doPrintResult.IsChecked = true;
                        if (doPrintPureGold != null) doPrintPureGold.IsChecked = true;
                        if (doPrintLabor != null) doPrintLabor.IsChecked = true;
                        if (doPrintTotalPureGoldCash != null) doPrintTotalPureGoldCash.IsChecked = true;
                        if (doPrintGoldRateTest != null) doPrintGoldRateTest.IsChecked = true;
                        if (doPrintCustomerNameTest != null) doPrintCustomerNameTest.IsChecked = true;
                        if (notesTextboxTest != null) notesTextboxTest.Text = "12.50\r\n8.25\r\n15.00";
                        updateTestCalculations();

                        await System.Threading.Tasks.Task.Delay(700);
                        CaptureVisualToFile(this, outPath);

                        // Capture Single Test Large Receipt
                        if (!string.IsNullOrEmpty(receiptOutPath))
                        {
                            SetSlipSize(false);
                            FlowDocument receiptDoc = CreateFlowDocument(DateTime.Now);
                            CaptureReceiptDocumentToFile(receiptDoc, receiptOutPath);
                        }

                        // Capture Single Test Small Receipt
                        if (!string.IsNullOrEmpty(smallReceiptOutPath))
                        {
                            SetSlipSize(true);
                            FlowDocument smallDoc = CreateFlowDocument(DateTime.Now);
                            CaptureReceiptDocumentToFile(smallDoc, smallReceiptOutPath);
                            SetSlipSize(false); // revert to large
                        }

                        // Multi Test Tab Setup
                        if (multiTestTab != null)
                        {
                            if (goldRateTextboxMulti != null) goldRateTextboxMulti.Text = "285,000";
                            if (customersMulti != null) customersMulti.Text = "Haji Munir & Sons";
                            if (radioGoldMulti != null) radioGoldMulti.IsChecked = true;

                            BatchItems.Clear();
                            BatchItems.Add(new BatchItem
                            {
                                Index = 1,
                                Metal = "Gold",
                                ItemType = "Set",
                                Weight = 12.500,
                                Taka = 0.500,
                                TotalWeight = 12.000,
                                Ratti = 1.50,
                                Karat = 23.63,
                                PurityPercent = 98.44,
                                PureGold = 11.810,
                                Labor = 360,
                                Cash = 288623
                            });
                            BatchItems.Add(new BatchItem
                            {
                                Index = 2,
                                Metal = "Gold",
                                ItemType = "Ring",
                                Weight = 6.200,
                                Taka = 0.000,
                                TotalWeight = 6.200,
                                Ratti = 2.00,
                                Karat = 23.50,
                                PurityPercent = 97.92,
                                PureGold = 6.070,
                                Labor = 350,
                                Cash = 148166
                            });
                            BatchItems.Add(new BatchItem
                            {
                                Index = 3,
                                Metal = "Gold",
                                ItemType = "Chain",
                                Weight = 18.400,
                                Taka = 0.800,
                                TotalWeight = 17.600,
                                Ratti = 1.00,
                                Karat = 23.75,
                                PurityPercent = 98.96,
                                PureGold = 17.410,
                                Labor = 528,
                                Cash = 425510
                            });

                            if (notesTextboxMulti != null)
                            {
                                notesTextboxMulti.Text = "12.5\n6.2\n17.6\nAvg sample check";
                            }
                            updateBatchTotals();

                            // Capture Multi Test Large Batch Receipt
                            if (!string.IsNullOrEmpty(batchReceiptOutPath))
                            {
                                SetSlipSize(false);
                                FlowDocument batchDoc = CreateBatchFlowDocument(DateTime.Now);
                                CaptureReceiptDocumentToFile(batchDoc, batchReceiptOutPath);
                            }

                            // Capture Multi Test Small Batch Receipt
                            if (!string.IsNullOrEmpty(smallBatchReceiptOutPath))
                            {
                                SetSlipSize(true);
                                FlowDocument smallBatchDoc = CreateBatchFlowDocument(DateTime.Now);
                                CaptureReceiptDocumentToFile(smallBatchDoc, smallBatchReceiptOutPath);
                                SetSlipSize(false);
                            }

                            if (!string.IsNullOrEmpty(multiOutPath))
                            {
                                mainTabControl.SelectedItem = multiTestTab;
                                await System.Threading.Tasks.Task.Delay(700);
                                CaptureVisualToFile(this, multiOutPath);
                            }
                        }

                        if (!string.IsNullOrEmpty(historyOutPath) && historyTab != null)
                        {
                            if (_allPrintRecords.Count == 0 || !_allPrintRecords.Any(r => r.CustomerName == "Ahsan Bhai"))
                            {
                                FlowDocument sampleDoc = CreateFlowDocument(DateTime.Now);
                                RecordCurrentPrint(DateTime.Now, sampleDoc);
                            }

                            mainTabControl.SelectedItem = historyTab;
                            _allPrintRecords = LoadPrintHistory();
                            ApplyHistoryFilters();
                            if (historyDataGrid.ItemsSource is IEnumerable<PrintRecordDisplay> dispList && dispList.Any())
                            {
                                dispList.First().IsSelected = true;
                                UpdateHistorySelectionCount();
                            }
                            await System.Threading.Tasks.Task.Delay(700);
                            CaptureVisualToFile(this, historyOutPath);
                        }

                        if (!string.IsNullOrEmpty(settingsOutPath) && settingsTab != null)
                        {
                            mainTabControl.SelectedItem = settingsTab;
                            await System.Threading.Tasks.Task.Delay(700);
                            CaptureVisualToFile(this, settingsOutPath);
                        }

                        Application.Current.Shutdown();
                    });
                    break;
                }
            }
        }

        private void CaptureReceiptDocumentToFile(FlowDocument doc, string filePath)
        {
            try
            {
                double targetWidth = doc.PageWidth > 0 ? (doc.PageWidth + 16) : 310;
                double cardWidth = targetWidth + 12;

                FlowDocumentScrollViewer viewer = new FlowDocumentScrollViewer
                {
                    Document = doc,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                    Background = Brushes.White,
                    Width = targetWidth
                };
                Border card = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(4),
                    Child = viewer,
                    Width = cardWidth
                };
                card.Measure(new Size(cardWidth, 4000));
                card.Arrange(new Rect(0, 0, cardWidth, card.DesiredSize.Height));
                card.UpdateLayout();

                int h = Math.Max(380, (int)card.DesiredSize.Height);
                RenderTargetBitmap rtb = new RenderTargetBitmap((int)cardWidth, h, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(card);

                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    encoder.Save(fs);
                }
            }
            catch { }
        }

        private void CaptureVisualToFile(Visual visual, string filePath)
        {
            try
            {
                this.UpdateLayout();
                double width = this.ActualWidth > 0 ? this.ActualWidth : 800;
                double height = this.ActualHeight > 0 ? this.ActualHeight : 700;

                RenderTargetBitmap rtb = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(visual);

                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));

                using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    encoder.Save(fs);
                }
            }
            catch { }
        }

        private string GetPrintSettingsJsonPath()
        {
            return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "print_settings.json");
        }

        private void PrintCheckBox_Click(object sender, RoutedEventArgs e)
        {
            SavePrintSettings();
        }

        private void SavePrintSettings()
        {
            try
            {
                var settings = new Dictionary<string, bool>();

                // XRF Test Tab
                if (doPrintGoldRateTest != null) settings["doPrintGoldRateTest"] = doPrintGoldRateTest.IsChecked ?? false;
                if (doPrintCustomerNameTest != null) settings["doPrintCustomerNameTest"] = doPrintCustomerNameTest.IsChecked ?? false;
                if (doPrintWeight != null) settings["doPrintWeight"] = doPrintWeight.IsChecked ?? false;
                if (doPrintTaka != null) settings["doPrintTaka"] = doPrintTaka.IsChecked ?? false;
                if (doPrintTotalWeight != null) settings["doPrintTotalWeight"] = doPrintTotalWeight.IsChecked ?? false;
                if (doPrintResult != null) settings["doPrintResult"] = doPrintResult.IsChecked ?? false;
                if (doPrintPureGold != null) settings["doPrintPureGold"] = doPrintPureGold.IsChecked ?? false;
                if (doPrintLabor != null) settings["doPrintLabor"] = doPrintLabor.IsChecked ?? false;
                if (doPrintGrossPureGoldCash != null) settings["doPrintGrossPureGoldCash"] = doPrintGrossPureGoldCash.IsChecked ?? false;
                if (doPrintTotalPureGoldCash != null) settings["doPrintTotalPureGoldCash"] = doPrintTotalPureGoldCash.IsChecked ?? false;
                if (doPrintKarat != null) settings["doPrintKarat"] = doPrintKarat.IsChecked ?? false;
                if (doPrintPurityPercent != null) settings["doPrintPurityPercent"] = doPrintPurityPercent.IsChecked ?? false;
                if (doPrintNotes != null) settings["doPrintNotes"] = doPrintNotes.IsChecked ?? false;
                settings["isSmallSlip"] = IsSmallSlipSelected;

                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(GetPrintSettingsJsonPath(), json);
            }
            catch { }
        }

        private void LoadPrintSettings()
        {
            try
            {
                string path = GetPrintSettingsJsonPath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var settings = JsonConvert.DeserializeObject<Dictionary<string, bool>>(json);
                    if (settings != null)
                    {
                        // XRF Test Tab
                        if (settings.TryGetValue("doPrintGoldRateTest", out bool val1) && doPrintGoldRateTest != null) doPrintGoldRateTest.IsChecked = val1;
                        if (settings.TryGetValue("doPrintCustomerNameTest", out bool val2) && doPrintCustomerNameTest != null) doPrintCustomerNameTest.IsChecked = val2;
                        if (settings.TryGetValue("doPrintWeight", out bool valWeight) && doPrintWeight != null) doPrintWeight.IsChecked = valWeight;
                        if (settings.TryGetValue("doPrintTaka", out bool valTaka) && doPrintTaka != null) doPrintTaka.IsChecked = valTaka;
                        if (settings.TryGetValue("doPrintTotalWeight", out bool val3) && doPrintTotalWeight != null) doPrintTotalWeight.IsChecked = val3;
                        if (settings.TryGetValue("doPrintResult", out bool val4) && doPrintResult != null) doPrintResult.IsChecked = val4;
                        if (settings.TryGetValue("doPrintPureGold", out bool val5) && doPrintPureGold != null) doPrintPureGold.IsChecked = val5;
                        if (settings.TryGetValue("doPrintLabor", out bool val6) && doPrintLabor != null) doPrintLabor.IsChecked = val6;
                        if (settings.TryGetValue("doPrintGrossPureGoldCash", out bool val7) && doPrintGrossPureGoldCash != null) doPrintGrossPureGoldCash.IsChecked = val7;
                        if (settings.TryGetValue("doPrintTotalPureGoldCash", out bool val8) && doPrintTotalPureGoldCash != null) doPrintTotalPureGoldCash.IsChecked = val8;
                        if (settings.TryGetValue("doPrintKarat", out bool valKarat) && doPrintKarat != null) doPrintKarat.IsChecked = valKarat;
                        if (settings.TryGetValue("doPrintPurityPercent", out bool valPurity) && doPrintPurityPercent != null) doPrintPurityPercent.IsChecked = valPurity;
                        if (settings.TryGetValue("doPrintNotes", out bool valNotes) && doPrintNotes != null) doPrintNotes.IsChecked = valNotes;

                        if (settings.TryGetValue("isSmallSlip", out bool isSmall))
                        {
                            SetSlipSize(isSmall);
                        }
                    }
                }
            }
            catch { }
        }

        #region Application Settings

        public AppSettings CurrentSettings { get; set; } = new AppSettings();

        private string GetAppSettingsJsonPath()
        {
            return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_settings.json");
        }

        private void LoadAppSettings()
        {
            try
            {
                string path = GetAppSettingsJsonPath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    if (settings != null)
                    {
                        CurrentSettings = settings;
                    }
                }
            }
            catch { }

            ApplyAppSettingsToUI();
        }

        private void SaveAppSettings()
        {
            try
            {
                // Read from UI
                if (int.TryParse(txtSettingCopies?.Text, out int copies))
                {
                    CurrentSettings.NumberOfCopies = Math.Max(1, copies);
                }
                CurrentSettings.IsSmallSlip = radioSettingsSlipSmall?.IsChecked == true;
                CurrentSettings.IsBoldEnabled = radioSettingBold?.IsChecked == true;

                string path = GetAppSettingsJsonPath();
                string json = JsonConvert.SerializeObject(CurrentSettings, Formatting.Indented);
                File.WriteAllText(path, json);

                // Synchronize slip size with print_settings
                SetSlipSize(CurrentSettings.IsSmallSlip);
                SavePrintSettings();

                ApplyAppSettingsToUI();

                if (txtSettingsStatus != null)
                {
                    txtSettingsStatus.Text = "Settings saved successfully! / ترتیبات محفوظ ہو گئیں ✓";
                    var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                    timer.Tick += (s, e) => { txtSettingsStatus.Text = ""; timer.Stop(); };
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Save Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ApplyAppSettingsToUI()
        {
            if (txtSettingCopies != null)
                txtSettingCopies.Text = CurrentSettings.NumberOfCopies.ToString();

            if (radioSettingsSlipSmall != null && radioSettingsSlipLarge != null)
            {
                radioSettingsSlipSmall.IsChecked = CurrentSettings.IsSmallSlip;
                radioSettingsSlipLarge.IsChecked = !CurrentSettings.IsSmallSlip;
            }

            SetSlipSize(CurrentSettings.IsSmallSlip);

            if (radioSettingBold != null && radioSettingRegular != null)
            {
                radioSettingBold.IsChecked = CurrentSettings.IsBoldEnabled;
                radioSettingRegular.IsChecked = !CurrentSettings.IsBoldEnabled;
            }

            // Apply font weight to window
            this.FontWeight = CurrentSettings.IsBoldEnabled ? FontWeights.Bold : FontWeights.Normal;
            TextElement.SetFontWeight(this, CurrentSettings.IsBoldEnabled ? FontWeights.Bold : FontWeights.Normal);

            // Font size delta display
            if (txtSettingFontSizeDelta != null)
            {
                if (CurrentSettings.FontSizeDelta == 0)
                    txtSettingFontSizeDelta.Text = "Current: Normal (0px)";
                else if (CurrentSettings.FontSizeDelta > 0)
                    txtSettingFontSizeDelta.Text = $"Current: +{CurrentSettings.FontSizeDelta}px";
                else
                    txtSettingFontSizeDelta.Text = $"Current: {CurrentSettings.FontSizeDelta}px";
            }

            if (txtSettingSamplePreview != null)
            {
                txtSettingSamplePreview.FontSize = Math.Max(10, 16 + CurrentSettings.FontSizeDelta);
                txtSettingSamplePreview.FontWeight = CurrentSettings.IsBoldEnabled ? FontWeights.Bold : FontWeights.Normal;
            }
        }

        private void btnDecreaseCopies_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtSettingCopies?.Text, out int c))
            {
                int newCopies = Math.Max(1, c - 1);
                if (txtSettingCopies != null) txtSettingCopies.Text = newCopies.ToString();
                CurrentSettings.NumberOfCopies = newCopies;
            }
        }

        private void btnIncreaseCopies_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtSettingCopies?.Text, out int c))
            {
                int newCopies = Math.Min(10, c + 1);
                if (txtSettingCopies != null) txtSettingCopies.Text = newCopies.ToString();
                CurrentSettings.NumberOfCopies = newCopies;
            }
        }

        private void btnPresetCopies_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int p))
            {
                if (txtSettingCopies != null) txtSettingCopies.Text = p.ToString();
                CurrentSettings.NumberOfCopies = p;
            }
        }

        private void settingsSlipSize_Checked(object sender, RoutedEventArgs e)
        {
            bool isSmall = radioSettingsSlipSmall?.IsChecked == true;
            CurrentSettings.IsSmallSlip = isSmall;
            SetSlipSize(isSmall);
        }

        private void btnDecreaseFontSize_Click(object sender, RoutedEventArgs e)
        {
            CurrentSettings.FontSizeDelta = Math.Max(-4, CurrentSettings.FontSizeDelta - 1);
            ApplyAppSettingsToUI();
        }

        private void btnIncreaseFontSize_Click(object sender, RoutedEventArgs e)
        {
            CurrentSettings.FontSizeDelta = Math.Min(6, CurrentSettings.FontSizeDelta + 1);
            ApplyAppSettingsToUI();
        }

        private void btnResetFontSize_Click(object sender, RoutedEventArgs e)
        {
            CurrentSettings.FontSizeDelta = 0;
            ApplyAppSettingsToUI();
        }

        private void settingsBold_Checked(object sender, RoutedEventArgs e)
        {
            CurrentSettings.IsBoldEnabled = radioSettingBold?.IsChecked == true;
            ApplyAppSettingsToUI();
        }

        private void btnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            SaveAppSettings();
        }

        private void btnRestoreSettingsDefaults_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show(
                "Are you sure you want to restore default settings?\n\nThis will reset print copies to 2, slip size to Large (80mm), font size to default, and text to bold.\n\nکیا آپ پہلے والی ترتیبات بحال کرنا چاہتے ہیں؟",
                "Restore Defaults",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                CurrentSettings = new AppSettings();
                SaveAppSettings();
            }
        }

        #endregion

        private bool _isCalculatingRatti = false;
        private double _rawBaseGoldRate = 0;
        private bool _isUpdatingGoldRate = false;

        private bool IsDescendantOf(DependencyObject? element, DependencyObject? parent)
        {
            if (element == null || parent == null) return false;
            DependencyObject? current = element;
            while (current != null)
            {
                if (current == parent) return true;
                current = VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current);
            }
            return false;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                var focusedElement = Keyboard.FocusedElement as UIElement;

                // Allow natural click action when focus is on a Button
                if (focusedElement is Button) return;

                // Allow natural Enter key in multiline textboxes
                if (focusedElement is TextBox tb && tb.AcceptsReturn) return;

                // 1. Customer ComboBox (or textboxes inside it)
                if (focusedElement == customersTest || IsDescendantOf(focusedElement, customersTest))
                {
                    if (!string.IsNullOrWhiteSpace(customersTest.Text))
                    {
                        customersTest.Text = CapitalizeCustomerName(customersTest.Text);
                        AppendUniqueName(customersTest.Text);
                        if (doPrintCustomerNameTest != null) doPrintCustomerNameTest.IsChecked = true;
                    }
                    weightInputTextbox.Focus();
                    e.Handled = true;
                    return;
                }

                // 2. Test Tab Primary Flow
                if (focusedElement == goldRateTextboxTest)
                {
                    if (goldRateDiffTextbox != null)
                    {
                        goldRateDiffTextbox.Focus();
                    }
                    else
                    {
                        customersTest.Focus();
                    }
                    e.Handled = true;
                    return;
                }
                if (focusedElement == goldRateDiffTextbox)
                {
                    customersTest.Focus();
                    e.Handled = true;
                    return;
                }
                if (focusedElement == weightInputTextbox)
                {
                    takaTextbox.Focus();
                    e.Handled = true;
                    return;
                }
                if (focusedElement == takaTextbox)
                {
                    resultTextbox.Focus();
                    e.Handled = true;
                    return;
                }
                if (focusedElement == totalWeightTextbox)
                {
                    resultTextbox.Focus();
                    e.Handled = true;
                    return;
                }
                if (focusedElement == resultTextbox)
                {
                    laborTextbox.Focus();
                    e.Handled = true;
                    return;
                }
                if (focusedElement == laborTextbox)
                {
                    SaveButton.Focus();
                    e.Handled = true;
                    return;
                }

                // 3. Test Tab Conversion & Testing Helpers (Right side)
                if (focusedElement == sampleWeightTextbox)
                {
                    resultWeightTextbox.Focus();
                    e.Handled = true;
                    return;
                }
                if (focusedElement == resultWeightTextbox ||
                    focusedElement == karatInputTextbox ||
                    focusedElement == rattiConversionTextbox ||
                    focusedElement == purityPercentTextbox ||
                    focusedElement == purityRattiResultTextbox)
                {
                    weightInputTextbox.Focus();
                    e.Handled = true;
                    return;
                }

                // 4. Multi Test Primary Flow
                if (focusedElement == goldRateTextboxMulti)
                {
                    if (goldRateDiffTextboxMulti != null) goldRateDiffTextboxMulti.Focus();
                    else customersMulti.Focus();
                    e.Handled = true;
                    return;
                }
                if (focusedElement == goldRateDiffTextboxMulti)
                {
                    customersMulti.Focus();
                    e.Handled = true;
                    return;
                }
                if (focusedElement == customersMulti || IsDescendantOf(focusedElement, customersMulti))
                {
                    if (!string.IsNullOrWhiteSpace(customersMulti.Text))
                    {
                        customersMulti.Text = CapitalizeCustomerName(customersMulti.Text);
                        AppendUniqueName(customersMulti.Text);
                    }
                    weightInputTextboxMulti.Focus();
                    e.Handled = true;
                    return;
                }
                if (focusedElement == weightInputTextboxMulti)
                {
                    takaTextboxMulti.Focus();
                    e.Handled = true;
                    return;
                }
                if (focusedElement == takaTextboxMulti)
                {
                    resultTextboxMulti.Focus();
                    e.Handled = true;
                    return;
                }
                if (focusedElement == totalWeightTextboxMulti)
                {
                    resultTextboxMulti.Focus();
                    e.Handled = true;
                    return;
                }
                if (focusedElement == resultTextboxMulti)
                {
                    laborTextboxMulti.Focus();
                    e.Handled = true;
                    return;
                }
                if (focusedElement == laborTextboxMulti)
                {
                    addBatchItemBtn.Focus();
                    e.Handled = true;
                    return;
                }
                if (focusedElement == karatInputTextboxMulti ||
                    focusedElement == purityPercentTextboxMulti)
                {
                    weightInputTextboxMulti.Focus();
                    e.Handled = true;
                    return;
                }

                // General fallback navigation
                if (focusedElement != null)
                {
                    focusedElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    e.Handled = true;
                }
            }
        }

        private bool IsValidInput(string input, TextBox textBox)
        {
            string currentText = textBox.Text.Insert(textBox.SelectionStart, input);
            return Regex.IsMatch(currentText, @"^-?\d*\.?\d*$");
        }

        private bool doPrint(dynamic sender)
        {
            switch (sender)
            {
                case TextBox textBox when textBox == goldRateTextboxTest:
                    return doPrintGoldRateTest?.IsChecked ?? false;

                case ComboBox comboBox when comboBox == customersTest:
                    return doPrintCustomerNameTest?.IsChecked ?? false;

                case TextBox text when text == weightInputTextbox:
                    return doPrintWeight?.IsChecked ?? false;
                case TextBox text when text == takaTextbox:
                    return doPrintTaka?.IsChecked ?? false;
                case TextBox text when text == totalWeightTextbox:
                    return doPrintTotalWeight?.IsChecked ?? false;
                case TextBox text when text == resultTextbox:
                    return doPrintResult?.IsChecked ?? false;
                case TextBox text when text == pureGoldTextbox:
                    return doPrintPureGold?.IsChecked ?? false;
                case TextBox text when text == laborTextbox:
                    return doPrintLabor?.IsChecked ?? false;
                case TextBox text when text == grossPureGoldCashTextbox:
                    return doPrintGrossPureGoldCash?.IsChecked ?? false;
                case TextBox text when text == totalPureGoldCashTextbox:
                    return doPrintTotalPureGoldCash?.IsChecked ?? false;
                case TextBox text when text == karatInputTextbox:
                    return doPrintKarat?.IsChecked ?? false;
                case TextBox text when text == purityPercentTextbox:
                    return doPrintPurityPercent?.IsChecked ?? false;
                case TextBox text when text == notesTextboxTest:
                    return doPrintNotes?.IsChecked ?? false;

                default:
                    return false;
            }
        }
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mainTabControl.SelectedItem == testTab)
            {
                updateTestCalculations();
                if (saveButtonText != null)
                {
                    saveButtonText.Inlines.Clear();
                    saveButtonText.Inlines.Add(new Run("Print / ") { FontWeight = FontWeights.Bold });
                    saveButtonText.Inlines.Add(new Run("پرنٹ") { FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./font/#Alvi Nastaleeq Regular"), FontSize = 24, FontWeight = FontWeights.Bold });
                }
            }
            else if (mainTabControl.SelectedItem == multiTestTab)
            {
                updateMultiTestCalculations();
                if (saveButtonText != null)
                {
                    saveButtonText.Inlines.Clear();
                    saveButtonText.Inlines.Add(new Run("Print Batch / ") { FontWeight = FontWeights.Bold });
                    saveButtonText.Inlines.Add(new Run("پرنٹ بیچ") { FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./font/#Alvi Nastaleeq Regular"), FontSize = 24, FontWeight = FontWeights.Bold });
                }
            }
            else if (mainTabControl.SelectedItem == historyTab)
            {
                _allPrintRecords = LoadPrintHistory();
                ApplyHistoryFilters();
                if (saveButtonText != null)
                {
                    saveButtonText.Inlines.Clear();
                    saveButtonText.Inlines.Add(new Run("Print Selected / ") { FontWeight = FontWeights.Bold });
                    saveButtonText.Inlines.Add(new Run("منتخب پرنٹ کریں") { FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./font/#Alvi Nastaleeq Regular"), FontSize = 24, FontWeight = FontWeights.Bold });
                }
            }
            else if (mainTabControl.SelectedItem == settingsTab)
            {
                ApplyAppSettingsToUI();
                if (saveButtonText != null)
                {
                    saveButtonText.Inlines.Clear();
                    saveButtonText.Inlines.Add(new Run("Save Settings / ") { FontWeight = FontWeights.Bold });
                    saveButtonText.Inlines.Add(new Run("ترتیبات محفوظ کریں") { FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./font/#Alvi Nastaleeq Regular"), FontSize = 24, FontWeight = FontWeights.Bold });
                }
            }
        }

        #region Print History & DataGrid

        private string GetPrintHistoryJsonPath()
        {
            return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "print_history.json");
        }

        private List<PrintRecord> _allPrintRecords = new List<PrintRecord>();
        private bool _isApplyingHistoryFilters = false;

        private List<PrintRecord> LoadPrintHistory()
        {
            try
            {
                string path = GetPrintHistoryJsonPath();
                if (File.Exists(path) && new FileInfo(path).Length > 2)
                {
                    string json = File.ReadAllText(path);
                    var list = JsonConvert.DeserializeObject<List<PrintRecord>>(json);
                    if (list != null && list.Count > 0)
                    {
                        return list;
                    }
                }

                // Auto-recover from history_backups if main file was empty or missing
                string backupDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "history_backups");
                if (Directory.Exists(backupDir))
                {
                    var backupFiles = new DirectoryInfo(backupDir)
                        .GetFiles("*.json")
                        .OrderByDescending(f => f.LastWriteTime)
                        .ToList();

                    foreach (var bf in backupFiles)
                    {
                        if (bf.Length > 2)
                        {
                            string bJson = File.ReadAllText(bf.FullName);
                            var bList = JsonConvert.DeserializeObject<List<PrintRecord>>(bJson);
                            if (bList != null && bList.Count > 0)
                            {
                                try { File.WriteAllText(path, bJson); } catch { }
                                return bList;
                            }
                        }
                    }
                }
            }
            catch { }
            return new List<PrintRecord>();
        }

        private void SavePrintRecord(PrintRecord record)
        {
            try
            {
                _allPrintRecords = LoadPrintHistory();
                _allPrintRecords.Insert(0, record);

                string json = JsonConvert.SerializeObject(_allPrintRecords, Formatting.Indented);
                string mainPath = GetPrintHistoryJsonPath();
                File.WriteAllText(mainPath, json);

                // Create persistent rolling backup in history_backups directory
                try
                {
                    string backupDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "history_backups");
                    if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);
                    File.WriteAllText(System.IO.Path.Combine(backupDir, $"history_backup_{DateTime.Now:yyyyMMdd}.json"), json);
                    File.WriteAllText(System.IO.Path.Combine(backupDir, "history_latest_backup.json"), json);
                }
                catch { }

                Dispatcher.Invoke(() => ApplyHistoryFilters());
            }
            catch { }
        }

        private void RecordCurrentPrint(DateTime printTime, FlowDocument? doc = null)
        {
            try
            {
                PrintRecord record = new PrintRecord
                {
                    Timestamp = printTime
                };

                record.TabType = "Test";
                record.CustomerName = CapitalizeCustomerName(customersTest?.Text ?? "");
                record.Metal = GetSelectedMetal();
                record.ItemType = GetSelectedItemType();
                record.Weight = NumberFormattingHelper.GetCleanDouble(weightInputTextbox);
                record.Taka = NumberFormattingHelper.GetCleanDouble(takaTextbox);
                record.TotalWeight = NumberFormattingHelper.GetCleanDouble(totalWeightTextbox);
                record.PureGold = NumberFormattingHelper.GetCleanDouble(pureGoldTextbox);
                record.Labor = NumberFormattingHelper.GetCleanDouble(laborTextbox);
                record.GoldRate = NumberFormattingHelper.GetCleanDouble(goldRateTextboxTest);
                record.Ratti = NumberFormattingHelper.GetCleanDouble(resultTextbox);
                record.Karat = NumberFormattingHelper.GetCleanDouble(karatInputTextbox);
                record.PurityPercent = NumberFormattingHelper.GetCleanDouble(purityPercentTextbox);

                double totalCash = NumberFormattingHelper.GetCleanDouble(totalPureGoldCashTextbox);
                double grossCash = NumberFormattingHelper.GetCleanDouble(grossPureGoldCashTextbox);
                record.Cash = totalCash > 0 ? totalCash : grossCash;

                record.Notes = notesTextboxTest?.Text?.Trim() ?? "";

                if (doc != null)
                {
                    record.ReceiptXaml = System.Windows.Markup.XamlWriter.Save(doc);
                }

                SavePrintRecord(record);
            }
            catch { }
        }

        private void ApplyHistoryFilters()
        {
            if (historyDataGrid == null) return;

            _isApplyingHistoryFilters = true;
            try
            {
                if (_allPrintRecords == null || _allPrintRecords.Count == 0)
                {
                    _allPrintRecords = LoadPrintHistory();
                }

                string searchCustomer = historySearchCustomer?.Text?.Trim().ToLower() ?? "";
                DateTime? fromDate = historyFromDatePicker?.SelectedDate;
                DateTime? toDate = historyToDatePicker?.SelectedDate;

                var filtered = _allPrintRecords.Where(r =>
                {
                    if (!string.IsNullOrEmpty(searchCustomer))
                    {
                        if (string.IsNullOrEmpty(r.CustomerName) || !r.CustomerName.ToLower().Contains(searchCustomer))
                            return false;
                    }

                    if (fromDate.HasValue && r.Timestamp.Date < fromDate.Value.Date)
                        return false;

                    if (toDate.HasValue && r.Timestamp.Date > toDate.Value.Date)
                        return false;

                    return true;
                });

                var displayList = filtered.Select(r => new PrintRecordDisplay
                {
                    Id = r.Id,
                    Timestamp = r.Timestamp,
                    CustomerName = string.IsNullOrWhiteSpace(r.CustomerName) ? "-" : r.CustomerName,
                    Metal = string.IsNullOrWhiteSpace(r.Metal) ? "Gold" : r.Metal,
                    ItemType = r.ItemType ?? "",
                    WeightValue = r.Weight,
                    TakaValue = r.Taka,
                    RattiValue = r.Ratti,
                    KaratValue = r.Karat,
                    PurityPercentValue = r.PurityPercent,
                    TotalWeightValue = r.TotalWeight,
                    PureGoldValue = r.PureGold,
                    LaborValue = r.Labor,
                    CashValue = r.Cash,
                    GoldRateValue = r.GoldRate,
                    NotesDisplay = !string.IsNullOrWhiteSpace(r.Notes) && r.Notes != "-" ? r.Notes : 
                                   (!string.IsNullOrWhiteSpace(r.Status) && r.Status != "-" ? r.Status : "-")
                }).ToList();

                historyDataGrid.ItemsSource = displayList;
                if (historySelectAllHeaderCheckBox != null)
                {
                    historySelectAllHeaderCheckBox.IsChecked = false;
                }
                UpdateHistorySelectionCount();
            }
            finally
            {
                _isApplyingHistoryFilters = false;
            }
        }

        private void historyFilter_Changed(object sender, RoutedEventArgs e)
        {
            ApplyHistoryFilters();
        }

        private void historyThisMonth_Click(object sender, RoutedEventArgs e)
        {
            DateTime now = DateTime.Now;
            if (historyFromDatePicker != null) historyFromDatePicker.SelectedDate = new DateTime(now.Year, now.Month, 1);
            if (historyToDatePicker != null) historyToDatePicker.SelectedDate = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
            ApplyHistoryFilters();
        }

        private void historyClearFilters_Click(object sender, RoutedEventArgs e)
        {
            if (historySearchCustomer != null) historySearchCustomer.Text = "";
            if (historyFromDatePicker != null) historyFromDatePicker.SelectedDate = null;
            if (historyToDatePicker != null) historyToDatePicker.SelectedDate = null;
            ApplyHistoryFilters();
        }

        private void historySelectAllHeaderCheckBox_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = historySelectAllHeaderCheckBox?.IsChecked == true;
            if (historyDataGrid.ItemsSource is IEnumerable<PrintRecordDisplay> list)
            {
                foreach (var item in list) item.IsSelected = isChecked;
            }
            UpdateHistorySelectionCount();
        }

        private void historyRowCheckBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is PrintRecordDisplay display)
            {
                display.IsSelected = !display.IsSelected;
                UpdateHistorySelectionCount();
                e.Handled = true;
            }
        }

        private void historyRowCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.DataContext is PrintRecordDisplay display)
            {
                display.IsSelected = cb.IsChecked == true;
            }
            UpdateHistorySelectionCount();
        }

        private void UpdateHistorySelectionCount()
        {
            var displayed = (historyDataGrid.ItemsSource as IEnumerable<PrintRecordDisplay>)?.ToList() ?? new List<PrintRecordDisplay>();
            int total = displayed.Count;
            int selected = displayed.Count(d => d.IsSelected);

            if (historySelectAllHeaderCheckBox != null)
            {
                if (total == 0 || selected == 0)
                    historySelectAllHeaderCheckBox.IsChecked = false;
                else if (selected == total)
                    historySelectAllHeaderCheckBox.IsChecked = true;
                else
                    historySelectAllHeaderCheckBox.IsChecked = null; // Indeterminate
            }

            if (historyCountText != null)
            {
                if (selected > 0)
                    historyCountText.Text = $"Records: {total} ({selected} selected)";
                else
                    historyCountText.Text = $"Records: {total}";
            }
        }

        public void PrintReceipt(FlowDocument doc)
        {
            if (doc == null) return;

            PrintDialog printDlg = new PrintDialog();
            IDocumentPaginatorSource idpSource = doc;

            bool? print = printDlg.ShowDialog();
            if (print != null && (print ?? false))
            {
                int copies = Math.Max(1, CurrentSettings.NumberOfCopies);
                for (int c = 0; c < copies; c++)
                {
                    printDlg.PrintDocument(idpSource.DocumentPaginator, $"Printing Receipt ({c + 1}/{copies})");
                }
            }
        }

        public void PrintHistoryRecordById(string recordId)
        {
            if (string.IsNullOrWhiteSpace(recordId)) return;

            var record = _allPrintRecords.FirstOrDefault(r => r.Id == recordId);
            if (record == null)
            {
                _allPrintRecords = LoadPrintHistory();
                record = _allPrintRecords.FirstOrDefault(r => r.Id == recordId);
            }

            if (record != null)
            {
                FlowDocument doc = CreateFlowDocumentFromRecord(record);
                PrintReceipt(doc);
            }
        }

        public void PrintSelectedHistoryRecords()
        {
            var displayed = (historyDataGrid.ItemsSource as IEnumerable<PrintRecordDisplay>)?.ToList() ?? new List<PrintRecordDisplay>();
            var selected = displayed.Where(d => d.IsSelected).ToList();

            // If none checked via CheckBox, check if any row is selected in the DataGrid
            if (selected.Count == 0 && historyDataGrid.SelectedItems.Count > 0)
            {
                foreach (var item in historyDataGrid.SelectedItems)
                {
                    if (item is PrintRecordDisplay d && !selected.Contains(d))
                    {
                        selected.Add(d);
                    }
                }
            }

            if (selected.Count == 0 && historyDataGrid.SelectedItem is PrintRecordDisplay currentItem)
            {
                selected.Add(currentItem);
            }

            if (selected.Count == 0)
            {
                MessageBox.Show("Please select one or more records from the history table to print.\n\nبراہ کرم پرنٹ کرنے کے لیے ایک یا زیادہ ریکارڈ منتخب کریں۔", "Print Receipt", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (selected.Count == 1)
            {
                PrintHistoryRecordById(selected[0].Id);
                return;
            }

            var selectedRecords = new List<PrintRecord>();
            foreach (var display in selected)
            {
                var record = _allPrintRecords.FirstOrDefault(r => r.Id == display.Id);
                if (record == null)
                {
                    _allPrintRecords = LoadPrintHistory();
                    record = _allPrintRecords.FirstOrDefault(r => r.Id == display.Id);
                }
                if (record != null) selectedRecords.Add(record);
            }

            if (selectedRecords.Count == 0) return;

            // When multiple items printed from history, compile and print as a single combined Batch receipt
            var batchItems = new List<BatchItem>();
            int idx = 1;
            foreach (var r in selectedRecords)
            {
                if (r.BatchItems != null && r.BatchItems.Count > 0)
                {
                    foreach (var sub in r.BatchItems)
                    {
                        batchItems.Add(new BatchItem
                        {
                            Index = idx++,
                            Metal = !string.IsNullOrWhiteSpace(sub.Metal) ? sub.Metal : "Gold",
                            ItemType = sub.ItemType ?? "",
                            Weight = sub.Weight,
                            Taka = sub.Taka,
                            TotalWeight = sub.TotalWeight > 0 ? sub.TotalWeight : (sub.Weight - sub.Taka),
                            Ratti = sub.Ratti,
                            Karat = sub.Karat,
                            PurityPercent = sub.PurityPercent,
                            PureGold = sub.PureGold,
                            Labor = sub.Labor,
                            Cash = sub.Cash
                        });
                    }
                }
                else
                {
                    batchItems.Add(new BatchItem
                    {
                        Index = idx++,
                        Metal = !string.IsNullOrWhiteSpace(r.Metal) ? r.Metal : "Gold",
                        ItemType = r.ItemType ?? "",
                        Weight = r.Weight > 0 ? r.Weight : r.TotalWeight,
                        Taka = r.Taka,
                        TotalWeight = r.TotalWeight > 0 ? r.TotalWeight : r.Weight,
                        Ratti = r.Ratti,
                        Karat = r.Karat,
                        PurityPercent = r.PurityPercent,
                        PureGold = r.PureGold,
                        Labor = r.Labor,
                        Cash = r.Cash
                    });
                }
            }

            var distinctCustomers = selectedRecords
                .Select(r => r.CustomerName)
                .Where(c => !string.IsNullOrWhiteSpace(c) && c != "-")
                .Distinct()
                .ToList();

            string batchCustomer = "";
            if (distinctCustomers.Count == 1)
                batchCustomer = distinctCustomers[0];
            else if (distinctCustomers.Count > 1)
                batchCustomer = string.Join(", ", distinctCustomers);

            var confirm = MessageBox.Show(
                $"You have selected {selectedRecords.Count} records ({batchItems.Count} total items).\n\nDo you want to print them as a single combined Batch receipt?\n\nکیا آپ ان {selectedRecords.Count} ریکارڈز کو ایک مشترکہ بیچ رسید کے طور پر پرنٹ کرنا چاہتے ہیں؟",
                "Batch Print / بیچ پرنٹ",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            FlowDocument batchDoc = CreateBatchFlowDocument(
                batchItems,
                batchCustomer,
                goldRate: 0,
                date: DateTime.Now,
                printCustomer: !string.IsNullOrWhiteSpace(batchCustomer),
                printGoldRate: false,
                printNotes: false,
                notes: "");

            PrintReceipt(batchDoc);
        }

        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        private void PromptAndOpenRecord(string recordId)
        {
            if (string.IsNullOrWhiteSpace(recordId)) return;

            var record = _allPrintRecords.FirstOrDefault(r => r.Id == recordId);
            if (record == null)
            {
                _allPrintRecords = LoadPrintHistory();
                record = _allPrintRecords.FirstOrDefault(r => r.Id == recordId);
            }

            if (record == null) return;

            string customer = string.IsNullOrWhiteSpace(record.CustomerName) || record.CustomerName == "-"
                ? "this transaction"
                : record.CustomerName;

            bool isMulti = record.TabType == "Multi Test" || 
                           (record.BatchItems != null && record.BatchItems.Count > 0) || 
                           (record.ItemType != null && record.ItemType.StartsWith("Batch"));
            string tabName = isMulti ? "Multi Test" : "Test";
            string tabNameUrdu = isMulti ? "ملٹی ٹیسٹ" : "ٹیسٹ";

            MessageBoxResult res = MessageBox.Show(
                $"Do you want to open the record for '{customer}' in the {tabName} tab?\n\nThis will load all details into the {tabName} tab so you can edit and reprint.\n\nکیا آپ یہ ریکارڈ {tabNameUrdu} ٹیب میں کھولنا چاہتے ہیں؟",
                "Open Record / ریکارڈ کھولیں",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.Yes);

            if (res == MessageBoxResult.Yes)
            {
                if (isMulti)
                {
                    PopulateMultiTestTabFromRecord(record);
                    if (mainTabControl != null && multiTestTab != null)
                    {
                        mainTabControl.SelectedItem = multiTestTab;
                    }
                }
                else
                {
                    PopulateTestTabFromRecord(record);
                    updateTestCalculations();
                    if (mainTabControl != null && testTab != null)
                    {
                        mainTabControl.SelectedItem = testTab;
                    }
                }
            }
        }

        private void historyRowOpen_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string recordId && !string.IsNullOrWhiteSpace(recordId))
            {
                PromptAndOpenRecord(recordId);
            }
        }

        private void historyRowPrint_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string recordId && !string.IsNullOrWhiteSpace(recordId))
            {
                PrintHistoryRecordById(recordId);
            }
        }

        private void historyPrintSelected_Click(object sender, RoutedEventArgs e)
        {
            PrintSelectedHistoryRecords();
        }

        private void historyDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dep = e.OriginalSource as DependencyObject;
            if (dep == null) return;
            if (FindVisualParent<Button>(dep) != null) return;
            if (FindVisualParent<DataGridColumnHeader>(dep) != null) return;
            if (FindVisualParent<CheckBox>(dep) != null) return;

            if (historyDataGrid.SelectedItem is PrintRecordDisplay display)
            {
                PromptAndOpenRecord(display.Id);
                e.Handled = true;
            }
        }

        private void historyDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                if (historyDataGrid.SelectedItem is PrintRecordDisplay display)
                {
                    PromptAndOpenRecord(display.Id);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Space)
            {
                if (historyDataGrid.SelectedItem is PrintRecordDisplay display)
                {
                    display.IsSelected = !display.IsSelected;
                    UpdateHistorySelectionCount();
                    e.Handled = true;
                }
            }
        }

        private void historyDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateHistorySelectionCount();
        }

        private void PopulateTestTabFromRecord(PrintRecord record)
        {
            if (record == null) return;
            try
            {
                if (customersTest != null)
                {
                    customersTest.Text = record.CustomerName == "-" ? "" : record.CustomerName;
                    if (doPrintCustomerNameTest != null)
                        doPrintCustomerNameTest.IsChecked = !string.IsNullOrWhiteSpace(record.CustomerName) && record.CustomerName != "-";
                }

                // Metal radio buttons
                if (string.Equals(record.Metal, "Silver", StringComparison.OrdinalIgnoreCase))
                {
                    if (radioSilver != null) radioSilver.IsChecked = true;
                    if (radioGold != null) radioGold.IsChecked = false;
                }
                else
                {
                    if (radioGold != null) radioGold.IsChecked = true;
                    if (radioSilver != null) radioSilver.IsChecked = false;
                }

                // Item Type dropdown
                if (itemTypeDropdownTest != null)
                {
                    string target = record.ItemType;
                    if (string.Equals(target, "Necklace", StringComparison.OrdinalIgnoreCase)) target = "Set";
                    bool matched = false;
                    foreach (ComboBoxItem item in itemTypeDropdownTest.Items)
                    {
                        if (string.Equals(item.Tag?.ToString(), target, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(item.Content?.ToString(), target, StringComparison.OrdinalIgnoreCase))
                        {
                            itemTypeDropdownTest.SelectedItem = item;
                            matched = true;
                            break;
                        }
                    }
                    if (!matched) itemTypeDropdownTest.SelectedIndex = 0;
                }

                if (weightInputTextbox != null)
                {
                    double wt = record.Weight > 0 ? record.Weight : record.TotalWeight;
                    weightInputTextbox.Text = wt > 0 ? wt.ToString("0.000") : "0";
                    if (doPrintWeight != null)
                        doPrintWeight.IsChecked = record.Weight > 0;
                }

                if (takaTextbox != null)
                {
                    takaTextbox.Text = record.Taka > 0 ? record.Taka.ToString("0.000") : "0";
                    if (doPrintTaka != null)
                        doPrintTaka.IsChecked = record.Taka > 0;
                }

                if (totalWeightTextbox != null)
                {
                    totalWeightTextbox.Text = record.TotalWeight > 0 ? record.TotalWeight.ToString("0.000") : "0";
                    if (doPrintTotalWeight != null)
                        doPrintTotalWeight.IsChecked = record.TotalWeight > 0;
                }

                if (resultTextbox != null)
                {
                    resultTextbox.Text = record.Ratti > 0 ? record.Ratti.ToString("0.###") : "0";
                    if (doPrintResult != null)
                        doPrintResult.IsChecked = record.Ratti > 0;
                }

                if (record.Ratti > 0)
                {
                    SyncAllPurityFields(record.Ratti, null);
                }

                if (pureGoldTextbox != null)
                {
                    pureGoldTextbox.Text = record.PureGold > 0 ? record.PureGold.ToString("F3") : "0.000";
                    if (doPrintPureGold != null)
                        doPrintPureGold.IsChecked = record.PureGold > 0;
                }

                _isLaborManuallyEdited = true;
                if (laborTextbox != null)
                {
                    laborTextbox.Text = record.Labor > 0 ? record.Labor.ToString("N0") : "0";
                    if (doPrintLabor != null)
                        doPrintLabor.IsChecked = record.Labor > 0;
                }

                if (goldRateTextboxTest != null)
                {
                    goldRateTextboxTest.Text = record.GoldRate > 0 ? record.GoldRate.ToString("N0") : "0";
                    _rawBaseGoldRate = record.GoldRate;
                    if (doPrintGoldRateTest != null)
                        doPrintGoldRateTest.IsChecked = record.GoldRate > 0;
                }

                if (goldRateDiffTextbox != null)
                {
                    goldRateDiffTextbox.Text = "0";
                }

                if (karatInputTextbox != null)
                {
                    karatInputTextbox.Text = record.Karat > 0 ? record.Karat.ToString("0.##") : "0";
                    if (doPrintKarat != null)
                        doPrintKarat.IsChecked = record.Karat > 0;
                }

                if (purityPercentTextbox != null)
                {
                    purityPercentTextbox.Text = record.PurityPercent > 0 ? record.PurityPercent.ToString("0.##") : "0";
                    if (doPrintPurityPercent != null)
                        doPrintPurityPercent.IsChecked = record.PurityPercent > 0;
                }

                if (notesTextboxTest != null)
                {
                    notesTextboxTest.Text = "";
                }
                if (doPrintNotes != null)
                {
                    doPrintNotes.IsChecked = false;
                }

                if (totalPureGoldCashTextbox != null)
                {
                    totalPureGoldCashTextbox.Text = record.Cash > 0 ? record.Cash.ToString("N0") : "0";
                    if (doPrintTotalPureGoldCash != null)
                        doPrintTotalPureGoldCash.IsChecked = record.Cash > 0;
                }
            }
            catch { }
        }

        private void PopulateMultiTestTabFromRecord(PrintRecord record)
        {
            if (record == null) return;
            try
            {
                BatchItems.Clear();

                if (record.BatchItems != null && record.BatchItems.Count > 0)
                {
                    for (int i = 0; i < record.BatchItems.Count; i++)
                    {
                        var it = record.BatchItems[i];
                        it.Index = i + 1;
                        BatchItems.Add(it);
                    }
                }
                else if (record.TotalWeight > 0 || record.Cash > 0)
                {
                    // Fallback for legacy records saved without individual items
                    BatchItems.Add(new BatchItem
                    {
                        Index = 1,
                        Metal = record.Metal ?? "Gold",
                        ItemType = record.ItemType ?? "Item",
                        Weight = record.Weight > 0 ? record.Weight : record.TotalWeight,
                        Taka = record.Taka,
                        TotalWeight = record.TotalWeight,
                        Ratti = record.Ratti,
                        Karat = record.Karat,
                        PurityPercent = record.PurityPercent,
                        PureGold = record.PureGold,
                        Labor = record.Labor,
                        Cash = record.Cash
                    });
                }

                if (customersMulti != null)
                {
                    customersMulti.Text = record.CustomerName == "-" ? "" : (record.CustomerName ?? "");
                    if (doPrintCustomerNameMulti != null)
                        doPrintCustomerNameMulti.IsChecked = !string.IsNullOrWhiteSpace(customersMulti.Text);
                }

                if (goldRateTextboxMulti != null)
                {
                    goldRateTextboxMulti.Text = record.GoldRate > 0 ? record.GoldRate.ToString("N0") : "0";
                    _rawBaseGoldRateMulti = record.GoldRate;
                    if (doPrintGoldRateMulti != null)
                        doPrintGoldRateMulti.IsChecked = record.GoldRate > 0;
                }

                if (notesTextboxMulti != null)
                {
                    notesTextboxMulti.Text = "";
                }
                if (doPrintNotesMulti != null)
                {
                    doPrintNotesMulti.IsChecked = false;
                }

                // Reset individual item input boxes on Multi Test tab
                if (weightInputTextboxMulti != null) weightInputTextboxMulti.Text = "0";
                if (takaTextboxMulti != null) takaTextboxMulti.Text = "0";
                if (totalWeightTextboxMulti != null) totalWeightTextboxMulti.Text = "0";
                if (resultTextboxMulti != null) resultTextboxMulti.Text = "0";
                if (pureGoldTextboxMulti != null) pureGoldTextboxMulti.Text = "0";
                if (laborTextboxMulti != null) laborTextboxMulti.Text = "0";
                if (grossPureGoldCashTextboxMulti != null) grossPureGoldCashTextboxMulti.Text = "0";
                if (totalPureGoldCashTextboxMulti != null) totalPureGoldCashTextboxMulti.Text = "0";
                if (karatInputTextboxMulti != null) karatInputTextboxMulti.Text = "0";
                if (purityPercentTextboxMulti != null) purityPercentTextboxMulti.Text = "0";

                updateBatchTotals();
                batchDataGrid?.Items.Refresh();
            }
            catch { }
        }

        public FlowDocument CreateFlowDocumentFromRecord(PrintRecord record)
        {
            if (record == null) return new FlowDocument();

            if (record.BatchItems != null && record.BatchItems.Count > 0)
            {
                return CreateBatchFlowDocument(
                    record.BatchItems,
                    record.CustomerName,
                    goldRate: record.GoldRate,
                    date: record.Timestamp,
                    printCustomer: !string.IsNullOrWhiteSpace(record.CustomerName) && record.CustomerName != "-",
                    printGoldRate: record.GoldRate > 0,
                    printNotes: false,
                    notes: "");
            }

            if (!string.IsNullOrWhiteSpace(record.ReceiptXaml))
            {
                try
                {
                    var parsed = (FlowDocument)System.Windows.Markup.XamlReader.Parse(record.ReceiptXaml);
                    if (parsed != null)
                    {
                        StripNotesFromFlowDocument(parsed);
                        return parsed;
                    }
                }
                catch { }
            }

            FlowDocument flowDoc = new FlowDocument();
            flowDoc.PageWidth = IsSmallSlipSelected ? 190 : 288;
            flowDoc.PagePadding = IsSmallSlipSelected ? new Thickness(4) : new Thickness(12);
            flowDoc.FontFamily = new FontFamily("Cambria");
            int baseFontSize = (IsSmallSlipSelected ? 11 : 16) + CurrentSettings.FontSizeDelta;
            flowDoc.FontSize = Math.Max(8, baseFontSize);
            flowDoc.FontWeight = CurrentSettings.IsBoldEnabled ? FontWeights.Bold : FontWeights.Normal;
            flowDoc.Background = Brushes.White;

            // Header Section
            Paragraph headerParagraph = new Paragraph(new Run("INSTANT DEALING RECEIPT"))
            {
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold,
                FontSize = 20,
                Margin = new Thickness(0, 0, 0, 8),
                Foreground = Brushes.Black
            };
            flowDoc.Blocks.Add(headerParagraph);

            // Date and Time (from original record)
            Paragraph dateParagraph = new Paragraph(new Run(record.Timestamp.ToString("dddd, MMMM dd, yyyy - HH:mm")))
            {
                TextAlignment = TextAlignment.Center,
                FontStyle = FontStyles.Italic,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = Brushes.Black
            };
            flowDoc.Blocks.Add(dateParagraph);

            // Customer
            if (!string.IsNullOrWhiteSpace(record.CustomerName) && record.CustomerName != "-")
            {
                Paragraph customerParagraph = new Paragraph();
                customerParagraph.Inlines.Add(new Run("Customer: ") { FontWeight = FontWeights.Bold });
                customerParagraph.Inlines.Add(new Bold(new Run(record.CustomerName)) { Foreground = Brushes.Black });
                customerParagraph.TextAlignment = TextAlignment.Left;
                customerParagraph.FontSize = 16;
                customerParagraph.Margin = new Thickness(0, 0, 0, 8);
                flowDoc.Blocks.Add(customerParagraph);
            }

            bool hasTransactionData = false;

            // Test Transaction Table
            bool hasTestFields = record.TotalWeight > 0 || record.Ratti > 0 || record.Karat > 0 ||
                                 record.PurityPercent > 0 || record.PureGold > 0 || record.Labor > 0 || record.Cash > 0;

            if (hasTestFields)
            {
                if (hasTransactionData) flowDoc.Blocks.Add(CreateSeparator());
                hasTransactionData = true;

                string metal = !string.IsNullOrWhiteSpace(record.Metal) ? record.Metal : "Gold";
                string itemType = record.ItemType ?? "";
                string itemDesc = !string.IsNullOrEmpty(itemType) ? $"{metal} {itemType}" : metal;

                Paragraph testHeader = new Paragraph();
                string headerTitle = !string.IsNullOrEmpty(itemDesc) 
                    ? $"TEST TRANSACTION  •  {itemDesc.ToUpper()}" 
                    : "TEST TRANSACTION";
                testHeader.Inlines.Add(new Underline(new Bold(new Run(headerTitle))));
                testHeader.TextAlignment = TextAlignment.Center;
                testHeader.FontSize = 14;
                testHeader.Margin = new Thickness(0, 8, 0, 5);
                testHeader.Foreground = Brushes.Black;
                flowDoc.Blocks.Add(testHeader);

                Table testTable = new Table() { CellSpacing = 2 };
                testTable.Columns.Add(new TableColumn() { Width = new GridLength(140) });
                testTable.Columns.Add(new TableColumn() { Width = new GridLength(100) });

                TableRowGroup testRowGroup = new TableRowGroup();

                if (record.Weight > 0)
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Weight:")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"{record.Weight:0.000}g")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
                    testRowGroup.Rows.Add(row);
                }

                if (record.Taka > 0)
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Taka:")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"{record.Taka:0.000}g")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
                    testRowGroup.Rows.Add(row);
                }

                if (record.TotalWeight > 0)
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Total Weight:")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"{record.TotalWeight:0.000}g")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
                    testRowGroup.Rows.Add(row);
                }

                if (record.Ratti > 0)
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Result (Ratti):")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"{record.Ratti:0.###}")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
                    testRowGroup.Rows.Add(row);
                }

                if (record.Karat > 0)
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Karat:")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"{record.Karat:0.##}K")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
                    testRowGroup.Rows.Add(row);
                }

                if (record.PurityPercent > 0)
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Purity:")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"{record.PurityPercent:0.##}%")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
                    testRowGroup.Rows.Add(row);
                }

                if (record.PureGold > 0)
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Pure Gold:")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"{record.PureGold:0.000}g")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
                    testRowGroup.Rows.Add(row);
                }

                if (record.Labor > 0)
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Labor Charges:")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"Rs. {record.Labor:N0}")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
                    testRowGroup.Rows.Add(row);
                }

                if (record.Cash > 0)
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Cash:")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"Rs. {record.Cash:N0}")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
                    testRowGroup.Rows.Add(row);
                }

                testTable.RowGroups.Add(testRowGroup);
                flowDoc.Blocks.Add(testTable);
            }

            // Gold Rate Section
            if (record.GoldRate > 0)
            {
                if (hasTransactionData) flowDoc.Blocks.Add(CreateSeparator());
                Paragraph goldRateParagraph = new Paragraph();
                goldRateParagraph.Inlines.Add(new Run($"Gold Rate: {record.GoldRate:N0}") { FontWeight = FontWeights.Bold, FontSize = 13 });
                goldRateParagraph.TextAlignment = TextAlignment.Center;
                goldRateParagraph.Margin = new Thickness(0, 6, 0, 6);
                flowDoc.Blocks.Add(goldRateParagraph);
            }

            // NOTE: Notes are not printed when reprinting from history per requirement
            
            // Footer
            flowDoc.Blocks.Add(CreateSeparator());
            Paragraph footer = new Paragraph(new Run("Thank You for Your Business!"))
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 11,
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(0, 5, 0, 0)
            };
            flowDoc.Blocks.Add(footer);

            return flowDoc;
        }

        private static void StripNotesFromFlowDocument(FlowDocument doc)
        {
            if (doc == null) return;
            var blocksToRemove = new List<Block>();
            bool inNotesSection = false;
            Block? lastSeparatorBeforeNotes = null;

            foreach (var block in doc.Blocks)
            {
                string text = new TextRange(block.ContentStart, block.ContentEnd).Text;
                if (!inNotesSection)
                {
                    if (text.Contains("═") || text.Contains("- - -") || text.Contains("---"))
                    {
                        lastSeparatorBeforeNotes = block;
                    }
                    else if (text.Contains("NOTES") || text.Contains("نوٹ"))
                    {
                        inNotesSection = true;
                        if (lastSeparatorBeforeNotes != null)
                        {
                            blocksToRemove.Add(lastSeparatorBeforeNotes);
                        }
                        blocksToRemove.Add(block);
                    }
                }
                else
                {
                    if (text.Contains("Thank You") || text.Contains("═") || text.Contains("- - -") || text.Contains("---"))
                    {
                        break;
                    }
                    blocksToRemove.Add(block);
                }
            }

            foreach (var b in blocksToRemove)
            {
                doc.Blocks.Remove(b);
            }
        }

        #endregion

        private void labTextbox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsValidInput(e.Text, (TextBox)sender);
        }
        public static class NumberFormattingHelper
        {
            public static void FormatTextBoxNumber(TextBox textBox)
            {
                if (textBox == null) return;

                int cursorPosition = textBox.SelectionStart;
                string originalText = textBox.Text;

                // Remove non-digit characters except for standard text cleanup
                string cleanText = originalText.Replace(",", "").Replace(" ", "");

                if (decimal.TryParse(cleanText, out decimal value))
                {
                    // Format with American comma style
                    string formattedText = value.ToString("#,##0");

                    // Only update if text actually changed to avoid infinite loop
                    if (formattedText != originalText)
                    {
                        textBox.Text = formattedText;

                        // Try to maintain cursor position
                        int newCursorPosition = Math.Min(cursorPosition + (formattedText.Length - originalText.Length), formattedText.Length);
                        textBox.SelectionStart = Math.Max(0, newCursorPosition);
                    }
                }
                else if (string.IsNullOrEmpty(cleanText))
                {
                    // Handle empty text
                    textBox.Text = "0";
                }
            }

            public static decimal GetCleanValue(TextBox textBox)
            {
                if (textBox == null) return 0;

                string cleanText = textBox.Text.Replace(",", "").Replace(" ", "");
                return decimal.TryParse(cleanText, out decimal value) ? value : 0;
            }

            public static double GetCleanDouble(TextBox textBox)
            {
                if (textBox == null || string.IsNullOrWhiteSpace(textBox.Text)) return 0;
                string cleanText = textBox.Text.Replace(",", "").Replace(" ", "");
                return double.TryParse(cleanText, out double value) ? value : 0;
            }
        }

        private void SetBaseGoldRate(double baseRate)
        {
            _rawBaseGoldRate = baseRate;
            ApplyGoldRateDifference();
        }

        private void ApplyGoldRateDifference()
        {
            if (_isUpdatingGoldRate) return;
            _isUpdatingGoldRate = true;
            try
            {
                double diff = 0;
                if (goldRateDiffTextbox != null && !string.IsNullOrWhiteSpace(goldRateDiffTextbox.Text))
                {
                    double.TryParse(goldRateDiffTextbox.Text.Replace(",", "").Replace(" ", ""), out diff);
                }

                if (_rawBaseGoldRate <= 0 && goldRateTextboxTest != null)
                {
                    double currentRate = (double)NumberFormattingHelper.GetCleanValue(goldRateTextboxTest);
                    if (currentRate > 0)
                    {
                        _rawBaseGoldRate = currentRate + diff;
                    }
                }

                if (_rawBaseGoldRate > 0 && goldRateTextboxTest != null)
                {
                    double finalRate = Math.Max(0, _rawBaseGoldRate - diff);
                    goldRateTextboxTest.Text = finalRate.ToString("N0");
                }
            }
            finally
            {
                _isUpdatingGoldRate = false;
            }
            updateTestCalculations();
        }

        private void goldRateDiffTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (goldRateDiffTextbox != null && goldRateDiffTextbox.IsFocused && doPrintGoldRateTest != null)
            {
                doPrintGoldRateTest.IsChecked = true;
            }
            ApplyGoldRateDifference();
        }

        private void notesTextboxTest_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (notesTextboxTest == null) return;

            string text = notesTextboxTest.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                if (notesSumTextTest != null) notesSumTextTest.Text = "Sum: 0.00";
                if (notesAvgTextTest != null) notesAvgTextTest.Text = "Avg: 0.00";
                return;
            }

            ExtractAndApplyGoldRateFromNotes(text);

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            double sum = 0;
            int count = 0;

            foreach (var rawLine in lines)
            {
                string cleaned = rawLine.Trim().Replace(",", "");
                if (double.TryParse(cleaned, out double val))
                {
                    sum += val;
                    count++;
                }
                else
                {
                    var match = Regex.Match(cleaned, @"-?\d+(\.\d+)?");
                    if (match.Success && double.TryParse(match.Value, out double parsedVal))
                    {
                        sum += parsedVal;
                        count++;
                    }
                }
            }

            double avg = count > 0 ? (sum / count) : 0;
            if (notesSumTextTest != null) notesSumTextTest.Text = $"Sum: {sum:N2}";
            if (notesAvgTextTest != null) notesAvgTextTest.Text = $"Avg: {avg:N2}";
        }

        private void ExtractAndApplyGoldRateFromNotes(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            double extractedRate = 0;

            // Pattern 1: Match paired b and s rates like "b481,700/s82,700" or "b481700 s82700" or "b499,500/s00,200"
            var pairedMatch = Regex.Match(text, @"(?i)\bb\s*([0-9]{1,3}(?:,[0-9]{3})+|[0-9]{4,6})\s*/?\s*s\s*([0-9]{1,3}(?:,[0-9]{3})+|[0-9]{1,6})");

            if (pairedMatch.Success)
            {
                string bStr = pairedMatch.Groups[1].Value.Replace(",", "").Replace(" ", "");
                string sStr = pairedMatch.Groups[2].Value.Replace(",", "").Replace(" ", "");

                if (double.TryParse(bStr, out double bVal) && double.TryParse(sStr, out double sVal))
                {
                    if (sVal >= 100000)
                    {
                        // Full 6-digit s rate provided
                        extractedRate = sVal;
                    }
                    else
                    {
                        // Extract lakh base from bVal (e.g. 481,700 -> 400,000)
                        double lakhBase = Math.Floor(bVal / 100000.0) * 100000.0;
                        double candidate = lakhBase + sVal;

                        // Boundary check: if candidate < bVal, it crossed into the next lakh (e.g. 499,500 / 00,200 -> 500,200)
                        if (candidate < bVal)
                        {
                            candidate += 100000.0;
                        }

                        extractedRate = candidate;
                    }
                }
            }

            // Fallback Pattern 2: Single s rate like "s482,700"
            if (extractedRate <= 0)
            {
                var sMatch = Regex.Match(text, @"(?i)\bs\s*([0-9]{1,3}(?:,[0-9]{3})+|[0-9]{4,6})");
                if (sMatch.Success)
                {
                    string sStr = sMatch.Groups[1].Value.Replace(",", "").Replace(" ", "");
                    if (double.TryParse(sStr, out double sVal) && sVal > 10000)
                    {
                        extractedRate = sVal;
                    }
                }
            }

            // Fallback Pattern 3: Single b rate like "b481,700"
            if (extractedRate <= 0)
            {
                var bMatch = Regex.Match(text, @"(?i)\bb\s*([0-9]{1,3}(?:,[0-9]{3})+|[0-9]{4,6})");
                if (bMatch.Success)
                {
                    string bStr = bMatch.Groups[1].Value.Replace(",", "").Replace(" ", "");
                    if (double.TryParse(bStr, out double bVal) && bVal > 10000)
                    {
                        extractedRate = bVal;
                    }
                }
            }

            if (extractedRate > 10000)
            {
                SetBaseGoldRate(extractedRate);
            }
        }

        private bool _isFormattingZeroDecimal = false;

        private void FormatAsZeroDecimal(TextBox textBox)
        {
            if (textBox == null || _isFormattingZeroDecimal) return;

            string originalText = textBox.Text;
            if (string.IsNullOrWhiteSpace(originalText)) return;

            // Extract all raw digits from the text
            string digits = Regex.Replace(originalText, @"[^\d]", "");

            // Trim leading zeros from digits
            digits = digits.TrimStart('0');

            if (string.IsNullOrEmpty(digits))
            {
                if (originalText != "0")
                {
                    _isFormattingZeroDecimal = true;
                    try
                    {
                        textBox.Text = "0";
                        textBox.SelectionStart = textBox.Text.Length;
                    }
                    finally { _isFormattingZeroDecimal = false; }
                }
                return;
            }

            string formatted = "0." + digits;

            if (formatted != originalText)
            {
                _isFormattingZeroDecimal = true;
                try
                {
                    textBox.Text = formatted;
                    textBox.SelectionStart = textBox.Text.Length;
                }
                finally
                {
                    _isFormattingZeroDecimal = false;
                }
            }
        }

        private bool _isSyncingPurity = false;

        private void SyncAllPurityFields(double rattiValue, object? sourceControl)
        {
            if (_isSyncingPurity) return;
            _isSyncingPurity = true;
            try
            {
                if (rattiValue < 0) rattiValue = 0;
                if (rattiValue > 96.0) rattiValue = 96.0;

                string rattiStr = rattiValue.ToString("F2").TrimEnd('0').TrimEnd('.');
                if (string.IsNullOrEmpty(rattiStr)) rattiStr = "0";

                // Karat Formula: K = 24 - (Ratti / 4)
                double karatVal = Math.Max(0, 24.0 - (rattiValue / 4.0));
                string karatStr = karatVal.ToString("F2").TrimEnd('0').TrimEnd('.');
                if (string.IsNullOrEmpty(karatStr)) karatStr = "0";

                // Purity % Formula: Purity% = ((96 - Ratti) / 96) * 100
                double purityVal = Math.Max(0, ((96.0 - rattiValue) / 96.0) * 100.0);
                string purityStr = purityVal.ToString("F2").TrimEnd('0').TrimEnd('.');
                if (string.IsNullOrEmpty(purityStr)) purityStr = "0";

                // 1. Update left side 4th row resultTextbox (Result Ratti)
                if (resultTextbox != null && sourceControl != resultTextbox)
                {
                    resultTextbox.Text = rattiStr;
                }

                // 2. Update Karat input field
                if (karatInputTextbox != null && sourceControl != karatInputTextbox)
                {
                    karatInputTextbox.Text = karatStr;
                }

                // 3. Update Ratti conversion field (Row 2)
                if (rattiConversionTextbox != null && sourceControl != rattiConversionTextbox)
                {
                    rattiConversionTextbox.Text = rattiStr;
                }

                // 4. Update Purity % input field (Row 4)
                if (purityPercentTextbox != null && sourceControl != purityPercentTextbox)
                {
                    purityPercentTextbox.Text = purityStr;
                }

                // 5. Update Purity Ratti result field (Row 4)
                if (purityRattiResultTextbox != null && sourceControl != purityRattiResultTextbox)
                {
                    purityRattiResultTextbox.Text = rattiStr;
                }
            }
            catch { }
            finally
            {
                _isSyncingPurity = false;
            }
        }

        private void karatInputTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingPurity) return;
            if (karatInputTextbox != null && karatInputTextbox.IsFocused)
            {
                if (doPrintKarat != null) doPrintKarat.IsChecked = true;
                double karat = NumberFormattingHelper.GetCleanDouble(karatInputTextbox);
                if (karat > 24.0) karat = 24.0;
                if (karat < 0) karat = 0;

                // Ratti = 96 - (Karat * 4)
                double ratti = Math.Max(0, 96.0 - (karat * 4.0));
                SyncAllPurityFields(ratti, karatInputTextbox);
            }
        }

        private void rattiConversionTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingPurity) return;
            if (rattiConversionTextbox != null && rattiConversionTextbox.IsFocused)
            {
                if (doPrintResult != null) doPrintResult.IsChecked = true;
                double ratti = NumberFormattingHelper.GetCleanDouble(rattiConversionTextbox);
                SyncAllPurityFields(ratti, rattiConversionTextbox);
            }
        }

        private void purityPercentTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingPurity) return;
            if (purityPercentTextbox != null && purityPercentTextbox.IsFocused)
            {
                if (doPrintPurityPercent != null) doPrintPurityPercent.IsChecked = true;
                double purityPct = NumberFormattingHelper.GetCleanDouble(purityPercentTextbox);
                if (purityPct > 100.0) purityPct = 100.0;
                if (purityPct < 0) purityPct = 0;

                // Ratti = 96 - (Purity% * 0.96)
                double ratti = Math.Max(0, 96.0 - (purityPct * 0.96));
                SyncAllPurityFields(ratti, purityPercentTextbox);
            }
        }

        private void purityRattiResultTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingPurity) return;
            if (purityRattiResultTextbox != null && purityRattiResultTextbox.IsFocused)
            {
                if (doPrintResult != null) doPrintResult.IsChecked = true;
                double ratti = NumberFormattingHelper.GetCleanDouble(purityRattiResultTextbox);
                SyncAllPurityFields(ratti, purityRattiResultTextbox);
            }
        }

        private void sampleResultWeight_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isCalculatingRatti) return;

            var tb = sender as TextBox;
            if (tb != null)
            {
                FormatAsZeroDecimal(tb);
            }

            _isCalculatingRatti = true;
            try
            {
                double sample = NumberFormattingHelper.GetCleanDouble(sampleWeightTextbox);
                double resWeight = NumberFormattingHelper.GetCleanDouble(resultWeightTextbox);

                if (sample > 0)
                {
                    double ratti = ((sample - resWeight) / sample) * 96.0;
                    if (ratti < 0) ratti = 0;
                    SyncAllPurityFields(ratti, sampleWeightTextbox);
                    if (doPrintResult != null) doPrintResult.IsChecked = true;
                }
            }
            catch { }
            finally
            {
                _isCalculatingRatti = false;
            }
            updateTestCalculations();
        }

        private void goldRateTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (goldRateTextboxTest != null && goldRateTextboxTest.IsFocused && !_isUpdatingGoldRate)
            {
                if (doPrintGoldRateTest != null) doPrintGoldRateTest.IsChecked = true;
                double entered = NumberFormattingHelper.GetCleanDouble(goldRateTextboxTest);
                double diff = (goldRateDiffTextbox != null && !string.IsNullOrWhiteSpace(goldRateDiffTextbox.Text)) 
                    ? NumberFormattingHelper.GetCleanDouble(goldRateDiffTextbox) : 0;
                _rawBaseGoldRate = entered + diff;
            }
            updateTestCalculations();
        }

        private void metalRadio_Checked(object sender, RoutedEventArgs e)
        {
            // Metal radio button selected
        }

        private void itemTypeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Auto update if needed
        }

        private void slipSizeRadio_Checked(object sender, RoutedEventArgs e)
        {
            bool isSmall = (sender == radioSlipSmall || sender == radioSlipSmallMulti);
            SetSlipSize(isSmall);
            SavePrintSettings();
        }

        public bool IsSmallSlipSelected => radioSlipSmall?.IsChecked == true || radioSlipSmallMulti?.IsChecked == true;

        public void SetSlipSize(bool isSmall)
        {
            if (radioSlipSmall != null) radioSlipSmall.IsChecked = isSmall;
            if (radioSlipLarge != null) radioSlipLarge.IsChecked = !isSmall;
            if (radioSlipSmallMulti != null) radioSlipSmallMulti.IsChecked = isSmall;
            if (radioSlipLargeMulti != null) radioSlipLargeMulti.IsChecked = !isSmall;
        }

        private string GetSelectedMetal()
        {
            if (radioSilver != null && radioSilver.IsChecked == true) return "Silver";
            return "Gold";
        }

        private string GetSelectedItemType()
        {
            if (itemTypeDropdownTest?.SelectedItem is ComboBoxItem cbi)
            {
                string tag = cbi.Tag?.ToString() ?? "";
                if (!string.IsNullOrEmpty(tag)) return tag;
                string text = cbi.Content?.ToString() ?? "";
                if (text.Contains("None") || text.Contains("کوئی نہیں")) return "";
                return text;
            }
            return "";
        }

        private string GetSelectedMultiItemType()
        {
            if (itemTypeDropdownMulti?.SelectedItem is ComboBoxItem cbi)
            {
                string tag = cbi.Tag?.ToString() ?? "";
                if (!string.IsNullOrEmpty(tag)) return tag;
                string text = cbi.Content?.ToString() ?? "";
                if (text.Contains("None") || text.Contains("کوئی نہیں")) return "";
                return text;
            }
            return "";
        }

        private string GetItemDescription()
        {
            string metal = GetSelectedMetal();
            string itemType = GetSelectedItemType();
            if (!string.IsNullOrEmpty(itemType))
            {
                return $"{metal} {itemType}";
            }
            return metal;
        }

        private void weightTaka_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb && tb.IsFocused)
            {
                if (tb == weightInputTextbox && doPrintWeight != null)
                    doPrintWeight.IsChecked = true;
                else if (tb == takaTextbox && doPrintTaka != null)
                    doPrintTaka.IsChecked = true;
            }
            updateTestCalculations();
        }

        private void testTabInputs_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null && tb.IsFocused)
            {
                if (tb == totalWeightTextbox && doPrintTotalWeight != null)
                    doPrintTotalWeight.IsChecked = true;
                else if (tb == resultTextbox && doPrintResult != null)
                    doPrintResult.IsChecked = true;
            }
            updateTestCalculations();
        }

        private void testRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            updateTestCalculations();
        }

        private bool _isLaborManuallyEdited = false;

        private void laborTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (laborTextbox != null && laborTextbox.IsFocused)
            {
                _isLaborManuallyEdited = true;
                if (doPrintLabor != null) doPrintLabor.IsChecked = true;
            }
            updateTestCalculations();
        }

        private void laborTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (laborTextbox != null)
            {
                if (string.IsNullOrWhiteSpace(laborTextbox.Text))
                {
                    laborTextbox.Text = "0";
                }
                else if (double.TryParse(laborTextbox.Text.Replace(",", "").Replace(" ", ""), out double val))
                {
                    laborTextbox.Text = val.ToString("N0");
                }
            }
            updateTestCalculations();
        }

        private void updateTestCalculations()
        {
            if (pureGoldTextbox == null || laborTextbox == null ||
                grossPureGoldCashTextbox == null || totalPureGoldCashTextbox == null) return;

            try
            {
                double goldRate = 0;
                double totalWeight = 0;
                double resultRatti = 0;

                string rateText = !string.IsNullOrWhiteSpace(goldRateTextboxTest?.Text) ? goldRateTextboxTest.Text : "0";
                double.TryParse(rateText.Replace(",", "").Replace(" ", ""), out goldRate);

                double grossWeight = 0;
                double taka = 0;
                if (weightInputTextbox != null)
                    grossWeight = NumberFormattingHelper.GetCleanDouble(weightInputTextbox);
                if (takaTextbox != null)
                    taka = NumberFormattingHelper.GetCleanDouble(takaTextbox);

                if (grossWeight > 0 || taka > 0)
                {
                    totalWeight = Math.Max(0, grossWeight - taka);
                    if (totalWeightTextbox != null)
                        totalWeightTextbox.Text = totalWeight.ToString("0.000");
                }
                else
                {
                    if (totalWeightTextbox != null)
                        double.TryParse(totalWeightTextbox.Text.Replace(",", "").Replace(" ", ""), out totalWeight);
                }

                if (resultTextbox != null)
                {
                    double.TryParse(resultTextbox.Text.Replace(",", "").Replace(" ", ""), out resultRatti);
                    if (resultTextbox.IsFocused)
                    {
                        SyncAllPurityFields(resultRatti, resultTextbox);
                    }
                }

                // Bi-directional calculation: Update Result Weight field ONLY if user is NOT actively typing in resultWeightTextbox
                if (!_isCalculatingRatti && sampleWeightTextbox != null && resultWeightTextbox != null && !resultWeightTextbox.IsFocused)
                {
                    double sample = NumberFormattingHelper.GetCleanDouble(sampleWeightTextbox);
                    if (sample > 0)
                    {
                        double resWeight = sample * ((96.0 - resultRatti) / 96.0);
                        if (resWeight < 0) resWeight = 0;
                        _isCalculatingRatti = true;
                        try
                        {
                            string formattedRes = resWeight.ToString("F4").TrimEnd('0').TrimEnd('.');
                            if (string.IsNullOrEmpty(formattedRes)) formattedRes = "0";
                            if (formattedRes.StartsWith(".")) formattedRes = "0" + formattedRes;
                            else if (!formattedRes.StartsWith("0.") && resWeight > 0 && resWeight < 1) formattedRes = "0." + formattedRes;

                            resultWeightTextbox.Text = formattedRes;
                        }
                        finally
                        {
                            _isCalculatingRatti = false;
                        }
                    }
                }

                // 1. Pure Gold Calculation (Grams):
                double pureRatti = Math.Max(0, 96.0 - resultRatti);
                double pureGold = 0;
                if (totalWeight > 0)
                {
                    pureGold = totalWeight * (pureRatti / 96.0);
                    // Floor at 2 decimal places so the 3rd decimal digit is 0 (e.g. 3.122 or 3.129 -> 3.120)
                    pureGold = Math.Floor(pureGold * 100.0) / 100.0;
                }
                pureGoldTextbox.Text = pureGold.ToString("F3");

                // 2. Labor Charges Calculation
                // If user manually edited labor, use that custom value (including 0).
                // Otherwise calculate default:
                // If total weight <= 11.5g -> Fixed 350 PKR
                // If total weight > 11.5g -> 30 PKR per gram (total weight * 30)
                double laborCharges = 0;
                if (_isLaborManuallyEdited || (laborTextbox != null && laborTextbox.IsFocused))
                {
                    if (laborTextbox != null && !string.IsNullOrWhiteSpace(laborTextbox.Text))
                    {
                        double.TryParse(laborTextbox.Text.Replace(",", "").Replace(" ", ""), out laborCharges);
                    }
                }
                else
                {
                    if (totalWeight > 0)
                    {
                        if (totalWeight <= 11.5)
                            laborCharges = 350;
                        else
                            laborCharges = totalWeight * 30;
                    }
                    if (laborTextbox != null)
                    {
                        laborTextbox.Text = laborCharges.ToString("N0");
                    }
                }

                if (totalWeight <= 0 && (laborTextbox == null || !laborTextbox.IsFocused))
                {
                    _isLaborManuallyEdited = false;
                }

                // 3. Rate per Gram (1 Tola = 11.664 grams)
                double ratePerGram = goldRate > 0 ? (goldRate / 11.664) : 0;

                // 4. Box 1: Cash Without Labor (Gross Cash)
                double grossPureGoldCash = pureGold * ratePerGram;
                grossPureGoldCashTextbox.Text = grossPureGoldCash.ToString("N0");

                // 5. Box 2: Cash With Labor (Total Pure Gold Cash after subtracting labor charges)
                double totalPureGoldCash = grossPureGoldCash > 0 ? Math.Max(0, grossPureGoldCash - laborCharges) : 0;
                totalPureGoldCashTextbox.Text = totalPureGoldCash.ToString("N0");
            }
            catch
            {
                pureGoldTextbox.Text = "0";
                laborTextbox.Text = "0";
                grossPureGoldCashTextbox.Text = "0";
                totalPureGoldCashTextbox.Text = "0";
            }
        }

        #region Multi Test Calculations & Batch Grid

        private void metalRadioMulti_Checked(object sender, RoutedEventArgs e)
        {
            updateMultiTestCalculations();
        }

        private void goldRateTextboxMulti_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.Text = NumberFormattingHelper.GetCleanValue(textBox).ToString("N0");
            }
        }

        private void goldRateTextboxMulti_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (goldRateTextboxMulti != null && goldRateTextboxMulti.IsFocused && !_isUpdatingGoldRateMulti)
            {
                double entered = NumberFormattingHelper.GetCleanDouble(goldRateTextboxMulti);
                double diff = (goldRateDiffTextboxMulti != null && !string.IsNullOrWhiteSpace(goldRateDiffTextboxMulti.Text))
                    ? NumberFormattingHelper.GetCleanDouble(goldRateDiffTextboxMulti) : 0;
                _rawBaseGoldRateMulti = entered + diff;
            }
            updateMultiTestCalculations();
        }

        private void goldRateDiffTextboxMulti_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (goldRateDiffTextboxMulti == null || !goldRateDiffTextboxMulti.IsFocused) return;
            ApplyGoldRateDifferenceMulti();
        }

        private void ApplyGoldRateDifferenceMulti()
        {
            if (goldRateTextboxMulti == null || goldRateDiffTextboxMulti == null) return;
            if (_isUpdatingGoldRateMulti) return;

            double diff = NumberFormattingHelper.GetCleanDouble(goldRateDiffTextboxMulti);
            if (_rawBaseGoldRateMulti <= 0)
            {
                _rawBaseGoldRateMulti = NumberFormattingHelper.GetCleanDouble(goldRateTextboxMulti);
            }

            if (_rawBaseGoldRateMulti > 0)
            {
                _isUpdatingGoldRateMulti = true;
                try
                {
                    double finalRate = Math.Max(0, _rawBaseGoldRateMulti - diff);
                    goldRateTextboxMulti.Text = finalRate.ToString("N0");
                }
                finally
                {
                    _isUpdatingGoldRateMulti = false;
                }
                updateMultiTestCalculations();
            }
        }

        private void weightTakaMulti_TextChanged(object sender, TextChangedEventArgs e)
        {
            updateMultiTestCalculations();
        }

        private void testTabInputsMulti_TextChanged(object sender, TextChangedEventArgs e)
        {
            updateMultiTestCalculations();
        }

        private void laborTextboxMulti_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (laborTextboxMulti != null && laborTextboxMulti.IsFocused)
            {
                _isLaborManuallyEditedMulti = true;
            }
            updateMultiTestCalculations();
        }

        private void laborTextboxMulti_LostFocus(object sender, RoutedEventArgs e)
        {
            if (laborTextboxMulti != null)
            {
                if (string.IsNullOrWhiteSpace(laborTextboxMulti.Text))
                {
                    laborTextboxMulti.Text = "0";
                }
                else if (double.TryParse(laborTextboxMulti.Text.Replace(",", "").Replace(" ", ""), out double val))
                {
                    laborTextboxMulti.Text = val.ToString("N0");
                }
            }
            updateMultiTestCalculations();
        }

        private void karatInputMulti_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingPurityMulti) return;
            if (karatInputTextboxMulti != null && karatInputTextboxMulti.IsFocused)
            {
                double karat = NumberFormattingHelper.GetCleanDouble(karatInputTextboxMulti);
                if (karat > 24.0) karat = 24.0;
                if (karat < 0) karat = 0;
                double ratti = Math.Max(0, 96.0 - (karat / 24.0) * 96.0);
                SyncAllPurityFieldsMulti(ratti, karatInputTextboxMulti);
            }
            updateMultiTestCalculations();
        }

        private void purityPercentMulti_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingPurityMulti) return;
            if (purityPercentTextboxMulti != null && purityPercentTextboxMulti.IsFocused)
            {
                double pct = NumberFormattingHelper.GetCleanDouble(purityPercentTextboxMulti);
                if (pct > 100.0) pct = 100.0;
                if (pct < 0) pct = 0;
                double ratti = Math.Max(0, 96.0 - (pct * 0.96));
                SyncAllPurityFieldsMulti(ratti, purityPercentTextboxMulti);
            }
            updateMultiTestCalculations();
        }

        private void SyncAllPurityFieldsMulti(double ratti, TextBox? source)
        {
            if (_isSyncingPurityMulti) return;
            _isSyncingPurityMulti = true;
            try
            {
                if (ratti < 0) ratti = 0;
                if (ratti > 96) ratti = 96;

                double pureRatti = 96.0 - ratti;
                double purityPct = (pureRatti / 96.0) * 100.0;
                double karat = (pureRatti / 96.0) * 24.0;

                string rattiStr = ratti.ToString("0.##");
                string karatStr = karat.ToString("0.##");
                string pctStr = purityPct.ToString("0.##");

                if (source != resultTextboxMulti && resultTextboxMulti != null)
                    resultTextboxMulti.Text = rattiStr;

                if (source != karatInputTextboxMulti && karatInputTextboxMulti != null)
                    karatInputTextboxMulti.Text = karatStr;

                if (source != purityPercentTextboxMulti && purityPercentTextboxMulti != null)
                    purityPercentTextboxMulti.Text = pctStr;
            }
            finally
            {
                _isSyncingPurityMulti = false;
            }
        }

        private void notesTextboxMulti_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (notesTextboxMulti == null) return;

            string text = notesTextboxMulti.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                if (notesSumTextMulti != null) notesSumTextMulti.Text = "Sum: 0.00";
                if (notesAvgTextMulti != null) notesAvgTextMulti.Text = "Avg: 0.00";
                return;
            }

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            double sum = 0;
            int count = 0;

            foreach (var rawLine in lines)
            {
                string cleaned = rawLine.Trim().Replace(",", "");
                if (double.TryParse(cleaned, out double val))
                {
                    sum += val;
                    count++;
                }
                else
                {
                    var match = Regex.Match(cleaned, @"-?\d+(\.\d+)?");
                    if (match.Success && double.TryParse(match.Value, out double parsedVal))
                    {
                        sum += parsedVal;
                        count++;
                    }
                }
            }

            double avg = count > 0 ? (sum / count) : 0;
            if (notesSumTextMulti != null) notesSumTextMulti.Text = $"Sum: {sum:N2}";
            if (notesAvgTextMulti != null) notesAvgTextMulti.Text = $"Avg: {avg:N2}";
        }

        private void updateMultiTestCalculations()
        {
            if (pureGoldTextboxMulti == null || laborTextboxMulti == null ||
                grossPureGoldCashTextboxMulti == null || totalPureGoldCashTextboxMulti == null) return;

            try
            {
                double goldRate = 0;
                double totalWeight = 0;
                double resultRatti = 0;

                string rateText = !string.IsNullOrWhiteSpace(goldRateTextboxMulti?.Text) ? goldRateTextboxMulti.Text : "0";
                double.TryParse(rateText.Replace(",", "").Replace(" ", ""), out goldRate);

                double grossWeight = 0;
                double taka = 0;
                if (weightInputTextboxMulti != null)
                    grossWeight = NumberFormattingHelper.GetCleanDouble(weightInputTextboxMulti);
                if (takaTextboxMulti != null)
                    taka = NumberFormattingHelper.GetCleanDouble(takaTextboxMulti);

                if (grossWeight > 0 || taka > 0)
                {
                    totalWeight = Math.Max(0, grossWeight - taka);
                    if (totalWeightTextboxMulti != null)
                        totalWeightTextboxMulti.Text = totalWeight.ToString("0.000");
                }
                else
                {
                    if (totalWeightTextboxMulti != null)
                        double.TryParse(totalWeightTextboxMulti.Text.Replace(",", "").Replace(" ", ""), out totalWeight);
                }

                if (resultTextboxMulti != null)
                {
                    double.TryParse(resultTextboxMulti.Text.Replace(",", "").Replace(" ", ""), out resultRatti);
                    if (resultTextboxMulti.IsFocused)
                    {
                        SyncAllPurityFieldsMulti(resultRatti, resultTextboxMulti);
                    }
                }


                // 1. Pure Gold:
                double pureRatti = Math.Max(0, 96.0 - resultRatti);
                double pureGold = 0;
                if (totalWeight > 0)
                {
                    pureGold = totalWeight * (pureRatti / 96.0);
                    pureGold = Math.Floor(pureGold * 100.0) / 100.0;
                }
                pureGoldTextboxMulti.Text = pureGold.ToString("F3");

                // 2. Labor Charges:
                double laborCharges = 0;
                if (_isLaborManuallyEditedMulti || (laborTextboxMulti != null && laborTextboxMulti.IsFocused))
                {
                    if (laborTextboxMulti != null && !string.IsNullOrWhiteSpace(laborTextboxMulti.Text))
                    {
                        double.TryParse(laborTextboxMulti.Text.Replace(",", "").Replace(" ", ""), out laborCharges);
                    }
                }
                else
                {
                    if (totalWeight > 0)
                    {
                        if (totalWeight <= 11.5)
                            laborCharges = 350;
                        else
                            laborCharges = totalWeight * 30;
                    }
                    if (laborTextboxMulti != null)
                    {
                        laborTextboxMulti.Text = laborCharges.ToString("N0");
                    }
                }

                if (totalWeight <= 0 && (laborTextboxMulti == null || !laborTextboxMulti.IsFocused))
                {
                    _isLaborManuallyEditedMulti = false;
                }

                // 3. Rate per Gram (1 Tola = 11.664 grams):
                double ratePerGram = goldRate > 0 ? (goldRate / 11.664) : 0;

                // 4. Gross Cash:
                double grossPureGoldCash = pureGold * ratePerGram;
                grossPureGoldCashTextboxMulti.Text = grossPureGoldCash.ToString("N0");

                // 5. Total Cash with Labor:
                double totalPureGoldCash = grossPureGoldCash > 0 ? Math.Max(0, grossPureGoldCash - laborCharges) : 0;
                totalPureGoldCashTextboxMulti.Text = totalPureGoldCash.ToString("N0");
            }
            catch
            {
                pureGoldTextboxMulti.Text = "0";
                laborTextboxMulti.Text = "0";
                grossPureGoldCashTextboxMulti.Text = "0";
                totalPureGoldCashTextboxMulti.Text = "0";
            }
        }

        private void addBatchItemBtn_Click(object sender, RoutedEventArgs e)
        {
            double grossWeight = NumberFormattingHelper.GetCleanDouble(weightInputTextboxMulti);
            double taka = NumberFormattingHelper.GetCleanDouble(takaTextboxMulti);
            double totalWeight = NumberFormattingHelper.GetCleanDouble(totalWeightTextboxMulti);
            double ratti = NumberFormattingHelper.GetCleanDouble(resultTextboxMulti);
            double karat = NumberFormattingHelper.GetCleanDouble(karatInputTextboxMulti);
            double purityPct = NumberFormattingHelper.GetCleanDouble(purityPercentTextboxMulti);
            double pureGold = NumberFormattingHelper.GetCleanDouble(pureGoldTextboxMulti);
            double labor = NumberFormattingHelper.GetCleanDouble(laborTextboxMulti);
            double cash = NumberFormattingHelper.GetCleanDouble(totalPureGoldCashTextboxMulti);

            if (totalWeight <= 0 && grossWeight <= 0)
            {
                MessageBox.Show("Please enter the product weight before adding to the batch.", "Missing Weight", MessageBoxButton.OK, MessageBoxImage.Warning);
                weightInputTextboxMulti?.Focus();
                return;
            }

            if (totalWeight <= 0)
            {
                totalWeight = Math.Max(0, grossWeight - taka);
            }

            string metal = radioSilverMulti?.IsChecked == true ? "Silver" : "Gold";
            string itemType = GetSelectedMultiItemType();

            var item = new BatchItem
            {
                Index = BatchItems.Count + 1,
                Metal = metal,
                ItemType = itemType,
                Weight = grossWeight,
                Taka = taka,
                TotalWeight = totalWeight,
                Ratti = ratti,
                Karat = karat,
                PurityPercent = purityPct,
                PureGold = pureGold,
                Labor = labor,
                Cash = cash
            };

            BatchItems.Add(item);
            updateBatchTotals();

            // Reset item input fields for the next product
            if (weightInputTextboxMulti != null) weightInputTextboxMulti.Text = "0";
            if (takaTextboxMulti != null) takaTextboxMulti.Text = "0";
            if (totalWeightTextboxMulti != null) totalWeightTextboxMulti.Text = "0";
            if (resultTextboxMulti != null) resultTextboxMulti.Text = "0";
            if (pureGoldTextboxMulti != null) pureGoldTextboxMulti.Text = "0";
            if (laborTextboxMulti != null) laborTextboxMulti.Text = "0";
            if (grossPureGoldCashTextboxMulti != null) grossPureGoldCashTextboxMulti.Text = "0";
            if (totalPureGoldCashTextboxMulti != null) totalPureGoldCashTextboxMulti.Text = "0";

            if (karatInputTextboxMulti != null) karatInputTextboxMulti.Text = "0";
            if (purityPercentTextboxMulti != null) purityPercentTextboxMulti.Text = "0";

            _isLaborManuallyEditedMulti = false;
            updateMultiTestCalculations();

            weightInputTextboxMulti?.Focus();
        }

        private void removeBatchItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int index = 0;
                if (btn.Tag is int intVal)
                {
                    index = intVal;
                }
                else if (int.TryParse(btn.Tag.ToString(), out int parsed))
                {
                    index = parsed;
                }

                var item = BatchItems.FirstOrDefault(b => b.Index == index);
                if (item != null)
                {
                    BatchItems.Remove(item);
                    for (int i = 0; i < BatchItems.Count; i++)
                    {
                        BatchItems[i].Index = i + 1;
                    }
                    batchDataGrid?.Items.Refresh();
                    updateBatchTotals();
                }
            }
        }

        private void updateBatchTotals()
        {
            double totalWeight = BatchItems.Sum(b => b.TotalWeight);
            double totalPure = BatchItems.Sum(b => b.PureGold);
            double totalLabor = BatchItems.Sum(b => b.Labor);
            double totalCash = BatchItems.Sum(b => b.Cash);

            if (batchTotalWeightDisplay != null)
                batchTotalWeightDisplay.Text = $"{totalWeight:0.000}g";
            if (batchTotalPureGoldDisplay != null)
                batchTotalPureGoldDisplay.Text = $"{totalPure:0.000}g";
            if (batchTotalLaborDisplay != null)
                batchTotalLaborDisplay.Text = $"Rs. {totalLabor:N0}";
            if (batchTotalCashDisplay != null)
                batchTotalCashDisplay.Text = $"Rs. {totalCash:N0}";
        }

        public void ClearAllMultiTestFields()
        {
            BatchItems.Clear();

            if (customersMulti != null) customersMulti.Text = "";
            if (weightInputTextboxMulti != null) weightInputTextboxMulti.Text = "0";
            if (takaTextboxMulti != null) takaTextboxMulti.Text = "0";
            if (totalWeightTextboxMulti != null) totalWeightTextboxMulti.Text = "0";
            if (resultTextboxMulti != null) resultTextboxMulti.Text = "0";
            if (pureGoldTextboxMulti != null) pureGoldTextboxMulti.Text = "0";
            if (laborTextboxMulti != null) laborTextboxMulti.Text = "0";
            if (grossPureGoldCashTextboxMulti != null) grossPureGoldCashTextboxMulti.Text = "0";
            if (totalPureGoldCashTextboxMulti != null) totalPureGoldCashTextboxMulti.Text = "0";

            if (notesTextboxMulti != null) notesTextboxMulti.Text = "";
            if (notesSumTextMulti != null) notesSumTextMulti.Text = "Sum: 0.00";
            if (notesAvgTextMulti != null) notesAvgTextMulti.Text = "Avg: 0.00";

            if (karatInputTextboxMulti != null) karatInputTextboxMulti.Text = "0";
            if (purityPercentTextboxMulti != null) purityPercentTextboxMulti.Text = "0";

            _isLaborManuallyEditedMulti = false;

            updateBatchTotals();
            updateMultiTestCalculations();
        }

        private void PrintMultiTestBatch()
        {
            if (BatchItems.Count == 0)
            {
                double currentGross = NumberFormattingHelper.GetCleanDouble(weightInputTextboxMulti);
                double currentTotal = NumberFormattingHelper.GetCleanDouble(totalWeightTextboxMulti);
                if (currentGross > 0 || currentTotal > 0)
                {
                    var res = MessageBox.Show(
                        "You have an item entered in the fields that hasn't been added to the batch yet.\n\nWould you like to add it to the batch and proceed with printing?\n\nکیا آپ موجودہ آئٹم کو بیچ میں شامل کر کے پرنٹ کرنا چاہتے ہیں؟",
                        "Add Item to Batch",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (res == MessageBoxResult.Yes)
                    {
                        addBatchItemBtn_Click(this, new RoutedEventArgs());
                    }
                }
            }

            if (BatchItems.Count == 0)
            {
                MessageBox.Show("Please add at least one tested product to the batch before printing.", "Empty Batch", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            PrintDialog printDlg = new PrintDialog();
            DateTime printTime = DateTime.Now;
            FlowDocument doc = CreateBatchFlowDocument(printTime);
            doc.Name = "FlowDoc";
            IDocumentPaginatorSource idpSource = doc;

            bool? print = printDlg.ShowDialog();
            if (print != null && (print ?? false))
            {
                try
                {
                    int copies = Math.Max(1, CurrentSettings.NumberOfCopies);
                    for (int c = 0; c < copies; c++)
                    {
                        printDlg.PrintDocument(idpSource.DocumentPaginator, $"Printing Batch Receipt ({c + 1}/{copies})");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Printing error: {ex.Message}", "Print", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                finally
                {
                    RecordBatchPrint(printTime, doc);
                    ClearAllMultiTestFields();
                }
            }
        }

        private void RecordBatchPrint(DateTime printTime, FlowDocument doc)
        {
            try
            {
                string customerName = "";
                if (customersMulti != null && !string.IsNullOrWhiteSpace(customersMulti.Text))
                    customerName = customersMulti.Text;
                else if (customersTest != null && !string.IsNullOrWhiteSpace(customersTest.Text))
                    customerName = customersTest.Text;

                double sumTotalWeight = BatchItems.Sum(b => b.TotalWeight);
                double sumPureGold = BatchItems.Sum(b => b.PureGold);
                double sumLabor = BatchItems.Sum(b => b.Labor);
                double sumCash = BatchItems.Sum(b => b.Cash);

                double goldRate = 0;
                if (goldRateTextboxMulti != null)
                    double.TryParse(goldRateTextboxMulti.Text.Replace(",", "").Replace(" ", ""), out goldRate);

                string notes = notesTextboxMulti?.Text ?? "";

                var record = new PrintRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    Timestamp = printTime,
                    CustomerName = customerName,
                    TabType = "Multi Test",
                    Metal = radioSilverMulti?.IsChecked == true ? "Silver" : "Gold",
                    ItemType = $"Batch ({BatchItems.Count} items)",
                    Weight = BatchItems.Sum(b => b.Weight),
                    Taka = BatchItems.Sum(b => b.Taka),
                    TotalWeight = sumTotalWeight,
                    PureGold = sumPureGold,
                    Labor = sumLabor,
                    Cash = sumCash,
                    GoldRate = goldRate,
                    Notes = notes,
                    BatchItems = BatchItems.ToList(),
                    ReceiptXaml = System.Windows.Markup.XamlWriter.Save(doc)
                };

                SavePrintRecord(record);
            }
            catch { }
        }

        #endregion

        private void clearOnFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (textBox.Text == "0")
                {
                    textBox.Clear();
                }
                else
                {
                    textBox.SelectAll();
                }
            }
        }

        private void selectOnFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        private void SelectAllCheckBoxes_Click(object sender, RoutedEventArgs e)
        {
            if (mainTabControl.SelectedItem == historyTab)
            {
                if (historyDataGrid.ItemsSource is IEnumerable<PrintRecordDisplay> list)
                {
                    foreach (var item in list) item.IsSelected = true;
                }
                if (historySelectAllHeaderCheckBox != null) historySelectAllHeaderCheckBox.IsChecked = true;
                UpdateHistorySelectionCount();
                return;
            }

            SetAllCheckBoxesState(true);
        }

        private void DeselectAllCheckBoxes_Click(object sender, RoutedEventArgs e)
        {
            if (mainTabControl.SelectedItem == historyTab)
            {
                if (historyDataGrid.ItemsSource is IEnumerable<PrintRecordDisplay> list)
                {
                    foreach (var item in list) item.IsSelected = false;
                }
                if (historySelectAllHeaderCheckBox != null) historySelectAllHeaderCheckBox.IsChecked = false;
                UpdateHistorySelectionCount();
                return;
            }

            SetAllCheckBoxesState(false);
        }

        private void SetAllCheckBoxesState(bool isChecked)
        {
            if (mainTabControl.SelectedItem == multiTestTab)
            {
                if (doPrintGoldRateMulti != null) doPrintGoldRateMulti.IsChecked = isChecked;
                if (doPrintCustomerNameMulti != null) doPrintCustomerNameMulti.IsChecked = isChecked;
                if (doPrintWeightMulti != null) doPrintWeightMulti.IsChecked = isChecked;
                if (doPrintTotalWeightMulti != null) doPrintTotalWeightMulti.IsChecked = isChecked;
                if (doPrintResultMulti != null) doPrintResultMulti.IsChecked = isChecked;
                if (doPrintPureGoldMulti != null) doPrintPureGoldMulti.IsChecked = isChecked;
                if (doPrintLaborMulti != null) doPrintLaborMulti.IsChecked = isChecked;
                if (doPrintGrossPureGoldCashMulti != null) doPrintGrossPureGoldCashMulti.IsChecked = isChecked;
                if (doPrintTotalPureGoldCashMulti != null) doPrintTotalPureGoldCashMulti.IsChecked = isChecked;
                if (doPrintKaratMulti != null) doPrintKaratMulti.IsChecked = isChecked;
                if (doPrintPurityPercentMulti != null) doPrintPurityPercentMulti.IsChecked = isChecked;
                if (doPrintNotesMulti != null) doPrintNotesMulti.IsChecked = isChecked;
                return;
            }

            // Test Tab CheckBoxes
            if (doPrintWeight != null) doPrintWeight.IsChecked = isChecked;
            if (doPrintTaka != null) doPrintTaka.IsChecked = isChecked;
            if (doPrintTotalWeight != null) doPrintTotalWeight.IsChecked = isChecked;
            if (doPrintResult != null) doPrintResult.IsChecked = isChecked;
            if (doPrintPureGold != null) doPrintPureGold.IsChecked = isChecked;
            if (doPrintLabor != null) doPrintLabor.IsChecked = isChecked;
            if (doPrintGrossPureGoldCash != null) doPrintGrossPureGoldCash.IsChecked = isChecked;
            if (doPrintTotalPureGoldCash != null) doPrintTotalPureGoldCash.IsChecked = isChecked;
            if (doPrintKarat != null) doPrintKarat.IsChecked = isChecked;
            if (doPrintPurityPercent != null) doPrintPurityPercent.IsChecked = isChecked;
            if (doPrintGoldRateTest != null) doPrintGoldRateTest.IsChecked = isChecked;
            if (doPrintCustomerNameTest != null) doPrintCustomerNameTest.IsChecked = isChecked;
            if (doPrintNotes != null) doPrintNotes.IsChecked = isChecked;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainTabControl.SelectedItem == historyTab)
            {
                PrintSelectedHistoryRecords();
                return;
            }
            else if (mainTabControl.SelectedItem == settingsTab)
            {
                SaveAppSettings();
                return;
            }
            else if (mainTabControl.SelectedItem == multiTestTab)
            {
                PrintMultiTestBatch();
                return;
            }

            PrintDialog printDlg = new PrintDialog();

            DateTime printTime = DateTime.Now;
            FlowDocument doc = CreateFlowDocument(printTime);
            doc.Name = "FlowDoc";
            IDocumentPaginatorSource idpSource = doc;
            documentView dwin = new documentView(doc);
            dwin.Width = IsSmallSlipSelected ? 220 : 288;
            //dwin.Show();

            bool? print = printDlg.ShowDialog();
            if (print != null && (print ?? false))
            {
                try
                {
                    int copies = Math.Max(1, CurrentSettings.NumberOfCopies);
                    for (int c = 0; c < copies; c++)
                    {
                        printDlg.PrintDocument(idpSource.DocumentPaginator, $"Printing Receipt ({c + 1}/{copies})");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Printing error: {ex.Message}", "Print", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                finally
                {
                    RecordCurrentPrint(printTime, doc);
                    ClearAllTestFields();
                }
            }
        }

        public FlowDocument CreateFlowDocument(DateTime date)
        {
            FlowDocument flowDoc = new FlowDocument();

            // Set page width and padding
            int pageWidth = IsSmallSlipSelected ? 190 : 288;
            flowDoc.PageWidth = pageWidth;
            flowDoc.PagePadding = IsSmallSlipSelected ? new Thickness(4) : new Thickness(12);
            flowDoc.FontFamily = new FontFamily("Cambria");
            int baseFontSize = (IsSmallSlipSelected ? 11 : 16) + CurrentSettings.FontSizeDelta;
            flowDoc.FontSize = Math.Max(8, baseFontSize);
            flowDoc.FontWeight = CurrentSettings.IsBoldEnabled ? FontWeights.Bold : FontWeights.Normal;
            flowDoc.Background = Brushes.White;

            // Header Section
            Paragraph headerParagraph = new Paragraph(new Run("INSTANT DEALING RECEIPT"))
            {
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold,
                FontSize = IsSmallSlipSelected ? 15 : 20,
                Margin = new Thickness(0, 0, 0, IsSmallSlipSelected ? 4 : 8),
                Foreground = Brushes.Black
            };
            flowDoc.Blocks.Add(headerParagraph);

            // Date and Time
            Paragraph dateParagraph = new Paragraph(new Run(date.ToString("dddd, MMMM dd, yyyy - HH:mm")))
            {
                TextAlignment = TextAlignment.Center,
                FontStyle = FontStyles.Italic,
                FontSize = IsSmallSlipSelected ? 10 : 14,
                Margin = new Thickness(0, 0, 0, IsSmallSlipSelected ? 6 : 10),
                Foreground = Brushes.Black
            };
            flowDoc.Blocks.Add(dateParagraph);

            // Customer Information
            string customerName = "";
            if (doPrint(customersTest) && !string.IsNullOrWhiteSpace(customersTest?.Text))
                customerName = customersTest.Text;

            if (!string.IsNullOrWhiteSpace(customerName))
            {
                Paragraph customerParagraph = new Paragraph();
                customerParagraph.Inlines.Add(new Run("Customer: ") { FontWeight = FontWeights.Bold });
                customerParagraph.Inlines.Add(new Bold(new Run(customerName)) { Foreground = Brushes.Black });
                customerParagraph.TextAlignment = TextAlignment.Left;
                customerParagraph.FontSize = IsSmallSlipSelected ? 12 : 16;
                customerParagraph.Margin = new Thickness(0, 0, 0, IsSmallSlipSelected ? 4 : 8);
                flowDoc.Blocks.Add(customerParagraph);
            }

            bool hasTransactionData = false;

            // Test Transaction Section
            bool shouldPrintTestSection = doPrint(weightInputTextbox) ||
                                         doPrint(takaTextbox) ||
                                         doPrint(totalWeightTextbox) || 
                                         doPrint(resultTextbox) || 
                                         doPrint(pureGoldTextbox) || 
                                         doPrint(laborTextbox) || 
                                         doPrint(grossPureGoldCashTextbox) || 
                                         doPrint(totalPureGoldCashTextbox) || 
                                         doPrint(karatInputTextbox) ||
                                         doPrint(purityPercentTextbox);

            if (shouldPrintTestSection)
            {
                if (hasTransactionData) flowDoc.Blocks.Add(CreateSeparator());
                hasTransactionData = true;

                string metal = GetSelectedMetal();
                string itemType = GetSelectedItemType();
                string itemDesc = !string.IsNullOrEmpty(itemType) ? $"{metal} {itemType}" : metal;

                Paragraph testHeader = new Paragraph();
                string headerTitle = !string.IsNullOrEmpty(itemDesc) 
                    ? $"TEST TRANSACTION  •  {itemDesc.ToUpper()}" 
                    : "TEST TRANSACTION";
                testHeader.Inlines.Add(new Underline(new Bold(new Run(headerTitle))));
                testHeader.TextAlignment = TextAlignment.Center;
                testHeader.FontSize = IsSmallSlipSelected ? 11 : 14;
                testHeader.Margin = new Thickness(0, IsSmallSlipSelected ? 4 : 8, 0, IsSmallSlipSelected ? 3 : 5);
                testHeader.Foreground = Brushes.Black;
                flowDoc.Blocks.Add(testHeader);

                Table testTable = new Table() { CellSpacing = 2 };
                testTable.Columns.Add(new TableColumn() { Width = new GridLength(IsSmallSlipSelected ? 100 : 140) });
                testTable.Columns.Add(new TableColumn() { Width = new GridLength(IsSmallSlipSelected ? 80 : 100) });

                TableRowGroup testRowGroup = new TableRowGroup();

                if (doPrint(weightInputTextbox) && !string.IsNullOrWhiteSpace(weightInputTextbox?.Text) && weightInputTextbox.Text != "0")
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Weight:")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"{weightInputTextbox.Text}g")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
                    testRowGroup.Rows.Add(row);
                }

                if (doPrint(takaTextbox) && NumberFormattingHelper.GetCleanDouble(takaTextbox) > 0)
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Taka:")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"{takaTextbox.Text}g")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
                    testRowGroup.Rows.Add(row);
                }

                if (doPrint(totalWeightTextbox) && !string.IsNullOrWhiteSpace(totalWeightTextbox?.Text) && totalWeightTextbox.Text != "0")
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Total Weight:")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"{totalWeightTextbox.Text}g")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
                    testRowGroup.Rows.Add(row);
                }

                if (doPrint(resultTextbox) && !string.IsNullOrWhiteSpace(resultTextbox?.Text) && resultTextbox.Text != "0")
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Result (Ratti):")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(resultTextbox.Text)) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
                    testRowGroup.Rows.Add(row);
                }

                if (doPrint(karatInputTextbox) && !string.IsNullOrWhiteSpace(karatInputTextbox?.Text) && karatInputTextbox.Text != "0")
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Karat:")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"{karatInputTextbox.Text}K")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
                    testRowGroup.Rows.Add(row);
                }

                if (doPrint(purityPercentTextbox) && !string.IsNullOrWhiteSpace(purityPercentTextbox?.Text) && purityPercentTextbox.Text != "0")
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Purity:")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"{purityPercentTextbox.Text}%")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
                    testRowGroup.Rows.Add(row);
                }

                if (doPrint(pureGoldTextbox) && !string.IsNullOrWhiteSpace(pureGoldTextbox?.Text) && pureGoldTextbox.Text != "0")
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Pure Gold:")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"{pureGoldTextbox.Text}g")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
                    testRowGroup.Rows.Add(row);
                }

                if (doPrint(laborTextbox) && !string.IsNullOrWhiteSpace(laborTextbox?.Text) && laborTextbox.Text != "0")
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Labor Charges:")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"Rs. {laborTextbox.Text}")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
                    testRowGroup.Rows.Add(row);
                }

                if (doPrint(grossPureGoldCashTextbox) && !string.IsNullOrWhiteSpace(grossPureGoldCashTextbox?.Text) && grossPureGoldCashTextbox.Text != "0")
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Cash W/O Labor:")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"Rs. {grossPureGoldCashTextbox.Text}")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
                    testRowGroup.Rows.Add(row);
                }

                if (doPrint(totalPureGoldCashTextbox) && !string.IsNullOrWhiteSpace(totalPureGoldCashTextbox?.Text) && totalPureGoldCashTextbox.Text != "0")
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Cash W/ Labor:")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"Rs. {totalPureGoldCashTextbox.Text}")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
                    testRowGroup.Rows.Add(row);
                }

                testTable.RowGroups.Add(testRowGroup);
                flowDoc.Blocks.Add(testTable);
            }

            // Gold Rate Section
            if (doPrint(goldRateTextboxTest))
            {
                string finalRateStr = goldRateTextboxTest?.Text ?? "";

                if (!string.IsNullOrWhiteSpace(finalRateStr) && finalRateStr != "0")
                {
                    if (hasTransactionData) flowDoc.Blocks.Add(CreateSeparator());

                    double diff = 0;
                    if (goldRateDiffTextbox != null && !string.IsNullOrWhiteSpace(goldRateDiffTextbox.Text))
                    {
                        double.TryParse(goldRateDiffTextbox.Text.Replace(",", "").Replace(" ", ""), out diff);
                    }

                    double finalRate = 0;
                    double.TryParse(finalRateStr.Replace(",", "").Replace(" ", ""), out finalRate);

                    string goldRateDisplayText;
                    if (diff > 0 && _rawBaseGoldRate > 0)
                    {
                        goldRateDisplayText = $"Gold Rate: {_rawBaseGoldRate:N0} - {diff:N0} = {finalRate:N0}";
                    }
                    else if (diff > 0 && finalRate > 0)
                    {
                        double baseRate = finalRate + diff;
                        goldRateDisplayText = $"Gold Rate: {baseRate:N0} - {diff:N0} = {finalRate:N0}";
                    }
                    else
                    {
                        goldRateDisplayText = $"Gold Rate: {finalRateStr}";
                    }

                    Paragraph goldRateParagraph = new Paragraph();
                    goldRateParagraph.Inlines.Add(new Run(goldRateDisplayText) { FontWeight = FontWeights.Bold, FontSize = IsSmallSlipSelected ? 10 : 13 });
                    goldRateParagraph.TextAlignment = TextAlignment.Center;
                    goldRateParagraph.Margin = new Thickness(0, 6, 0, 6);
                    flowDoc.Blocks.Add(goldRateParagraph);
                }
            }

            // Notes Section
            if (doPrint(notesTextboxTest) && !string.IsNullOrWhiteSpace(notesTextboxTest?.Text))
            {
                flowDoc.Blocks.Add(CreateSeparator());

                Paragraph notesHeader = new Paragraph();
                notesHeader.Inlines.Add(new Bold(new Run("NOTES / نوٹ:")));
                notesHeader.FontSize = IsSmallSlipSelected ? 10 : 13;
                notesHeader.Margin = new Thickness(0, 4, 0, 2);
                notesHeader.Foreground = Brushes.Black;
                flowDoc.Blocks.Add(notesHeader);

                Paragraph notesBody = new Paragraph(new Run(notesTextboxTest.Text))
                {
                    FontSize = IsSmallSlipSelected ? 9 : 11,
                    Margin = new Thickness(0, 0, 0, 4),
                    Foreground = Brushes.DarkSlateGray
                };
                flowDoc.Blocks.Add(notesBody);
            }

            // Footer
            flowDoc.Blocks.Add(CreateSeparator());
            Paragraph footer = new Paragraph(new Run("Thank You for Your Business!"))
            {
                TextAlignment = TextAlignment.Center,
                FontSize = IsSmallSlipSelected ? 9 : 11,
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(0, 5, 0, 0)
            };
            flowDoc.Blocks.Add(footer);

            return flowDoc;
        }

        public FlowDocument CreateBatchFlowDocument(DateTime date)
        {
            string cust = customersMulti?.Text ?? "";
            bool printCust = doPrintCustomerNameMulti?.IsChecked == true && !string.IsNullOrWhiteSpace(cust);
            double goldRate = 0;
            string rateDisplay = "";
            if (goldRateTextboxMulti != null && !string.IsNullOrWhiteSpace(goldRateTextboxMulti.Text))
            {
                string rateStr = goldRateTextboxMulti.Text;
                double diff = 0;
                if (goldRateDiffTextboxMulti != null && !string.IsNullOrWhiteSpace(goldRateDiffTextboxMulti.Text))
                {
                    double.TryParse(goldRateDiffTextboxMulti.Text.Replace(",", "").Replace(" ", ""), out diff);
                }
                if (diff > 0 && _rawBaseGoldRateMulti > 0)
                {
                    double.TryParse(rateStr.Replace(",", "").Replace(" ", ""), out double finalR);
                    rateDisplay = $"Gold Rate: {_rawBaseGoldRateMulti:N0} - {diff:N0} = {finalR:N0}";
                    goldRate = finalR;
                }
                else
                {
                    double.TryParse(rateStr.Replace(",", "").Replace(" ", ""), out goldRate);
                    rateDisplay = $"Gold Rate: {rateStr}";
                }
            }

            bool printRate = doPrintGoldRateMulti?.IsChecked == true && goldRate > 0;
            bool printNotes = doPrintNotesMulti?.IsChecked == true && !string.IsNullOrWhiteSpace(notesTextboxMulti?.Text);
            string notes = notesTextboxMulti?.Text ?? "";

            return CreateBatchFlowDocument(
                BatchItems,
                cust,
                goldRate,
                date,
                printCust,
                printRate,
                printNotes,
                notes,
                rateDisplay);
        }

        public FlowDocument CreateBatchFlowDocument(
            IList<BatchItem> items,
            string customerName,
            double goldRate,
            DateTime date,
            bool printCustomer = true,
            bool printGoldRate = false,
            bool printNotes = false,
            string notes = "",
            string customRateDisplay = "")
        {
            FlowDocument flowDoc = new FlowDocument();

            int pageWidth = IsSmallSlipSelected ? 190 : 288;
            flowDoc.PageWidth = pageWidth;
            flowDoc.PagePadding = IsSmallSlipSelected ? new Thickness(4) : new Thickness(12);
            flowDoc.FontFamily = new FontFamily("Cambria");
            int baseFontSize = (IsSmallSlipSelected ? 13 : 17) + CurrentSettings.FontSizeDelta;
            flowDoc.FontSize = Math.Max(8, baseFontSize);
            flowDoc.FontWeight = CurrentSettings.IsBoldEnabled ? FontWeights.Bold : FontWeights.Normal;
            flowDoc.Background = Brushes.White;

            // Header Section
            Paragraph headerParagraph = new Paragraph(new Run("INSTANT DEALING RECEIPT"))
            {
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold,
                FontSize = IsSmallSlipSelected ? 17 : 22,
                Margin = new Thickness(0, 0, 0, IsSmallSlipSelected ? 4 : 8),
                Foreground = Brushes.Black
            };
            flowDoc.Blocks.Add(headerParagraph);

            // Date and Time
            Paragraph dateParagraph = new Paragraph(new Run(date.ToString("dddd, MMMM dd, yyyy - HH:mm")))
            {
                TextAlignment = TextAlignment.Center,
                FontStyle = FontStyles.Italic,
                FontSize = IsSmallSlipSelected ? 12 : 15,
                Margin = new Thickness(0, 0, 0, IsSmallSlipSelected ? 6 : 10),
                Foreground = Brushes.Black
            };
            flowDoc.Blocks.Add(dateParagraph);

            // Customer Information
            if (printCustomer && !string.IsNullOrWhiteSpace(customerName) && customerName != "-")
            {
                Paragraph customerParagraph = new Paragraph();
                customerParagraph.Inlines.Add(new Run("Customer: ") { FontWeight = FontWeights.Bold });
                customerParagraph.Inlines.Add(new Bold(new Run(customerName)) { Foreground = Brushes.Black });
                customerParagraph.TextAlignment = TextAlignment.Left;
                customerParagraph.FontSize = IsSmallSlipSelected ? 14 : 17;
                customerParagraph.Margin = new Thickness(0, 0, 0, IsSmallSlipSelected ? 4 : 8);
                flowDoc.Blocks.Add(customerParagraph);
            }

            flowDoc.Blocks.Add(CreateSeparator());

            // Section Title
            Paragraph batchHeader = new Paragraph();
            batchHeader.Inlines.Add(new Underline(new Bold(new Run($"BATCH TEST REPORT ({items.Count} ITEMS)"))));
            batchHeader.TextAlignment = TextAlignment.Center;
            batchHeader.FontSize = IsSmallSlipSelected ? 13 : 16;
            batchHeader.Margin = new Thickness(0, 2, 0, 4);
            batchHeader.Foreground = Brushes.Black;
            flowDoc.Blocks.Add(batchHeader);

            // Item Details
            if (IsSmallSlipSelected)
            {
                // Compact card layout for Small (58mm) receipt
                foreach (var item in items)
                {
                    Paragraph itemPara = new Paragraph()
                    {
                        Margin = new Thickness(0, 2, 0, 3),
                        FontSize = 12,
                        LineHeight = 16
                    };

                    itemPara.Inlines.Add(new Bold(new Run($"#{item.Index}. {item.ItemDisplay}")) { Foreground = Brushes.Black });
                    itemPara.Inlines.Add(new LineBreak());

                    string wtText = item.Taka > 0 
                        ? $"Net: {item.TotalWeight:0.000}g (Gross: {item.Weight:0.000}g, Taka: {item.Taka:0.000}g)"
                        : $"Net Wt: {item.TotalWeight:0.000}g";
                    itemPara.Inlines.Add(new Run(wtText) { FontWeight = FontWeights.Bold });
                    itemPara.Inlines.Add(new LineBreak());

                    string purityText = item.Ratti > 0 
                        ? $"Ratti: {item.Ratti:0.##} ({item.Karat:0.##}K / {item.PurityPercent:0.##}%)"
                        : $"Purity: {item.Karat:0.##}K";
                    itemPara.Inlines.Add(new Run(purityText) { FontWeight = FontWeights.Bold });
                    itemPara.Inlines.Add(new LineBreak());

                    itemPara.Inlines.Add(new Run($"Pure: {item.PureGold:0.000}g  |  Labor: Rs. {item.Labor:N0}") { FontWeight = FontWeights.Bold });
                    itemPara.Inlines.Add(new LineBreak());

                    itemPara.Inlines.Add(new Bold(new Run($"Cash: Rs. {item.Cash:N0}")) { Foreground = Brushes.Black });

                    flowDoc.Blocks.Add(itemPara);
                }
            }
            else
            {
                // Structured Table layout for Large (80mm) receipt
                Table batchTable = new Table() { CellSpacing = 0 };
                batchTable.Columns.Add(new TableColumn() { Width = new GridLength(14) });  // #
                batchTable.Columns.Add(new TableColumn() { Width = new GridLength(62) });  // Item
                batchTable.Columns.Add(new TableColumn() { Width = new GridLength(48) });  // Net Wt
                batchTable.Columns.Add(new TableColumn() { Width = new GridLength(42) });  // Purity
                batchTable.Columns.Add(new TableColumn() { Width = new GridLength(48) });  // Pure
                batchTable.Columns.Add(new TableColumn() { Width = new GridLength(50) });  // Cash

                TableRowGroup headerGroup = new TableRowGroup();
                TableRow headerRow = new TableRow();
                headerRow.FontWeight = FontWeights.Bold;
                headerRow.FontSize = 12;
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("#")) { Margin = new Thickness(0) }));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Item")) { Margin = new Thickness(0) }));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Net Wt")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right }));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Purity")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Center }));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Pure")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right }));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Cash")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right }));
                headerGroup.Rows.Add(headerRow);
                batchTable.RowGroups.Add(headerGroup);

                TableRowGroup rowsGroup = new TableRowGroup();
                foreach (var item in items)
                {
                    TableRow row = new TableRow();
                    row.FontSize = 12;
                    row.FontWeight = FontWeights.Bold;

                    row.Cells.Add(new TableCell(new Paragraph(new Run(item.Index.ToString())) { Margin = new Thickness(0) }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(item.ItemDisplay)) { Margin = new Thickness(0) }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"{item.TotalWeight:0.000}g")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right }));

                    string pur = item.Ratti > 0 ? $"{item.Ratti:0.##}R" : (item.Karat > 0 ? $"{item.Karat:0.##}K" : "-");
                    row.Cells.Add(new TableCell(new Paragraph(new Run(pur)) { Margin = new Thickness(0), TextAlignment = TextAlignment.Center }));

                    row.Cells.Add(new TableCell(new Paragraph(new Run($"{item.PureGold:0.000}g")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(item.Cash.ToString("N0"))) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right }));

                    rowsGroup.Rows.Add(row);
                }
                batchTable.RowGroups.Add(rowsGroup);
                flowDoc.Blocks.Add(batchTable);
            }

            flowDoc.Blocks.Add(CreateSeparator());

            // Grand Summary Section
            Paragraph summaryTitle = new Paragraph(new Bold(new Run("BATCH SUMMARY / کل خلاصہ")))
            {
                TextAlignment = TextAlignment.Center,
                FontSize = IsSmallSlipSelected ? 13 : 16,
                Margin = new Thickness(0, 2, 0, 4)
            };
            flowDoc.Blocks.Add(summaryTitle);

            double totalWeightSum = items.Sum(b => b.TotalWeight);
            double totalPureSum = items.Sum(b => b.PureGold);
            double totalLaborSum = items.Sum(b => b.Labor);
            double totalCashSum = items.Sum(b => b.Cash);

            Table summaryTable = new Table() { CellSpacing = 0 };
            summaryTable.Columns.Add(new TableColumn() { Width = new GridLength(IsSmallSlipSelected ? 82 : 154) });
            summaryTable.Columns.Add(new TableColumn() { Width = new GridLength(IsSmallSlipSelected ? 100 : 110) });

            TableRowGroup summaryGroup = new TableRowGroup();
            int summaryFontSize = IsSmallSlipSelected ? 12 : 14;

            // Total Items
            TableRow itemsRow = new TableRow() { FontSize = summaryFontSize };
            itemsRow.Cells.Add(new TableCell(new Paragraph(new Run("Total Items:")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
            itemsRow.Cells.Add(new TableCell(new Paragraph(new Run(items.Count.ToString())) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
            summaryGroup.Rows.Add(itemsRow);

            // Total Weight
            TableRow wtRow = new TableRow() { FontSize = summaryFontSize };
            wtRow.Cells.Add(new TableCell(new Paragraph(new Run(IsSmallSlipSelected ? "Net Weight:" : "Total Net Weight:")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
            wtRow.Cells.Add(new TableCell(new Paragraph(new Run($"{totalWeightSum:0.000}g")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
            summaryGroup.Rows.Add(wtRow);

            // Total Pure Gold
            TableRow pureRow = new TableRow() { FontSize = summaryFontSize };
            pureRow.Cells.Add(new TableCell(new Paragraph(new Run(IsSmallSlipSelected ? "Pure Gold:" : "Total Pure Gold:")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
            pureRow.Cells.Add(new TableCell(new Paragraph(new Run($"{totalPureSum:0.000}g")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
            summaryGroup.Rows.Add(pureRow);

            // Total Labor
            if (totalLaborSum > 0)
            {
                TableRow laborRow = new TableRow() { FontSize = summaryFontSize };
                laborRow.Cells.Add(new TableCell(new Paragraph(new Run(IsSmallSlipSelected ? "Labor:" : "Total Labor:")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
                laborRow.Cells.Add(new TableCell(new Paragraph(new Run($"Rs. {totalLaborSum:N0}")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
                summaryGroup.Rows.Add(laborRow);
            }

            // TOTAL CASH
            TableRow cashRow = new TableRow() { FontSize = IsSmallSlipSelected ? 13 : 16 };
            cashRow.Cells.Add(new TableCell(new Paragraph(new Run("TOTAL CASH:")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
            cashRow.Cells.Add(new TableCell(new Paragraph(new Run($"Rs. {totalCashSum:N0}")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
            summaryGroup.Rows.Add(cashRow);

            summaryTable.RowGroups.Add(summaryGroup);
            flowDoc.Blocks.Add(summaryTable);

            // Gold Rate (if entered and checked)
            if (printGoldRate && (goldRate > 0 || !string.IsNullOrWhiteSpace(customRateDisplay)))
            {
                flowDoc.Blocks.Add(CreateSeparator());
                string rateDisplay = !string.IsNullOrWhiteSpace(customRateDisplay) ? customRateDisplay : $"Gold Rate: {goldRate:N0}";
                Paragraph ratePara = new Paragraph(new Run(rateDisplay))
                {
                    TextAlignment = TextAlignment.Center,
                    FontWeight = FontWeights.Bold,
                    FontSize = IsSmallSlipSelected ? 12 : 15,
                    Margin = new Thickness(0, 4, 0, 4)
                };
                flowDoc.Blocks.Add(ratePara);
            }

            // Notes Section (if entered and checked) - printed as-is without avg/sum
            if (printNotes && !string.IsNullOrWhiteSpace(notes))
            {
                flowDoc.Blocks.Add(CreateSeparator());
                Paragraph notesHeader = new Paragraph(new Bold(new Run("NOTES / نوٹ:")))
                {
                    FontSize = IsSmallSlipSelected ? 12 : 14,
                    Margin = new Thickness(0, 2, 0, 2)
                };
                flowDoc.Blocks.Add(notesHeader);

                Paragraph notesBody = new Paragraph(new Run(notes))
                {
                    FontSize = IsSmallSlipSelected ? 11 : 13,
                    Margin = new Thickness(0, 0, 0, 2),
                    Foreground = Brushes.DarkSlateGray
                };
                flowDoc.Blocks.Add(notesBody);
            }

            // Footer
            flowDoc.Blocks.Add(CreateSeparator());
            Paragraph footer = new Paragraph(new Run("Thank You for Your Business!"))
            {
                TextAlignment = TextAlignment.Center,
                FontSize = IsSmallSlipSelected ? 11 : 13,
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(0, 5, 0, 0)
            };
            flowDoc.Blocks.Add(footer);

            return flowDoc;
        }


        private Paragraph CreateSeparator()
        {
            int count = IsSmallSlipSelected ? 22 : 35;
            return new Paragraph(new Run(new string('═', count)))
            {
                TextAlignment = TextAlignment.Center,
                FontSize = IsSmallSlipSelected ? 10 : 12,
                Margin = new Thickness(0, IsSmallSlipSelected ? 3 : 6, 0, IsSmallSlipSelected ? 3 : 6),
                Foreground = Brushes.Black
            };
        }

        private string CapitalizeCustomerName(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var words = input.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                string w = words[i];
                if (w.Length == 1)
                {
                    words[i] = w.ToUpper();
                }
                else if (w.Length > 1)
                {
                    words[i] = char.ToUpper(w[0]) + w.Substring(1).ToLower();
                }
            }
            return string.Join(" ", words);
        }

        private void AppendUniqueName(string newName)
        {
            try
            {
                newName = CapitalizeCustomerName(newName);
                if (string.IsNullOrWhiteSpace(newName)) return;

                string jsonPath = GetCustomerJsonPath();
                var jsonData = File.ReadAllText(jsonPath);
                JObject jsonObject = JObject.Parse(jsonData);
                List<string> names = jsonObject["Names"]?.ToObject<List<string>>() ?? new List<string>();

                if (!names.Any(n => string.Equals(n, newName, StringComparison.OrdinalIgnoreCase)))
                {
                    names.Add(newName);
                    jsonObject["Names"] = JArray.FromObject(names);
                    File.WriteAllText(jsonPath, jsonObject.ToString(Formatting.Indented));
                    if (customersTest != null) customersTest.ItemsSource = names;
                }
            }
            catch { }
        }

        private void customersComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var cb = sender as ComboBox ?? customersTest;
            if (cb != null && !string.IsNullOrWhiteSpace(cb.Text))
            {
                string cap = CapitalizeCustomerName(cb.Text);
                cb.Text = cap;
                AppendUniqueName(cap);
                if (doPrintCustomerNameTest != null) doPrintCustomerNameTest.IsChecked = true;
            }
        }

        private void customersComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (customersTest != null && customersTest.SelectedItem != null && doPrintCustomerNameTest != null)
            {
                doPrintCustomerNameTest.IsChecked = true;
            }
        }

        private void customers_GotFocus(object sender, RoutedEventArgs e)
        {
            var names = LoadCustomerNames();
            if (customersTest != null) customersTest.ItemsSource = names;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainTabControl.SelectedItem == historyTab)
            {
                historyClearFilters_Click(this, new RoutedEventArgs());
                if (historyDataGrid.ItemsSource is IEnumerable<PrintRecordDisplay> list)
                {
                    foreach (var item in list) item.IsSelected = false;
                }
                UpdateHistorySelectionCount();
            }
            else if (mainTabControl.SelectedItem == settingsTab)
            {
                btnRestoreSettingsDefaults_Click(this, new RoutedEventArgs());
            }
            else if (mainTabControl.SelectedItem == multiTestTab)
            {
                ClearAllMultiTestFields();
            }
            else
            {
                ClearAllTestFields();
            }
        }

        public void ClearAllTestFields()
        {
            if (goldRateTextboxTest != null) goldRateTextboxTest.Text = "0";
            if (goldRateDiffTextbox != null) goldRateDiffTextbox.Text = "0";
            _rawBaseGoldRate = 0;

            if (customersTest != null)
            {
                customersTest.SelectedIndex = -1;
                customersTest.Text = "";
            }

            if (radioGold != null) radioGold.IsChecked = true;
            if (radioSilver != null) radioSilver.IsChecked = false;
            if (itemTypeDropdownTest != null) itemTypeDropdownTest.SelectedIndex = 0;

            if (weightInputTextbox != null) weightInputTextbox.Text = "0";
            if (takaTextbox != null) takaTextbox.Text = "0";
            if (totalWeightTextbox != null) totalWeightTextbox.Text = "0";

            if (resultTextbox != null) resultTextbox.Text = "0";
            if (pureGoldTextbox != null) pureGoldTextbox.Text = "0";
            if (laborTextbox != null) laborTextbox.Text = "0";
            _isLaborManuallyEdited = false;

            if (grossPureGoldCashTextbox != null) grossPureGoldCashTextbox.Text = "0";
            if (totalPureGoldCashTextbox != null) totalPureGoldCashTextbox.Text = "0";

            if (notesTextboxTest != null) notesTextboxTest.Text = "";
            if (notesSumTextTest != null) notesSumTextTest.Text = "Sum: 0.00";
            if (notesAvgTextTest != null) notesAvgTextTest.Text = "Avg: 0.00";

            if (sampleWeightTextbox != null) sampleWeightTextbox.Text = "0";
            if (resultWeightTextbox != null) resultWeightTextbox.Text = "0";
            if (karatInputTextbox != null) karatInputTextbox.Text = "0";
            if (rattiConversionTextbox != null) rattiConversionTextbox.Text = "0";
            if (purityPercentTextbox != null) purityPercentTextbox.Text = "0";
            if (purityRattiResultTextbox != null) purityRattiResultTextbox.Text = "0";

            if (doPrintWeight != null) doPrintWeight.IsChecked = false;
            if (doPrintTaka != null) doPrintTaka.IsChecked = false;
            if (doPrintKarat != null) doPrintKarat.IsChecked = false;
            if (doPrintPurityPercent != null) doPrintPurityPercent.IsChecked = false;
            if (doPrintNotes != null) doPrintNotes.IsChecked = false;
            if (doPrintCustomerNameTest != null) doPrintCustomerNameTest.IsChecked = false;

            updateTestCalculations();
        }

        private void goldRateTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                textBox.Text = NumberFormattingHelper.GetCleanValue(textBox).ToString("N0");
            }
        }
    }

    public class BatchItem : INotifyPropertyChanged
    {
        private int _index;
        public int Index
        {
            get => _index;
            set { _index = value; OnPropertyChanged(nameof(Index)); }
        }

        public string Metal { get; set; } = "Gold";
        public string ItemType { get; set; } = "";
        public string ItemDisplay
        {
            get
            {
                string m = string.IsNullOrWhiteSpace(Metal) ? "Gold" : Metal;
                if (!string.IsNullOrWhiteSpace(ItemType) && ItemType != "-")
                    return $"{m} {ItemType}";
                return m;
            }
        }

        public double Weight { get; set; }
        public string WeightDisplay => Weight > 0 ? $"{Weight:0.000}g" : "0.000g";

        public double Taka { get; set; }
        public string TakaDisplay => Taka > 0 ? $"{Taka:0.000}g" : "0.000g";

        public double TotalWeight { get; set; }
        public string TotalWeightDisplay => TotalWeight > 0 ? $"{TotalWeight:0.000}g" : "0.000g";

        public double Ratti { get; set; }
        public string RattiDisplay => Ratti > 0 ? Ratti.ToString("0.##") : "-";

        public double Karat { get; set; }
        public string KaratDisplay => Karat > 0 ? $"{Karat:0.##}K" : "-";

        public double PurityPercent { get; set; }
        public string PurityPercentDisplay => PurityPercent > 0 ? $"{PurityPercent:0.##}%" : "-";

        public double PureGold { get; set; }
        public string PureGoldDisplay => PureGold > 0 ? $"{PureGold:0.000}g" : "0.000g";

        public double Labor { get; set; }
        public string LaborDisplay => Labor > 0 ? Labor.ToString("N0") : "0";

        public double Cash { get; set; }
        public string CashDisplay => Cash > 0 ? Cash.ToString("N0") : "0";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }

    public class PrintRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string CustomerName { get; set; } = "";
        public string TabType { get; set; } = "";
        public string Metal { get; set; } = "Gold";
        public string ItemType { get; set; } = "";
        public double Weight { get; set; }
        public double Taka { get; set; }
        public double Ratti { get; set; }
        public double Karat { get; set; }
        public double PurityPercent { get; set; }
        public double TotalWeight { get; set; }
        public double PureGold { get; set; }
        public double Labor { get; set; }
        public double Cash { get; set; }
        public double GoldRate { get; set; }
        public string Status { get; set; } = "";
        public string Notes { get; set; } = "";
        public List<BatchItem> BatchItems { get; set; } = new List<BatchItem>();
        public string ReceiptXaml { get; set; } = "";
    }

    public class PrintRecordDisplay : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public string Id { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string FormattedDate => Timestamp.ToString("dd-MMM-yyyy hh:mm tt");
        public string CustomerName { get; set; } = "-";
        public string TabType { get; set; } = "-";
        public string Metal { get; set; } = "Gold";
        public string ItemType { get; set; } = "";
        public string ItemDisplay
        {
            get
            {
                string m = string.IsNullOrWhiteSpace(Metal) ? "Gold" : Metal;
                if (!string.IsNullOrWhiteSpace(ItemType) && ItemType != "-")
                    return $"{m} {ItemType}";
                return m;
            }
        }
        public double WeightValue { get; set; }
        public string WeightDisplay => WeightValue > 0 ? $"{WeightValue:0.000}g" : "-";
        public double TakaValue { get; set; }
        public string TakaDisplay => TakaValue > 0 ? $"{TakaValue:0.000}g" : "-";
        public double RattiValue { get; set; }
        public string RattiDisplay => RattiValue > 0 ? RattiValue.ToString("0.###") : "-";
        public double KaratValue { get; set; }
        public string KaratDisplay => KaratValue > 0 ? $"{KaratValue:0.##}K" : "-";
        public double PurityPercentValue { get; set; }
        public string PurityPercentDisplay => PurityPercentValue > 0 ? $"{PurityPercentValue:0.##}%" : "-";
        public double TotalWeightValue { get; set; }
        public string TotalWeightDisplay => TotalWeightValue > 0 ? $"{TotalWeightValue:0.000}g" : "-";
        public double PureGoldValue { get; set; }
        public string PureGoldDisplay => PureGoldValue > 0 ? $"{PureGoldValue:0.000}g" : "-";
        public double LaborValue { get; set; }
        public string LaborDisplay => LaborValue > 0 ? LaborValue.ToString("N0") : "-";
        public double CashValue { get; set; }
        public string CashDisplay => CashValue > 0 ? CashValue.ToString("N0") : "-";
        public double GoldRateValue { get; set; }
        public string GoldRateDisplay => GoldRateValue > 0 ? GoldRateValue.ToString("N0") : "-";
        public string NotesDisplay { get; set; } = "-";
    }

    public class AppSettings
    {
        public int NumberOfCopies { get; set; } = 2;
        public bool IsSmallSlip { get; set; } = false;
        public int FontSizeDelta { get; set; } = 0;
        public bool IsBoldEnabled { get; set; } = true;
    }
}