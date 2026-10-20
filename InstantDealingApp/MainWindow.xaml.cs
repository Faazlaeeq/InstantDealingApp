using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
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
            LoadPrintSettings();
            _allPrintRecords = LoadPrintHistory();
            ApplyHistoryFilters();
            mainTabControl.SelectedItem = testTab;
            updateTestCalculations();

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
                    string? historyOutPath = (i + 2 < args.Length && !args[i + 2].StartsWith("--")) ? args[i + 2] : null;
                    string? receiptOutPath = null;

                    for (int j = 0; j < args.Length; j++)
                    {
                        if (args[j] == "--receipt" && j + 1 < args.Length)
                        {
                            receiptOutPath = args[j + 1];
                            break;
                        }
                    }

                    Dispatcher.InvokeAsync(async () =>
                    {
                        // Set realistic sample data for screenshots
                        if (goldRateTextboxTest != null) goldRateTextboxTest.Text = "285,000";
                        if (customersTest != null) customersTest.Text = "Ahsan Bhai";
                        if (radioGold != null) radioGold.IsChecked = true;
                        if (radioChori != null) radioChori.IsChecked = true;
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
                        updateTestCalculations();

                        await System.Threading.Tasks.Task.Delay(700);
                        CaptureVisualToFile(this, outPath);

                        if (!string.IsNullOrEmpty(receiptOutPath))
                        {
                            FlowDocument receiptDoc = CreateFlowDocument(DateTime.Now);
                            CaptureReceiptDocumentToFile(receiptDoc, receiptOutPath);
                        }

                        if (!string.IsNullOrEmpty(historyOutPath) && historyTab != null)
                        {
                            // Ensure a sample record is recorded in history so it displays in table
                            if (_allPrintRecords.Count == 0 || !_allPrintRecords.Any(r => r.CustomerName == "Ahsan Bhai"))
                            {
                                FlowDocument sampleDoc = CreateFlowDocument(DateTime.Now);
                                RecordCurrentPrint(DateTime.Now, sampleDoc);
                            }

                            mainTabControl.SelectedItem = historyTab;
                            await System.Threading.Tasks.Task.Delay(700);
                            CaptureVisualToFile(this, historyOutPath);
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
                FlowDocumentScrollViewer viewer = new FlowDocumentScrollViewer
                {
                    Document = doc,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                    Background = Brushes.White,
                    Width = 310
                };
                Border card = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(6),
                    Child = viewer,
                    Width = 322
                };
                card.Measure(new Size(330, 2000));
                card.Arrange(new Rect(0, 0, 322, card.DesiredSize.Height));
                card.UpdateLayout();

                int h = Math.Max(420, (int)card.DesiredSize.Height);
                RenderTargetBitmap rtb = new RenderTargetBitmap(322, h, 96, 96, PixelFormats.Pbgra32);
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
                    }
                }
            }
            catch { }
        }

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
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var list = JsonConvert.DeserializeObject<List<PrintRecord>>(json);
                    if (list != null)
                    {
                        return list;
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
                File.WriteAllText(GetPrintHistoryJsonPath(), json);

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
                DateTime? selectedDate = historyDatePicker?.SelectedDate;

                var filtered = _allPrintRecords.Where(r =>
                {
                    if (!string.IsNullOrEmpty(searchCustomer))
                    {
                        if (string.IsNullOrEmpty(r.CustomerName) || !r.CustomerName.ToLower().Contains(searchCustomer))
                            return false;
                    }

                    if (selectedDate.HasValue)
                    {
                        if (r.Timestamp.Date != selectedDate.Value.Date)
                            return false;
                    }

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
                if (historyCountText != null)
                {
                    historyCountText.Text = $"Records: {displayList.Count}";
                }
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

        private void historyClearFilters_Click(object sender, RoutedEventArgs e)
        {
            if (historySearchCustomer != null) historySearchCustomer.Text = "";
            if (historyDatePicker != null) historyDatePicker.SelectedDate = null;
            ApplyHistoryFilters();
        }

        public void PrintReceipt(FlowDocument doc)
        {
            if (doc == null) return;

            PrintDialog printDlg = new PrintDialog();
            IDocumentPaginatorSource idpSource = doc;

            bool? print = printDlg.ShowDialog();
            if (print != null && (print ?? false))
            {
                printDlg.PrintDocument(idpSource.DocumentPaginator, "Printing Reciept.");
                printDlg.PrintDocument(idpSource.DocumentPaginator, "Printing.");
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

            MessageBoxResult res = MessageBox.Show(
                $"Do you want to open the record for '{customer}' in the Test tab?\n\nThis will load all details into the Test tab so you can edit and reprint.\n\nکیا آپ یہ ریکارڈ ٹیسٹ ٹیب میں کھولنا چاہتے ہیں؟",
                "Open Record / ریکارڈ کھولیں",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.Yes);

            if (res == MessageBoxResult.Yes)
            {
                PopulateTestTabFromRecord(record);
                updateTestCalculations();
                if (mainTabControl != null && testTab != null)
                {
                    mainTabControl.SelectedItem = testTab;
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
            if (historyDataGrid.SelectedItem is PrintRecordDisplay display)
            {
                PrintHistoryRecordById(display.Id);
            }
            else
            {
                MessageBox.Show("Please select a record from the history table to print.", "Print Receipt", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void historyDataGrid_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var dep = e.OriginalSource as DependencyObject;
            if (dep == null) return;

            // If user clicked inside a Button (Open or Print), let the Button's Click event handle it
            if (FindVisualParent<Button>(dep) != null) return;

            // If user clicked column header (sorting), ignore
            if (FindVisualParent<DataGridColumnHeader>(dep) != null) return;

            // If user clicked a scrollbar, ignore
            if (FindVisualParent<ScrollBar>(dep) != null) return;

            // Identify clicked row
            var row = FindVisualParent<DataGridRow>(dep);
            if (row != null && row.Item is PrintRecordDisplay display)
            {
                PromptAndOpenRecord(display.Id);
            }
        }

        private void historyDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dep = e.OriginalSource as DependencyObject;
            if (dep == null) return;
            if (FindVisualParent<Button>(dep) != null) return;
            if (FindVisualParent<DataGridColumnHeader>(dep) != null) return;

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
        }

        private void historyDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Row selection changes alone do not overwrite the Test tab.
            // Populating the Test tab requires user confirmation via PromptAndOpenRecord.
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

                // Item Type radio buttons
                if (radioSet != null) radioSet.IsChecked = string.Equals(record.ItemType, "Set", StringComparison.OrdinalIgnoreCase) || string.Equals(record.ItemType, "Necklace", StringComparison.OrdinalIgnoreCase);
                if (radioTops != null) radioTops.IsChecked = string.Equals(record.ItemType, "Tops", StringComparison.OrdinalIgnoreCase);
                if (radioChain != null) radioChain.IsChecked = string.Equals(record.ItemType, "Chain", StringComparison.OrdinalIgnoreCase);
                if (radioRing != null) radioRing.IsChecked = string.Equals(record.ItemType, "Ring", StringComparison.OrdinalIgnoreCase);
                if (radioChori != null) radioChori.IsChecked = string.Equals(record.ItemType, "Chori", StringComparison.OrdinalIgnoreCase);
                if (radioOther != null) radioOther.IsChecked = string.Equals(record.ItemType, "Other", StringComparison.OrdinalIgnoreCase);

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
                    notesTextboxTest.Text = record.Notes == "-" ? "" : (record.Notes ?? "");
                    if (doPrintNotes != null)
                        doPrintNotes.IsChecked = !string.IsNullOrWhiteSpace(notesTextboxTest.Text);
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

        public FlowDocument CreateFlowDocumentFromRecord(PrintRecord record)
        {
            if (record == null) return new FlowDocument();

            if (!string.IsNullOrWhiteSpace(record.ReceiptXaml))
            {
                try
                {
                    var parsed = (FlowDocument)System.Windows.Markup.XamlReader.Parse(record.ReceiptXaml);
                    if (parsed != null) return parsed;
                }
                catch { }
            }

            FlowDocument flowDoc = new FlowDocument();
            flowDoc.PageWidth = 288;
            flowDoc.PagePadding = new Thickness(12);
            flowDoc.FontFamily = new FontFamily("Cambria");
            flowDoc.FontSize = 16;
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

            // Notes Section
            if (!string.IsNullOrWhiteSpace(record.Notes) && record.Notes != "-")
            {
                flowDoc.Blocks.Add(CreateSeparator());
                Paragraph notesHeader = new Paragraph();
                notesHeader.Inlines.Add(new Bold(new Run("NOTES / نوٹ:")));
                notesHeader.FontSize = 13;
                notesHeader.Margin = new Thickness(0, 4, 0, 2);
                notesHeader.Foreground = Brushes.Black;
                flowDoc.Blocks.Add(notesHeader);

                Paragraph notesBody = new Paragraph(new Run(record.Notes))
                {
                    FontSize = 11,
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
                FontSize = 11,
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(0, 5, 0, 0)
            };
            flowDoc.Blocks.Add(footer);

            return flowDoc;
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
            if (notesTextboxTest != null && notesTextboxTest.IsFocused && doPrintNotes != null)
            {
                doPrintNotes.IsChecked = true;
            }

            if (notesTextboxTest == null || string.IsNullOrWhiteSpace(notesTextboxTest.Text)) return;

            ExtractAndApplyGoldRateFromNotes(notesTextboxTest.Text);
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

        private void itemTypeRadio_Checked(object sender, RoutedEventArgs e)
        {
            // Item type radio button selected
        }

        private void itemTypeRadio_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is RadioButton rb && rb.IsChecked == true)
            {
                rb.IsChecked = false;
                e.Handled = true;
            }
        }

        private string GetSelectedMetal()
        {
            if (radioSilver != null && radioSilver.IsChecked == true) return "Silver";
            return "Gold";
        }

        private string GetSelectedItemType()
        {
            if (radioSet != null && radioSet.IsChecked == true) return "Set";
            if (radioTops != null && radioTops.IsChecked == true) return "Tops";
            if (radioChain != null && radioChain.IsChecked == true) return "Chain";
            if (radioRing != null && radioRing.IsChecked == true) return "Ring";
            if (radioChori != null && radioChori.IsChecked == true) return "Chori";
            if (radioOther != null && radioOther.IsChecked == true) return "Other";
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
            SetAllCheckBoxesState(true);
        }

        private void DeselectAllCheckBoxes_Click(object sender, RoutedEventArgs e)
        {
            SetAllCheckBoxesState(false);
        }

        private void SetAllCheckBoxesState(bool isChecked)
        {
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
                if (historyDataGrid.SelectedItem is PrintRecordDisplay selectedDisplay)
                {
                    PrintHistoryRecordById(selectedDisplay.Id);
                    return;
                }
                else
                {
                    MessageBox.Show("Please select a record from the history table to print.", "Print Receipt", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            PrintDialog printDlg = new PrintDialog();

            DateTime printTime = DateTime.Now;
            FlowDocument doc = CreateFlowDocument(printTime);
            doc.Name = "FlowDoc";
            IDocumentPaginatorSource idpSource = doc;
            documentView dwin = new documentView(doc);
            dwin.Width = 288;
            //dwin.Show();

            bool? print = printDlg.ShowDialog();
            if (print != null && (print ?? false))
            {
                try
                {
                    printDlg.PrintDocument(idpSource.DocumentPaginator, "Printing Reciept.");
                    printDlg.PrintDocument(idpSource.DocumentPaginator, "Printing.");
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
            int pageWidth = 288;
            flowDoc.PageWidth = pageWidth;
            flowDoc.PagePadding = new Thickness(12);
            flowDoc.FontFamily = new FontFamily("Cambria");
            flowDoc.FontSize = 16;
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

            // Date and Time
            Paragraph dateParagraph = new Paragraph(new Run(date.ToString("dddd, MMMM dd, yyyy - HH:mm")))
            {
                TextAlignment = TextAlignment.Center,
                FontStyle = FontStyles.Italic,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10),
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
                customerParagraph.FontSize = 16;
                customerParagraph.Margin = new Thickness(0, 0, 0, 8);
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
                testHeader.FontSize = 14;
                testHeader.Margin = new Thickness(0, 8, 0, 5);
                testHeader.Foreground = Brushes.Black;
                flowDoc.Blocks.Add(testHeader);

                Table testTable = new Table() { CellSpacing = 2 };
                testTable.Columns.Add(new TableColumn() { Width = new GridLength(140) });
                testTable.Columns.Add(new TableColumn() { Width = new GridLength(100) });

                TableRowGroup testRowGroup = new TableRowGroup();

                if (doPrint(weightInputTextbox) && !string.IsNullOrWhiteSpace(weightInputTextbox?.Text) && weightInputTextbox.Text != "0")
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Weight:")) { Margin = new Thickness(0), FontWeight = FontWeights.Bold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"{weightInputTextbox.Text}g")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
                    testRowGroup.Rows.Add(row);
                }

                if (doPrint(takaTextbox) && !string.IsNullOrWhiteSpace(takaTextbox?.Text) && takaTextbox.Text != "0")
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
                    goldRateParagraph.Inlines.Add(new Run(goldRateDisplayText) { FontWeight = FontWeights.Bold, FontSize = 13 });
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
                notesHeader.FontSize = 13;
                notesHeader.Margin = new Thickness(0, 4, 0, 2);
                notesHeader.Foreground = Brushes.Black;
                flowDoc.Blocks.Add(notesHeader);

                Paragraph notesBody = new Paragraph(new Run(notesTextboxTest.Text))
                {
                    FontSize = 11,
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
                FontSize = 11,
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(0, 5, 0, 0)
            };
            flowDoc.Blocks.Add(footer);

            return flowDoc;
        }

        private Paragraph CreateSeparator()
        {
            return new Paragraph(new Run(new string('═', 35)))
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 12,
                Margin = new Thickness(0, 6, 0, 6),
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
            ClearAllTestFields();
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
            if (radioSet != null) radioSet.IsChecked = false;
            if (radioTops != null) radioTops.IsChecked = false;
            if (radioChain != null) radioChain.IsChecked = false;
            if (radioRing != null) radioRing.IsChecked = false;
            if (radioChori != null) radioChori.IsChecked = false;
            if (radioOther != null) radioOther.IsChecked = false;

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
        public string ReceiptXaml { get; set; } = "";
    }

    public class PrintRecordDisplay
    {
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
}