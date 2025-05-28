using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Интерфейс_программы
{
    public partial class Cal_Poin : Form
    {
        public LPI_to_e2v.callibrationPoint point;
        public int result = 0;
        double nm_to_eV = 1239.825;

        public Cal_Poin()
        {
            InitializeComponent();
        }

        private void numericUpDown_x_ValueChanged(object sender, EventArgs e)
        {
            numericUpDown_x.Value = Math.Floor(numericUpDown_x.Value);
            point.x = (int)numericUpDown_x.Value;
        }

        private void numericUpDown_y_ValueChanged(object sender, EventArgs e)
        {
            numericUpDown_y.Value = Math.Floor(numericUpDown_y.Value);
            point.y = (int)numericUpDown_y.Value;
        }

        private void numericUpDown_nm_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDown_nm.Value == 0) numericUpDown_eV.Value = 999999;
            else numericUpDown_eV.Value = (decimal)nm_to_eV / numericUpDown_nm.Value;
            point.nm = (double)numericUpDown_nm.Value;
        }

        private void numericUpDown_eV_ValueChanged(object sender, EventArgs e)
        {

            if (numericUpDown_eV.Value == 0) numericUpDown_nm.Value = 999999;
            else numericUpDown_nm.Value = (decimal)nm_to_eV / numericUpDown_eV.Value;
            point.eV = (double)numericUpDown_eV.Value;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            result = 1;
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            result = 0;
            if (point.x == 0 && point.y ==0 && point.nm == 1) result = 2;
            this.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            result = 2;
            this.Close();
        }

        private void Cal_Poin_Load(object sender, EventArgs e)
        {
            numericUpDown_x.Value = point.x;
            numericUpDown_y.Value = point.y;
            numericUpDown_nm.Value = (decimal)point.nm;
            numericUpDown_eV.Value = (decimal)point.eV;
        }
    }
}
