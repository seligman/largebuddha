namespace ShowProgress
{
    partial class ProgressForm
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
            this.m_buddha = new System.Windows.Forms.CheckBox();
            this.m_progress = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.m_progress)).BeginInit();
            this.SuspendLayout();
            // 
            // m_buddha
            // 
            this.m_buddha.AutoSize = true;
            this.m_buddha.Location = new System.Drawing.Point(138, 16);
            this.m_buddha.Name = "m_buddha";
            this.m_buddha.Size = new System.Drawing.Size(93, 17);
            this.m_buddha.TabIndex = 1;
            this.m_buddha.Text = "Show Buddha";
            this.m_buddha.UseVisualStyleBackColor = true;
            this.m_buddha.CheckedChanged += new System.EventHandler(this.Buddha_CheckedChanged);
            // 
            // m_progress
            // 
            this.m_progress.DecimalPlaces = 4;
            this.m_progress.Increment = new decimal(new int[] {
            25,
            0,
            0,
            131072});
            this.m_progress.Location = new System.Drawing.Point(12, 15);
            this.m_progress.Name = "m_progress";
            this.m_progress.Size = new System.Drawing.Size(120, 20);
            this.m_progress.TabIndex = 0;
            this.m_progress.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.m_progress.ValueChanged += new System.EventHandler(this.Progress_ValueChanged);
            // 
            // ProgressForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.m_progress);
            this.Controls.Add(this.m_buddha);
            this.DoubleBuffered = true;
            this.Name = "ProgressForm";
            this.Text = "Progress";
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.ProgressForm_Paint);
            ((System.ComponentModel.ISupportInitialize)(this.m_progress)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox m_buddha;
        private System.Windows.Forms.NumericUpDown m_progress;
    }
}

