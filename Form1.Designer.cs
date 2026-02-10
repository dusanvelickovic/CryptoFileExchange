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
            tabControl = new TabControl();
            tabFileWatcher = new TabPage();
            fileWatcherPanel = new CryptoFileExchange.UI.FileWatcherPanel();
            tabFileExchange = new TabPage();
            fileExchangePanel = new CryptoFileExchange.UI.FileExchangePanel();
            tabControl.SuspendLayout();
            tabFileWatcher.SuspendLayout();
            tabFileExchange.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl
            // 
            tabControl.Controls.Add(tabFileWatcher);
            tabControl.Controls.Add(tabFileExchange);
            tabControl.Dock = DockStyle.Fill;
            tabControl.Location = new Point(0, 0);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(900, 650);
            tabControl.TabIndex = 0;
            // 
            // tabFileWatcher
            // 
            tabFileWatcher.Controls.Add(fileWatcherPanel);
            tabFileWatcher.Location = new Point(4, 24);
            tabFileWatcher.Name = "tabFileWatcher";
            tabFileWatcher.Padding = new Padding(3);
            tabFileWatcher.Size = new Size(892, 622);
            tabFileWatcher.TabIndex = 0;
            tabFileWatcher.Text = "Auto Encryption";
            tabFileWatcher.UseVisualStyleBackColor = true;
            // 
            // fileWatcherPanel
            // 
            fileWatcherPanel.Dock = DockStyle.Fill;
            fileWatcherPanel.Location = new Point(3, 3);
            fileWatcherPanel.Name = "fileWatcherPanel";
            fileWatcherPanel.Size = new Size(886, 616);
            fileWatcherPanel.TabIndex = 0;
            // 
            // tabFileExchange
            // 
            tabFileExchange.Controls.Add(fileExchangePanel);
            tabFileExchange.Location = new Point(4, 24);
            tabFileExchange.Name = "tabFileExchange";
            tabFileExchange.Padding = new Padding(3);
            tabFileExchange.Size = new Size(892, 622);
            tabFileExchange.TabIndex = 1;
            tabFileExchange.Text = "File Exchange";
            tabFileExchange.UseVisualStyleBackColor = true;
            // 
            // fileExchangePanel
            // 
            fileExchangePanel.Dock = DockStyle.Fill;
            fileExchangePanel.Location = new Point(3, 3);
            fileExchangePanel.Name = "fileExchangePanel";
            fileExchangePanel.Size = new Size(886, 616);
            fileExchangePanel.TabIndex = 0;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(900, 650);
            Controls.Add(tabControl);
            Name = "Form1";
            Text = "Crypto File Exchange";
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
