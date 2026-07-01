namespace vDosConfig.Forms
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            panelHeader = new Panel();
            labelHeaderTitle = new Label();
            pictureBoxVDosLogo = new PictureBox();
            panelButtons = new Panel();
            tableLayoutPanelLptPorts = new TableLayoutPanel();
            panelLpt1 = new Panel();
            labelLpt1WindowsPorts = new Label();
            comboBoxLpt1WindowsPort = new ComboBox();
            labelLpt1 = new Label();
            labelLpt1PrinterType = new Label();
            comboBoxLpt1PrinterType = new ComboBox();
            labelLpt1IpAddress = new Label();
            textBoxLpt1IPAddress = new TextBox();
            labelLpt1Port = new Label();
            textBoxLpt1Port = new TextBox();
            labelLpt1WindowsPrinters = new Label();
            comboBoxLpt1WindowsPrinters = new ComboBox();
            buttonLpt1Assign = new Button();
            panelLpt2 = new Panel();
            labelLpt2WindowsPort = new Label();
            comboBoxLpt2WindowsPort = new ComboBox();
            labelLpt2 = new Label();
            labelLpt2PrinterType = new Label();
            comboBoxLpt2PrinterType = new ComboBox();
            labelLpt2IpAddress = new Label();
            textBoxLpt2IPAddress = new TextBox();
            labelLpt2Port = new Label();
            textBoxLpt2Port = new TextBox();
            labelLpt2WindowsPrinters = new Label();
            comboBoxLpt2WindowsPrinters = new ComboBox();
            buttonLpt2Assign = new Button();
            panelLpt3 = new Panel();
            labelLpt3WindowsPort = new Label();
            comboBoxLpt3WindowsPort = new ComboBox();
            labelLpt3 = new Label();
            labelLpt3PrinterType = new Label();
            comboBoxLpt3PrinterType = new ComboBox();
            labelLpt3IpAddress = new Label();
            textBoxLpt3IPAddress = new TextBox();
            labelLpt3Port = new Label();
            textBoxLpt3Port = new TextBox();
            labelLpt3WindowsPrinters = new Label();
            comboBoxLpt3WindowsPrinters = new ComboBox();
            buttonLpt3Assign = new Button();
            panelApplication = new Panel();
            labelScaleValue = new Label();
            labelAppScale = new Label();
            hScrollBarScale = new HScrollBar();
            checkBoxFoxPro = new CheckBox();
            checkBoxDosX = new CheckBox();
            buttonAssignTarget = new Button();
            buttonFindTarget = new Button();
            textBoxTargetpath = new TextBox();
            labelTargetPath = new Label();
            labelApplication = new Label();
            tableLayoutPanelButtons = new TableLayoutPanel();
            buttonOK = new Button();
            buttonCancel = new Button();
            openFileDialog1 = new OpenFileDialog();
            checkBoxAppMouseOn = new CheckBox();
            panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxVDosLogo).BeginInit();
            panelButtons.SuspendLayout();
            tableLayoutPanelLptPorts.SuspendLayout();
            panelLpt1.SuspendLayout();
            panelLpt2.SuspendLayout();
            panelLpt3.SuspendLayout();
            panelApplication.SuspendLayout();
            tableLayoutPanelButtons.SuspendLayout();
            SuspendLayout();
            // 
            // panelHeader
            // 
            panelHeader.BackColor = Color.FromArgb(38, 38, 38);
            panelHeader.Controls.Add(labelHeaderTitle);
            panelHeader.Controls.Add(pictureBoxVDosLogo);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Name = "panelHeader";
            panelHeader.Size = new Size(1087, 96);
            panelHeader.TabIndex = 0;
            // 
            // labelHeaderTitle
            // 
            labelHeaderTitle.AutoSize = true;
            labelHeaderTitle.Font = new Font("Segoe UI Semibold", 27.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            labelHeaderTitle.ForeColor = Color.White;
            labelHeaderTitle.Location = new Point(97, 23);
            labelHeaderTitle.Name = "labelHeaderTitle";
            labelHeaderTitle.Size = new Size(436, 50);
            labelHeaderTitle.TabIndex = 1;
            labelHeaderTitle.Text = "vDOS 2026 Configurator";
            // 
            // pictureBoxVDosLogo
            // 
            pictureBoxVDosLogo.Image = Properties.Resources.vDosLogo512;
            pictureBoxVDosLogo.Location = new Point(21, 18);
            pictureBoxVDosLogo.Name = "pictureBoxVDosLogo";
            pictureBoxVDosLogo.Size = new Size(60, 60);
            pictureBoxVDosLogo.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBoxVDosLogo.TabIndex = 0;
            pictureBoxVDosLogo.TabStop = false;
            // 
            // panelButtons
            // 
            panelButtons.BackColor = Color.FromArgb(51, 51, 51);
            panelButtons.Controls.Add(tableLayoutPanelLptPorts);
            panelButtons.Controls.Add(tableLayoutPanelButtons);
            panelButtons.Dock = DockStyle.Fill;
            panelButtons.Location = new Point(0, 96);
            panelButtons.Name = "panelButtons";
            panelButtons.Size = new Size(1087, 586);
            panelButtons.TabIndex = 1;
            // 
            // tableLayoutPanelLptPorts
            // 
            tableLayoutPanelLptPorts.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tableLayoutPanelLptPorts.ColumnCount = 4;
            tableLayoutPanelLptPorts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayoutPanelLptPorts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayoutPanelLptPorts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayoutPanelLptPorts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tableLayoutPanelLptPorts.Controls.Add(panelLpt1, 0, 0);
            tableLayoutPanelLptPorts.Controls.Add(panelLpt2, 1, 0);
            tableLayoutPanelLptPorts.Controls.Add(panelLpt3, 2, 0);
            tableLayoutPanelLptPorts.Controls.Add(panelApplication, 3, 0);
            tableLayoutPanelLptPorts.Location = new Point(21, 19);
            tableLayoutPanelLptPorts.Name = "tableLayoutPanelLptPorts";
            tableLayoutPanelLptPorts.RowCount = 1;
            tableLayoutPanelLptPorts.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelLptPorts.Size = new Size(1040, 444);
            tableLayoutPanelLptPorts.TabIndex = 0;
            // 
            // panelLpt1
            // 
            panelLpt1.BackColor = Color.FromArgb(45, 45, 45);
            panelLpt1.BorderStyle = BorderStyle.FixedSingle;
            panelLpt1.Controls.Add(labelLpt1WindowsPorts);
            panelLpt1.Controls.Add(comboBoxLpt1WindowsPort);
            panelLpt1.Controls.Add(labelLpt1);
            panelLpt1.Controls.Add(labelLpt1PrinterType);
            panelLpt1.Controls.Add(comboBoxLpt1PrinterType);
            panelLpt1.Controls.Add(labelLpt1IpAddress);
            panelLpt1.Controls.Add(textBoxLpt1IPAddress);
            panelLpt1.Controls.Add(labelLpt1Port);
            panelLpt1.Controls.Add(textBoxLpt1Port);
            panelLpt1.Controls.Add(labelLpt1WindowsPrinters);
            panelLpt1.Controls.Add(comboBoxLpt1WindowsPrinters);
            panelLpt1.Controls.Add(buttonLpt1Assign);
            panelLpt1.Dock = DockStyle.Fill;
            panelLpt1.Location = new Point(3, 3);
            panelLpt1.Name = "panelLpt1";
            panelLpt1.Size = new Size(254, 438);
            panelLpt1.TabIndex = 0;
            // 
            // labelLpt1WindowsPorts
            // 
            labelLpt1WindowsPorts.AutoSize = true;
            labelLpt1WindowsPorts.Font = new Font("Segoe UI", 10F);
            labelLpt1WindowsPorts.ForeColor = Color.White;
            labelLpt1WindowsPorts.Location = new Point(24, 280);
            labelLpt1WindowsPorts.Name = "labelLpt1WindowsPorts";
            labelLpt1WindowsPorts.Size = new Size(100, 19);
            labelLpt1WindowsPorts.TabIndex = 10;
            labelLpt1WindowsPorts.Text = "Windows Ports";
            // 
            // comboBoxLpt1WindowsPort
            // 
            comboBoxLpt1WindowsPort.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboBoxLpt1WindowsPort.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxLpt1WindowsPort.FlatStyle = FlatStyle.Flat;
            comboBoxLpt1WindowsPort.Font = new Font("Segoe UI", 11F);
            comboBoxLpt1WindowsPort.FormattingEnabled = true;
            comboBoxLpt1WindowsPort.Location = new Point(20, 307);
            comboBoxLpt1WindowsPort.Name = "comboBoxLpt1WindowsPort";
            comboBoxLpt1WindowsPort.Size = new Size(216, 28);
            comboBoxLpt1WindowsPort.TabIndex = 5;
            // 
            // labelLpt1
            // 
            labelLpt1.AutoSize = true;
            labelLpt1.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            labelLpt1.ForeColor = Color.White;
            labelLpt1.Location = new Point(20, 14);
            labelLpt1.Name = "labelLpt1";
            labelLpt1.Size = new Size(69, 32);
            labelLpt1.TabIndex = 0;
            labelLpt1.Text = "LPT1:";
            // 
            // labelLpt1PrinterType
            // 
            labelLpt1PrinterType.AutoSize = true;
            labelLpt1PrinterType.Font = new Font("Segoe UI", 10F);
            labelLpt1PrinterType.ForeColor = Color.White;
            labelLpt1PrinterType.Location = new Point(24, 61);
            labelLpt1PrinterType.Name = "labelLpt1PrinterType";
            labelLpt1PrinterType.Size = new Size(85, 19);
            labelLpt1PrinterType.TabIndex = 1;
            labelLpt1PrinterType.Text = "Printer Type:";
            // 
            // comboBoxLpt1PrinterType
            // 
            comboBoxLpt1PrinterType.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboBoxLpt1PrinterType.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxLpt1PrinterType.FlatStyle = FlatStyle.Flat;
            comboBoxLpt1PrinterType.Font = new Font("Segoe UI", 11F);
            comboBoxLpt1PrinterType.FormattingEnabled = true;
            comboBoxLpt1PrinterType.Items.AddRange(new object[] { "None", "TCP/IP", "Windows Printer", "Windows Port" });
            comboBoxLpt1PrinterType.Location = new Point(20, 86);
            comboBoxLpt1PrinterType.Name = "comboBoxLpt1PrinterType";
            comboBoxLpt1PrinterType.Size = new Size(216, 28);
            comboBoxLpt1PrinterType.TabIndex = 1;
            comboBoxLpt1PrinterType.SelectedIndexChanged += comboBoxLpt1PrinterType_SelectedIndexChanged;
            // 
            // labelLpt1IpAddress
            // 
            labelLpt1IpAddress.AutoSize = true;
            labelLpt1IpAddress.Font = new Font("Segoe UI", 10F);
            labelLpt1IpAddress.ForeColor = Color.White;
            labelLpt1IpAddress.Location = new Point(24, 132);
            labelLpt1IpAddress.Name = "labelLpt1IpAddress";
            labelLpt1IpAddress.Size = new Size(74, 19);
            labelLpt1IpAddress.TabIndex = 3;
            labelLpt1IpAddress.Text = "IP Address";
            // 
            // textBoxLpt1IPAddress
            // 
            textBoxLpt1IPAddress.Font = new Font("Segoe UI", 11F);
            textBoxLpt1IPAddress.Location = new Point(20, 158);
            textBoxLpt1IPAddress.Name = "textBoxLpt1IPAddress";
            textBoxLpt1IPAddress.Size = new Size(154, 27);
            textBoxLpt1IPAddress.TabIndex = 2;
            // 
            // labelLpt1Port
            // 
            labelLpt1Port.AutoSize = true;
            labelLpt1Port.Font = new Font("Segoe UI", 10F);
            labelLpt1Port.ForeColor = Color.White;
            labelLpt1Port.Location = new Point(179, 132);
            labelLpt1Port.Name = "labelLpt1Port";
            labelLpt1Port.Size = new Size(34, 19);
            labelLpt1Port.TabIndex = 5;
            labelLpt1Port.Text = "Port";
            // 
            // textBoxLpt1Port
            // 
            textBoxLpt1Port.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            textBoxLpt1Port.Font = new Font("Segoe UI", 11F);
            textBoxLpt1Port.Location = new Point(179, 158);
            textBoxLpt1Port.Name = "textBoxLpt1Port";
            textBoxLpt1Port.Size = new Size(56, 27);
            textBoxLpt1Port.TabIndex = 3;
            // 
            // labelLpt1WindowsPrinters
            // 
            labelLpt1WindowsPrinters.AutoSize = true;
            labelLpt1WindowsPrinters.Font = new Font("Segoe UI", 10F);
            labelLpt1WindowsPrinters.ForeColor = Color.White;
            labelLpt1WindowsPrinters.Location = new Point(24, 207);
            labelLpt1WindowsPrinters.Name = "labelLpt1WindowsPrinters";
            labelLpt1WindowsPrinters.Size = new Size(116, 19);
            labelLpt1WindowsPrinters.TabIndex = 7;
            labelLpt1WindowsPrinters.Text = "Windows Printers";
            // 
            // comboBoxLpt1WindowsPrinters
            // 
            comboBoxLpt1WindowsPrinters.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboBoxLpt1WindowsPrinters.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxLpt1WindowsPrinters.FlatStyle = FlatStyle.Flat;
            comboBoxLpt1WindowsPrinters.Font = new Font("Segoe UI", 11F);
            comboBoxLpt1WindowsPrinters.FormattingEnabled = true;
            comboBoxLpt1WindowsPrinters.Location = new Point(20, 233);
            comboBoxLpt1WindowsPrinters.Name = "comboBoxLpt1WindowsPrinters";
            comboBoxLpt1WindowsPrinters.Size = new Size(216, 28);
            comboBoxLpt1WindowsPrinters.TabIndex = 4;
            // 
            // buttonLpt1Assign
            // 
            buttonLpt1Assign.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            buttonLpt1Assign.BackColor = Color.FromArgb(60, 60, 60);
            buttonLpt1Assign.FlatAppearance.BorderSize = 0;
            buttonLpt1Assign.FlatAppearance.MouseDownBackColor = Color.RoyalBlue;
            buttonLpt1Assign.FlatAppearance.MouseOverBackColor = Color.RoyalBlue;
            buttonLpt1Assign.FlatStyle = FlatStyle.Flat;
            buttonLpt1Assign.Font = new Font("Segoe UI", 10F);
            buttonLpt1Assign.ForeColor = Color.White;
            buttonLpt1Assign.Location = new Point(19, 387);
            buttonLpt1Assign.Name = "buttonLpt1Assign";
            buttonLpt1Assign.Size = new Size(215, 36);
            buttonLpt1Assign.TabIndex = 6;
            buttonLpt1Assign.Text = "Assign LPT1";
            buttonLpt1Assign.UseVisualStyleBackColor = false;
            buttonLpt1Assign.Click += buttonLpt1Assign_Click;
            // 
            // panelLpt2
            // 
            panelLpt2.BackColor = Color.FromArgb(45, 45, 45);
            panelLpt2.BorderStyle = BorderStyle.FixedSingle;
            panelLpt2.Controls.Add(labelLpt2WindowsPort);
            panelLpt2.Controls.Add(comboBoxLpt2WindowsPort);
            panelLpt2.Controls.Add(labelLpt2);
            panelLpt2.Controls.Add(labelLpt2PrinterType);
            panelLpt2.Controls.Add(comboBoxLpt2PrinterType);
            panelLpt2.Controls.Add(labelLpt2IpAddress);
            panelLpt2.Controls.Add(textBoxLpt2IPAddress);
            panelLpt2.Controls.Add(labelLpt2Port);
            panelLpt2.Controls.Add(textBoxLpt2Port);
            panelLpt2.Controls.Add(labelLpt2WindowsPrinters);
            panelLpt2.Controls.Add(comboBoxLpt2WindowsPrinters);
            panelLpt2.Controls.Add(buttonLpt2Assign);
            panelLpt2.Dock = DockStyle.Fill;
            panelLpt2.Location = new Point(263, 3);
            panelLpt2.Name = "panelLpt2";
            panelLpt2.Size = new Size(254, 438);
            panelLpt2.TabIndex = 1;
            // 
            // labelLpt2WindowsPort
            // 
            labelLpt2WindowsPort.AutoSize = true;
            labelLpt2WindowsPort.Font = new Font("Segoe UI", 10F);
            labelLpt2WindowsPort.ForeColor = Color.White;
            labelLpt2WindowsPort.Location = new Point(20, 280);
            labelLpt2WindowsPort.Name = "labelLpt2WindowsPort";
            labelLpt2WindowsPort.Size = new Size(100, 19);
            labelLpt2WindowsPort.TabIndex = 12;
            labelLpt2WindowsPort.Text = "Windows Ports";
            // 
            // comboBoxLpt2WindowsPort
            // 
            comboBoxLpt2WindowsPort.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboBoxLpt2WindowsPort.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxLpt2WindowsPort.FlatStyle = FlatStyle.Flat;
            comboBoxLpt2WindowsPort.Font = new Font("Segoe UI", 11F);
            comboBoxLpt2WindowsPort.FormattingEnabled = true;
            comboBoxLpt2WindowsPort.Location = new Point(17, 307);
            comboBoxLpt2WindowsPort.Name = "comboBoxLpt2WindowsPort";
            comboBoxLpt2WindowsPort.Size = new Size(219, 28);
            comboBoxLpt2WindowsPort.TabIndex = 11;
            // 
            // labelLpt2
            // 
            labelLpt2.AutoSize = true;
            labelLpt2.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            labelLpt2.ForeColor = Color.White;
            labelLpt2.Location = new Point(20, 14);
            labelLpt2.Name = "labelLpt2";
            labelLpt2.Size = new Size(69, 32);
            labelLpt2.TabIndex = 0;
            labelLpt2.Text = "LPT2:";
            // 
            // labelLpt2PrinterType
            // 
            labelLpt2PrinterType.AutoSize = true;
            labelLpt2PrinterType.Font = new Font("Segoe UI", 10F);
            labelLpt2PrinterType.ForeColor = Color.White;
            labelLpt2PrinterType.Location = new Point(24, 61);
            labelLpt2PrinterType.Name = "labelLpt2PrinterType";
            labelLpt2PrinterType.Size = new Size(85, 19);
            labelLpt2PrinterType.TabIndex = 1;
            labelLpt2PrinterType.Text = "Printer Type:";
            // 
            // comboBoxLpt2PrinterType
            // 
            comboBoxLpt2PrinterType.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboBoxLpt2PrinterType.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxLpt2PrinterType.FlatStyle = FlatStyle.Flat;
            comboBoxLpt2PrinterType.Font = new Font("Segoe UI", 11F);
            comboBoxLpt2PrinterType.FormattingEnabled = true;
            comboBoxLpt2PrinterType.Items.AddRange(new object[] { "None", "TCP/IP", "Windows Printer", "Windows Port" });
            comboBoxLpt2PrinterType.Location = new Point(20, 86);
            comboBoxLpt2PrinterType.Name = "comboBoxLpt2PrinterType";
            comboBoxLpt2PrinterType.Size = new Size(216, 28);
            comboBoxLpt2PrinterType.TabIndex = 7;
            comboBoxLpt2PrinterType.SelectedIndexChanged += comboBoxLpt2PrinterType_SelectedIndexChanged;
            // 
            // labelLpt2IpAddress
            // 
            labelLpt2IpAddress.AutoSize = true;
            labelLpt2IpAddress.Font = new Font("Segoe UI", 10F);
            labelLpt2IpAddress.ForeColor = Color.White;
            labelLpt2IpAddress.Location = new Point(24, 132);
            labelLpt2IpAddress.Name = "labelLpt2IpAddress";
            labelLpt2IpAddress.Size = new Size(74, 19);
            labelLpt2IpAddress.TabIndex = 3;
            labelLpt2IpAddress.Text = "IP Address";
            // 
            // textBoxLpt2IPAddress
            // 
            textBoxLpt2IPAddress.Font = new Font("Segoe UI", 11F);
            textBoxLpt2IPAddress.Location = new Point(20, 158);
            textBoxLpt2IPAddress.Name = "textBoxLpt2IPAddress";
            textBoxLpt2IPAddress.Size = new Size(154, 27);
            textBoxLpt2IPAddress.TabIndex = 8;
            // 
            // labelLpt2Port
            // 
            labelLpt2Port.AutoSize = true;
            labelLpt2Port.Font = new Font("Segoe UI", 10F);
            labelLpt2Port.ForeColor = Color.White;
            labelLpt2Port.Location = new Point(178, 132);
            labelLpt2Port.Name = "labelLpt2Port";
            labelLpt2Port.Size = new Size(34, 19);
            labelLpt2Port.TabIndex = 5;
            labelLpt2Port.Text = "Port";
            // 
            // textBoxLpt2Port
            // 
            textBoxLpt2Port.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            textBoxLpt2Port.Font = new Font("Segoe UI", 11F);
            textBoxLpt2Port.Location = new Point(178, 158);
            textBoxLpt2Port.Name = "textBoxLpt2Port";
            textBoxLpt2Port.Size = new Size(57, 27);
            textBoxLpt2Port.TabIndex = 9;
            // 
            // labelLpt2WindowsPrinters
            // 
            labelLpt2WindowsPrinters.AutoSize = true;
            labelLpt2WindowsPrinters.Font = new Font("Segoe UI", 10F);
            labelLpt2WindowsPrinters.ForeColor = Color.White;
            labelLpt2WindowsPrinters.Location = new Point(24, 207);
            labelLpt2WindowsPrinters.Name = "labelLpt2WindowsPrinters";
            labelLpt2WindowsPrinters.Size = new Size(116, 19);
            labelLpt2WindowsPrinters.TabIndex = 7;
            labelLpt2WindowsPrinters.Text = "Windows Printers";
            // 
            // comboBoxLpt2WindowsPrinters
            // 
            comboBoxLpt2WindowsPrinters.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboBoxLpt2WindowsPrinters.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxLpt2WindowsPrinters.FlatStyle = FlatStyle.Flat;
            comboBoxLpt2WindowsPrinters.Font = new Font("Segoe UI", 11F);
            comboBoxLpt2WindowsPrinters.FormattingEnabled = true;
            comboBoxLpt2WindowsPrinters.Location = new Point(20, 233);
            comboBoxLpt2WindowsPrinters.Name = "comboBoxLpt2WindowsPrinters";
            comboBoxLpt2WindowsPrinters.Size = new Size(216, 28);
            comboBoxLpt2WindowsPrinters.TabIndex = 10;
            // 
            // buttonLpt2Assign
            // 
            buttonLpt2Assign.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            buttonLpt2Assign.BackColor = Color.FromArgb(60, 60, 60);
            buttonLpt2Assign.FlatAppearance.BorderSize = 0;
            buttonLpt2Assign.FlatAppearance.MouseDownBackColor = Color.RoyalBlue;
            buttonLpt2Assign.FlatAppearance.MouseOverBackColor = Color.RoyalBlue;
            buttonLpt2Assign.FlatStyle = FlatStyle.Flat;
            buttonLpt2Assign.Font = new Font("Segoe UI", 10F);
            buttonLpt2Assign.ForeColor = Color.White;
            buttonLpt2Assign.Location = new Point(16, 387);
            buttonLpt2Assign.Name = "buttonLpt2Assign";
            buttonLpt2Assign.Size = new Size(219, 36);
            buttonLpt2Assign.TabIndex = 12;
            buttonLpt2Assign.Text = "Assign LPT2";
            buttonLpt2Assign.UseVisualStyleBackColor = false;
            buttonLpt2Assign.Click += buttonLpt2Assign_Click;
            // 
            // panelLpt3
            // 
            panelLpt3.BackColor = Color.FromArgb(45, 45, 45);
            panelLpt3.BorderStyle = BorderStyle.FixedSingle;
            panelLpt3.Controls.Add(labelLpt3WindowsPort);
            panelLpt3.Controls.Add(comboBoxLpt3WindowsPort);
            panelLpt3.Controls.Add(labelLpt3);
            panelLpt3.Controls.Add(labelLpt3PrinterType);
            panelLpt3.Controls.Add(comboBoxLpt3PrinterType);
            panelLpt3.Controls.Add(labelLpt3IpAddress);
            panelLpt3.Controls.Add(textBoxLpt3IPAddress);
            panelLpt3.Controls.Add(labelLpt3Port);
            panelLpt3.Controls.Add(textBoxLpt3Port);
            panelLpt3.Controls.Add(labelLpt3WindowsPrinters);
            panelLpt3.Controls.Add(comboBoxLpt3WindowsPrinters);
            panelLpt3.Controls.Add(buttonLpt3Assign);
            panelLpt3.Dock = DockStyle.Fill;
            panelLpt3.Location = new Point(523, 3);
            panelLpt3.Name = "panelLpt3";
            panelLpt3.Size = new Size(254, 438);
            panelLpt3.TabIndex = 2;
            // 
            // labelLpt3WindowsPort
            // 
            labelLpt3WindowsPort.AutoSize = true;
            labelLpt3WindowsPort.Font = new Font("Segoe UI", 10F);
            labelLpt3WindowsPort.ForeColor = Color.White;
            labelLpt3WindowsPort.Location = new Point(24, 280);
            labelLpt3WindowsPort.Name = "labelLpt3WindowsPort";
            labelLpt3WindowsPort.Size = new Size(100, 19);
            labelLpt3WindowsPort.TabIndex = 12;
            labelLpt3WindowsPort.Text = "Windows Ports";
            // 
            // comboBoxLpt3WindowsPort
            // 
            comboBoxLpt3WindowsPort.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboBoxLpt3WindowsPort.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxLpt3WindowsPort.FlatStyle = FlatStyle.Flat;
            comboBoxLpt3WindowsPort.Font = new Font("Segoe UI", 11F);
            comboBoxLpt3WindowsPort.FormattingEnabled = true;
            comboBoxLpt3WindowsPort.Location = new Point(20, 307);
            comboBoxLpt3WindowsPort.Name = "comboBoxLpt3WindowsPort";
            comboBoxLpt3WindowsPort.Size = new Size(215, 28);
            comboBoxLpt3WindowsPort.TabIndex = 17;
            // 
            // labelLpt3
            // 
            labelLpt3.AutoSize = true;
            labelLpt3.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            labelLpt3.ForeColor = Color.White;
            labelLpt3.Location = new Point(20, 14);
            labelLpt3.Name = "labelLpt3";
            labelLpt3.Size = new Size(69, 32);
            labelLpt3.TabIndex = 0;
            labelLpt3.Text = "LPT3:";
            // 
            // labelLpt3PrinterType
            // 
            labelLpt3PrinterType.AutoSize = true;
            labelLpt3PrinterType.Font = new Font("Segoe UI", 10F);
            labelLpt3PrinterType.ForeColor = Color.White;
            labelLpt3PrinterType.Location = new Point(24, 61);
            labelLpt3PrinterType.Name = "labelLpt3PrinterType";
            labelLpt3PrinterType.Size = new Size(85, 19);
            labelLpt3PrinterType.TabIndex = 1;
            labelLpt3PrinterType.Text = "Printer Type:";
            // 
            // comboBoxLpt3PrinterType
            // 
            comboBoxLpt3PrinterType.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboBoxLpt3PrinterType.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxLpt3PrinterType.FlatStyle = FlatStyle.Flat;
            comboBoxLpt3PrinterType.Font = new Font("Segoe UI", 11F);
            comboBoxLpt3PrinterType.FormattingEnabled = true;
            comboBoxLpt3PrinterType.Items.AddRange(new object[] { "None", "TCP/IP", "Windows Printer", "Windows Port" });
            comboBoxLpt3PrinterType.Location = new Point(20, 86);
            comboBoxLpt3PrinterType.Name = "comboBoxLpt3PrinterType";
            comboBoxLpt3PrinterType.Size = new Size(215, 28);
            comboBoxLpt3PrinterType.TabIndex = 13;
            comboBoxLpt3PrinterType.SelectedIndexChanged += comboBoxLpt3PrinterType_SelectedIndexChanged;
            // 
            // labelLpt3IpAddress
            // 
            labelLpt3IpAddress.AutoSize = true;
            labelLpt3IpAddress.Font = new Font("Segoe UI", 10F);
            labelLpt3IpAddress.ForeColor = Color.White;
            labelLpt3IpAddress.Location = new Point(24, 132);
            labelLpt3IpAddress.Name = "labelLpt3IpAddress";
            labelLpt3IpAddress.Size = new Size(74, 19);
            labelLpt3IpAddress.TabIndex = 3;
            labelLpt3IpAddress.Text = "IP Address";
            // 
            // textBoxLpt3IPAddress
            // 
            textBoxLpt3IPAddress.Font = new Font("Segoe UI", 11F);
            textBoxLpt3IPAddress.Location = new Point(20, 158);
            textBoxLpt3IPAddress.Name = "textBoxLpt3IPAddress";
            textBoxLpt3IPAddress.Size = new Size(154, 27);
            textBoxLpt3IPAddress.TabIndex = 14;
            // 
            // labelLpt3Port
            // 
            labelLpt3Port.AutoSize = true;
            labelLpt3Port.Font = new Font("Segoe UI", 10F);
            labelLpt3Port.ForeColor = Color.White;
            labelLpt3Port.Location = new Point(178, 132);
            labelLpt3Port.Name = "labelLpt3Port";
            labelLpt3Port.Size = new Size(34, 19);
            labelLpt3Port.TabIndex = 5;
            labelLpt3Port.Text = "Port";
            // 
            // textBoxLpt3Port
            // 
            textBoxLpt3Port.Font = new Font("Segoe UI", 11F);
            textBoxLpt3Port.Location = new Point(178, 158);
            textBoxLpt3Port.Name = "textBoxLpt3Port";
            textBoxLpt3Port.Size = new Size(56, 27);
            textBoxLpt3Port.TabIndex = 15;
            // 
            // labelLpt3WindowsPrinters
            // 
            labelLpt3WindowsPrinters.AutoSize = true;
            labelLpt3WindowsPrinters.Font = new Font("Segoe UI", 10F);
            labelLpt3WindowsPrinters.ForeColor = Color.White;
            labelLpt3WindowsPrinters.Location = new Point(24, 207);
            labelLpt3WindowsPrinters.Name = "labelLpt3WindowsPrinters";
            labelLpt3WindowsPrinters.Size = new Size(116, 19);
            labelLpt3WindowsPrinters.TabIndex = 7;
            labelLpt3WindowsPrinters.Text = "Windows Printers";
            // 
            // comboBoxLpt3WindowsPrinters
            // 
            comboBoxLpt3WindowsPrinters.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboBoxLpt3WindowsPrinters.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxLpt3WindowsPrinters.FlatStyle = FlatStyle.Flat;
            comboBoxLpt3WindowsPrinters.Font = new Font("Segoe UI", 11F);
            comboBoxLpt3WindowsPrinters.FormattingEnabled = true;
            comboBoxLpt3WindowsPrinters.Location = new Point(20, 233);
            comboBoxLpt3WindowsPrinters.Name = "comboBoxLpt3WindowsPrinters";
            comboBoxLpt3WindowsPrinters.Size = new Size(215, 28);
            comboBoxLpt3WindowsPrinters.TabIndex = 16;
            // 
            // buttonLpt3Assign
            // 
            buttonLpt3Assign.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            buttonLpt3Assign.BackColor = Color.FromArgb(60, 60, 60);
            buttonLpt3Assign.FlatAppearance.BorderSize = 0;
            buttonLpt3Assign.FlatAppearance.MouseDownBackColor = Color.RoyalBlue;
            buttonLpt3Assign.FlatAppearance.MouseOverBackColor = Color.RoyalBlue;
            buttonLpt3Assign.FlatStyle = FlatStyle.Flat;
            buttonLpt3Assign.Font = new Font("Segoe UI", 10F);
            buttonLpt3Assign.ForeColor = Color.White;
            buttonLpt3Assign.Location = new Point(19, 387);
            buttonLpt3Assign.Name = "buttonLpt3Assign";
            buttonLpt3Assign.Size = new Size(214, 36);
            buttonLpt3Assign.TabIndex = 18;
            buttonLpt3Assign.Text = "Assign LPT3";
            buttonLpt3Assign.UseVisualStyleBackColor = false;
            buttonLpt3Assign.Click += buttonLpt3Assign_Click;
            // 
            // panelApplication
            // 
            panelApplication.BackColor = Color.FromArgb(45, 45, 45);
            panelApplication.BorderStyle = BorderStyle.FixedSingle;
            panelApplication.Controls.Add(checkBoxAppMouseOn);
            panelApplication.Controls.Add(labelScaleValue);
            panelApplication.Controls.Add(labelAppScale);
            panelApplication.Controls.Add(hScrollBarScale);
            panelApplication.Controls.Add(checkBoxFoxPro);
            panelApplication.Controls.Add(checkBoxDosX);
            panelApplication.Controls.Add(buttonAssignTarget);
            panelApplication.Controls.Add(buttonFindTarget);
            panelApplication.Controls.Add(textBoxTargetpath);
            panelApplication.Controls.Add(labelTargetPath);
            panelApplication.Controls.Add(labelApplication);
            panelApplication.Dock = DockStyle.Fill;
            panelApplication.Location = new Point(783, 2);
            panelApplication.Margin = new Padding(3, 2, 3, 2);
            panelApplication.Name = "panelApplication";
            panelApplication.Size = new Size(254, 440);
            panelApplication.TabIndex = 3;
            // 
            // labelScaleValue
            // 
            labelScaleValue.AutoSize = true;
            labelScaleValue.Font = new Font("Segoe UI", 10F);
            labelScaleValue.ForeColor = Color.White;
            labelScaleValue.Location = new Point(219, 281);
            labelScaleValue.Name = "labelScaleValue";
            labelScaleValue.Size = new Size(25, 19);
            labelScaleValue.TabIndex = 19;
            labelScaleValue.Text = "15";
            labelScaleValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // labelAppScale
            // 
            labelAppScale.AutoSize = true;
            labelAppScale.Font = new Font("Segoe UI", 10F);
            labelAppScale.ForeColor = Color.White;
            labelAppScale.Location = new Point(27, 281);
            labelAppScale.Name = "labelAppScale";
            labelAppScale.Size = new Size(118, 19);
            labelAppScale.TabIndex = 15;
            labelAppScale.Text = "DOS Window Size";
            // 
            // hScrollBarScale
            // 
            hScrollBarScale.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            hScrollBarScale.LargeChange = 5;
            hScrollBarScale.Location = new Point(19, 308);
            hScrollBarScale.Maximum = 54;
            hScrollBarScale.Minimum = 5;
            hScrollBarScale.Name = "hScrollBarScale";
            hScrollBarScale.Size = new Size(217, 28);
            hScrollBarScale.TabIndex = 23;
            hScrollBarScale.TabStop = true;
            hScrollBarScale.Value = 15;
            // 
            // checkBoxFoxPro
            // 
            checkBoxFoxPro.AutoSize = true;
            checkBoxFoxPro.Font = new Font("Segoe UI", 11F);
            checkBoxFoxPro.ForeColor = Color.White;
            checkBoxFoxPro.Location = new Point(19, 236);
            checkBoxFoxPro.Margin = new Padding(3, 2, 3, 2);
            checkBoxFoxPro.Name = "checkBoxFoxPro";
            checkBoxFoxPro.Size = new Size(122, 24);
            checkBoxFoxPro.TabIndex = 22;
            checkBoxFoxPro.Text = "Enable FoxPro";
            checkBoxFoxPro.UseVisualStyleBackColor = true;
            // 
            // checkBoxDosX
            // 
            checkBoxDosX.AutoSize = true;
            checkBoxDosX.Font = new Font("Segoe UI", 11F);
            checkBoxDosX.ForeColor = Color.White;
            checkBoxDosX.Location = new Point(19, 208);
            checkBoxDosX.Margin = new Padding(3, 2, 3, 2);
            checkBoxDosX.Name = "checkBoxDosX";
            checkBoxDosX.Size = new Size(112, 24);
            checkBoxDosX.TabIndex = 21;
            checkBoxDosX.Text = "Enable DosX";
            checkBoxDosX.UseVisualStyleBackColor = true;
            // 
            // buttonAssignTarget
            // 
            buttonAssignTarget.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            buttonAssignTarget.BackColor = Color.FromArgb(60, 60, 60);
            buttonAssignTarget.FlatAppearance.BorderSize = 0;
            buttonAssignTarget.FlatAppearance.MouseDownBackColor = Color.RoyalBlue;
            buttonAssignTarget.FlatAppearance.MouseOverBackColor = Color.RoyalBlue;
            buttonAssignTarget.FlatStyle = FlatStyle.Flat;
            buttonAssignTarget.Font = new Font("Segoe UI", 10F);
            buttonAssignTarget.ForeColor = Color.White;
            buttonAssignTarget.Location = new Point(17, 388);
            buttonAssignTarget.Name = "buttonAssignTarget";
            buttonAssignTarget.Size = new Size(220, 36);
            buttonAssignTarget.TabIndex = 24;
            buttonAssignTarget.Text = "Assign Target ";
            buttonAssignTarget.UseVisualStyleBackColor = false;
            buttonAssignTarget.Click += buttonAssignTarget_Click;
            // 
            // buttonFindTarget
            // 
            buttonFindTarget.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonFindTarget.BackColor = Color.FromArgb(60, 60, 60);
            buttonFindTarget.FlatAppearance.BorderSize = 0;
            buttonFindTarget.FlatAppearance.MouseDownBackColor = Color.RoyalBlue;
            buttonFindTarget.FlatAppearance.MouseOverBackColor = Color.RoyalBlue;
            buttonFindTarget.FlatStyle = FlatStyle.Flat;
            buttonFindTarget.Font = new Font("Segoe UI", 10F);
            buttonFindTarget.ForeColor = Color.White;
            buttonFindTarget.Location = new Point(16, 158);
            buttonFindTarget.Name = "buttonFindTarget";
            buttonFindTarget.Size = new Size(220, 36);
            buttonFindTarget.TabIndex = 20;
            buttonFindTarget.Text = "Find Application Target";
            buttonFindTarget.UseVisualStyleBackColor = false;
            buttonFindTarget.Click += buttonFindTarget_Click;
            // 
            // textBoxTargetpath
            // 
            textBoxTargetpath.Font = new Font("Segoe UI", 11F);
            textBoxTargetpath.Location = new Point(19, 88);
            textBoxTargetpath.Multiline = true;
            textBoxTargetpath.Name = "textBoxTargetpath";
            textBoxTargetpath.Size = new Size(220, 63);
            textBoxTargetpath.TabIndex = 19;
            // 
            // labelTargetPath
            // 
            labelTargetPath.AutoSize = true;
            labelTargetPath.Font = new Font("Segoe UI", 10F);
            labelTargetPath.ForeColor = Color.White;
            labelTargetPath.Location = new Point(27, 62);
            labelTargetPath.Name = "labelTargetPath";
            labelTargetPath.Size = new Size(78, 19);
            labelTargetPath.TabIndex = 4;
            labelTargetPath.Text = "Target Path";
            // 
            // labelApplication
            // 
            labelApplication.AutoSize = true;
            labelApplication.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            labelApplication.ForeColor = Color.White;
            labelApplication.Location = new Point(19, 16);
            labelApplication.Name = "labelApplication";
            labelApplication.Size = new Size(134, 32);
            labelApplication.TabIndex = 1;
            labelApplication.Text = "Application";
            // 
            // tableLayoutPanelButtons
            // 
            tableLayoutPanelButtons.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            tableLayoutPanelButtons.ColumnCount = 2;
            tableLayoutPanelButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanelButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanelButtons.Controls.Add(buttonOK, 0, 0);
            tableLayoutPanelButtons.Controls.Add(buttonCancel, 1, 0);
            tableLayoutPanelButtons.Location = new Point(753, 504);
            tableLayoutPanelButtons.Name = "tableLayoutPanelButtons";
            tableLayoutPanelButtons.RowCount = 1;
            tableLayoutPanelButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelButtons.Size = new Size(308, 56);
            tableLayoutPanelButtons.TabIndex = 1;
            // 
            // buttonOK
            // 
            buttonOK.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            buttonOK.BackColor = Color.FromArgb(60, 60, 60);
            buttonOK.FlatAppearance.BorderSize = 0;
            buttonOK.FlatAppearance.MouseDownBackColor = Color.RoyalBlue;
            buttonOK.FlatAppearance.MouseOverBackColor = Color.RoyalBlue;
            buttonOK.FlatStyle = FlatStyle.Flat;
            buttonOK.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            buttonOK.ForeColor = Color.White;
            buttonOK.Location = new Point(3, 3);
            buttonOK.Margin = new Padding(3, 3, 8, 3);
            buttonOK.Name = "buttonOK";
            buttonOK.Size = new Size(143, 50);
            buttonOK.TabIndex = 25;
            buttonOK.Text = "OK";
            buttonOK.UseVisualStyleBackColor = false;
            buttonOK.Click += buttonOK_Click;
            // 
            // buttonCancel
            // 
            buttonCancel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            buttonCancel.BackColor = Color.FromArgb(60, 60, 60);
            buttonCancel.FlatAppearance.BorderSize = 0;
            buttonCancel.FlatAppearance.MouseDownBackColor = Color.RoyalBlue;
            buttonCancel.FlatAppearance.MouseOverBackColor = Color.RoyalBlue;
            buttonCancel.FlatStyle = FlatStyle.Flat;
            buttonCancel.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            buttonCancel.ForeColor = Color.White;
            buttonCancel.Location = new Point(162, 3);
            buttonCancel.Margin = new Padding(8, 3, 3, 3);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(143, 50);
            buttonCancel.TabIndex = 26;
            buttonCancel.Text = "Cancel";
            buttonCancel.UseVisualStyleBackColor = false;
            buttonCancel.Click += buttonCancel_Click;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // checkBoxAppMouseOn
            // 
            checkBoxAppMouseOn.AutoSize = true;
            checkBoxAppMouseOn.Font = new Font("Segoe UI", 11F);
            checkBoxAppMouseOn.ForeColor = Color.White;
            checkBoxAppMouseOn.Location = new Point(19, 352);
            checkBoxAppMouseOn.Margin = new Padding(3, 2, 3, 2);
            checkBoxAppMouseOn.Name = "checkBoxAppMouseOn";
            checkBoxAppMouseOn.Size = new Size(121, 24);
            checkBoxAppMouseOn.TabIndex = 25;
            checkBoxAppMouseOn.Text = "Enable Mouse";
            checkBoxAppMouseOn.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(51, 51, 51);
            ClientSize = new Size(1087, 682);
            Controls.Add(panelButtons);
            Controls.Add(panelHeader);
            MinimumSize = new Size(900, 598);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "vDos Configurator";
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxVDosLogo).EndInit();
            panelButtons.ResumeLayout(false);
            tableLayoutPanelLptPorts.ResumeLayout(false);
            panelLpt1.ResumeLayout(false);
            panelLpt1.PerformLayout();
            panelLpt2.ResumeLayout(false);
            panelLpt2.PerformLayout();
            panelLpt3.ResumeLayout(false);
            panelLpt3.PerformLayout();
            panelApplication.ResumeLayout(false);
            panelApplication.PerformLayout();
            tableLayoutPanelButtons.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel panelHeader;
        private PictureBox pictureBoxVDosLogo;
        private Label labelHeaderTitle;
        private Panel panelButtons;
        private TableLayoutPanel tableLayoutPanelLptPorts;
        private TableLayoutPanel tableLayoutPanelButtons;

        private Label labelLpt1;
        private Label labelLpt2;
        private Label labelLpt3;

        private Panel panelLpt1;
        private Panel panelLpt2;
        private Panel panelLpt3;

        private Label labelLpt1IpAddress;
        private Label labelLpt2IpAddress;
        private Label labelLpt3IpAddress;

        private Label labelLpt1Port;
        private Label labelLpt2Port;
        private Label labelLpt3Port;

        private TextBox textBoxLpt1IPAddress;
        private TextBox textBoxLpt1Port;
        private TextBox textBoxLpt2IPAddress;
        private TextBox textBoxLpt2Port;
        private TextBox textBoxLpt3IPAddress;
        private TextBox textBoxLpt3Port;

        private Label labelLpt1PrinterType;
        private Label labelLpt2PrinterType;
        private Label labelLpt3PrinterType;

        private ComboBox comboBoxLpt1PrinterType;
        private ComboBox comboBoxLpt2PrinterType;
        private ComboBox comboBoxLpt3PrinterType;

        private Label labelLpt1WindowsPrinters;
        private Label labelLpt2WindowsPrinters;
        private Label labelLpt3WindowsPrinters;

        private ComboBox comboBoxLpt1WindowsPrinters;
        private ComboBox comboBoxLpt2WindowsPrinters;
        private ComboBox comboBoxLpt3WindowsPrinters;

        private Button buttonLpt1Assign;
        private Button buttonLpt2Assign;
        private Button buttonLpt3Assign;

        private Button buttonOK;
        private Button buttonCancel;
        private Label labelLpt1WindowsPorts;
        private ComboBox comboBoxLpt1WindowsPort;
        private Label labelLpt2WindowsPort;
        private ComboBox comboBoxLpt2WindowsPort;
        private Label labelLpt3WindowsPort;
        private ComboBox comboBoxLpt3WindowsPort;
        private Panel panelApplication;
        private Label labelApplication;
        private Label labelTargetPath;
        private Button buttonFindTarget;
        private TextBox textBoxTargetpath;
        private OpenFileDialog openFileDialog1;
        private Button buttonAssignTarget;
        private CheckBox checkBoxDosX;
        private CheckBox checkBoxFoxPro;
        private Label labelAppScale;
        private HScrollBar hScrollBarScale;
        private Label labelScaleValue;
        private CheckBox checkBoxAppMouseOn;
    }
}







