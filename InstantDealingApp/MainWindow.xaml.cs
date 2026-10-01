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
                    }
                    totalWeightTextbox.Focus();
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
                    totalWeightTextbox.Focus();
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
            }
            else if (mainTabControl.SelectedItem == historyTab)
            {
                _allPrintRecords = LoadPrintHistory();
                ApplyHistoryFilters();
            }
        }

        #region Print History & DataGrid

        private string GetPrintHistoryJsonPath()
        {
            return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "print_history.json");
        }

        private List<PrintRecord> _allPrintRecords = new List<PrintRecord>();

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

        private void RecordCurrentPrint()
        {
            try
            {
                PrintRecord record = new PrintRecord
                {
                    Timestamp = DateTime.Now
                };

                record.TabType = "Test";
                record.CustomerName = CapitalizeCustomerName(customersTest?.Text ?? "");
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

                SavePrintRecord(record);
            }
            catch { }
        }

        private void ApplyHistoryFilters()
        {
            if (historyDataGrid == null) return;

            if (_allPrintRecords == null || _allPrintRecords.Count == 0)
            {
                _allPrintRecords = LoadPrintHistory();
            }

            string searchCustomer = historySearchCustomer?.Text?.Trim().ToLower() ?? "";
            DateTime? selectedDate = historyDatePicker?.SelectedDate;
            string selectedTab = (historyTabFilter?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Tabs";

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

                if (selectedTab != "All Tabs" && !string.IsNullOrEmpty(selectedTab))
                {
                    if (!string.Equals(r.TabType, selectedTab, StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                return true;
            });

            var displayList = filtered.Select(r => new PrintRecordDisplay
            {
                Id = r.Id,
                Timestamp = r.Timestamp,
                CustomerName = string.IsNullOrWhiteSpace(r.CustomerName) ? "-" : r.CustomerName,
                TabType = string.IsNullOrWhiteSpace(r.TabType) ? "-" : r.TabType,
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

        private void historyFilter_Changed(object sender, RoutedEventArgs e)
        {
            ApplyHistoryFilters();
        }

        private void historyClearFilters_Click(object sender, RoutedEventArgs e)
        {
            if (historySearchCustomer != null) historySearchCustomer.Text = "";
            if (historyDatePicker != null) historyDatePicker.SelectedDate = null;
            if (historyTabFilter != null) historyTabFilter.SelectedIndex = 0;
            ApplyHistoryFilters();
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
        }

        private void goldRateDiffTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyGoldRateDifference();
        }

        private void notesTextboxTest_TextChanged(object sender, TextChangedEventArgs e)
        {
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

        private void SyncAllPurityFields(double rattiValue, object sourceControl)
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
            updateTestCalculations();
        }

        private void testTabInputs_TextChanged(object sender, TextChangedEventArgs e)
        {
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

                if (totalWeightTextbox != null)
                    double.TryParse(totalWeightTextbox.Text.Replace(",", "").Replace(" ", ""), out totalWeight);

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
            TextBox textBox = (TextBox)sender;
            textBox.Clear();
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
            PrintDialog printDlg = new PrintDialog();

            FlowDocument doc = CreateFlowDocument(DateTime.Now);
            doc.Name = "FlowDoc";
            IDocumentPaginatorSource idpSource = doc;
            documentView dwin = new documentView(doc);
            dwin.Width = 288;
            //dwin.Show();

            bool? print = printDlg.ShowDialog();
            if (print != null && (print ?? false))
            {
                printDlg.PrintDocument(idpSource.DocumentPaginator, "Printing Reciept.");
                printDlg.PrintDocument(idpSource.DocumentPaginator, "Printing.");
                RecordCurrentPrint();
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
            Paragraph dateParagraph = new Paragraph(new Run(DateTime.Now.ToString("dddd, MMMM dd, yyyy - HH:mm")))
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
                customerParagraph.Inlines.Add(new Run("Customer: ") { FontWeight = FontWeights.SemiBold });
                customerParagraph.Inlines.Add(new Bold(new Run(customerName)) { Foreground = Brushes.Black });
                customerParagraph.TextAlignment = TextAlignment.Left;
                customerParagraph.FontSize = 16;
                customerParagraph.Margin = new Thickness(0, 0, 0, 8);
                flowDoc.Blocks.Add(customerParagraph);
            }

            bool hasTransactionData = false;

            // Test Transaction Section
            bool shouldPrintTestSection = doPrint(totalWeightTextbox) || 
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

                Paragraph testHeader = new Paragraph();
                testHeader.Inlines.Add(new Underline(new Bold(new Run("TEST TRANSACTION"))));
                testHeader.TextAlignment = TextAlignment.Center;
                testHeader.FontSize = 18;
                testHeader.Margin = new Thickness(0, 8, 0, 5);
                testHeader.Foreground = Brushes.Black;
                flowDoc.Blocks.Add(testHeader);

                Table testTable = new Table() { CellSpacing = 2 };
                testTable.Columns.Add(new TableColumn() { Width = new GridLength(140) });
                testTable.Columns.Add(new TableColumn() { Width = new GridLength(100) });

                TableRowGroup testRowGroup = new TableRowGroup();

                if (doPrint(totalWeightTextbox) && !string.IsNullOrWhiteSpace(totalWeightTextbox?.Text) && totalWeightTextbox.Text != "0")
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Total Weight:")) { Margin = new Thickness(0), FontWeight = FontWeights.SemiBold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"{totalWeightTextbox.Text}g")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right }));
                    testRowGroup.Rows.Add(row);
                }

                if (doPrint(resultTextbox) && !string.IsNullOrWhiteSpace(resultTextbox?.Text) && resultTextbox.Text != "0")
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Result (Ratti):")) { Margin = new Thickness(0), FontWeight = FontWeights.SemiBold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(resultTextbox.Text)) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right }));
                    testRowGroup.Rows.Add(row);
                }

                if (doPrint(karatInputTextbox) && !string.IsNullOrWhiteSpace(karatInputTextbox?.Text) && karatInputTextbox.Text != "0")
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Karat:")) { Margin = new Thickness(0), FontWeight = FontWeights.SemiBold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"{karatInputTextbox.Text}K")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right }));
                    testRowGroup.Rows.Add(row);
                }

                if (doPrint(purityPercentTextbox) && !string.IsNullOrWhiteSpace(purityPercentTextbox?.Text) && purityPercentTextbox.Text != "0")
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Purity:")) { Margin = new Thickness(0), FontWeight = FontWeights.SemiBold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"{purityPercentTextbox.Text}%")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right }));
                    testRowGroup.Rows.Add(row);
                }

                if (doPrint(pureGoldTextbox) && !string.IsNullOrWhiteSpace(pureGoldTextbox?.Text) && pureGoldTextbox.Text != "0")
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Pure Gold:")) { Margin = new Thickness(0), FontWeight = FontWeights.SemiBold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"{pureGoldTextbox.Text}g")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.Bold }));
                    testRowGroup.Rows.Add(row);
                }

                if (doPrint(laborTextbox) && !string.IsNullOrWhiteSpace(laborTextbox?.Text) && laborTextbox.Text != "0")
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Labor Charges:")) { Margin = new Thickness(0), FontWeight = FontWeights.SemiBold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"Rs. {laborTextbox.Text}")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right }));
                    testRowGroup.Rows.Add(row);
                }

                if (doPrint(grossPureGoldCashTextbox) && !string.IsNullOrWhiteSpace(grossPureGoldCashTextbox?.Text) && grossPureGoldCashTextbox.Text != "0")
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Cash W/O Labor:")) { Margin = new Thickness(0), FontWeight = FontWeights.SemiBold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run($"Rs. {grossPureGoldCashTextbox.Text}")) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right }));
                    testRowGroup.Rows.Add(row);
                }

                if (doPrint(totalPureGoldCashTextbox) && !string.IsNullOrWhiteSpace(totalPureGoldCashTextbox?.Text) && totalPureGoldCashTextbox.Text != "0")
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run("Cash W/ Labor:")) { Margin = new Thickness(0), FontWeight = FontWeights.SemiBold }));
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
            }
        }

        private void customers_GotFocus(object sender, RoutedEventArgs e)
        {
            var names = LoadCustomerNames();
            if (customersTest != null) customersTest.ItemsSource = names;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (goldRateTextboxTest != null) goldRateTextboxTest.Text = "0";
            if (customersTest != null)
            {
                customersTest.SelectedIndex = -1;
                customersTest.Text = "";
            }

            totalWeightTextbox.Text = "0";
            resultTextbox.Text = "0";
            pureGoldTextbox.Text = "0";
            laborTextbox.Text = "0";
            _isLaborManuallyEdited = false;
            grossPureGoldCashTextbox.Text = "0";
            totalPureGoldCashTextbox.Text = "0";
            if (notesTextboxTest != null) notesTextboxTest.Text = "";
            if (sampleWeightTextbox != null) sampleWeightTextbox.Text = "0";
            if (resultWeightTextbox != null) resultWeightTextbox.Text = "0";
            if (karatInputTextbox != null) karatInputTextbox.Text = "0";
            if (rattiConversionTextbox != null) rattiConversionTextbox.Text = "0";
            if (purityPercentTextbox != null) purityPercentTextbox.Text = "0";
            if (purityRattiResultTextbox != null) purityRattiResultTextbox.Text = "0";
            if (goldRateDiffTextbox != null) goldRateDiffTextbox.Text = "0";
            if (doPrintKarat != null) doPrintKarat.IsChecked = false;
            if (doPrintPurityPercent != null) doPrintPurityPercent.IsChecked = false;
            _rawBaseGoldRate = 0;
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
    }

    public class PrintRecordDisplay
    {
        public string Id { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string FormattedDate => Timestamp.ToString("dd-MMM-yyyy hh:mm tt");
        public string CustomerName { get; set; } = "-";
        public string TabType { get; set; } = "-";
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