namespace TacticPlanner.gui {
    partial class AddTank {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.label1 = new System.Windows.Forms.Label();
            this.playername = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.add = new System.Windows.Forms.Button();
            this.cancel = new System.Windows.Forms.Button();
            this.cmbTank = new System.Windows.Forms.ComboBox();
            this.chkisAlly = new System.Windows.Forms.RadioButton();
            this.chkisEnemy = new System.Windows.Forms.RadioButton();
            this.cmbnation = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(68, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Player name:";
            // 
            // playername
            // 
            this.playername.BackColor = System.Drawing.Color.Gray;
            this.playername.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.playername.ForeColor = System.Drawing.Color.White;
            this.playername.Location = new System.Drawing.Point(124, 15);
            this.playername.MaxLength = 10;
            this.playername.Name = "playername";
            this.playername.Size = new System.Drawing.Size(156, 20);
            this.playername.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 86);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Tank:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 122);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(31, 13);
            this.label3.TabIndex = 3;
            this.label3.Text = "Side:";
            // 
            // add
            // 
            this.add.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.add.Location = new System.Drawing.Point(205, 154);
            this.add.Name = "add";
            this.add.Size = new System.Drawing.Size(75, 23);
            this.add.TabIndex = 4;
            this.add.Text = "Add";
            this.add.UseVisualStyleBackColor = true;
            this.add.Click += new System.EventHandler(this.add_Click);
            // 
            // cancel
            // 
            this.cancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cancel.Location = new System.Drawing.Point(124, 154);
            this.cancel.Name = "cancel";
            this.cancel.Size = new System.Drawing.Size(75, 23);
            this.cancel.TabIndex = 5;
            this.cancel.Text = "Cancel";
            this.cancel.UseVisualStyleBackColor = true;
            this.cancel.Click += new System.EventHandler(this.cancel_Click);
            // 
            // cmbTank
            // 
            this.cmbTank.BackColor = System.Drawing.Color.Gray;
            this.cmbTank.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTank.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbTank.ForeColor = System.Drawing.Color.White;
            this.cmbTank.FormattingEnabled = true;
            this.cmbTank.Location = new System.Drawing.Point(124, 83);
            this.cmbTank.Name = "cmbTank";
            this.cmbTank.Size = new System.Drawing.Size(156, 21);
            this.cmbTank.TabIndex = 6;
            // 
            // chkisAlly
            // 
            this.chkisAlly.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkisAlly.Checked = true;
            this.chkisAlly.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkisAlly.Location = new System.Drawing.Point(124, 116);
            this.chkisAlly.Name = "chkisAlly";
            this.chkisAlly.Size = new System.Drawing.Size(75, 25);
            this.chkisAlly.TabIndex = 7;
            this.chkisAlly.TabStop = true;
            this.chkisAlly.Text = "Ally";
            this.chkisAlly.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.chkisAlly.UseVisualStyleBackColor = true;
            // 
            // chkisEnemy
            // 
            this.chkisEnemy.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkisEnemy.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkisEnemy.Location = new System.Drawing.Point(205, 116);
            this.chkisEnemy.Name = "chkisEnemy";
            this.chkisEnemy.Size = new System.Drawing.Size(75, 25);
            this.chkisEnemy.TabIndex = 8;
            this.chkisEnemy.Text = "Enemy";
            this.chkisEnemy.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.chkisEnemy.UseVisualStyleBackColor = true;
            // 
            // cmbnation
            // 
            this.cmbnation.BackColor = System.Drawing.Color.Gray;
            this.cmbnation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbnation.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbnation.ForeColor = System.Drawing.Color.White;
            this.cmbnation.FormattingEnabled = true;
            this.cmbnation.Location = new System.Drawing.Point(124, 45);
            this.cmbnation.Name = "cmbnation";
            this.cmbnation.Size = new System.Drawing.Size(156, 21);
            this.cmbnation.TabIndex = 10;
            this.cmbnation.SelectedIndexChanged += new System.EventHandler(this.cmbnation_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 48);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(41, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "Nation:";
            // 
            // AddTank
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(289, 189);
            this.Controls.Add(this.cmbnation);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.chkisEnemy);
            this.Controls.Add(this.chkisAlly);
            this.Controls.Add(this.cmbTank);
            this.Controls.Add(this.cancel);
            this.Controls.Add(this.add);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.playername);
            this.Controls.Add(this.label1);
            this.ForeColor = System.Drawing.Color.White;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AddTank";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Add tank";
            this.Load += new System.EventHandler(this.AddTank_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox playername;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button add;
        private System.Windows.Forms.Button cancel;
        private System.Windows.Forms.ComboBox cmbTank;
        private System.Windows.Forms.RadioButton chkisAlly;
        private System.Windows.Forms.RadioButton chkisEnemy;
        private System.Windows.Forms.ComboBox cmbnation;
        private System.Windows.Forms.Label label4;
    }
}