namespace ThermalCamera;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        listBoxCameras = new ListBox();
        btnConnect = new Button();
        btnDisconnect = new Button();
        btnCaptureAndRecognize = new Button();
        textBoxTemperature = new TextBox();
        richTextBoxLog = new RichTextBox();
        labelCameras = new Label();
        labelTemperature = new Label();
        labelLog = new Label();
        pictureBoxPreview = new PictureBox();
        ((System.ComponentModel.ISupportInitialize)pictureBoxPreview).BeginInit();
        SuspendLayout();
        //
        // listBoxCameras
        //
        listBoxCameras.FormattingEnabled = true;
        listBoxCameras.ItemHeight = 15;
        listBoxCameras.Location = new Point(12, 28);
        listBoxCameras.Name = "listBoxCameras";
        listBoxCameras.Size = new Size(260, 94);
        listBoxCameras.TabIndex = 0;
        //
        // btnConnect
        //
        btnConnect.Location = new Point(12, 128);
        btnConnect.Name = "btnConnect";
        btnConnect.Size = new Size(125, 28);
        btnConnect.TabIndex = 1;
        btnConnect.Text = "Connect";
        btnConnect.UseVisualStyleBackColor = true;
        btnConnect.Click += btnConnect_Click;
        //
        // btnDisconnect
        //
        btnDisconnect.Location = new Point(147, 128);
        btnDisconnect.Name = "btnDisconnect";
        btnDisconnect.Size = new Size(125, 28);
        btnDisconnect.TabIndex = 2;
        btnDisconnect.Text = "Disconnect";
        btnDisconnect.UseVisualStyleBackColor = true;
        btnDisconnect.Click += btnDisconnect_Click;
        //
        // btnCaptureAndRecognize
        //
        btnCaptureAndRecognize.Location = new Point(12, 162);
        btnCaptureAndRecognize.Name = "btnCaptureAndRecognize";
        btnCaptureAndRecognize.Size = new Size(260, 28);
        btnCaptureAndRecognize.TabIndex = 3;
        btnCaptureAndRecognize.Text = "Image 가져오기 및 온도 인식";
        //
        // btnRecognizeFlir
        //
        btnRecognizeFlir = new Button();
        btnRecognizeFlir.Location = new Point(12, 196);
        btnRecognizeFlir.Name = "btnRecognizeFlir";
        btnRecognizeFlir.Size = new Size(260, 28);
        btnRecognizeFlir.TabIndex = 10;
        btnRecognizeFlir.Text = "Flir.png에서 온도 인식";
        btnRecognizeFlir.UseVisualStyleBackColor = true;
        btnRecognizeFlir.Click += btnRecognizeFlir_Click;
        btnCaptureAndRecognize.UseVisualStyleBackColor = true;
        btnCaptureAndRecognize.Click += btnCaptureAndRecognize_Click;
        //
        // textBoxTemperature
        //
        textBoxTemperature.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        textBoxTemperature.Location = new Point(12, 244);
        textBoxTemperature.Name = "textBoxTemperature";
        textBoxTemperature.ReadOnly = true;
        textBoxTemperature.Size = new Size(260, 29);
        textBoxTemperature.TabIndex = 4;
        textBoxTemperature.Text = "—";
        textBoxTemperature.TextAlign = HorizontalAlignment.Center;
        //
        // richTextBoxLog
        //
        richTextBoxLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        richTextBoxLog.Location = new Point(12, 268);
        richTextBoxLog.Name = "richTextBoxLog";
        richTextBoxLog.ReadOnly = true;
        richTextBoxLog.Size = new Size(560, 220);
        richTextBoxLog.TabIndex = 5;
        richTextBoxLog.Text = "";
        //
        // labelCameras
        //
        labelCameras.AutoSize = true;
        labelCameras.Location = new Point(12, 10);
        labelCameras.Name = "labelCameras";
        labelCameras.Size = new Size(120, 15);
        labelCameras.TabIndex = 6;
        labelCameras.Text = "USB 열화상 카메라 목록";
        //
        // labelTemperature
        //
        labelTemperature.AutoSize = true;
        labelTemperature.Location = new Point(12, 226);
        labelTemperature.Name = "labelTemperature";
        labelTemperature.Size = new Size(58, 15);
        labelTemperature.TabIndex = 7;
        labelTemperature.Text = "온도값 (°C)";
        //
        // labelLog
        //
        labelLog.AutoSize = true;
        labelLog.Location = new Point(12, 250);
        labelLog.Name = "labelLog";
        labelLog.Size = new Size(57, 15);
        labelLog.TabIndex = 8;
        labelLog.Text = "로그 출력";
        //
        // pictureBoxPreview
        //
        pictureBoxPreview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
        pictureBoxPreview.BackColor = SystemColors.ControlDark;
        pictureBoxPreview.BorderStyle = BorderStyle.FixedSingle;
        pictureBoxPreview.Location = new Point(288, 28);
        pictureBoxPreview.Name = "pictureBoxPreview";
        pictureBoxPreview.Size = new Size(284, 234);
        pictureBoxPreview.SizeMode = PictureBoxSizeMode.Zoom;
        pictureBoxPreview.TabIndex = 9;
        pictureBoxPreview.TabStop = false;
        //
        // MainForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(584, 501);
        Controls.Add(pictureBoxPreview);
        Controls.Add(labelLog);
        Controls.Add(labelTemperature);
        Controls.Add(labelCameras);
        Controls.Add(richTextBoxLog);
        Controls.Add(textBoxTemperature);
        Controls.Add(btnRecognizeFlir);
        Controls.Add(btnCaptureAndRecognize);
        Controls.Add(btnDisconnect);
        Controls.Add(btnConnect);
        Controls.Add(listBoxCameras);
        MinimumSize = new Size(500, 400);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "USB 열화상 카메라 - 온도 인식";
        FormClosing += MainForm_FormClosing;
        Load += MainForm_Load;
        ((System.ComponentModel.ISupportInitialize)pictureBoxPreview).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private ListBox listBoxCameras;
    private Button btnConnect;
    private Button btnDisconnect;
    private Button btnCaptureAndRecognize;
    private TextBox textBoxTemperature;
    private RichTextBox richTextBoxLog;
    private Label labelCameras;
    private Label labelTemperature;
    private Label labelLog;
    private PictureBox pictureBoxPreview;
    private Button btnRecognizeFlir;
}
