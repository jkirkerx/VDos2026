using System.Text.Json;
using vDosConfig.Services;

namespace vDosConfig.Forms
{
    public partial class MainForm : Form
    {
        private const int MinDosWindowSize = 5;
        private const int MaxDosWindowSize = 50;
        private const int DefaultDosWindowSize = 15;
        private const int DefaultXmemMb = 16;
        private const string DefaultXmemOption = "vDos default";
        private static readonly int[] XmemMbOptions = { 4, 8, 16, 32, 64 };
        private static readonly Color MutedPlum = Color.FromArgb(112, 82, 112);
        private static readonly JsonSerializerOptions SettingsJsonOptions = new() { WriteIndented = true };
        private static readonly string SettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "vDos 2026");
        private static readonly string SettingsPath = Path.Combine(SettingsFolder, "vDosConfig.settings.json");

        private readonly LptAssignment?[] _lptAssignments = new LptAssignment?[3];
        private ApplicationTarget? _applicationTarget;

        public MainForm()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            LoadWindowsPrinters();
            LoadWindowsPorts();

            ConfigureApplicationTargetPicker();
            ConfigureXmemPicker();

            checkBoxDosX.Checked = false;
            checkBoxFoxPro.Checked = false;
            checkBoxAppMouseOn.Checked = false;
            SetDosWindowSize(DefaultDosWindowSize);
            SetXmem(DefaultXmemMb);

            comboBoxLpt1PrinterType.SelectedItem = "None";
            comboBoxLpt2PrinterType.SelectedItem = "None";
            comboBoxLpt3PrinterType.SelectedItem = "None";

            WireIpAddressKeyDownHandlers();
            WirePortKeyDownHandlers();
            hScrollBarScale.ValueChanged += hScrollBarScale_ValueChanged;
            InitializeLptControlStates();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            LoadSettings();
        }
        private void WireIpAddressKeyDownHandlers()
        {
            textBoxLpt1IPAddress.KeyDown += TextBoxIpAddress_KeyDown;
            textBoxLpt2IPAddress.KeyDown += TextBoxIpAddress_KeyDown;
            textBoxLpt3IPAddress.KeyDown += TextBoxIpAddress_KeyDown;
        }

        private void TextBoxIpAddress_KeyDown(object? sender, KeyEventArgs e)
        {
            var isDigit = e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9;
            var isNumberPadDigit = e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9;
            var isPeriod = e.KeyCode == Keys.OemPeriod || e.KeyCode == Keys.Decimal;
            var isEditOrNavigationKey = e.KeyCode is Keys.Back or Keys.Delete or Keys.Tab or Keys.Left or Keys.Right or Keys.Home or Keys.End;
            var isAllowedShortcut = e.Control && e.KeyCode is Keys.A or Keys.C or Keys.V or Keys.X;

            if (isDigit || isNumberPadDigit || isPeriod || isEditOrNavigationKey || isAllowedShortcut)
                return;

            e.SuppressKeyPress = true;
        }
        private void WirePortKeyDownHandlers()
        {
            textBoxLpt1Port.KeyDown += TextBoxPort_KeyDown;
            textBoxLpt2Port.KeyDown += TextBoxPort_KeyDown;
            textBoxLpt3Port.KeyDown += TextBoxPort_KeyDown;
        }

        private void TextBoxPort_KeyDown(object? sender, KeyEventArgs e)
        {
            var isDigit = e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9;
            var isNumberPadDigit = e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9;
            var isEditOrNavigationKey = e.KeyCode is Keys.Back or Keys.Delete or Keys.Tab or Keys.Left or Keys.Right or Keys.Home or Keys.End;
            var isAllowedShortcut = e.Control && e.KeyCode is Keys.A or Keys.C or Keys.V or Keys.X;

            if (isDigit || isNumberPadDigit || isEditOrNavigationKey || isAllowedShortcut)
                return;

            e.SuppressKeyPress = true;
        }
        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return;

                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<ConfiguratorSettings>(json, SettingsJsonOptions);
                if (settings != null)
                    ApplySettings(settings);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Saved configurator settings could not be loaded.\r\n\r\n{ex.Message}",
                    "vDos Configurator",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void SaveSettings()
        {
            Directory.CreateDirectory(SettingsFolder);
            var json = JsonSerializer.Serialize(CaptureSettings(), SettingsJsonOptions);
            File.WriteAllText(SettingsPath, json);
        }

        private ConfiguratorSettings CaptureSettings()
        {
            var targetPath = NormalizeApplicationTargetPath(textBoxTargetpath.Text.Trim());
            return new ConfiguratorSettings
            {
                TargetPath = targetPath,
                EnableDosX = checkBoxDosX.Checked,
                EnableFoxPro = checkBoxFoxPro.Checked,
                MouseEnabled = checkBoxAppMouseOn.Checked,
                DosWindowSize = hScrollBarScale.Value,
                XmemMb = GetSelectedXmemMb(),
                LptDestinations = new[]
                {
                    (_lptAssignments[0] ?? LptAssignment.Dummy(1)).Destination,
                    (_lptAssignments[1] ?? LptAssignment.Dummy(2)).Destination,
                    (_lptAssignments[2] ?? LptAssignment.Dummy(3)).Destination
                }
            };
        }

        private void ApplySettings(ConfiguratorSettings settings)
        {
            checkBoxDosX.Checked = settings.EnableDosX;
            checkBoxFoxPro.Checked = settings.EnableFoxPro;
            checkBoxAppMouseOn.Checked = settings.MouseEnabled;
            SetDosWindowSize(settings.DosWindowSize <= 0 ? DefaultDosWindowSize : settings.DosWindowSize);
            SetXmem(settings.XmemMb <= 0 ? DefaultXmemMb : settings.XmemMb);

            for (var index = 0; index < Math.Min(settings.LptDestinations.Length, 3); index++)
            {
                var destination = settings.LptDestinations[index];
                if (!string.IsNullOrWhiteSpace(destination))
                    ApplyLoadedLptAssignment(index + 1, destination);
            }

            if (string.IsNullOrWhiteSpace(settings.TargetPath))
                return;

            var targetPath = NormalizeApplicationTargetPath(settings.TargetPath.Trim());
            textBoxTargetpath.Text = targetPath;
            var targetFolder = Path.GetDirectoryName(targetPath) ?? string.Empty;
            var startupCommand = Path.GetFileName(targetPath);
            _applicationTarget = new ApplicationTarget(targetPath, targetFolder, startupCommand, settings.EnableDosX, settings.EnableFoxPro);
        }

        private void LoadConfigSettings(string configPath)
        {
            if (!File.Exists(configPath))
                return;

            foreach (var rawLine in File.ReadAllLines(configPath))
            {
                var line = rawLine.Trim();
                if (!line.StartsWith("LPT", StringComparison.OrdinalIgnoreCase))
                {
                    if (line.StartsWith("WINDOW", StringComparison.OrdinalIgnoreCase))
                        ApplyLoadedDosWindowSize(line);
                    else if (line.StartsWith("MOUSE", StringComparison.OrdinalIgnoreCase))
                        ApplyLoadedMouseSetting(line);
                    else if (line.StartsWith("XMEM", StringComparison.OrdinalIgnoreCase))
                        ApplyLoadedXmemSetting(line);

                    continue;
                }

                var equalsIndex = line.IndexOf('=');
                if (equalsIndex < 0)
                    continue;

                var lptName = line[..equalsIndex].Trim();
                if (lptName.Length != 4 || !int.TryParse(lptName[3].ToString(), out var lptNumber) || lptNumber < 1 || lptNumber > 3)
                    continue;

                ApplyLoadedLptAssignment(lptNumber, line[(equalsIndex + 1)..].Trim());
            }
        }

        private void ApplyLoadedDosWindowSize(string line)
        {
            var equalsIndex = line.IndexOf('=');
            if (equalsIndex < 0)
                return;

            var value = line[(equalsIndex + 1)..].Trim();
            var commaIndex = value.IndexOf(',');
            if (commaIndex >= 0)
                value = value[..commaIndex].Trim();

            if (int.TryParse(value, out var dosWindowSize))
                SetDosWindowSize(dosWindowSize);
        }

        private void ApplyLoadedMouseSetting(string line)
        {
            var equalsIndex = line.IndexOf('=');
            if (equalsIndex < 0)
                return;

            var value = line[(equalsIndex + 1)..].Trim();
            checkBoxAppMouseOn.Checked = value.Equals("ON", StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyLoadedXmemSetting(string line)
        {
            var equalsIndex = line.IndexOf('=');
            if (equalsIndex < 0)
                return;

            var valueParts = line[(equalsIndex + 1)..]
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (valueParts.Length > 0 && int.TryParse(valueParts[0], out var xmemMb))
                SetXmem(xmemMb);
        }

        private void SetDosWindowSize(int value)
        {
            var clampedValue = Math.Clamp(value, MinDosWindowSize, MaxDosWindowSize);
            hScrollBarScale.Value = clampedValue;
            labelScaleValue.Text = clampedValue.ToString();
        }

        private void ConfigureXmemPicker()
        {
            cbXmem.DropDownStyle = ComboBoxStyle.DropDownList;
            cbXmem.Items.Clear();
            cbXmem.Items.Add(DefaultXmemOption);
            foreach (var xmemMb in XmemMbOptions)
                cbXmem.Items.Add(FormatXmemOption(xmemMb));
        }

        private void SetXmem(int xmemMb)
        {
            if (xmemMb == 0)
            {
                cbXmem.SelectedItem = DefaultXmemOption;
                return;
            }

            if (!XmemMbOptions.Contains(xmemMb))
                xmemMb = DefaultXmemMb;

            cbXmem.SelectedItem = FormatXmemOption(xmemMb);
        }

        private int GetSelectedXmemMb()
        {
            var selectedText = cbXmem.SelectedItem?.ToString() ?? cbXmem.Text;
            if (selectedText.Equals(DefaultXmemOption, StringComparison.OrdinalIgnoreCase))
                return 0;

            var firstPart = selectedText.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

            return int.TryParse(firstPart, out var xmemMb) ? xmemMb : DefaultXmemMb;
        }

        private static string FormatXmemOption(int xmemMb) => $"{xmemMb} MB XMS";

        private void hScrollBarScale_ValueChanged(object? sender, EventArgs e)
        {
            labelScaleValue.Text = hScrollBarScale.Value.ToString();
        }

        private void ApplyLoadedLptAssignment(int lptNumber, string destination)
        {
            var controls = GetLptControls(lptNumber);

            if (string.Equals(destination, "DUMMY", StringComparison.OrdinalIgnoreCase))
            {
                controls.PrinterTypeComboBox.SelectedItem = "None";
                ClearLptAssignmentFields(controls);
                _lptAssignments[lptNumber - 1] = null;
                return;
            }

            if (destination.StartsWith("TCP ", StringComparison.OrdinalIgnoreCase))
            {
                var endpoint = destination[4..].Trim();
                var separatorIndex = endpoint.LastIndexOf(':');
                if (separatorIndex > 0)
                {
                    controls.PrinterTypeComboBox.SelectedItem = "TCP/IP";
                    controls.IpAddressTextBox.Text = endpoint[..separatorIndex].Trim();
                    controls.TcpPortTextBox.Text = endpoint[(separatorIndex + 1)..].Trim();
                    controls.WindowsPrintersComboBox.SelectedIndex = -1;
                    controls.WindowsPortsComboBox.SelectedIndex = -1;
                    _lptAssignments[lptNumber - 1] = LptAssignment.FromConfigDestination(lptNumber, destination);
                }

                return;
            }

            if (destination.StartsWith("PRINTER ", StringComparison.OrdinalIgnoreCase))
            {
                var printerName = UnquoteConfigValue(destination[8..].Trim());
                controls.PrinterTypeComboBox.SelectedItem = "Windows Printer";
                controls.WindowsPortsComboBox.SelectedIndex = -1;
                SelectComboBoxValue(controls.WindowsPrintersComboBox, printerName);
                _lptAssignments[lptNumber - 1] = LptAssignment.FromConfigDestination(lptNumber, destination);
                return;
            }

            if (destination.StartsWith("PORT ", StringComparison.OrdinalIgnoreCase))
            {
                var portName = destination[5..].Trim();
                controls.PrinterTypeComboBox.SelectedItem = "Windows Port";
                controls.WindowsPrintersComboBox.SelectedIndex = -1;
                SelectComboBoxValue(controls.WindowsPortsComboBox, portName);
                _lptAssignments[lptNumber - 1] = LptAssignment.FromConfigDestination(lptNumber, destination);
            }
        }

        private void LoadAutoexecSettings(string autoexecPath)
        {
            if (!File.Exists(autoexecPath))
                return;

            var lines = File.ReadAllLines(autoexecPath);
            var targetFolder = string.Empty;
            var startupCommand = string.Empty;
            var enableDosX = false;
            var enableFoxPro = false;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                if (line.StartsWith("USE C:", StringComparison.OrdinalIgnoreCase))
                    targetFolder = RemoveTrailingBackslash(UnquoteBatchArgument(line[6..].Trim()));
                else if (line.StartsWith("CALL ", StringComparison.OrdinalIgnoreCase))
                    startupCommand = UnquoteBatchArgument(line[5..].Trim());
                else if (line.Equals("SET DOSX=-NOVM", StringComparison.OrdinalIgnoreCase))
                    enableDosX = true;
                else if (line.Equals("SET FOXPROX=-NOVM", StringComparison.OrdinalIgnoreCase))
                    enableFoxPro = true;
            }

            checkBoxDosX.Checked = enableDosX;
            checkBoxFoxPro.Checked = enableFoxPro;

            if (string.IsNullOrWhiteSpace(targetFolder) || string.IsNullOrWhiteSpace(startupCommand))
                return;

            var targetPath = NormalizeApplicationTargetPath(Path.Combine(targetFolder, startupCommand));
            textBoxTargetpath.Text = targetPath;
            _applicationTarget = new ApplicationTarget(targetPath, targetFolder, startupCommand, enableDosX, enableFoxPro);
        }

        private LptControls GetLptControls(int lptNumber) => lptNumber switch
        {
            1 => new LptControls(comboBoxLpt1PrinterType, textBoxLpt1IPAddress, textBoxLpt1Port, comboBoxLpt1WindowsPrinters, comboBoxLpt1WindowsPort),
            2 => new LptControls(comboBoxLpt2PrinterType, textBoxLpt2IPAddress, textBoxLpt2Port, comboBoxLpt2WindowsPrinters, comboBoxLpt2WindowsPort),
            3 => new LptControls(comboBoxLpt3PrinterType, textBoxLpt3IPAddress, textBoxLpt3Port, comboBoxLpt3WindowsPrinters, comboBoxLpt3WindowsPort),
            _ => throw new ArgumentOutOfRangeException(nameof(lptNumber))
        };

        private static void ClearLptAssignmentFields(LptControls controls)
        {
            controls.IpAddressTextBox.Clear();
            controls.TcpPortTextBox.Clear();
            controls.WindowsPrintersComboBox.SelectedIndex = -1;
            controls.WindowsPortsComboBox.SelectedIndex = -1;
        }

        private static void SelectComboBoxValue(ComboBox comboBox, string value)
        {
            for (var index = 0; index < comboBox.Items.Count; index++)
            {
                var itemValue = comboBox.GetItemText(comboBox.Items[index]);
                if (string.Equals(itemValue, value, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedIndex = index;
                    return;
                }
            }
        }

        private static string UnquoteConfigValue(string value)
        {
            if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
                return value[1..^1].Replace("\"\"", "\"");

            return value;
        }

        private static string UnquoteBatchArgument(string value)
        {
            if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
                return value[1..^1];

            return value;
        }

        private static string RemoveTrailingBackslash(string path) =>
            path.Length > 3 && path.EndsWith("\\", StringComparison.Ordinal) ? path[..^1] : path;
        private void InitializeLptControlStates()
        {
            SetLptControlsEnabled(
                textBoxLpt1IPAddress,
                textBoxLpt1Port,
                comboBoxLpt1WindowsPrinters,
                comboBoxLpt1WindowsPort,
                buttonLpt1Assign,
                ipAddressEnabled: false,
                portEnabled: false,
                windowsPrintersEnabled: false,
                windowsPortsEnabled: false,
                assignEnabled: false);

            SetLptControlsEnabled(
                textBoxLpt2IPAddress,
                textBoxLpt2Port,
                comboBoxLpt2WindowsPrinters,
                comboBoxLpt2WindowsPort,
                buttonLpt2Assign,
                ipAddressEnabled: false,
                portEnabled: false,
                windowsPrintersEnabled: false,
                windowsPortsEnabled: false,
                assignEnabled: false);

            SetLptControlsEnabled(
                textBoxLpt3IPAddress,
                textBoxLpt3Port,
                comboBoxLpt3WindowsPrinters,
                comboBoxLpt3WindowsPort,
                buttonLpt3Assign,
                ipAddressEnabled: false,
                portEnabled: false,
                windowsPrintersEnabled: false,
                windowsPortsEnabled: false,
                assignEnabled: false);
        }

        private static void SetLptControlsEnabled(
            TextBox ipAddressTextBox,
            TextBox portTextBox,
            ComboBox windowsPrintersComboBox,
            ComboBox windowsPortsComboBox,
            Button assignButton,
            bool ipAddressEnabled,
            bool portEnabled,
            bool windowsPrintersEnabled,
            bool windowsPortsEnabled,
            bool assignEnabled)
        {
            ipAddressTextBox.Enabled = ipAddressEnabled;
            portTextBox.Enabled = portEnabled;
            windowsPrintersComboBox.Enabled = windowsPrintersEnabled;
            windowsPortsComboBox.Enabled = windowsPortsEnabled;
            assignButton.Enabled = assignEnabled;
        }

        private void UpdateLptControlsForPrinterType(
            ComboBox printerTypeComboBox,
            TextBox ipAddressTextBox,
            TextBox portTextBox,
            ComboBox windowsPrintersComboBox,
            ComboBox windowsPortsComboBox,
            Button assignButton)
        {
            var printerType = printerTypeComboBox.SelectedItem?.ToString();
            var isNone = string.Equals(printerType, "None", StringComparison.OrdinalIgnoreCase);
            var isTcpIp = string.Equals(printerType, "TCP/IP", StringComparison.OrdinalIgnoreCase);
            var isWindowsPrinter = string.Equals(printerType, "Windows Printer", StringComparison.OrdinalIgnoreCase);
            var isWindowsPort = string.Equals(printerType, "Windows Port", StringComparison.OrdinalIgnoreCase);

            if (isNone)
            {
                ipAddressTextBox.Clear();
                portTextBox.Clear();
                windowsPrintersComboBox.SelectedIndex = -1;
                windowsPortsComboBox.SelectedIndex = -1;
            }
            else if (isTcpIp && string.IsNullOrWhiteSpace(portTextBox.Text))
                portTextBox.Text = "9100";

            SetLptControlsEnabled(
                ipAddressTextBox,
                portTextBox,
                windowsPrintersComboBox,
                windowsPortsComboBox,
                assignButton,
                ipAddressEnabled: isTcpIp,
                portEnabled: isTcpIp,
                windowsPrintersEnabled: isWindowsPrinter,
                windowsPortsEnabled: isWindowsPort,
                assignEnabled: isNone || isTcpIp || isWindowsPrinter || isWindowsPort);
        }

        private void comboBoxLpt1PrinterType_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateLptControlsForPrinterType(
                comboBoxLpt1PrinterType,
                textBoxLpt1IPAddress,
                textBoxLpt1Port,
                comboBoxLpt1WindowsPrinters,
                comboBoxLpt1WindowsPort,
                buttonLpt1Assign);
        }

        private void comboBoxLpt2PrinterType_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateLptControlsForPrinterType(
                comboBoxLpt2PrinterType,
                textBoxLpt2IPAddress,
                textBoxLpt2Port,
                comboBoxLpt2WindowsPrinters,
                comboBoxLpt2WindowsPort,
                buttonLpt2Assign);
        }

        private void comboBoxLpt3PrinterType_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateLptControlsForPrinterType(
                comboBoxLpt3PrinterType,
                textBoxLpt3IPAddress,
                textBoxLpt3Port,
                comboBoxLpt3WindowsPrinters,
                comboBoxLpt3WindowsPort,
                buttonLpt3Assign);
        }

        private void LoadWindowsPrinters()
        {
            IReadOnlyList<WindowsPrinter> printers;

            try
            {
                printers = WindowsPrinterDiscovery.GetInstalledPrinters();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Windows printers could not be queried.\r\n\r\n{ex.Message}",
                    "vDos Configurator",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                printers = Array.Empty<WindowsPrinter>();
            }

            LoadWindowsPrinterCombo(comboBoxLpt1WindowsPrinters, printers);
            LoadWindowsPrinterCombo(comboBoxLpt2WindowsPrinters, printers);
            LoadWindowsPrinterCombo(comboBoxLpt3WindowsPrinters, printers);
        }

        private void LoadWindowsPorts()
        {
            IReadOnlyList<WindowsPrinterPort> ports;

            try
            {
                ports = WindowsPrinterDiscovery.GetInstalledPorts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Windows printer ports could not be queried.\r\n\r\n{ex.Message}",
                    "vDos Configurator",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                ports = Array.Empty<WindowsPrinterPort>();
            }

            LoadWindowsPortCombo(comboBoxLpt1WindowsPort, ports);
            LoadWindowsPortCombo(comboBoxLpt2WindowsPort, ports);
            LoadWindowsPortCombo(comboBoxLpt3WindowsPort, ports);
        }

        private static void LoadWindowsPrinterCombo(ComboBox comboBox, IReadOnlyList<WindowsPrinter> printers)
        {
            comboBox.DisplayMember = nameof(WindowsPrinter.Name);
            comboBox.ValueMember = nameof(WindowsPrinter.Name);
            comboBox.DataSource = printers.ToArray();

            var defaultIndex = printers.ToList().FindIndex(printer => printer.IsDefault);
            if (defaultIndex >= 0)
                comboBox.SelectedIndex = defaultIndex;
        }

        private static void LoadWindowsPortCombo(ComboBox comboBox, IReadOnlyList<WindowsPrinterPort> ports)
        {
            comboBox.DisplayMember = nameof(WindowsPrinterPort.Name);
            comboBox.ValueMember = nameof(WindowsPrinterPort.Name);
            comboBox.DataSource = ports.ToArray();
            comboBox.SelectedIndex = -1;
        }

        private void ConfigureApplicationTargetPicker()
        {
            openFileDialog1.Title = "Select DOS application startup file";
            openFileDialog1.Filter = "DOS startup files (*.bat;*.cmd;*.exe;*.com)|*.bat;*.cmd;*.exe;*.com|Batch files (*.bat;*.cmd)|*.bat;*.cmd|Programs (*.exe;*.com)|*.exe;*.com|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;
            openFileDialog1.Multiselect = false;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.FileName = string.Empty;
        }

        private void buttonFindTarget_Click(object sender, EventArgs e)
        {
            var currentPath = textBoxTargetpath.Text.Trim();
            if (File.Exists(currentPath))
                openFileDialog1.InitialDirectory = Path.GetDirectoryName(currentPath);
            else if (Directory.Exists(currentPath))
                openFileDialog1.InitialDirectory = currentPath;

            textBoxTargetpath.Clear();
            openFileDialog1.FileName = string.Empty;

            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                textBoxTargetpath.Text = NormalizeApplicationTargetPath(openFileDialog1.FileName);
                DetectAndApplyFoxProSettings(textBoxTargetpath.Text, showAdvisory: true);
            }
        }

        private static string NormalizeApplicationTargetPath(string path)
        {
            foreach (var extension in new[] { ".bat", ".cmd", ".exe", ".com" })
            {
                var duplicateExtension = extension + extension;
                if (path.EndsWith(duplicateExtension, StringComparison.OrdinalIgnoreCase))
                    return path[..^extension.Length];
            }

            return path;
        }

        private void DetectAndApplyFoxProSettings(string targetPath, bool showAdvisory)
        {
            if (!IsLikelyFoxProApplication(targetPath))
                return;

            var changedSettings = !checkBoxFoxPro.Checked || !checkBoxDosX.Checked;
            checkBoxFoxPro.Checked = true;
            checkBoxDosX.Checked = true;

            if (showAdvisory && changedSettings)
                ShowFoxProCompatibilityAdvisory();
        }

        private void ShowFoxProCompatibilityAdvisory()
        {
            MessageBox.Show(
                this,
                "This target looks like a FoxPro application, so FoxPro support and DOSX compatibility were enabled.\r\n\r\nDOSX compatibility writes SET DOSX=-NOVM to help older protected-mode DOS runtimes behave reliably in vDos.",
                "vDos Configurator",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static bool IsLikelyFoxProApplication(string targetPath)
        {
            if (string.IsNullOrWhiteSpace(targetPath))
                return false;

            var targetFolder = File.Exists(targetPath)
                ? Path.GetDirectoryName(targetPath)
                : targetPath;

            if (string.IsNullOrWhiteSpace(targetFolder) || !Directory.Exists(targetFolder))
                return false;

            if (TargetFileMentionsFoxPro(targetPath))
                return true;

            var foxProFilePatterns = new[]
            {
                "foxpro*.exe", "fox*.esl", "foxuser.*", "vfp*.dll",
                "*.app", "*.fxp", "*.prg", "*.pjx", "*.pjt",
                "*.dbc", "*.dcx", "*.dct", "*.scx", "*.sct",
                "*.frx", "*.frt", "*.vcx", "*.vct"
            };

            try
            {
                return foxProFilePatterns.Any(pattern => Directory.EnumerateFiles(targetFolder, pattern, SearchOption.TopDirectoryOnly).Any());
            }
            catch
            {
                return false;
            }
        }

        private static bool TargetFileMentionsFoxPro(string targetPath)
        {
            if (!File.Exists(targetPath))
                return false;

            var extension = Path.GetExtension(targetPath);
            if (!string.Equals(extension, ".bat", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".cmd", StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                var text = File.ReadAllText(targetPath);
                var markers = new[] { "FOXPRO", "FOXPROX", "FOXW", "FOX.EXE", "FOXPRO.EXE", "VFP", "FOXUSER" };
                return markers.Any(marker => text.Contains(marker, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }
        private void buttonLpt1Assign_Click(object sender, EventArgs e) => AssignLpt(1, comboBoxLpt1PrinterType, textBoxLpt1IPAddress, textBoxLpt1Port, comboBoxLpt1WindowsPrinters, comboBoxLpt1WindowsPort, buttonLpt1Assign);

        private void buttonLpt2Assign_Click(object sender, EventArgs e) => AssignLpt(2, comboBoxLpt2PrinterType, textBoxLpt2IPAddress, textBoxLpt2Port, comboBoxLpt2WindowsPrinters, comboBoxLpt2WindowsPort, buttonLpt2Assign);

        private void buttonLpt3Assign_Click(object sender, EventArgs e) => AssignLpt(3, comboBoxLpt3PrinterType, textBoxLpt3IPAddress, textBoxLpt3Port, comboBoxLpt3WindowsPrinters, comboBoxLpt3WindowsPort, buttonLpt3Assign);

        private void AssignLpt(
            int lptNumber,
            ComboBox printerTypeComboBox,
            TextBox ipAddressTextBox,
            TextBox tcpPortTextBox,
            ComboBox windowsPrintersComboBox,
            ComboBox windowsPortsComboBox,
            Button assignButton)
        {
            if (!TryCreateLptAssignment(lptNumber, printerTypeComboBox, ipAddressTextBox, tcpPortTextBox, windowsPrintersComboBox, windowsPortsComboBox, out var assignment))
                return;

            _lptAssignments[lptNumber - 1] = assignment;
            ApplySessionAssignedButtonStyle(assignButton);
            var action = assignment.IsDummy ? "unassigned" : "assigned";
            MessageBox.Show(
                this,
                $"LPT{lptNumber} {action} in memory.\r\n\r\n{assignment.ToConfigLine()}",
                "vDos Configurator",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static void ApplySessionAssignedButtonStyle(Button assignButton)
        {
            assignButton.BackColor = MutedPlum;
            assignButton.FlatAppearance.MouseDownBackColor = MutedPlum;
            assignButton.FlatAppearance.MouseOverBackColor = MutedPlum;
        }

        private bool TryCreateLptAssignment(
            int lptNumber,
            ComboBox printerTypeComboBox,
            TextBox ipAddressTextBox,
            TextBox tcpPortTextBox,
            ComboBox windowsPrintersComboBox,
            ComboBox windowsPortsComboBox,
            out LptAssignment assignment)
        {
            assignment = LptAssignment.Dummy(lptNumber);
            var printerType = printerTypeComboBox.SelectedItem?.ToString();

            if (string.Equals(printerType, "None", StringComparison.OrdinalIgnoreCase))
            {
                assignment = LptAssignment.Dummy(lptNumber);
                return true;
            }

            if (string.Equals(printerType, "TCP/IP", StringComparison.OrdinalIgnoreCase))
            {
                var host = ipAddressTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(host))
                {
                    ShowAssignmentWarning($"Enter an IP address or host name for LPT{lptNumber}.");
                    return false;
                }

                if (!int.TryParse(tcpPortTextBox.Text.Trim(), out var tcpPort) || tcpPort < 1 || tcpPort > 65535)
                {
                    ShowAssignmentWarning($"Enter a valid TCP port for LPT{lptNumber}.");
                    return false;
                }

                assignment = LptAssignment.Tcp(lptNumber, host, tcpPort);
                return true;
            }

            if (string.Equals(printerType, "Windows Printer", StringComparison.OrdinalIgnoreCase))
            {
                var printerName = windowsPrintersComboBox.SelectedValue?.ToString() ?? windowsPrintersComboBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(printerName))
                {
                    ShowAssignmentWarning($"Select a Windows printer for LPT{lptNumber}.");
                    return false;
                }

                assignment = LptAssignment.WindowsPrinter(lptNumber, printerName);
                return true;
            }

            if (string.Equals(printerType, "Windows Port", StringComparison.OrdinalIgnoreCase))
            {
                var portName = windowsPortsComboBox.SelectedValue?.ToString() ?? windowsPortsComboBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(portName))
                {
                    ShowAssignmentWarning($"Select a Windows port for LPT{lptNumber}.");
                    return false;
                }

                assignment = LptAssignment.WindowsPort(lptNumber, portName);
                return true;
            }

            ShowAssignmentWarning($"Select a printer type for LPT{lptNumber} before assigning it.");
            return false;
        }

        private void ShowAssignmentWarning(string message)
        {
            MessageBox.Show(
                this,
                message,
                "vDos Configurator",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private void buttonAssignTarget_Click(object sender, EventArgs e)
        {
            var targetPath = NormalizeApplicationTargetPath(textBoxTargetpath.Text.Trim());
            textBoxTargetpath.Text = targetPath;
            DetectAndApplyFoxProSettings(targetPath, showAdvisory: true);

            if (!TryAssignApplicationTarget(targetPath, showSuccessMessage: true))
                return;

            ApplySessionAssignedButtonStyle(buttonAssignTarget);
        }

        private bool TryAssignApplicationTarget(string targetPath, bool showSuccessMessage)
        {
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                MessageBox.Show(
                    this,
                    "Select an application target first.",
                    "vDos Configurator",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return false;
            }

            if (!File.Exists(targetPath))
            {
                MessageBox.Show(
                    this,
                    $"The selected application target does not exist.\r\n\r\n{targetPath}",
                    "vDos Configurator",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            var targetFolder = Path.GetDirectoryName(targetPath) ?? string.Empty;
            var startupCommand = Path.GetFileName(targetPath);
            _applicationTarget = new ApplicationTarget(targetPath, targetFolder, startupCommand, checkBoxDosX.Checked, checkBoxFoxPro.Checked);

            if (showSuccessMessage)
            {
                MessageBox.Show(
                    this,
                    $"Application target assigned in memory.\r\n\r\nWindows folder: {targetFolder}\r\nStartup command: {startupCommand}",
                    "vDos Configurator",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            return true;
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            var targetPath = NormalizeApplicationTargetPath(textBoxTargetpath.Text.Trim());
            DetectAndApplyFoxProSettings(targetPath, showAdvisory: false);

            if (_applicationTarget == null ||
                !string.Equals(_applicationTarget.TargetPath, targetPath, StringComparison.OrdinalIgnoreCase) ||
                _applicationTarget.EnableDosX != checkBoxDosX.Checked ||
                _applicationTarget.EnableFoxPro != checkBoxFoxPro.Checked)
            {
                if (!TryAssignApplicationTarget(targetPath, showSuccessMessage: false))
                    return;
            }

            try
            {
                var outputFolder = AppContext.BaseDirectory;
                var configPath = Path.Combine(outputFolder, "config.txt");
                var autoexecPath = Path.Combine(outputFolder, "autoexec.txt");

                SaveSettings();
                File.WriteAllText(configPath, BuildConfigText());
                File.WriteAllText(autoexecPath, BuildAutoexecText(_applicationTarget!));

                MessageBox.Show(
                    this,
                    $"Configuration files written.\r\n\r\n{configPath}\r\n{autoexecPath}",
                    "vDos Configurator",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Configuration files could not be written.\r\n\r\n{ex.Message}",
                    "vDos Configurator",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private string BuildConfigText()
        {
            var lpt1 = _lptAssignments[0] ?? LptAssignment.Dummy(1);
            var lpt2 = _lptAssignments[1] ?? LptAssignment.Dummy(2);
            var lpt3 = _lptAssignments[2] ?? LptAssignment.Dummy(3);
            var lines = new List<string>
            {
                "rem vDos 2026 configuration file.",
                "rem This file was written by vDos Configurator.",
                "rem Changes made here may be replaced the next time the configurator writes settings.",
                "",
                "FRAME = ON",
                $"WINDOW = {hScrollBarScale.Value}"
            };

            var xmemMb = GetSelectedXmemMb();
            if (xmemMb > 0)
                lines.Add($"XMEM = {xmemMb} XMS");

            lines.AddRange(new[]
            {
                $"MOUSE = {(checkBoxAppMouseOn.Checked ? "ON" : "OFF")}",
                "",
                "REM Printing",
                "REM ========",
                lpt1.ToConfigLine(),
                lpt2.ToConfigLine(),
                lpt3.ToConfigLine(),
                ""
            });

            return string.Join(Environment.NewLine, lines);
        }

        private static string BuildAutoexecText(ApplicationTarget target)
        {
            var lines = new List<string>
            {
                "rem vDos 2026 startup file.",
                "rem This file was written by vDos Configurator.",
                "rem Changes made here may be replaced the next time the configurator writes settings.",
                "@ECHO OFF",
                "",
                "rem Map the application folder as vDos C:.",
                $"USE C: {QuoteBatchArgument(EnsureTrailingBackslash(target.TargetFolder))}",
                "C:",
                "CD \\"
            };

            if (target.EnableDosX)
                lines.Add("SET DOSX=-NOVM");

            if (target.EnableFoxPro)
                lines.Add("SET FOXPROX=-NOVM");

            lines.Add($"CALL {QuoteBatchArgument(target.StartupCommand)}");
            lines.Add("EXIT");
            lines.Add(string.Empty);

            return string.Join(Environment.NewLine, lines);
        }
        private static string EnsureTrailingBackslash(string path) =>
            path.EndsWith("\\", StringComparison.Ordinal) ? path : path + "\\";

        private static string QuoteConfigValue(string value) =>
            $"\"{value.Replace("\"", "\"\"")}\"";

        private static string QuoteBatchArgument(string value) =>
            value.Contains(' ') ? $"\"{value}\"" : value;

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            _applicationTarget = null;
            Array.Clear(_lptAssignments);
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private sealed record ApplicationTarget(string TargetPath, string TargetFolder, string StartupCommand, bool EnableDosX, bool EnableFoxPro);

        private sealed class ConfiguratorSettings
        {
            public string TargetPath { get; set; } = string.Empty;
            public bool EnableDosX { get; set; }
            public bool EnableFoxPro { get; set; }
            public bool MouseEnabled { get; set; }
            public int DosWindowSize { get; set; } = DefaultDosWindowSize;
            public int XmemMb { get; set; } = DefaultXmemMb;
            public string[] LptDestinations { get; set; } = { "DUMMY", "DUMMY", "DUMMY" };
        }


        private sealed record LptControls(
            ComboBox PrinterTypeComboBox,
            TextBox IpAddressTextBox,
            TextBox TcpPortTextBox,
            ComboBox WindowsPrintersComboBox,
            ComboBox WindowsPortsComboBox);

        private sealed class LptAssignment
        {
            private LptAssignment(int lptNumber, string destination)
            {
                LptNumber = lptNumber;
                Destination = destination;
            }

            private int LptNumber { get; }
            public string Destination { get; }
            public bool IsDummy => string.Equals(Destination, "DUMMY", StringComparison.OrdinalIgnoreCase);

            public static LptAssignment Dummy(int lptNumber) => new(lptNumber, "DUMMY");

            public static LptAssignment FromConfigDestination(int lptNumber, string destination) => new(lptNumber, destination);

            public static LptAssignment Tcp(int lptNumber, string host, int tcpPort) => new(lptNumber, $"TCP {host}:{tcpPort}");

            public static LptAssignment WindowsPrinter(int lptNumber, string printerName) => new(lptNumber, $"PRINTER {QuoteConfigValue(printerName)}");

            public static LptAssignment WindowsPort(int lptNumber, string portName) => new(lptNumber, $"PORT {portName}");

            public string ToConfigLine() => $"LPT{LptNumber} = {Destination}";
        }
    }
}



