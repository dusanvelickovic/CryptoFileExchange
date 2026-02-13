using System.Drawing;
using System.Windows.Forms;

namespace CryptoFileExchange.UI
{
    partial class FileWatcherPanel
    {
        private System.ComponentModel.IContainer components = null;

        #region Component Designer generated code

        private void InitializeComponent()
        {
            groupBoxConfig = new GroupBox();
            lblTargetDirectory = new Label();
            txtTargetDirectory = new TextBox();
            btnBrowseTarget = new Button();
            lblOutputDirectory = new Label();
            txtOutputDirectory = new TextBox();
            btnOpenOutput = new Button();
            btnToggleWatcher = new Button();
            btnEncryptFile = new Button();
            btnDecryptFile = new Button();
            lblStatus = new Label();
            groupBoxLog = new GroupBox();
            listViewLog = new ListView();
            btnClearLog = new Button();
            groupBoxConfig.SuspendLayout();
            groupBoxLog.SuspendLayout();
            SuspendLayout();
            // 
            // groupBoxConfig
            // 
            groupBoxConfig.Controls.Add(lblTargetDirectory);
            groupBoxConfig.Controls.Add(txtTargetDirectory);
            groupBoxConfig.Controls.Add(btnBrowseTarget);
            groupBoxConfig.Controls.Add(lblOutputDirectory);
            groupBoxConfig.Controls.Add(txtOutputDirectory);
            groupBoxConfig.Controls.Add(btnOpenOutput);
            groupBoxConfig.Controls.Add(btnToggleWatcher);
            groupBoxConfig.Controls.Add(btnEncryptFile);
            groupBoxConfig.Controls.Add(btnDecryptFile);
            groupBoxConfig.Controls.Add(lblStatus);
            groupBoxConfig.Dock = DockStyle.Top;
            groupBoxConfig.Location = new Point(0, 0);
            groupBoxConfig.Name = "groupBoxConfig";
            groupBoxConfig.Padding = new Padding(10);
            groupBoxConfig.Size = new Size(800, 180);
            groupBoxConfig.TabIndex = 0;
            groupBoxConfig.TabStop = false;
            groupBoxConfig.Text = "Configure File System Watcher";
            // 
            // lblTargetDirectory
            // 
            lblTargetDirectory.AutoSize = true;
            lblTargetDirectory.Location = new Point(22, 37);
            lblTargetDirectory.Name = "lblTargetDirectory";
            lblTargetDirectory.Size = new Size(93, 15);
            lblTargetDirectory.TabIndex = 0;
            lblTargetDirectory.Text = "Target directory:";
            // 
            // txtTargetDirectory
            // 
            txtTargetDirectory.Location = new Point(15, 50);
            txtTargetDirectory.Name = "txtTargetDirectory";
            txtTargetDirectory.PlaceholderText = "Choose directory for watching";
            txtTargetDirectory.Size = new Size(650, 23);
            txtTargetDirectory.TabIndex = 1;
            // 
            // btnBrowseTarget
            // 
            btnBrowseTarget.Location = new Point(670, 48);
            btnBrowseTarget.Name = "btnBrowseTarget";
            btnBrowseTarget.Size = new Size(110, 27);
            btnBrowseTarget.TabIndex = 2;
            btnBrowseTarget.Text = "Choose";
            btnBrowseTarget.UseVisualStyleBackColor = true;
            btnBrowseTarget.Click += btnBrowseTarget_Click;
            // 
            // lblOutputDirectory
            // 
            lblOutputDirectory.AutoSize = true;
            lblOutputDirectory.Location = new Point(22, 92);
            lblOutputDirectory.Name = "lblOutputDirectory";
            lblOutputDirectory.Size = new Size(146, 15);
            lblOutputDirectory.TabIndex = 3;
            lblOutputDirectory.Text = "Output directory (crypted)";
            // 
            // txtOutputDirectory
            // 
            txtOutputDirectory.BackColor = SystemColors.Control;
            txtOutputDirectory.Location = new Point(15, 105);
            txtOutputDirectory.Name = "txtOutputDirectory";
            txtOutputDirectory.ReadOnly = true;
            txtOutputDirectory.Size = new Size(650, 23);
            txtOutputDirectory.TabIndex = 4;
            // 
            // btnOpenOutput
            // 
            btnOpenOutput.Location = new Point(670, 103);
            btnOpenOutput.Name = "btnOpenOutput";
            btnOpenOutput.Size = new Size(110, 27);
            btnOpenOutput.TabIndex = 5;
            btnOpenOutput.Text = "Open";
            btnOpenOutput.UseVisualStyleBackColor = true;
            btnOpenOutput.Click += btnOpenOutput_Click;
            // 
            // btnToggleWatcher
            // 
            btnToggleWatcher.BackColor = Color.MediumSeaGreen;
            btnToggleWatcher.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnToggleWatcher.ForeColor = Color.White;
            btnToggleWatcher.Location = new Point(15, 140);
            btnToggleWatcher.Name = "btnToggleWatcher";
            btnToggleWatcher.Size = new Size(200, 35);
            btnToggleWatcher.TabIndex = 6;
            btnToggleWatcher.Text = "Start FWS";
            btnToggleWatcher.UseVisualStyleBackColor = false;
            btnToggleWatcher.Click += btnToggleWatcher_Click;
            // 
            // btnEncryptFile
            // 
            btnEncryptFile.BackColor = Color.DodgerBlue;
            btnEncryptFile.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnEncryptFile.ForeColor = Color.White;
            btnEncryptFile.Location = new Point(220, 140);
            btnEncryptFile.Name = "btnEncryptFile";
            btnEncryptFile.Size = new Size(150, 35);
            btnEncryptFile.TabIndex = 8;
            btnEncryptFile.Text = "Encrypt File...";
            btnEncryptFile.UseVisualStyleBackColor = false;
            btnEncryptFile.Click += btnEncryptFile_Click;
            // 
            // btnDecryptFile
            // 
            btnDecryptFile.BackColor = Color.Orange;
            btnDecryptFile.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnDecryptFile.ForeColor = Color.White;
            btnDecryptFile.Location = new Point(375, 140);
            btnDecryptFile.Name = "btnDecryptFile";
            btnDecryptFile.Size = new Size(150, 35);
            btnDecryptFile.TabIndex = 9;
            btnDecryptFile.Text = "Decrypt File...";
            btnDecryptFile.UseVisualStyleBackColor = false;
            btnDecryptFile.Click += btnDecryptFile_Click;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblStatus.ForeColor = Color.Gray;
            lblStatus.Location = new Point(535, 149);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(61, 19);
            lblStatus.TabIndex = 7;
            lblStatus.Text = "Inactive";
            // 
            // groupBoxLog
            // 
            groupBoxLog.Controls.Add(listViewLog);
            groupBoxLog.Controls.Add(btnClearLog);
            groupBoxLog.Dock = DockStyle.Fill;
            groupBoxLog.Location = new Point(0, 180);
            groupBoxLog.Name = "groupBoxLog";
            groupBoxLog.Padding = new Padding(10);
            groupBoxLog.Size = new Size(800, 420);
            groupBoxLog.TabIndex = 1;
            groupBoxLog.TabStop = false;
            groupBoxLog.Text = "Logs";
            // 
            // listViewLog
            // 
            listViewLog.Dock = DockStyle.Fill;
            listViewLog.Font = new Font("Consolas", 9F);
            listViewLog.FullRowSelect = true;
            listViewLog.GridLines = true;
            listViewLog.HeaderStyle = ColumnHeaderStyle.None;
            listViewLog.Location = new Point(10, 26);
            listViewLog.Name = "listViewLog";
            listViewLog.Size = new Size(780, 350);
            listViewLog.TabIndex = 0;
            listViewLog.UseCompatibleStateImageBehavior = false;
            listViewLog.View = View.Details;
            // 
            // btnClearLog
            // 
            btnClearLog.Dock = DockStyle.Bottom;
            btnClearLog.Location = new Point(10, 376);
            btnClearLog.Name = "btnClearLog";
            btnClearLog.Size = new Size(780, 34);
            btnClearLog.TabIndex = 1;
            btnClearLog.Text = "Clear logs";
            btnClearLog.UseVisualStyleBackColor = true;
            btnClearLog.Click += btnClearLog_Click;
            // 
            // FileWatcherPanel
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(groupBoxLog);
            Controls.Add(groupBoxConfig);
            Name = "FileWatcherPanel";
            Size = new Size(800, 600);
            groupBoxConfig.ResumeLayout(false);
            groupBoxConfig.PerformLayout();
            groupBoxLog.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupBoxConfig;
        private Label lblTargetDirectory;
        private TextBox txtTargetDirectory;
        private Button btnBrowseTarget;
        private Label lblOutputDirectory;
        private TextBox txtOutputDirectory;
        private Button btnOpenOutput;
        private Button btnToggleWatcher;
        private Button btnEncryptFile;
        private Button btnDecryptFile;
        private Label lblStatus;
        
        private GroupBox groupBoxLog;
        private ListView listViewLog;
        private Button btnClearLog;
    }
}
