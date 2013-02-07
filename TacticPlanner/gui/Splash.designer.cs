namespace TacticPlanner.gui {
    partial class Splash {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose ( bool disposing ) {
            if ( disposing && ( components != null ) ) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent () {
            this.components = new System.ComponentModel.Container();
            this.felirat = new System.Windows.Forms.Label();
            this.closer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // felirat
            // 
            this.felirat.AutoSize = true;
            this.felirat.BackColor = System.Drawing.Color.Transparent;
            this.felirat.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.felirat.ForeColor = System.Drawing.Color.Azure;
            this.felirat.Location = new System.Drawing.Point(99, 156);
            this.felirat.Name = "felirat";
            this.felirat.Size = new System.Drawing.Size(124, 29);
            this.felirat.TabIndex = 0;
            this.felirat.Text = "RiskaSoft";
            this.felirat.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // closer
            // 
            this.closer.Enabled = true;
            this.closer.Interval = 2000;
            this.closer.Tick += new System.EventHandler(this.closer_Tick);
            // 
            // Splash
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.BackgroundImage = global::TacticPlanner.Properties.Resources.spcow;
            this.ClientSize = new System.Drawing.Size(320, 240);
            this.Controls.Add(this.felirat);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Splash";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Splash";
            this.TopMost = true;
            this.TransparencyKey = System.Drawing.Color.Black;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label felirat;
        private System.Windows.Forms.Timer closer;
    }
}