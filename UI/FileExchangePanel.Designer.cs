using System.Drawing;
using System.Windows.Forms;

namespace CryptoFileExchange.UI
{
    partial class FileExchangePanel
    {
        private System.ComponentModel.IContainer components = null;

        #region Component Designer generated code

        private void InitializeComponent()
        {
            grpServer = new GroupBox();
            lblPort = new Label();
            txtPort = new TextBox();
            btnToggleServerMode = new Button();
            grpClient = new GroupBox();
            lblFilePath = new Label();
            txtFilePath = new TextBox();
            btnBrowseFile = new Button();
            lblIPAddress = new Label();
            txtIPAddress = new TextBox();
            lblRecipientPort = new Label();
            txtRecipientPort = new TextBox();
            btnSendFile = new Button();
            grpLog = new GroupBox();
            listViewLog = new ListView();
            btnClearLog = new Button();
            grpServer.SuspendLayout();
            grpClient.SuspendLayout();
            grpLog.SuspendLayout();
            SuspendLayout();
            // 
            // grpServer
            // 
            grpServer.Controls.Add(lblPort);
            grpServer.Controls.Add(txtPort);
            grpServer.Controls.Add(btnToggleServerMode);
            grpServer.Dock = DockStyle.Top;
            grpServer.Location = new Point(0, 0);
            grpServer.Name = "grpServer";
            grpServer.Padding = new Padding(10);
            grpServer.Size = new Size(800, 100);
            grpServer.TabIndex = 0;
            grpServer.TabStop = false;
            grpServer.Text = "Server Mode (Receive Files)";
            // 
            // lblPort
            // 
            lblPort.AutoSize = true;
            lblPort.Location = new Point(20, 30);
            lblPort.Name = "lblPort";
            lblPort.Size = new Size(32, 15);
            lblPort.TabIndex = 0;
            lblPort.Text = "Port:";
            // 
            // txtPort
            // 
            txtPort.Location = new Point(20, 48);
            txtPort.Name = "txtPort";
            txtPort.PlaceholderText = "Enter port number (e.g., 9999)";
            txtPort.Size = new Size(150, 23);
            txtPort.TabIndex = 1;
            txtPort.Text = "9999";
            // 
            // btnToggleServerMode
            // 
            btnToggleServerMode.Location = new Point(185, 46);
            btnToggleServerMode.Name = "btnToggleServerMode";
            btnToggleServerMode.Size = new Size(120, 27);
            btnToggleServerMode.TabIndex = 2;
            btnToggleServerMode.Text = "Start Server";
            btnToggleServerMode.UseVisualStyleBackColor = true;
            btnToggleServerMode.Click += btnToggleServerMode_Click;
            // 
            // grpClient
            // 
            grpClient.Controls.Add(lblFilePath);
            grpClient.Controls.Add(txtFilePath);
            grpClient.Controls.Add(btnBrowseFile);
            grpClient.Controls.Add(lblIPAddress);
            grpClient.Controls.Add(txtIPAddress);
            grpClient.Controls.Add(lblRecipientPort);
            grpClient.Controls.Add(txtRecipientPort);
            grpClient.Controls.Add(btnSendFile);
            grpClient.Dock = DockStyle.Top;
            grpClient.Location = new Point(0, 100);
            grpClient.Name = "grpClient";
            grpClient.Padding = new Padding(10);
            grpClient.Size = new Size(800, 180);
            grpClient.TabIndex = 1;
            grpClient.TabStop = false;
            grpClient.Text = "Client Mode (Send Files)";
            // 
            // lblFilePath
            // 
            lblFilePath.AutoSize = true;
            lblFilePath.Location = new Point(20, 30);
            lblFilePath.Name = "lblFilePath";
            lblFilePath.Size = new Size(83, 15);
            lblFilePath.TabIndex = 0;
            lblFilePath.Text = "File to send:";
            // 
            // txtFilePath
            // 
            txtFilePath.Location = new Point(20, 48);
            txtFilePath.Name = "txtFilePath";
            txtFilePath.PlaceholderText = "Select file to encrypt and send";
            txtFilePath.ReadOnly = true;
            txtFilePath.Size = new Size(600, 23);
            txtFilePath.TabIndex = 1;
            // 
            // btnBrowseFile
            // 
            btnBrowseFile.Location = new Point(630, 46);
            btnBrowseFile.Name = "btnBrowseFile";
            btnBrowseFile.Size = new Size(150, 27);
            btnBrowseFile.TabIndex = 2;
            btnBrowseFile.Text = "Browse...";
            btnBrowseFile.UseVisualStyleBackColor = true;
            btnBrowseFile.Click += btnBrowseFile_Click;
            // 
            // lblIPAddress
            // 
            lblIPAddress.AutoSize = true;
            lblIPAddress.Location = new Point(20, 85);
            lblIPAddress.Name = "lblIPAddress";
            lblIPAddress.Size = new Size(117, 15);
            lblIPAddress.TabIndex = 3;
            lblIPAddress.Text = "Recipient IP Address:";
            // 
            // txtIPAddress
            // 
            txtIPAddress.Location = new Point(20, 103);
            txtIPAddress.Name = "txtIPAddress";
            txtIPAddress.PlaceholderText = "e.g., 127.0.0.1 or 192.168.1.100";
            txtIPAddress.Size = new Size(250, 23);
            txtIPAddress.TabIndex = 4;
            txtIPAddress.Text = "127.0.0.1";
            // 
            // lblRecipientPort
            // 
            lblRecipientPort.AutoSize = true;
            lblRecipientPort.Location = new Point(285, 85);
            lblRecipientPort.Name = "lblRecipientPort";
            lblRecipientPort.Size = new Size(87, 15);
            lblRecipientPort.TabIndex = 5;
            lblRecipientPort.Text = "Recipient Port:";
            // 
            // txtRecipientPort
            // 
            txtRecipientPort.Location = new Point(285, 103);
            txtRecipientPort.Name = "txtRecipientPort";
            txtRecipientPort.PlaceholderText = "e.g., 9999";
            txtRecipientPort.Size = new Size(150, 23);
            txtRecipientPort.TabIndex = 6;
            txtRecipientPort.Text = "9999";
            // 
            // btnSendFile
            // 
            btnSendFile.BackColor = Color.LightGreen;
            btnSendFile.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSendFile.Location = new Point(20, 140);
            btnSendFile.Name = "btnSendFile";
            btnSendFile.Size = new Size(200, 30);
            btnSendFile.TabIndex = 7;
            btnSendFile.Text = "Encrypt && Send File";
            btnSendFile.UseVisualStyleBackColor = false;
            btnSendFile.Click += btnSendFile_Click;
            // 
            // grpLog
            // 
            grpLog.Controls.Add(listViewLog);
            grpLog.Controls.Add(btnClearLog);
            grpLog.Dock = DockStyle.Fill;
            grpLog.Location = new Point(0, 280);
            grpLog.Name = "grpLog";
            grpLog.Padding = new Padding(10);
            grpLog.Size = new Size(800, 320);
            grpLog.TabIndex = 2;
            grpLog.TabStop = false;
            grpLog.Text = "Activity Log";
            // 
            // listViewLog
            // 
            listViewLog.Dock = DockStyle.Fill;
            listViewLog.Location = new Point(10, 26);
            listViewLog.Name = "listViewLog";
            listViewLog.Size = new Size(780, 250);
            listViewLog.TabIndex = 0;
            listViewLog.UseCompatibleStateImageBehavior = false;
            listViewLog.View = View.Details;
            // 
            // btnClearLog
            // 
            btnClearLog.Dock = DockStyle.Bottom;
            btnClearLog.Location = new Point(10, 276);
            btnClearLog.Name = "btnClearLog";
            btnClearLog.Size = new Size(780, 34);
            btnClearLog.TabIndex = 1;
            btnClearLog.Text = "Clear Log";
            btnClearLog.UseVisualStyleBackColor = true;
            btnClearLog.Click += btnClearLog_Click;
            // 
            // FileExchangePanel
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(grpLog);
            Controls.Add(grpClient);
            Controls.Add(grpServer);
            Name = "FileExchangePanel";
            Size = new Size(800, 600);
            grpServer.ResumeLayout(false);
            grpServer.PerformLayout();
            grpClient.ResumeLayout(false);
            grpClient.PerformLayout();
            grpLog.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private GroupBox grpServer;
        private Label lblPort;
        private TextBox txtPort;
        private Button btnToggleServerMode;
        private GroupBox grpClient;
        private Label lblFilePath;
        private TextBox txtFilePath;
        private Button btnBrowseFile;
        private Label lblIPAddress;
        private TextBox txtIPAddress;
        private Label lblRecipientPort;
        private TextBox txtRecipientPort;
        private Button btnSendFile;
        private GroupBox grpLog;
        private ListView listViewLog;
        private Button btnClearLog;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _networkService?.StopListening();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
