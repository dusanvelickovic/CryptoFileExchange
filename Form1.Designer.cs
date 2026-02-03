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
            fileWatcherPanel = new UI.FileWatcherPanel();
            SuspendLayout();

            // 
            // fileWatcherPanel
            // 
            fileWatcherPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            fileWatcherPanel.Location = new System.Drawing.Point(0, 0);
            fileWatcherPanel.Name = "fileWatcherPanel";
            fileWatcherPanel.Size = new System.Drawing.Size(800, 600);
            fileWatcherPanel.TabIndex = 0;

            // 
            // Form1
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(800, 600);
            Controls.Add(fileWatcherPanel);
            Name = "Form1";
            Text = "CryptoFileExchange - Auto Encryption";
            ResumeLayout(false);
        }

        #endregion

        private UI.FileWatcherPanel fileWatcherPanel;
    }
}
