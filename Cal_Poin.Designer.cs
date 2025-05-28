namespace Интерфейс_программы
{
    partial class Cal_Poin
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
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.numericUpDown_x = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown_y = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown_nm = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown_eV = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_x)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_y)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_nm)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_eV)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 128);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "OK";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(93, 128);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 1;
            this.button2.Text = "Cancel";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(93, 157);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(75, 23);
            this.button3.TabIndex = 2;
            this.button3.Text = "Delete";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // numericUpDown_x
            // 
            this.numericUpDown_x.Location = new System.Drawing.Point(12, 12);
            this.numericUpDown_x.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.numericUpDown_x.Minimum = new decimal(new int[] {
            1000000,
            0,
            0,
            -2147483648});
            this.numericUpDown_x.Name = "numericUpDown_x";
            this.numericUpDown_x.Size = new System.Drawing.Size(120, 23);
            this.numericUpDown_x.TabIndex = 3;
            this.numericUpDown_x.ValueChanged += new System.EventHandler(this.numericUpDown_x_ValueChanged);
            // 
            // numericUpDown_y
            // 
            this.numericUpDown_y.Location = new System.Drawing.Point(12, 41);
            this.numericUpDown_y.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.numericUpDown_y.Minimum = new decimal(new int[] {
            1000000,
            0,
            0,
            -2147483648});
            this.numericUpDown_y.Name = "numericUpDown_y";
            this.numericUpDown_y.Size = new System.Drawing.Size(120, 23);
            this.numericUpDown_y.TabIndex = 4;
            this.numericUpDown_y.ValueChanged += new System.EventHandler(this.numericUpDown_y_ValueChanged);
            // 
            // numericUpDown_nm
            // 
            this.numericUpDown_nm.DecimalPlaces = 3;
            this.numericUpDown_nm.Location = new System.Drawing.Point(12, 70);
            this.numericUpDown_nm.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.numericUpDown_nm.Minimum = new decimal(new int[] {
            1000000,
            0,
            0,
            -2147483648});
            this.numericUpDown_nm.Name = "numericUpDown_nm";
            this.numericUpDown_nm.Size = new System.Drawing.Size(120, 23);
            this.numericUpDown_nm.TabIndex = 5;
            this.numericUpDown_nm.ValueChanged += new System.EventHandler(this.numericUpDown_nm_ValueChanged);
            // 
            // numericUpDown_eV
            // 
            this.numericUpDown_eV.DecimalPlaces = 3;
            this.numericUpDown_eV.Location = new System.Drawing.Point(12, 99);
            this.numericUpDown_eV.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.numericUpDown_eV.Minimum = new decimal(new int[] {
            1000000,
            0,
            0,
            -2147483648});
            this.numericUpDown_eV.Name = "numericUpDown_eV";
            this.numericUpDown_eV.Size = new System.Drawing.Size(120, 23);
            this.numericUpDown_eV.TabIndex = 6;
            this.numericUpDown_eV.ValueChanged += new System.EventHandler(this.numericUpDown_eV_ValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(138, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(13, 15);
            this.label1.TabIndex = 7;
            this.label1.Text = "x";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(138, 43);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(13, 15);
            this.label2.TabIndex = 7;
            this.label2.Text = "y";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(138, 72);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(25, 15);
            this.label3.TabIndex = 7;
            this.label3.Text = "nm";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(138, 101);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(20, 15);
            this.label4.TabIndex = 7;
            this.label4.Text = "eV";
            // 
            // Cal_Poin
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(174, 187);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.numericUpDown_eV);
            this.Controls.Add(this.numericUpDown_nm);
            this.Controls.Add(this.numericUpDown_y);
            this.Controls.Add(this.numericUpDown_x);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Cal_Poin";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Point";
            this.Load += new System.EventHandler(this.Cal_Poin_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_x)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_y)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_nm)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_eV)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Button button1;
        private Button button2;
        private Button button3;
        private NumericUpDown numericUpDown_x;
        private NumericUpDown numericUpDown_y;
        private NumericUpDown numericUpDown_nm;
        private NumericUpDown numericUpDown_eV;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
    }
}