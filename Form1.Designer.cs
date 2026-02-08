namespace CryptoFileExchange
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tabControl = new System.Windows.Forms.TabControl();
            tabFileWatcher = new System.Windows.Forms.TabPage();
            tabFileExchange = new System.Windows.Forms.TabPage();
            fileWatcherPanel = new UI.FileWatcherPanel();
            fileExchangePanel = new UI.FileExchangePanel();
            tabControl.SuspendLayout();
            tabFileWatcher.SuspendLayout();
            tabFileExchange.SuspendLayout();
            SuspendLayout();

            // 
            // tabControl
            // 
            tabControl.Controls.Add(tabFileWatcher);
            tabControl.Controls.Add(tabFileExchange);
            tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            tabControl.Location = new System.Drawing.Point(0, 0);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new System.Drawing.Size(900, 650);
            tabControl.TabIndex = 0;

            // 
            // tabFileWatcher
            // 
            tabFileWatcher.Controls.Add(fileWatcherPanel);
            tabFileWatcher.Location = new System.Drawing.Point(4, 24);
            tabFileWatcher.Name = "tabFileWatcher";
            tabFileWatcher.Padding = new System.Windows.Forms.Padding(3);
            tabFileWatcher.Size = new System.Drawing.Size(892, 622);
            tabFileWatcher.TabIndex = 0;
            tabFileWatcher.Text = "Auto Encryption";
            tabFileWatcher.UseVisualStyleBackColor = true;

            // 
            // tabFileExchange
            // 
            tabFileExchange.Controls.Add(fileExchangePanel);
            tabFileExchange.Location = new System.Drawing.Point(4, 24);
            tabFileExchange.Name = "tabFileExchange";
            tabFileExchange.Padding = new System.Windows.Forms.Padding(3);
            tabFileExchange.Size = new System.Drawing.Size(892, 622);
            tabFileExchange.TabIndex = 1;
            tabFileExchange.Text = "File Exchange (P2P)";
            tabFileExchange.UseVisualStyleBackColor = true;

            // 
            // fileWatcherPanel
            // 
            fileWatcherPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            fileWatcherPanel.Location = new System.Drawing.Point(3, 3);
            fileWatcherPanel.Name = "fileWatcherPanel";
            fileWatcherPanel.Size = new System.Drawing.Size(886, 616);
            fileWatcherPanel.TabIndex = 0;

            // 
            // fileExchangePanel
            // 
            fileExchangePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            fileExchangePanel.Location = new System.Drawing.Point(3, 3);
            fileExchangePanel.Name = "fileExchangePanel";
            fileExchangePanel.Size = new System.Drawing.Size(886, 616);
            fileExchangePanel.TabIndex = 0;

            // 
            // Form1
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(900, 650);
            Controls.Add(tabControl);
            Name = "Form1";
            Text = "CryptoFileExchange - Auto Encryption && P2P File Transfer";
            tabControl.ResumeLayout(false);
            tabFileWatcher.ResumeLayout(false);
            tabFileExchange.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabFileWatcher;
        private System.Windows.Forms.TabPage tabFileExchange;
        private UI.FileWatcherPanel fileWatcherPanel;
        private UI.FileExchangePanel fileExchangePanel;
    }
}
