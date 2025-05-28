using System;
using System.Collections;
using System.Drawing.Imaging;
using System.Globalization;
using System.Net.Sockets;
using System.Net;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Diagnostics;
using System.Drawing.Printing;

namespace Интерфейс_программы
{
    public partial class LPI_to_e2v : Form
    {

        public struct fit
        {
            public int[,] Data;
            public int Width;
            public int Height;
            public int Header;
            public string HeaderString;
            public string filePath;
            public fit Copy()
            {
                fit B = new fit();
                B.Data = (int[,])this.Data.Clone();
                B.Width = this.Width;
                B.Height = this.Height;
                B.Header = this.Header;
                B.HeaderString = this.HeaderString;
                B.filePath = this.filePath;
                return B;
            }
        }

        fit dataToShow;
        fit[] allData;
        public struct callibrationPoint
        {
            public int x;
            public int y;
            public double nm;
            public double eV;
        }

        callibrationPoint[] callibrationPoints = new callibrationPoint[0];
        double nm_to_eV = 1239.825;




        int CCD_Data_Width = 516;
        int CCD_Data_Height = 2148;
        int CCD_Byte = 3;
        int CCD_Readout_Freq = 4;
        int CCD_Exposition = 1;
        int CCD_Mode = 0;
        int CCD_Brust_N = 10;
        int CCD_Brust_N_left = 0;
        int CCD_Brust_Delay = 1000;

        int raw_Data_Width = 516;
        int raw_Data_Height = 2148;
        int raw_Data_Header = 2880;
        int raw_Data_Byte = 4;

        bool CCD_Started = false;

        double zoom = 1;

        Form Add_Callibration_Point = new Form();


        public LPI_to_e2v()
        {
            InitializeComponent();

            timer1.Start();

            numericUpDown_DataStr_Width.Value = raw_Data_Width;
            numericUpDown_DataStr_Height.Value = raw_Data_Height;
            numericUpDown_DataStr_Header.Value = raw_Data_Header;

            dataToShow.filePath = "DataToShow";
            dataToShow.Width = raw_Data_Width;
            dataToShow.Height = raw_Data_Height;
            dataToShow.Data = new int[dataToShow.Width, dataToShow.Height];
            dataToShow.Header = 0;
            dataToShow.HeaderString = "";


            Array.Resize(ref allData, 1);

            allData[0].filePath = "Current";
            allData[0].Width = raw_Data_Width;
            allData[0].Height = raw_Data_Height;
            allData[0].Data = new int[allData[0].Width, allData[0].Height];
            allData[0].Header = raw_Data_Header;
            allData[0].HeaderString = "";

            comboBox_Current_Images.Items.Clear();
            comboBox_Current_Images.Items.Add(allData[0].filePath);
            comboBox_Current_Images.Items.Add("Delete All");
            comboBox_Current_Images.SelectedIndex = comboBox_Current_Images.Items.Count - 2;

            comboBox_Current_Background.Items.Clear();
            comboBox_Current_Background.Items.Add(allData[0].filePath);
            comboBox_Current_Background.SelectedIndex = comboBox_Current_Background.Items.Count - 1;

            comboBox_Callibration_Points.Items.Clear();
            comboBox_Callibration_Points.Items.Add("");
            comboBox_Callibration_Points.Items.Add("Add");
            comboBox_Callibration_Points.Items.Add("Delete All");
            comboBox_Callibration_Points.SelectedIndex = 0;


            numericUpDown_CCD_Width.Value = CCD_Data_Width;
            numericUpDown_CCD_Height.Value = CCD_Data_Height;

            numericUpDown_DataStr_Width.Value = raw_Data_Width;
            numericUpDown_DataStr_Height.Value = raw_Data_Height;

            numericUpDown_Exposition.Value = CCD_Exposition;
            switch (CCD_Readout_Freq)
            {
                case 1:
                    radioButton_Readout_Freq_1000.Checked = true;
                    break;
                case 2:
                    radioButton_Readout_Freq_500.Checked = true;
                    break;
                case 4:
                    radioButton_Readout_Freq_250.Checked = true;
                    break;
            }
            if ((CCD_Mode & 0x04) == 1) checkBox_Trigger.Checked = true;
            else checkBox_Trigger.Checked = false;

            if ((CCD_Mode & 0x02) == 1) radioButton_ShotMode_Brust.Checked = true;
            else radioButton_ShotMode_Single.Checked = true;

            if ((CCD_Mode & 0x01) == 1) CCD_Started = true;
            else CCD_Started = false;

            numericUpDown_Brust_N_Shots.Value = CCD_Brust_N;
            numericUpDown_Brust_Delay.Value = CCD_Brust_Delay;

            numericUpDown_CCD_Width.Value = CCD_Data_Width;
            numericUpDown_CCD_Height.Value = CCD_Data_Height;
            numericUpDown_CCD_Byte.Value = CCD_Byte;

            textBox_Auto_Save_Name.Text = Set_Auto_Save_Name_Example();

            ReDraw_Image();
        }


        #region Files
        private void Open_New_Image_From_File()
        {
            string filePath;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = textBox_Auto_Save_Path.Text;
                openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = openFileDialog.FileName;

                    Read_Data_From_File(filePath);

                    comboBox_Current_Images.Items.RemoveAt(comboBox_Current_Images.Items.Count - 1);
                    comboBox_Current_Images.Items.Add(filePath);
                    comboBox_Current_Images.Items.Add("Delete All");
                    comboBox_Current_Images.SelectedIndex = comboBox_Current_Images.Items.Count - 2;

                    comboBox_Current_Background.Items.Add(filePath);
                }
            }
        }

        private void Read_Data_From_File(string filePath)
        {
            byte[] data_From_File = new byte[1] { 0 };

            try
            {
                data_From_File = File.ReadAllBytes(filePath);
  

            int index = allData.Length;
            Array.Resize(ref allData, index + 1);
            index = allData.Length - 1;

            allData[index].filePath = filePath;
            allData[index].Header = raw_Data_Header;
            allData[index].HeaderString = "";
            if (data_From_File.Length > allData[index].Header)
            {
                allData[index].HeaderString = System.Text.Encoding.UTF8.GetString(data_From_File[0..allData[index].Header]);

            }
            allData[index].Width = raw_Data_Width;
            allData[index].Height = raw_Data_Height;
            allData[index].Data = new int[allData[index].Width, allData[index].Height];

            int point_ij = 0;
            if (data_From_File.Length > 1)
                for (int i = 0; i < allData[index].Width; i++)
                    for (int j = 0; j < allData[index].Height; j++)
                    {
                        point_ij = allData[index].Header + (i * allData[index].Height + j) * raw_Data_Byte;
                        if (point_ij > data_From_File.Length - 1) point_ij = data_From_File.Length - raw_Data_Byte;
//                        byte[] d_array = data_From_File[point_ij..(point_ij + raw_Data_Byte)];
                            allData[index].Data[i, j] = BitConverter.ToInt32(data_From_File, point_ij);
                            //                        if (point_ij > data_From_File.Length - 1) point_ij = data_From_File.Length - 2;
                            //                        allData[index].Data[i, j] = (int)((data_From_File[point_ij]) * (256) + (data_From_File[point_ij + 1]));
                        }
            }
            catch (Exception q)
            {
                MessageBox.Show(q.Message);
            }
        }

        private void button_Open_new_image_Click(object sender, EventArgs e)
        {
            Open_New_Image_From_File();
            //Add
        }

        private void comboBox_Current_Images_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_Current_Images.SelectedIndex == comboBox_Current_Images.Items.Count - 1)
            {
                comboBox_Current_Images.Items.Clear();
                comboBox_Current_Images.Items.Add(allData[0].filePath);
                comboBox_Current_Images.Items.Add("Delete All");
                comboBox_Current_Images.SelectedIndex = comboBox_Current_Images.Items.Count - 2;

                comboBox_Current_Background.Items.Clear();
                comboBox_Current_Background.Items.Add(allData[0].filePath);
                comboBox_Current_Background.SelectedIndex = comboBox_Current_Background.Items.Count - 1;
            }
            else
            {
                DataToShow_Calc();
                ReDraw_Image();
            }
            //Add
        }

        private void button_Close_This_image_Click(object sender, EventArgs e)
        {
            int selIndex = comboBox_Current_Images.SelectedIndex;
            if (selIndex != 0)
            {
                comboBox_Current_Images.Items.RemoveAt(selIndex);
                comboBox_Current_Images.SelectedIndex = selIndex - 1;

                if (comboBox_Current_Background.SelectedIndex == selIndex) comboBox_Current_Background.SelectedIndex = selIndex - 1;
                comboBox_Current_Background.Items.RemoveAt(selIndex);
            }
            //Add
        }

        private void checkBox_Subtract_Background_CheckedChanged(object sender, EventArgs e)
        {
            checkBox_Subtract_Background_Copy.Checked = checkBox_Subtract_Background.Checked;
            DataToShow_Calc();
            ReDraw_Image();
        }

        private void checkBox_Subtract_Background_Copy_CheckedChanged(object sender, EventArgs e)
        {
            checkBox_Subtract_Background.Checked = checkBox_Subtract_Background_Copy.Checked;
        }

        private void comboBox_Current_Background_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (checkBox_Subtract_Background.Checked)
            {
                DataToShow_Calc();
                ReDraw_Image();
            }
        }

        private string Set_Auto_Save_Name()
        {
            string result = textBox_Auto_Save_Path.Text;
            result = "";
            string current_Data = DateTime.Now.ToString("u").Replace("Z", "").Replace(":", ".");
            //current_Data = "yyyy-MM-dd hh.mm.ss";
            string counter = ((int)numericUpDown_Auto_Save_Counter.Value).ToString("D" + numericUpDown_Auto_Save_Counter_Zeros.Value.ToString("G"));
            string seed = textBox_Auto_Save_Name_Seed.Text;

            if (checkBox_Add_Counter_1.Checked) result += counter + "--";
            if (checkBox_Add_Date_1.Checked) result += current_Data + "--";
            result += seed;
            if (checkBox_Add_Date_2.Checked) result += "--" + current_Data;
            if (checkBox_Add_Counter_2.Checked) result += "--" + counter;

            result += ".fits";
            return result;
        }

        private string Set_Auto_Save_Name_Example()
        {
            string result = "";
            string current_Data = DateTime.Now.ToString("u").Replace("Z", "").Replace(":", ".");
            current_Data = "yyyy-MM-dd hh.mm.ss";
            string counter = ((int)numericUpDown_Auto_Save_Counter.Value).ToString("D" + numericUpDown_Auto_Save_Counter_Zeros.Value.ToString("G"));
            string seed = textBox_Auto_Save_Name_Seed.Text;

            if (checkBox_Add_Counter_1.Checked) result += counter + "-";
            if (checkBox_Add_Date_1.Checked) result += current_Data + "-";
            result += seed;
            if (checkBox_Add_Date_2.Checked) result += "-" + current_Data;
            if (checkBox_Add_Counter_2.Checked) result += "-" + counter;

            return result;
        }

        private void Auto_Save_Name_Example_Changed(object sender, EventArgs e)
        {
            numericUpDown_Auto_Save_Counter.Value = Math.Round(numericUpDown_Auto_Save_Counter.Value);
            numericUpDown_Auto_Save_Counter_Zeros.Value = Math.Round(numericUpDown_Auto_Save_Counter_Zeros.Value);

            textBox_Auto_Save_Name.Text = Set_Auto_Save_Name_Example();
        }

        private void button_Save_BMP_Click(object sender, EventArgs e)
        {
            string filePath;

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory = textBox_Auto_Save_Path.Text;
                saveFileDialog.FileName = Set_Auto_Save_Name();
                saveFileDialog.Filter = "bmp files (*.bmp)|*.bmp|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = saveFileDialog.FileName;

                    pictureBox1.Image.Save(filePath);
                }
            }
        }



        private void Save_Data_To_File(string filePath, int index)
        {

            fit dataToSave = allData[index].Copy();

            byte[] data_To_File = new byte[dataToSave.Header + dataToSave.Width * dataToSave.Height * raw_Data_Byte];

            dataToSave.HeaderString = dataToSave.HeaderString.PadRight(dataToSave.Header);

            for (int i = 0; i < dataToSave.Header; i++) data_To_File[i] = (byte)dataToSave.HeaderString.ElementAt(i);
            Parallel.For(0, dataToSave.Width, i =>
            {
                for (int j = 0; j < dataToSave.Height; j++)
                {
                    int d = dataToSave.Data[i, j];
                    byte[] d_array = BitConverter.GetBytes(d);
                    for (int k = 0;k < raw_Data_Byte; k++) data_To_File[dataToSave.Header + i * dataToSave.Height * raw_Data_Byte + j * raw_Data_Byte + k] = d_array[k];
                    //                    int d = dataToSave.Data[i, j];
                    //                    if (d < 0) d = 0;
                    //                    if (d > 65535) d = 65535;
                    //                    byte d1 = (byte)Math.Floor(d / 256.0);
                    //                    byte d2 = (byte)(d - d1 * 256);
                    //                    data_To_File[dataToSave.Header + i * dataToSave.Height * 2 + j * 2] = d1;
                    //                    data_To_File[dataToSave.Header + i * dataToSave.Height * 2 + j * 2 + 1] = d2;


                }

            });

            try
            {
                using (FileStream fstream = new FileStream(filePath, FileMode.OpenOrCreate))
                {
                    fstream.Write(data_To_File, 0, data_To_File.Length);
                }
            }
            catch (Exception q) { MessageBox.Show(q.Message); };

        }


        private void button_Save_Data_Click(object sender, EventArgs e)
        {
            string filePath;

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory = textBox_Auto_Save_Path.Text;
                saveFileDialog.FileName = Set_Auto_Save_Name();
                saveFileDialog.Filter = "fits files (*.fits)|*.fits|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = saveFileDialog.FileName;
                    Save_Data_To_File(filePath, comboBox_Current_Images.SelectedIndex);
                }
            }

        }


        #endregion

        #region Visualisation

        private void ReDraw_Image()
        {
            pictureBox1.Height = (int)Math.Floor(dataToShow.Height * zoom);
            pictureBox1.Width = (int)Math.Floor(dataToShow.Width * zoom);
            ReDraw_Data();
            Draw_ColorScale();
            set_Base_Line();
            set_First_Line();
            set_Second_Line();
            calc_Spectrum();
            Draw_BaseLine();
            Draw_FirstLine();
            Draw_SecondLine();
            draw_Spectrum();
            Draw_Callibration();
            //Add
        }

        private void ReDraw_Data()
        {
            unsafe
            {
                Bitmap bmap = new Bitmap((int)Math.Floor(dataToShow.Width * zoom), (int)Math.Floor(dataToShow.Height * zoom));
                BitmapData bmapData = bmap.LockBits(new Rectangle(0, 0, bmap.Width, bmap.Height), ImageLockMode.ReadWrite, bmap.PixelFormat);
                int bytesPerPixel = Bitmap.GetPixelFormatSize(bmapData.PixelFormat) / 8;
                int heithInPixels = bmapData.Height;
                int widthInBytes = bmapData.Width * bytesPerPixel;

                byte* PtrFirstPixel = (byte*)bmapData.Scan0;

                Parallel.For(0, heithInPixels, y =>
                {
                    byte* currentLine = PtrFirstPixel + (y * bmapData.Stride);

                    for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                    {
                        byte[] color1 = Calc_Color(dataToShow.Data[(int)Math.Floor(x / bytesPerPixel / zoom), (int)Math.Floor(y / zoom)]);

                        currentLine[x] = color1[2];
                        currentLine[x + 1] = color1[1];
                        currentLine[x + 2] = color1[0];
                        currentLine[x + 3] = 255;

                    }
                });
                bmap.UnlockBits(bmapData);
                pictureBox1.Image = bmap;
            }
            System.GC.Collect();
        }

        private byte[] Calc_Color(int data)
        {
            byte[] calc_Color = new byte[3];
            double colorDouble = (double)data;
            double gamma = (double)numericUpDown_GammaFactor.Value;

            colorDouble = (colorDouble - (double)numericUpDown_Show_From.Value) / (double)(numericUpDown_Show_To.Value - numericUpDown_Show_From.Value);

            if (colorDouble < 0) colorDouble = 0;
            if (colorDouble > 1) colorDouble = 1;

            if (gamma > 0) colorDouble = Math.Pow(colorDouble, gamma);

            if (radioButton_Mode_GrayScale.Checked == true)
            {
                calc_Color[0] = (byte)(Math.Floor(255 * colorDouble));
                calc_Color[1] = calc_Color[0];
                calc_Color[2] = calc_Color[1];
            }
            else
            {
                if (colorDouble == 1)
                {
                    calc_Color[0] = 255;
                    calc_Color[1] = 255;
                    calc_Color[2] = 255;
                }
                else
                {
                    colorDouble = (1 - colorDouble) / 0.2;
                    byte i = (byte)Math.Floor(colorDouble);
                    byte Y = (byte)Math.Floor(255 * (colorDouble - i));
                    switch (i)
                    {
                        case 0: calc_Color[0] = 255; calc_Color[1] = Y; calc_Color[2] = 0; break;
                        case 1: calc_Color[0] = (byte)(255 - Y); calc_Color[1] = 255; calc_Color[2] = 0; break;
                        case 2: calc_Color[0] = 0; calc_Color[1] = 255; calc_Color[2] = Y; break;
                        case 3: calc_Color[0] = 0; calc_Color[1] = (byte)(255 - Y); calc_Color[2] = 255; break;
                        case 4: calc_Color[0] = Y; calc_Color[1] = 0; calc_Color[2] = 255; break;
                        case 5: calc_Color[0] = 0; calc_Color[1] = 0; calc_Color[2] = 0; break;
                    }
                }
            }
            return calc_Color;
        }

        private void numericUpDown_Show_From_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDown_Show_From.Value >= numericUpDown_Show_To.Value) numericUpDown_Show_From.Value = numericUpDown_Show_To.Value - 1;
            numericUpDown_Show_From.Value = Math.Round(numericUpDown_Show_From.Value, 0);
            ReDraw_Image();
        }

        private void numericUpDown_Show_To_ValueChanged(object sender, EventArgs e)
        {
            if (numericUpDown_Show_From.Value >= numericUpDown_Show_To.Value) numericUpDown_Show_To.Value = numericUpDown_Show_From.Value + 1;
            numericUpDown_Show_To.Value = Math.Round(numericUpDown_Show_To.Value, 0);
            ReDraw_Image();
        }


        private void radioButton_Mode_CheckedChanged(object sender, EventArgs e)
        {
            ReDraw_Image();
        }

        private void numericUpDown_GammaFactor_ValueChanged(object sender, EventArgs e)
        {
            numericUpDown_GammaFactor.Value = Math.Round(numericUpDown_GammaFactor.Value, 2);
            ReDraw_Image();
        }

        private void change_Image_View(object sender, EventArgs e)
        {
            numericUpDown_Binning_X.Value = Math.Round(numericUpDown_Binning_X.Value);
            numericUpDown_Binning_Y.Value = Math.Round(numericUpDown_Binning_Y.Value);
            DataToShow_Calc();
            ReDraw_Image();
        }

        private void Show_Color_Label_Changed(object sender, EventArgs e)
        {
            numericUpDown_Show_Color_Label_X_Position.Value = Math.Round(numericUpDown_Show_Color_Label_X_Position.Value);
            numericUpDown_Show_Color_Label_X_Scale.Value = Math.Round(numericUpDown_Show_Color_Label_X_Scale.Value);
            numericUpDown_Show_Color_Label_Y_Position.Value = Math.Round(numericUpDown_Show_Color_Label_Y_Position.Value);
            numericUpDown_Show_Color_Label_Y_Scale.Value = Math.Round(numericUpDown_Show_Color_Label_Y_Scale.Value);

            ReDraw_Image();
        }

        private void numericUpDown_Zoom_ValueChanged(object sender, EventArgs e)
        {
            numericUpDown_Zoom.Value = Math.Round(numericUpDown_Zoom.Value);
            if (numericUpDown_Zoom.Value == 0) if (zoom >= 1) numericUpDown_Zoom.Value = -2;
            if (numericUpDown_Zoom.Value == -1) if (zoom < 1) numericUpDown_Zoom.Value = 1;

            if (numericUpDown_Zoom.Value < 0) zoom = -1.0 / (double)numericUpDown_Zoom.Value;
            else zoom = (int)numericUpDown_Zoom.Value;
            ReDraw_Image();
        }

        private void Draw_ColorScale()
        {
            if (!checkBox_Show_Color_Label.Checked) return;

            Bitmap bmap = (Bitmap)pictureBox1.Image;

            int pos_X = (int)numericUpDown_Show_Color_Label_X_Position.Value;
            pos_X = Math.Min(bmap.Width - 1, pos_X);
            int pos_Y = (int)numericUpDown_Show_Color_Label_Y_Position.Value;
            pos_Y = Math.Min(bmap.Height - 1, pos_Y);
            int width_CL = (int)numericUpDown_Show_Color_Label_X_Scale.Value;
            int height_CL = (int)numericUpDown_Show_Color_Label_Y_Scale.Value;


            unsafe
            {
                BitmapData bmapData = bmap.LockBits(new Rectangle(pos_X, pos_Y, Math.Max(0, Math.Min(bmap.Width - pos_X, width_CL)), Math.Max(0, Math.Min(bmap.Height - pos_Y, height_CL))), ImageLockMode.ReadWrite, bmap.PixelFormat);
                int bytesPerPixel = Bitmap.GetPixelFormatSize(bmapData.PixelFormat) / 8;
                int heithInPixels = bmapData.Height;
                int widthInBytes = bmapData.Width * bytesPerPixel;

                byte* PtrFirstPixel = (byte*)bmapData.Scan0;

                Parallel.For(0, heithInPixels, y =>
                {
                    byte* currentLine = PtrFirstPixel + (y * bmapData.Stride);

                    for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                    {
                        byte[] color1 = Calc_Color((int)(((double)(x / bytesPerPixel) / (double)width_CL) * ((double)numericUpDown_Show_To.Value - (double)numericUpDown_Show_From.Value) + (double)numericUpDown_Show_From.Value));

                        currentLine[x] = color1[2];
                        currentLine[x + 1] = color1[1];
                        currentLine[x + 2] = color1[0];
                        currentLine[x + 3] = 255;

                    }
                });
                bmap.UnlockBits(bmapData);
                pictureBox1.Image = bmap;
            }
            System.GC.Collect();


            int min_value = (int)numericUpDown_Show_From.Value;
            int max_value = (int)numericUpDown_Show_To.Value;


            Font font = new Font("Times New Roman", (int)(height_CL * 0.6), GraphicsUnit.Pixel);
            Graphics e = Graphics.FromImage(pictureBox1.Image);

            Point point1 = new Point(pos_X - (int)(height_CL * 0.3) * (int)(2 + Math.Log10(min_value == 0 ? 1 : Math.Abs(min_value))), pos_Y + (int)(height_CL * 0.2));
            e.DrawString(min_value.ToString(), font, Brushes.White, point1);

            point1 = new Point(pos_X + width_CL, pos_Y + (int)(height_CL * 0.2));
            e.DrawString(max_value.ToString(), font, Brushes.White, point1);

            pictureBox1.Invalidate();

        }



        private void Draw_Callibration()
        {
            if (!checkBox_Show_Callibration.Checked) return;

            Font font = new Font("Times New Roman", (int)numericUpDown_Callibrations_Size.Value, GraphicsUnit.Pixel);
            Graphics e = Graphics.FromImage(pictureBox1.Image);
            SolidBrush drawBrush = new SolidBrush(button_Callibration_Color.BackColor);
            Pen pen = new Pen(button_Callibration_Color.BackColor);

            double st_x = 0;
            double st_y = 2.5*(double)numericUpDown_Callibrations_Size.Value;
            double st_v = baseLine.v;
            double st_u = baseLine.u;

            double st_x0 = baseLine.x0;
            double st_y0 = baseLine.y0;
            double st_x1 = st_x0;
            double st_y1 = st_y0;

            if (st_v == 0) { st_y0 = 0; st_y1 = dataToShow.Height; }
            else if (st_u == 0) { st_x0 = 0; st_x1 = dataToShow.Width; }
            else
            {
                double t = -st_x / st_v;
                st_x0 = 0;
                st_y0 = st_y + t * st_u;

                t = (dataToShow.Width - st_x) / st_v;
                st_x1 = dataToShow.Width;
                st_y1 = st_y + t * st_u;

                if (st_y0 < 0)
                {
                    st_y0 = 0;
                    t = -st_y / st_u;
                    st_x0 = st_x + t * st_v;
                }
                if (st_y0 > dataToShow.Height)
                {
                    st_y0 = dataToShow.Height;
                    t = (dataToShow.Height - st_y) / st_u;
                    st_x0 = st_x + t * st_v;
                }
                if (st_y1 < 0)
                {
                    st_y1 = 0;
                    t = -st_y / st_u;
                    st_x1 = st_x + t * st_v;
                }
                if (st_y1 > dataToShow.Height)
                {
                    st_y1 = dataToShow.Height;
                    t = (dataToShow.Height - st_y) / st_u;
                    st_x1 = st_x + t * st_v;
                }
            }

            double min_nm = callibration_nm(position_On_BaseLine(st_x0, st_y0));
            double max_nm = callibration_nm(position_On_BaseLine(st_x1, st_y1));
            double step = (double)numericUpDown_Callibration_Step.Value;

            if (min_nm > max_nm)
            {
                st_x = st_x1;
                st_y = st_y1;
                st_y1 = st_y0;
                st_x1 = st_x0;
                st_x0 = st_x;
                st_y0 = st_y;
                min_nm = callibration_nm(position_On_BaseLine(st_x0, st_y0));
                max_nm = callibration_nm(position_On_BaseLine(st_x1, st_y1));
            }

            st_v = st_x1 - st_x0;
            st_u = st_y1 - st_y0;

            st_x = Math.Sqrt(st_v * st_v + st_u * st_u);
            st_v = st_v / st_x;
            st_u = st_u / st_x;

            st_x = st_x0;
            st_y = st_y0;
            double current_nm = min_nm;
            double nm_to_achieve = step * Math.Round(min_nm / step);
            
            while (st_x < st_x1)
            {
                current_nm = callibration_nm(position_On_BaseLine(st_x, st_y));
                if (current_nm > nm_to_achieve)
                {
                    e.TranslateTransform((float)(st_x * zoom), (float)(st_y * zoom));
                    e.RotateTransform((float)(Math.Asin(st_u) / Math.PI * 180));
                    if (checkBox_Callibration_nm.Checked)
                    {
                        e.DrawString(nm_to_achieve.ToString("F"), font, drawBrush, 0, 0);
                        e.DrawLine(pen, 0, 0, 0, (int)(2 * numericUpDown_Callibrations_Size.Value));
                    }
                    if (checkBox_Callibration_eV.Checked)
                    {
                        e.DrawString((nm_to_eV/nm_to_achieve).ToString("F"), font, drawBrush, 0, font.Height);
                        e.DrawLine(pen, 0, 0, 0, (int)(2 * numericUpDown_Callibrations_Size.Value));
                    }
                    e.RotateTransform(-(float)(Math.Asin(st_u) / Math.PI * 180));
                    e.TranslateTransform(-(float)(st_x * zoom), -(float)(st_y * zoom));

                    nm_to_achieve = step * Math.Ceiling(current_nm / step);
                }
                st_x += st_v;
                st_y += st_u;

            }

            pictureBox1.Invalidate();
        }


        private void Draw_BaseLine()
        {
            if (!checkBox_Base_Line_Setup.Checked) return;
            Bitmap bmap = (Bitmap)pictureBox1.Image;
            double x = Math.Floor(baseLine.x0 / zoom);
            double y = Math.Floor(baseLine.y0 / zoom);
            double v = baseLine.v;
            double u = baseLine.u;
            double t = 0;
            Color color = Color.White;
            if (baseLine.v == 0) for (y = 0; y < bmap.Height; y++) bmap.SetPixel((int)x, (int)y, color);
            else if (baseLine.u == 0) for (x = 0; x < bmap.Width; x++) bmap.SetPixel((int)x, (int)y, color);
            else
            {
                t = -baseLine.x0 / baseLine.v;
                if (baseLine.y0 + t * baseLine.u < 0) t = -baseLine.y0 / baseLine.u;
                if (baseLine.y0 + t * baseLine.u > dataToShow.Height) t = (dataToShow.Height - baseLine.y0) / baseLine.u;
                x = (baseLine.x0 + t * baseLine.v);
                y = (baseLine.y0 + t * baseLine.u);

                if ((x == 0 && baseLine.v < 0) || (y == 0 && baseLine.u < 0) || (x == dataToShow.Width && baseLine.v > 0) || (y == dataToShow.Height && baseLine.u > 0))
                {
                    v = -v;
                    u = -u;
                }
                while (x >= 0 && y >= 0 && x <= dataToShow.Width && y <= dataToShow.Height)
                {
                    bmap.SetPixel(Math.Max(1, Math.Min(bmap.Width - 1, (int)Math.Floor(x * zoom))), Math.Max(1, Math.Min(bmap.Height - 1, (int)Math.Floor(y * zoom))), color);
                    x += v;
                    y += u;
                }
            }
        }

        private void Draw_FirstLine()
        {
            if (!checkBox_Show_Spectrum.Checked) return;

            double x0 = firstLine.x0;
            double y0 = firstLine.y0;
            double x1 = x0;
            double y1 = y0;
            double v = firstLine.v;
            double u = firstLine.u;

            if (v == 0)
            {
                y0 = 0;
                y1 = dataToShow.Height;
            }
            else if (u == 0)
            {
                x0 = 0;
                x1 = dataToShow.Width;
            }
            else
            {
                double t = 0;
                if (v < 0) { v = -v; u = -u; }

                t = x0 / v;
                x0 = x0 - t * v;
                y0 = y0 - t * u;

                t = (-dataToShow.Width + x0) / v;
                x1 = x0 - t * v;
                y1 = y0 - t * u;
            }


            Graphics e = Graphics.FromImage(pictureBox1.Image);
            Pen pen = new(button_Spectrum_First_Line_Color.BackColor);
            pen.Width = (float)numericUpDown_Spectrum_First_Line_Width.Value;
            //            pen.DashPattern = new float[] { 10, 0, 10, 0 };
            if (checkBox_Spectrum_First_Line_Dash.Checked) pen.DashPattern = new float[] { 5, 5, 5, 5 };
            e.DrawLine(pen, 0, 0,0,0);
            if (numericUpDown_Spectrum_First_Line_Width.Value > 0) e.DrawLine(pen, (int)(x0*zoom), (int)(y0*zoom), (int)(x1*zoom), (int)(y1*zoom));
        }


        private void Draw_SecondLine()
        {
            if (!checkBox_Show_Spectrum.Checked) return;

            double x0 = secondLine.x0;
            double y0 = secondLine.y0;
            double x1 = x0;
            double y1 = y0;
            double v = secondLine.v;
            double u = secondLine.u;

            if (v == 0)
            {
                y0 = 0;
                y1 = dataToShow.Height;
            }
            else if (u == 0)
            {
                x0 = 0;
                x1 = dataToShow.Width;
            }
            else
            {
                double t = 0;
                if (v < 0) { v = -v; u = -u; }

                t = x0 / v;
                x0 = x0 - t * v;
                y0 = y0 - t * u;

                t = (-dataToShow.Width + x0) / v;
                x1 = x0 - t * v;
                y1 = y0 - t * u;
            }


            Graphics e = Graphics.FromImage(pictureBox1.Image);
            Pen pen = new(button_Spectrum_Second_Line_Color.BackColor);
            pen.Width = (float)numericUpDown_Spectrum_Second_Line_Width.Value;
            //            pen.DashPattern = new float[] { 10, 0, 10, 0 };
            if (checkBox_Spectrum_Second_Line_Dash.Checked) pen.DashPattern = new float[] { 5, 5, 5, 5 };

            if (numericUpDown_Spectrum_Second_Line_Width.Value > 0) e.DrawLine(pen, (int)(x0 * zoom), (int)(y0 * zoom), (int)(x1 * zoom), (int)(y1 * zoom));
        }


        private void checkBox_DataStr_As_CCD_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_DataStr_As_CCD.Checked)
            {
                raw_Data_Header = 2880;
                raw_Data_Height = CCD_Data_Height;
                raw_Data_Width = CCD_Data_Width;
                numericUpDown_DataStr_Width.Enabled = false;
                numericUpDown_DataStr_Height.Enabled = false;
                numericUpDown_DataStr_Header.Enabled = false;
            }
            else
            {
                raw_Data_Width = (int)numericUpDown_DataStr_Width.Value;
                raw_Data_Height = (int)numericUpDown_DataStr_Height.Value;
                raw_Data_Header = (int)numericUpDown_DataStr_Header.Value;
                numericUpDown_DataStr_Width.Enabled = true;
                numericUpDown_DataStr_Height.Enabled = true;
                numericUpDown_DataStr_Header.Enabled = true;
            }
        }

        private void numericUpDown_DataStr_ValueChanged(object sender, EventArgs e)
        {
            numericUpDown_DataStr_Width.Value = Math.Round(numericUpDown_DataStr_Width.Value);
            numericUpDown_DataStr_Height.Value = Math.Round(numericUpDown_DataStr_Height.Value);
            numericUpDown_DataStr_Header.Value = Math.Round(numericUpDown_DataStr_Header.Value);
            raw_Data_Width = (int)numericUpDown_DataStr_Width.Value;
            raw_Data_Height = (int)numericUpDown_DataStr_Height.Value;
            raw_Data_Header = (int)numericUpDown_DataStr_Header.Value;
        }

        double[,] spectrum = new double[,] { { 0} };
        private void calc_Spectrum()
        {
            if (!checkBox_Show_Spectrum.Checked) return;
            double x0 = firstLine.x0;
            double y0 = firstLine.y0;
            double x1 = x0;
            double y1 = y0;
            double v = firstLine.v;
            double u = firstLine.u;
            double t = 0;

            if (v == 0)
            {
                y0 = 0;
                y1 = dataToShow.Height;
            }
            else if (u == 0)
            {
                x0 = 0;
                x1 = dataToShow.Width;
            }
            else
            {
                if (v < 0) { v = -v; u = -u; }

                t = x0 / v;
                x0 = x0 - t * v;
                y0 = y0 - t * u;

                t = (-dataToShow.Width + x0) / v;
                x1 = x0 - t * v;
                y1 = y0 - t * u;
            }

            int x = 0;
            int y = 0;
            t = (y0 - y) * v / (u * u + v * v) - (x0 - x) * u / (u * u + v * v);
            double xc = x - u * t;
            double yc = y + v * t;
            int max_l = (int)Math.Sqrt((xc - x0) * (xc - x0) + (yc - y0) * (yc - y0)) * (Math.Sign(xc - x0) != 0 ? Math.Sign(xc - x0) : Math.Sign(yc - y0));
            int min_l = max_l;

            x = dataToShow.Width;
            t = (y0 - y) * v / (u * u + v * v) - (x0 - x) * u / (u * u + v * v);
            xc = x - u * t;
            yc = y + v * t;
            max_l = Math.Max((int)Math.Sqrt((xc - x0) * (xc - x0) + (yc - y0) * (yc - y0)) * (Math.Sign(xc - x0) != 0 ? Math.Sign(xc - x0) : Math.Sign(yc - y0)), max_l);
            min_l = Math.Min((int)Math.Sqrt((xc - x0) * (xc - x0) + (yc - y0) * (yc - y0)) * (Math.Sign(xc - x0) != 0 ? Math.Sign(xc - x0) : Math.Sign(yc - y0)), min_l);

            y = dataToShow.Height;
            t = (y0 - y) * v / (u * u + v * v) - (x0 - x) * u / (u * u + v * v);
            xc = x - u * t;
            yc = y + v * t;
            max_l = Math.Max((int)Math.Sqrt((xc - x0) * (xc - x0) + (yc - y0) * (yc - y0)) * (Math.Sign(xc - x0) != 0 ? Math.Sign(xc - x0) : Math.Sign(yc - y0)), max_l);
            min_l = Math.Min((int)Math.Sqrt((xc - x0) * (xc - x0) + (yc - y0) * (yc - y0)) * (Math.Sign(xc - x0) != 0 ? Math.Sign(xc - x0) : Math.Sign(yc - y0)), min_l);

            x = 0;
            t = (y0 - y) * v / (u * u + v * v) - (x0 - x) * u / (u * u + v * v);
            xc = x - u * t;
            yc = y + v * t;
            max_l = Math.Max((int)Math.Sqrt((xc - x0) * (xc - x0) + (yc - y0) * (yc - y0)) * (Math.Sign(xc - x0) != 0 ? Math.Sign(xc - x0) : Math.Sign(yc - y0)), max_l);
            min_l = Math.Min((int)Math.Sqrt((xc - x0) * (xc - x0) + (yc - y0) * (yc - y0)) * (Math.Sign(xc - x0) != 0 ? Math.Sign(xc - x0) : Math.Sign(yc - y0)), min_l);

            int spectrum_length = max_l - min_l + 2;
            spectrum = new double[spectrum_length,6];
            for (int i = 0; i < spectrum_length;i++) { spectrum[i, 0] = 0; spectrum[i, 1] = 0; spectrum[i, 2] = 0; spectrum[i, 3] = 0; ; spectrum[i, 4] = 0; spectrum[i, 5] = 0; }
            for (x = 0; x < dataToShow.Width-1; x++) for (y = 0; y < dataToShow.Height-1; y++)
                {
                    t = (y0 - y) * v / (u * u + v * v) - (x0 - x) * u / (u * u + v * v);
                    double t1 = (secondLine.y0 - y) * v / (u * u + v * v) - (secondLine.x0 - x) * u / (u * u + v * v);
                    if (t1 * t > 0) continue;
                    double xs = x - u * t;
                    double ys = y + v * t;
                    t1 = dataToShow.Data.Length;
                    int i = (int)Math.Sqrt((xs - x0) * (xs - x0) + (ys - y0) * (ys - y0)) - min_l;
                    spectrum[i, 0] += dataToShow.Data[x, y];
                    spectrum[i, 1] += 1;
                    spectrum[i, 2] = (int)xs;
                    spectrum[i, 3] = (int)ys;
                    if (checkBox_Callibration_nm.Checked) spectrum[i, 4] = callibration_nm(position_On_BaseLine(xs, ys));
                    if (checkBox_Callibration_eV.Checked) spectrum[i, 5] = (nm_to_eV / callibration_nm(position_On_BaseLine(xs, ys)));
                }
/*            for (int i = 0; i < spectrum_length; i++)
            {
                if (spectrum[i, 1] != 0) spectrum[i, 0] = spectrum[i, 0] / spectrum[i, 1];
            }
 */       }

        private void draw_Spectrum()
        {
//            return;
            if (!checkBox_Show_Spectrum.Checked) return;
            double min_val = 10^9+7;
            double max_val = -10^9-7;
            for (int i = 0; i < spectrum.Length / 6; i++)
            {
                if (spectrum[i, 1] == 0) continue;
                min_val = Math.Min(min_val, spectrum[i, 0]/ spectrum[i, 1]);
                max_val = Math.Max(max_val, spectrum[i, 0]/ spectrum[i, 1]);
            }

            Point[] curve = new Point[spectrum.Length / 6];
            double x = 0 + firstLine.u * (0 - min_val) * (double)numericUpDown_Spectrum_Scale.Value / (max_val - min_val + 1) + firstLine.u * (double)numericUpDown_Spectrum_Ofset.Value;
            double y = 0 + -firstLine.v * (0 - min_val) * (double)numericUpDown_Spectrum_Scale.Value / (max_val - min_val + 1) - firstLine.v * (double)numericUpDown_Spectrum_Ofset.Value;

            Point zero_Point = new Point((int)(x*zoom), (int)(y*zoom));
            int non_zero = 0;
            double intence = 0;
            for (int i = 0; i < spectrum.Length / 6; i++)
            {
                x = spectrum[i, 2];
                y = spectrum[i, 3];
                if (spectrum[i, 1] == 0) if (i > 0) curve[i] = curve[i - 1];
                intence = spectrum[i, 0];
                if (spectrum[i, 1] != 0) intence = spectrum[i, 0] / spectrum[i, 1];
                x = spectrum[i, 2] + firstLine.u * (intence - min_val) * (double)numericUpDown_Spectrum_Scale.Value / (max_val - min_val + 1) + firstLine.u * (double)numericUpDown_Spectrum_Ofset.Value;
                y = spectrum[i, 3] + -firstLine.v * (intence - min_val) * (double)numericUpDown_Spectrum_Scale.Value / (max_val - min_val + 1) - firstLine.v * (double)numericUpDown_Spectrum_Ofset.Value;
                curve[i] = new Point((int)(x * zoom), (int)(y * zoom));
                if ((int)(x*zoom) != zero_Point.X || (int)(y*zoom) != zero_Point.Y) non_zero += 1;
            }

            Point[] non_zero_curve = new Point[non_zero];
            int non_zero_count = 0;

            for (int i = 0; i < spectrum.Length / 6; i++)
            {
                if (curve[i].X == zero_Point.X && curve[i].Y == zero_Point.Y) continue;
                non_zero_curve[non_zero_count] = curve[i];
                non_zero_count++;
            }

            Graphics e = Graphics.FromImage(pictureBox1.Image);
            Pen pen = new(button_Spectrum_Color.BackColor);
            pen.Width = (float)numericUpDown_Spectrum_Width.Value;

            if (checkBox_Spectrum_Dash.Checked) pen.DashPattern = new float[] { 5, 5, 5, 5 };

            if (non_zero_curve != null && numericUpDown_Spectrum_Width.Value > 0 && non_zero_curve.Length > 1) e.DrawCurve(pen, non_zero_curve);
        }


        #region Charts

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            int x = (int)Math.Floor((double)e.X / zoom);
            int y = (int)Math.Floor((double)e.Y / zoom);
            toolStripStatusLabel1.Text = "x = " + x + "; y = " + y + "; intensity = " + dataToShow.Data[x, y];

            if (checkBox_Callibration_nm.Checked) toolStripStatusLabel1.Text += "; nm = " + callibration_nm(position_On_BaseLine(x, y)).ToString();
            if (checkBox_Callibration_eV.Checked) toolStripStatusLabel1.Text += "; eV = " + (nm_to_eV / callibration_nm(position_On_BaseLine(x, y))).ToString();

            if (checkBox_Show_X_profile.Checked) DrawChart_X(y);

            if (checkBox_Show_Y_profile.Checked) DrawChart_Y(x);

            toolStripStatusLabel2.Text = position_On_BaseLine(x, y).ToString();
        }

        private void DrawChart_X(int y)
        {
            int[] dataForChart = new int[dataToShow.Width];

            Parallel.For(0, dataToShow.Width, i => dataForChart[i] = dataToShow.Data[i, y]);

            int max = (int)numericUpDown_Show_To.Value;
            int min = (int)numericUpDown_Show_From.Value;

            if (max == min) max = max + 1;

            PointF[] pointForChart = new PointF[dataToShow.Width];

            Parallel.For(0, dataToShow.Width, i => pointForChart[i] = new PointF(i * (float)zoom, 5 + (int)Math.Round((double)(chart_X.Height - 25) * (1 - (dataForChart[i] - min) / (double)(max - min)))));


            Graphics bmap = chart_X.CreateGraphics();
            bmap.Clear(Color.White);

            bmap.DrawLines(new Pen(Color.Green, 1), pointForChart);

        }

        private void DrawChart_Y(int x)
        {
            int[] dataForChart = new int[dataToShow.Height];

            Parallel.For(0, dataToShow.Height, j => dataForChart[j] = dataToShow.Data[x, j]);

            int max = (int)numericUpDown_Show_To.Value;
            int min = (int)numericUpDown_Show_From.Value;

            if (max == min) max = max + 1;

            PointF[] pointForChart = new PointF[dataToShow.Height];

            Parallel.For(0, dataToShow.Height, j => pointForChart[j] = new PointF(5 + (int)Math.Round((double)(chart_Y.Width - 25) * (1 - (dataForChart[j] - min) / (double)(max - min))), j * (float)zoom));


            Graphics bmap = chart_Y.CreateGraphics();
            bmap.Clear(Color.White);

            bmap.DrawLines(new Pen(Color.Blue, 1), pointForChart);

        }

        private void InsertChart()
        {
            if (checkBox_Show_X_profile.Checked)
            {
                panel_Image.Height = pictureBox1.Height + 100;
                chart_X.Location = new Point(panel_Image.Location.X, Math.Min(splitContainer_Image_Area.Panel1.Height - 100, panel_Image.Location.Y + panel_Image.Height - 97));
                chart_X.Width = pictureBox1.Width;
                chart_X.Height = 100;
                chart_X.Show();
            }
            else
            {
                panel_Image.Height = pictureBox1.Height;
                chart_X.Hide();
            }

            if (checkBox_Show_Y_profile.Checked)
            {
                panel_Image.Width = pictureBox1.Width + 100;
                chart_Y.Location = new Point(Math.Min(splitContainer_Image_Area.Panel1.Width - 100, panel_Image.Location.X + panel_Image.Width - 97), panel_Image.Location.Y);
                chart_Y.Height = pictureBox1.Height;
                chart_Y.Width = 100;
                chart_Y.Show();
            }
            else
            {
                panel_Image.Width = pictureBox1.Width;
                chart_Y.Hide();
            }


        }

        private void pictureBox1_Resize(object sender, EventArgs e)
        {
            InsertChart();
        }

        private void checkBox_Show_X_profile_CheckedChanged(object sender, EventArgs e)
        {
            InsertChart();
        }

        private void panel_Image_LocationChanged(object sender, EventArgs e)
        {
            InsertChart();
        }

        private void checkBox_Show_Y_profile_CheckedChanged(object sender, EventArgs e)
        {
            InsertChart();
        }


        #endregion

        #endregion

        private void DataToShow_Calc()
        {

            int binningX = (int)numericUpDown_Binning_X.Value;
            int binningY = (int)numericUpDown_Binning_Y.Value;
            int rotate = 0;
            if (radioButton_Rotate_Image_0.Checked) rotate = 0;
            if (radioButton_Rotate_Image_90.Checked) rotate = 1;
            if (radioButton_Rotate_Image_180.Checked) rotate = 2;
            if (radioButton_Rotate_Image_270.Checked) rotate = 3;
            int flip = 0;
            if (checkBox_Flip_Image_X.Checked) flip = (int)(flip + 1);
            if (checkBox_Flip_Image_Y.Checked) flip = (int)(flip + 2);

            dataToShow = allData[comboBox_Current_Images.SelectedIndex].Copy();

            fit newDataToShow;

            if (checkBox_Subtract_Background.Checked)
            {
                newDataToShow = allData[comboBox_Current_Background.SelectedIndex].Copy();
                if (newDataToShow.Height == dataToShow.Height && newDataToShow.Width == dataToShow.Width)
                {
                    Parallel.For(0, newDataToShow.Width, i =>
                    {
                        for (int j = 0; j < newDataToShow.Height; j++)
                        {
                            dataToShow.Data[i, j] = dataToShow.Data[i, j] - newDataToShow.Data[i, j];
                        }
                    });
                }
                else
                {
                    checkBox_Subtract_Background.Checked = false;
                }
            }

            newDataToShow = dataToShow;

            newDataToShow.Width = (int)Math.Floor((double)dataToShow.Width / (double)binningX);
            newDataToShow.Height = (int)Math.Floor((double)dataToShow.Height / (double)binningY);
            newDataToShow.Data = new int[newDataToShow.Width, newDataToShow.Height];


            Parallel.For(0, newDataToShow.Width, i =>
            {
                for (int j = 0; j < newDataToShow.Height; j++)
                {
                    int d = 0;
                    for (int t = 0; t < binningX; t++)
                    {
                        for (int l = 0; l < binningY; l++)
                        {
                            if (i * binningX + t < dataToShow.Width && j * binningY + l < dataToShow.Height)
                            {
                                d = d + dataToShow.Data[i * binningX + t, j * binningY + l];
                            }
                        }
                    }
                    newDataToShow.Data[i, j] = (int)d;
                }
            });


            dataToShow = newDataToShow;

            switch (rotate + 4 * flip)
            {
                case 0:
                    dataToShow.Data = new int[dataToShow.Width, dataToShow.Height];
                    Parallel.For(0, dataToShow.Width, i =>
                    {
                        for (int j = 0; j < dataToShow.Height; j++)
                        {
                            dataToShow.Data[i, j] = newDataToShow.Data[i, j];
                        }
                    });
                    break;
                case 1:
                    dataToShow.Width = newDataToShow.Height;
                    dataToShow.Height = newDataToShow.Width;
                    dataToShow.Data = new int[dataToShow.Width, dataToShow.Height];
                    Parallel.For(0, dataToShow.Height, j =>
                    {
                        for (int i = 0; i < dataToShow.Width; i++)
                        {
                            dataToShow.Data[i, j] = newDataToShow.Data[(dataToShow.Height - 1 - j), i];
                        }
                    });
                    break;
                case 2:
                    dataToShow.Data = new int[dataToShow.Width, dataToShow.Height];
                    Parallel.For(0, dataToShow.Width, i =>
                    {
                        for (int j = 0; j < dataToShow.Height; j++)
                        {
                            dataToShow.Data[i, j] = newDataToShow.Data[(dataToShow.Width - 1 - i), (dataToShow.Height - 1 - j)];
                        }
                    });
                    break;
                case 3:
                    dataToShow.Width = newDataToShow.Height;
                    dataToShow.Height = newDataToShow.Width;
                    dataToShow.Data = new int[dataToShow.Width, dataToShow.Height];
                    Parallel.For(0, dataToShow.Height, j =>
                    {
                        for (int i = 0; i < dataToShow.Width; i++)
                        {
                            dataToShow.Data[i, j] = newDataToShow.Data[j, (dataToShow.Width - 1 - i)];
                        }
                    });
                    break;


                case 0 + 4:
                    dataToShow.Data = new int[dataToShow.Width, dataToShow.Height];
                    Parallel.For(0, dataToShow.Width, i =>
                    {
                        for (int j = 0; j < dataToShow.Height; j++)
                        {
                            dataToShow.Data[i, j] = newDataToShow.Data[(dataToShow.Width - 1 - i), j];
                        }
                    });
                    break;
                case 1 + 4:
                    dataToShow.Width = newDataToShow.Height;
                    dataToShow.Height = newDataToShow.Width;
                    dataToShow.Data = new int[dataToShow.Width, dataToShow.Height];
                    Parallel.For(0, dataToShow.Height, j =>
                    {
                        for (int i = 0; i < dataToShow.Width; i++)
                        {
                            dataToShow.Data[i, j] = newDataToShow.Data[dataToShow.Height - 1 - (dataToShow.Height - 1 - j), i];
                        }
                    });
                    break;
                case 2 + 4:
                    dataToShow.Data = new int[dataToShow.Width, dataToShow.Height];
                    Parallel.For(0, dataToShow.Width, i =>
                    {
                        for (int j = 0; j < dataToShow.Height; j++)
                        {
                            dataToShow.Data[i, j] = newDataToShow.Data[dataToShow.Width - 1 - (dataToShow.Width - 1 - i), (dataToShow.Height - 1 - j)];
                        }
                    });
                    break;
                case 3 + 4:
                    dataToShow.Width = newDataToShow.Height;
                    dataToShow.Height = newDataToShow.Width;
                    dataToShow.Data = new int[dataToShow.Width, dataToShow.Height];
                    Parallel.For(0, dataToShow.Height, j =>
                    {
                        for (int i = 0; i < dataToShow.Width; i++)
                        {
                            dataToShow.Data[i, j] = newDataToShow.Data[dataToShow.Height - 1 - j, (dataToShow.Width - 1 - i)];
                        }
                    });
                    break;


                case 0 + 8:
                    dataToShow.Data = new int[dataToShow.Width, dataToShow.Height];
                    Parallel.For(0, dataToShow.Width, i =>
                    {
                        for (int j = 0; j < dataToShow.Height; j++)
                        {
                            dataToShow.Data[i, j] = newDataToShow.Data[i, dataToShow.Height - 1 - j];
                        }
                    });
                    break;
                case 1 + 8:
                    dataToShow.Width = newDataToShow.Height;
                    dataToShow.Height = newDataToShow.Width;
                    dataToShow.Data = new int[dataToShow.Width, dataToShow.Height];
                    Parallel.For(0, dataToShow.Height, j =>
                    {
                        for (int i = 0; i < dataToShow.Width; i++)
                        {
                            dataToShow.Data[i, j] = newDataToShow.Data[(dataToShow.Height - 1 - j), dataToShow.Width - 1 - i];
                        }
                    });
                    break;
                case 2 + 8:
                    dataToShow.Data = new int[dataToShow.Width, dataToShow.Height];
                    Parallel.For(0, dataToShow.Width, i =>
                    {
                        for (int j = 0; j < dataToShow.Height; j++)
                        {
                            dataToShow.Data[i, j] = newDataToShow.Data[(dataToShow.Width - 1 - i), dataToShow.Height - 1 - (dataToShow.Height - 1 - j)];
                        }
                    });
                    break;
                case 3 + 8:
                    dataToShow.Width = newDataToShow.Height;
                    dataToShow.Height = newDataToShow.Width;
                    dataToShow.Data = new int[dataToShow.Width, dataToShow.Height];
                    Parallel.For(0, dataToShow.Height, j =>
                    {
                        for (int i = 0; i < dataToShow.Width; i++)
                        {
                            dataToShow.Data[i, j] = newDataToShow.Data[j, dataToShow.Width - 1 - (dataToShow.Width - 1 - i)];
                        }
                    });
                    break;

                case 0 + 12:
                    dataToShow.Data = new int[dataToShow.Width, dataToShow.Height];
                    Parallel.For(0, dataToShow.Width, i =>
                    {
                        for (int j = 0; j < dataToShow.Height; j++)
                        {
                            dataToShow.Data[i, j] = newDataToShow.Data[dataToShow.Width - 1 - i, dataToShow.Height - 1 - j];
                        }
                    });
                    break;
                case 1 + 12:
                    dataToShow.Width = newDataToShow.Height;
                    dataToShow.Height = newDataToShow.Width;
                    dataToShow.Data = new int[dataToShow.Width, dataToShow.Height];
                    Parallel.For(0, dataToShow.Height, j =>
                    {
                        for (int i = 0; i < dataToShow.Width; i++)
                        {
                            dataToShow.Data[i, j] = newDataToShow.Data[dataToShow.Height - 1 - (dataToShow.Height - 1 - j), dataToShow.Width - 1 - i];
                        }
                    });
                    break;
                case 2 + 12:
                    dataToShow.Data = new int[dataToShow.Width, dataToShow.Height];
                    Parallel.For(0, dataToShow.Width, i =>
                    {
                        for (int j = 0; j < dataToShow.Height; j++)
                        {
                            dataToShow.Data[i, j] = newDataToShow.Data[dataToShow.Width - 1 - (dataToShow.Width - 1 - i), dataToShow.Height - 1 - (dataToShow.Height - 1 - j)];
                        }
                    });
                    break;
                case 3 + 12:
                    dataToShow.Width = newDataToShow.Height;
                    dataToShow.Height = newDataToShow.Width;
                    dataToShow.Data = new int[dataToShow.Width, dataToShow.Height];
                    Parallel.For(0, dataToShow.Height, j =>
                    {
                        for (int i = 0; i < dataToShow.Width; i++)
                        {
                            dataToShow.Data[i, j] = newDataToShow.Data[dataToShow.Height - 1 - j, dataToShow.Width - 1 - (dataToShow.Width - 1 - i)];
                        }
                    });
                    break;

            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
        }

        #region Get_UDP

        static int[,] data_From_UDP;
        static int packet_number;
        private const int listenPort = 986;
        static bool listen_UDP_Free = true;

        async private void StartListenerAsync()
        {
            while (CCD_Brust_N_left > 0)
            {
                if (listen_UDP_Free)
                {
                    listen_UDP_Free = false;
                    await Task.Run(() => StartListener());
                    allData[0].Width = CCD_Data_Width;
                    allData[0].Height = CCD_Data_Height;
                    allData[0].Header = 2880;
                    allData[0].HeaderString = "LPI to e2v Data file".PadRight(64)
                                              + ("CCD name = " + textBox_CCD_Name.Text).PadRight(64)
                                              + ("Aquisition time = " + DateTime.Now.ToString("u").Replace("Z", "").Replace(":", ".")).PadRight(64)
                                              + ("Exposition = " + CCD_Exposition.ToString() + " ms").PadRight(64)
                                              + ("Frequency mode = " + (1000 / CCD_Readout_Freq).ToString() + " kHz").PadRight(64)
                                              + ("Data Width = " + CCD_Data_Width.ToString()).PadRight(64)
                                              + ("Data Height = " + CCD_Data_Width.ToString()).PadRight(64)
                                              + ("Data Header Length = " + raw_Data_Header.ToString()).PadRight(64)
                                              + ("Brust mode = " + ((CCD_Mode & 0x02) == 0x02).ToString()).PadRight(64)
                                              + ("Brust mode shots = " + CCD_Brust_N.ToString()).PadRight(64)
                                              + ("Brust mode delay = " + CCD_Brust_Delay.ToString()).PadRight(64)
                                              + ("Brust shot number = " + (CCD_Brust_N - CCD_Brust_N_left)).PadRight(64);

                    allData[0].HeaderString.PadRight(raw_Data_Header);
                    allData[0].Data = data_From_UDP;
                    comboBox_Current_Images.SelectedIndex = 0;
                    DataToShow_Calc();
                    ReDraw_Image();

                    if (checkBox_Auto_Save.Checked)
                    {
                        await Task.Run(() => Save_Data_To_File(Set_Auto_Save_Name(), 0));
                        if (checkBox_Add_Counter_1.Checked) numericUpDown_Auto_Save_Counter.Value++;
                    }

                    listen_UDP_Free = true;
                }
                CCD_Brust_N_left--;
                if (CCD_Brust_N_left > 0)
                {
                    await Task.Delay(CCD_Brust_Delay);
                    CCD_Started = true;
                    Connect_to_CCD();
                }
                CCD_Started = false;
                CCD_Mode = CCD_Mode & 0xfe;
            }

            StopListener();
        }

        private void StartListener()
        {

            IPEndPoint e = new IPEndPoint(IPAddress.Any, listenPort);
            UdpClient u = new UdpClient(e);

            data_From_UDP = new int[CCD_Data_Width, CCD_Data_Height];
            int line_Number = 0;
            int start_Number = 0;

            packet_number = 0;
            bool finish = false;
            while (!finish)
            {
                byte[] receiveBytes = u.Receive(ref e);
                if ((receiveBytes.Length > 13) && (receiveBytes.Length < 15))
                {
                    finish = true;
                }
                else if ((receiveBytes.Length > 10) && receiveBytes[0] == 3 && receiveBytes[1] == 14 && receiveBytes[2] == 15 && receiveBytes[3] == 92 && receiveBytes[4] == 65 && receiveBytes[5] == 35)
                {
                    line_Number = receiveBytes[8] * 256 + receiveBytes[9];
                    start_Number = receiveBytes[6] * 256 + receiveBytes[7];
                    if (start_Number > 0) start_Number++;
                    if (line_Number < 486) line_Number++;
                    for (int i = 0; i <= (receiveBytes.Length - 11 - 2) / 3; i++)
                    {
                        if (i + start_Number < CCD_Data_Height)
                        {
                            data_From_UDP[line_Number, i + start_Number] = receiveBytes[i * 3 + 11] * 256 * 256 + receiveBytes[i * 3 + 11 + 1] * 256 + receiveBytes[i * 3 + 11 + 2];
                        }
                    }
                    packet_number++;
                }
            }
            u.Close();
        }

        private void StopListener()
        {
            CCD_Started = false;
            CCD_Brust_N_left = 0;

            button_Exposition_Start.Enabled = true;
            numericUpDown_Exposition.Enabled = true;
            groupBox_ShotMode.Enabled = true;
            groupBox_ReadoutFreq.Enabled = true;
            groupBox_CCD_Param.Enabled = true;
            try
            {

                IPEndPoint e = new IPEndPoint(IPAddress.Parse("192.168.1.1"), 986);
                UdpClient u = new UdpClient(e);
                byte[] sendBytes = new byte[256];

                u.Send(sendBytes, 14, e);
                u.Send(sendBytes, 14, e);
                u.Send(sendBytes, 14, e);
                u.Send(sendBytes, 14, e);
                u.Send(sendBytes, 14, e);
                u.Send(sendBytes, 14, e);

                u.Close();
            }
            catch (Exception q)
            {
                MessageBox.Show(q.Message);
            }
            Connect_to_CCD();
        }

        private void Connect_to_CCD()
        {

          IPEndPoint e = new IPEndPoint(IPAddress.Parse("192.168.1.57"), 987);
          TcpClient t = new TcpClient();
            try
            {
                var sw = new Stopwatch();
                sw.Start();
                t.ConnectAsync(e);
 //               t.Connect(e);
                
                while (!t.Connected) 
                {
                    if (sw.ElapsedMilliseconds > 2000)
                    {
                        t.Close();
                        break;
                    }
                }

                NetworkStream s = t.GetStream();

                CCD_Exposition = (int)numericUpDown_Exposition.Value;
                if (radioButton_Readout_Freq_1000.Checked) CCD_Readout_Freq = 1;
                if (radioButton_Readout_Freq_500.Checked) CCD_Readout_Freq = 2;
                if (radioButton_Readout_Freq_250.Checked) CCD_Readout_Freq = 4;

                CCD_Mode = 0;
                if (checkBox_Trigger.Checked) CCD_Mode += 4;
                if (radioButton_ShotMode_Brust.Checked) CCD_Mode += 2;
                if (CCD_Started) CCD_Mode += 1;

                CCD_Brust_N = (int)numericUpDown_Brust_N_Shots.Value;
                CCD_Brust_Delay = (int)numericUpDown_Brust_Delay.Value;

                byte[] sendBytes = new byte[256];
                sendBytes[0] = 3;
                sendBytes[1] = 14;
                sendBytes[2] = 15;
                sendBytes[3] = 92;
                sendBytes[4] = 65;
                sendBytes[5] = (byte)(CCD_Readout_Freq & 0xFF);
                sendBytes[6] = (byte)((CCD_Exposition & 0xFF0000) >> 16);
                sendBytes[7] = (byte)((CCD_Exposition & 0x00FF00) >> 8);
                sendBytes[8] = (byte)(CCD_Exposition & 0x0000FF);
                sendBytes[9] = (byte)(CCD_Mode & 0xFF);
                sendBytes[10] = (byte)((CCD_Brust_N & 0x00FF00) >> 8);
                sendBytes[11] = (byte)(CCD_Brust_N & 0x0000FF);
                sendBytes[12] = (byte)((CCD_Brust_Delay & 0xFF0000) >> 16);
                sendBytes[13] = (byte)((CCD_Brust_Delay & 0x00FF00) >> 8);
                sendBytes[14] = (byte)(CCD_Brust_Delay & 0x0000FF);

                s.Write(sendBytes, 0, 32);
                s.Read(sendBytes, 0, 32);

                CCD_Readout_Freq = sendBytes[5];
                CCD_Exposition = sendBytes[8] + 256 * sendBytes[7] + 256 * 256 * sendBytes[6];
                CCD_Mode = sendBytes[9];
                //            CCD_Brust_N = sendBytes[11] + 256 * sendBytes[10];
                //            CCD_Brust_Delay = sendBytes[14] + 256 * sendBytes[13] + 256 * 256 * sendBytes[12];

                CCD_Data_Width = sendBytes[16] + 256 * sendBytes[15];
                CCD_Data_Height = sendBytes[18] + 256 * sendBytes[17];
                CCD_Byte = sendBytes[19];

                numericUpDown_Exposition.Value = CCD_Exposition;
                switch (CCD_Readout_Freq)
                {
                    case 1:
                        radioButton_Readout_Freq_1000.Checked = true;
                        break;
                    case 2:
                        radioButton_Readout_Freq_500.Checked = true;
                        break;
                    case 4:
                        radioButton_Readout_Freq_250.Checked = true;
                        break;
                }
                if ((CCD_Mode & 0x04) == 0x04) checkBox_Trigger.Checked = true;
                else checkBox_Trigger.Checked = false;

                if ((CCD_Mode & 0x02) == 0x02) radioButton_ShotMode_Brust.Checked = true;
                else radioButton_ShotMode_Single.Checked = true;

//                if ((CCD_Mode & 0x01) == 0x01) CCD_Started = true;
//                else CCD_Started = false;

                numericUpDown_Brust_N_Shots.Value = CCD_Brust_N;
                numericUpDown_Brust_Delay.Value = CCD_Brust_Delay;

                numericUpDown_CCD_Width.Value = CCD_Data_Width;
                numericUpDown_CCD_Height.Value = CCD_Data_Height;
                numericUpDown_CCD_Byte.Value = CCD_Byte;

                String CCD_Name = "";
                for (int i = 20; i < 32; i++) CCD_Name += Convert.ToChar(sendBytes[i]);

                textBox_CCD_Name.Text = CCD_Name;

                s.Close();
                t.Close();
            }
            catch (Exception q)
            {
                MessageBox.Show(q.Message);
                t.Close();
            }
            CCD_Started = false;
            CCD_Mode = CCD_Mode & 0xfe;
        }


        private void StartCCD()
        {
            CCD_Started = true;
            button_Exposition_Start.Enabled = false;
            numericUpDown_Exposition.Enabled = false;
            groupBox_ShotMode.Enabled = false;
            groupBox_ReadoutFreq.Enabled = false;
            groupBox_CCD_Param.Enabled = false;

            StartListenerAsync();

            Connect_to_CCD();
        }


        private void button_Exposition_Start_Click(object sender, EventArgs e)
        {
            CCD_Started = false;
            CCD_Mode = CCD_Mode & 0xfe;
            Connect_to_CCD();
            CCD_Brust_N_left = 1;
            if ((CCD_Mode & 0x02) == 0x02) CCD_Brust_N_left = CCD_Brust_N;
            StartCCD();
        }


        private void button_Exposition_Stop_Click(object sender, EventArgs e)
        {
            StopListener();
        }

        private void button_CCD_Reconnect_Click(object sender, EventArgs e)
        {
            textBox_CCD_Name.Text = "Reconnect...";
            textBox_CCD_Name.Update();
            
            Connect_to_CCD();
        }

        private void numericUpDown_Exposition_ValueChanged(object sender, EventArgs e)
        {
            numericUpDown_Exposition.Value = Math.Round(numericUpDown_Exposition.Value);
        }

        private void numericUpDown_Brust_N_Shots_ValueChanged(object sender, EventArgs e)
        {
            numericUpDown_Brust_N_Shots.Value = Math.Round(numericUpDown_Brust_N_Shots.Value);
        }

        private void numericUpDown_Brust_Delay_ValueChanged(object sender, EventArgs e)
        {
            numericUpDown_Brust_Delay.Value = Math.Round(numericUpDown_Brust_Delay.Value);
        }
        #endregion

        #region Callibration

        bool comboBox_Callibration_Points_Working = false;
        private void comboBox_Callibration_Points_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!comboBox_Callibration_Points_Working)
            {
                if (comboBox_Callibration_Points.SelectedIndex == 2)
                {
                    comboBox_Callibration_Points.Items.Clear();
                    comboBox_Callibration_Points.Items.Add("");
                    comboBox_Callibration_Points.Items.Add("Add");
                    comboBox_Callibration_Points.Items.Add("Delete All");
                    Array.Resize(ref callibrationPoints, 0);
                    comboBox_Callibration_Points.SelectedIndex = 0;
                }
                else if (comboBox_Callibration_Points.SelectedIndex > 2)
                {
                    comboBox_Callibration_Points_Working = true;

                    int index = comboBox_Callibration_Points.SelectedIndex;
                    Cal_Poin cal_Poin = new Cal_Poin();
                    cal_Poin.point = callibrationPoints[index - 3];
                    cal_Poin.ShowDialog();
                    callibrationPoint cp = cal_Poin.point;
                    int result_cp = cal_Poin.result;
                    if (result_cp == 1)
                    {
                        callibrationPoints[index - 3] = cp;
                        comboBox_Callibration_Points.Items[index] = "x = " + cp.x.ToString()
                                                                + "; y = " + cp.y.ToString()
                                                                + "; nm = " + cp.nm.ToString()
                                                                + "; eV = " + cp.eV.ToString();
                    }
                    if (result_cp == 2)
                    {
                        comboBox_Callibration_Points.SelectedIndex = 0;
                        comboBox_Callibration_Points.Items.RemoveAt(index);
                        callibrationPoint[] new_callibrationPoints = new callibrationPoint[callibrationPoints.Length - 1];
                        for (int i = 0; i < index - 3; i++) { new_callibrationPoints[i] = callibrationPoints[i]; }
                        for (int i = index - 3; i < callibrationPoints.Length - 1; i++) { new_callibrationPoints[i] = callibrationPoints[i + 1]; }
                        callibrationPoints = new_callibrationPoints;
                    }
                }
                else if (comboBox_Callibration_Points.SelectedIndex == 1)
                {
                    Array.Resize(ref callibrationPoints, callibrationPoints.Length + 1);
                    callibrationPoints[callibrationPoints.Length - 1].x = 0;
                    callibrationPoints[callibrationPoints.Length - 1].y = 0;
                    callibrationPoints[callibrationPoints.Length - 1].nm = 1;
                    callibrationPoints[callibrationPoints.Length - 1].eV = nm_to_eV / 1;

                    comboBox_Callibration_Points.Items.Add("new");
                    comboBox_Callibration_Points.SelectedIndex = callibrationPoints.Length + 2;
                }
                comboBox_Callibration_Points_Working = false;
            }

        }


        private void button_Callibration_Recalc_Click(object sender, EventArgs e)
        {
            callibration_Recalc();
        }

        void callibration_Recalc()
        {
            int length = callibrationPoints.Length;
            double[] x = new double[length];
            double[] y = new double[length];


            for (int i = 0; i < length; i++)
            {
                x[i] = position_On_BaseLine(callibrationPoints[i].x, callibrationPoints[i].x);
                y[i] = callibrationPoints[i].nm;
            }

            callibration = Polynomial(x, y);

            for (int i = 0; i < 4; i++)
            {
                if (double.IsNaN(callibration[i])) callibration[i] = 999999;
                if (Math.Abs(callibration[i]) > 999999) callibration[i] = 999999;
            }
            numericUpDown_Callibration_Zero_Order.Value = (decimal)callibration[0];
            numericUpDown_Callibration_First_Order.Value = (decimal)callibration[1];
            numericUpDown_Callibration_Second_Order.Value = (decimal)callibration[2];
            numericUpDown_Callibration_Third_Order.Value = (decimal)callibration[3];
        }

        double callibration_nm(double x)
        {
            return callibration[0] + x * callibration[1] + x * x * callibration[2] + x * x * x * callibration[3];
        }

        public double[] Polynomial(double[] xval, double[] yval)
        {
            // 1, 2
            int _amount = 3;
            double[] _x = xval;
            double[] _y = yval;
            int n = _amount + 1;
            int count = _x.Length;

            double[,] a = new double[n, n];
            double[] b = new double[n];
            double[] c = new double[2 * n];
            double[] _coefficients = new double[n];


            if (xval.Length == 0)
            {
                _coefficients[0] = 0;
                _coefficients[1] = 0;
                _coefficients[2] = 0;
                _coefficients[3] = 0;
                return _coefficients;
            }
            if (xval.Length == 1)
            {
                _coefficients[0] = yval[0];
                _coefficients[1] = 0;
                _coefficients[2] = 0;
                _coefficients[3] = 0;
                return _coefficients;
            }
            if (xval.Length == 2)
            {
                _coefficients[1] = (yval[0] - yval[1]) / (xval[0] - xval[1]);

                _coefficients[0] = yval[0] - _coefficients[1] * xval[0];
                //               _coefficients[1] = 0;
                _coefficients[2] = 0;
                _coefficients[3] = 0;
                return _coefficients;
            }

            // 3
            for (int i = 0; i < count; i++)
            {
                double x = _x[i];
                double y = _y[i];

                double f = 1;

                for (int j = 0; j < 2 * n - 1; j++)
                {
                    if (j < n)
                    {
                        b[j] += y;
                        y *= x;
                    }

                    c[j] += f;
                    f *= x;
                }
            }

            // 4
            for (int i = 0; i < n; i++)
            {
                int k = i;

                for (int j = 0; j < n; j++)
                {
                    a[i, j] = c[k];
                    k++;
                }
            }

            //5
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    a[j, i] /= -a[i, i];

                    for (int k = i + 1; k < n; k++)
                    {
                        a[j, k] += a[j, i] * a[i, k];
                    }

                    b[j] += a[j, i] * b[i];
                }
            }

            // 6
            _coefficients[n - 1] = b[n - 1] / a[n - 1, n - 1];

            //7
            for (int i = n - 2; i >= 0; i--)
            {
                double h = b[i];

                for (int j = i + 1; j < n; j++)
                {
                    h -= _coefficients[j] * a[i, j];
                }

                _coefficients[i] = h / a[i, i];
            }

            return _coefficients;
        }


        double position_On_BaseLine(double x, double y)
        {
            double result = 0;
            double t1 = 0;
            double t2 = 0;
            double x1 = x;
            double y1 = y;
            if (baseLine.v == 0)
            {
                x1 = baseLine.x0;
                y1 = y;
            }
            else if (baseLine.u == 0)
            {
                x1 = x;
                y1 = baseLine.y0;
            }
            else
            {
                t1 = ((x - baseLine.x0) * baseLine.u - (y - baseLine.y0) * baseLine.v) / (baseLine.v * baseLine.v + baseLine.u * baseLine.u);
                t2 = (x - baseLine.x0 - t1 * baseLine.u) / baseLine.v;

                x1 = baseLine.x0 + t2 * baseLine.v;
                y1 = baseLine.y0 + t2 * baseLine.u;
            }

            result = Math.Sqrt((x1 - baseLine.x0) * (x1 - baseLine.x0) + (y1 - baseLine.y0) * (y1 - baseLine.y0));
            return result;
        }


        struct LineOnChart
        {
            public int x0;
            public int y0;
            public double v;
            public double u;
        }

        double[] callibration = new double[4];

        LineOnChart baseLine = new LineOnChart();
        LineOnChart firstLine = new LineOnChart();
        LineOnChart secondLine = new LineOnChart();


        void set_First_Line()
        {
            firstLine.x0 = (int)numericUpDown_Spectrum_First_Line_X.Value;
            firstLine.y0 = (int)numericUpDown_Spectrum_First_Line_Y.Value;
            numericUpDown_Spectrum_Angle_Deg.Value = (decimal)((double)numericUpDown_Spectrum_Angle_Rad.Value * 180 / Math.PI);
            firstLine.v = Math.Cos((double)numericUpDown_Spectrum_Angle_Rad.Value);
            firstLine.u = Math.Sin((double)numericUpDown_Spectrum_Angle_Rad.Value);

            if (firstLine.u == 1 || firstLine.u == -1) firstLine.v = 0;
            if (firstLine.v == 1 || firstLine.v == -1) firstLine.u = 0;

//            ReDraw_Image();
        }
        void set_Second_Line()
        {
            secondLine.x0 = (int)numericUpDown_Spectrum_Second_Line_X.Value;
            secondLine.y0 = (int)numericUpDown_Spectrum_Second_Line_Y.Value;
            numericUpDown_Spectrum_Angle_Deg.Value = (decimal)((double)numericUpDown_Spectrum_Angle_Rad.Value * 180 / Math.PI);
            secondLine.v = Math.Cos((double)numericUpDown_Spectrum_Angle_Rad.Value);
            secondLine.u = Math.Sin((double)numericUpDown_Spectrum_Angle_Rad.Value);
            if (secondLine.u == 1 || secondLine.u == -1) secondLine.v = 0;
            if (secondLine.v == 1 || secondLine.v == -1) secondLine.u = 0;

//            ReDraw_Image();
        }
        void set_Base_Line()
        {
            baseLine.x0 = (int)numericUpDown_Base_Line_Central_Point_X.Value;
            baseLine.y0 = (int)numericUpDown_Base_Line_Central_Point_Y.Value;
            double v = -(double)numericUpDown_Base_Line_Central_Point_X.Value + (double)numericUpDown_Base_Line_Second_Point_X.Value;
            double u = -(double)numericUpDown_Base_Line_Central_Point_Y.Value + (double)numericUpDown_Base_Line_Second_Point_Y.Value;
            if (v == 0) u = 9999999;
            if (u == 0) v = 9999999;
            baseLine.v = v / Math.Sqrt(v * v + u * u);
            baseLine.u = u / Math.Sqrt(v * v + u * u);

//            ReDraw_Image();
        }


        private void button_Base_Line_Central_Point_Set_Click(object sender, EventArgs e)
        {
            async_get_Pixel_Coordinate(0);
        }

        int[] get_Pixel_Result = new int[2];
        bool get_Pixel_Flag = false;


        async void async_get_Pixel_Coordinate(int witch_Button)
        {
            await Task.Run(() => { while (!get_Pixel_Flag) { } });
            get_Pixel_Coordinate(witch_Button);
        }

        void get_Pixel_Coordinate(int witch_Button)
        {
            switch (witch_Button)
            {
                case 0:
                    numericUpDown_Base_Line_Central_Point_X.Value = get_Pixel_Result[0];
                    numericUpDown_Base_Line_Central_Point_Y.Value = get_Pixel_Result[1];
                    break;
                case 1:
                    numericUpDown_Base_Line_Second_Point_X.Value = get_Pixel_Result[0];
                    numericUpDown_Base_Line_Second_Point_Y.Value = get_Pixel_Result[1];
                    break;
                case 2:
                    numericUpDown_Spectrum_First_Line_X.Value = get_Pixel_Result[0];
                    numericUpDown_Spectrum_First_Line_Y.Value = get_Pixel_Result[1];
                    break;
                case 3:
                    numericUpDown_Spectrum_Second_Line_X.Value = get_Pixel_Result[0];
                    numericUpDown_Spectrum_Second_Line_Y.Value = get_Pixel_Result[1];
                    break;
            }
            witch_Button = 10;
            get_Pixel_Flag = false;
        }


        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            int x = (int)Math.Floor((double)e.X / zoom);
            int y = (int)Math.Floor((double)e.Y / zoom);

            get_Pixel_Result[0] = x;
            get_Pixel_Result[1] = y;
            get_Pixel_Flag = true;
        }

        private void button_Base_Line_Second_Point_Set_Click(object sender, EventArgs e)
        {
            async_get_Pixel_Coordinate(1);
        }

        private void button_Spectrum_First_Set_Click(object sender, EventArgs e)
        {
            async_get_Pixel_Coordinate(2);
        }

        private void button_Spectrum_Second_Set_Click(object sender, EventArgs e)
        {
            async_get_Pixel_Coordinate(3);
        }

        private void checkBox_Base_Line_Setup_CheckedChanged(object sender, EventArgs e)
        {
            panel_Base_Line_Setup.Enabled = false;
            if (checkBox_Base_Line_Setup.Checked) panel_Base_Line_Setup.Enabled = true;

            ReDraw_Image();

        }


        bool baseLine_Changing = false;
        private void numericUpDown_Base_Line_Point_ValueChanged(object sender, EventArgs e)
        {
            if (baseLine_Changing) return;
            baseLine_Changing = true;
            if (numericUpDown_Base_Line_Central_Point_X.Value == numericUpDown_Base_Line_Second_Point_X.Value && numericUpDown_Base_Line_Central_Point_Y.Value == numericUpDown_Base_Line_Second_Point_Y.Value) numericUpDown_Base_Line_Second_Point_Y.Value++;
            set_Base_Line();
            numericUpDown_Base_Line_Angle_Rad.Value = (decimal)Math.Acos(baseLine.v);
            if (baseLine.u < 0) numericUpDown_Base_Line_Angle_Rad.Value = -numericUpDown_Base_Line_Angle_Rad.Value;
            numericUpDown_Base_Line_Angle_Deg.Value = (decimal)((double)numericUpDown_Base_Line_Angle_Rad.Value * 180 / Math.PI);

            baseLine_Changing = false;
            ReDraw_Image();
        }

        private void numericUpDown_Base_Line_Angle_Rad_ValueChanged(object sender, EventArgs e)
        {

            if (baseLine_Changing) return;
            baseLine_Changing = true;

            if (numericUpDown_Base_Line_Angle_Rad.Value > ((decimal)(2 * Math.PI))) numericUpDown_Base_Line_Angle_Rad.Value = ((decimal)(2 * Math.PI));
            if (numericUpDown_Base_Line_Angle_Rad.Value < -((decimal)(2 * Math.PI))) numericUpDown_Base_Line_Angle_Rad.Value = -((decimal)(2 * Math.PI));


            baseLine.v = Math.Cos((double)numericUpDown_Base_Line_Angle_Rad.Value);
            baseLine.u = Math.Sin((double)numericUpDown_Base_Line_Angle_Rad.Value);
            numericUpDown_Base_Line_Angle_Deg.Value = (decimal)((double)numericUpDown_Base_Line_Angle_Rad.Value * 180 / Math.PI);

            numericUpDown_Base_Line_Second_Point_Y.Value = Math.Max(numericUpDown_Base_Line_Second_Point_Y.Minimum, Math.Min(numericUpDown_Base_Line_Second_Point_Y.Maximum, (decimal)(baseLine.y0 + (double)baseLine.u * ((double)numericUpDown_Base_Line_Second_Point_X.Value - (double)baseLine.x0) / (double)baseLine.v)));

            if (checkBox_Spectrum_Angle.Checked)
            {
                numericUpDown_Spectrum_Angle_Deg.Value = (decimal)numericUpDown_Base_Line_Angle_Deg.Value;
                numericUpDown_Spectrum_Angle_Rad.Value = (decimal)numericUpDown_Base_Line_Angle_Rad.Value;
            }

            ReDraw_Image();
            baseLine_Changing = false;
        }

        private void numericUpDown_Base_Line_Angle_Deg_ValueChanged(object sender, EventArgs e)
        {


            if (baseLine_Changing) return;
            baseLine_Changing = true;
            if (numericUpDown_Base_Line_Angle_Deg.Value > 360) numericUpDown_Base_Line_Angle_Deg.Value = 360;
            if (numericUpDown_Base_Line_Angle_Deg.Value < -360) numericUpDown_Base_Line_Angle_Deg.Value = -360;

            numericUpDown_Base_Line_Angle_Rad.Value = (decimal)((double)numericUpDown_Base_Line_Angle_Deg.Value / 180.0 * Math.PI);

            baseLine.v = Math.Cos((double)numericUpDown_Base_Line_Angle_Rad.Value);
            baseLine.u = Math.Sin((double)numericUpDown_Base_Line_Angle_Rad.Value);

            numericUpDown_Base_Line_Second_Point_Y.Value = Math.Max(numericUpDown_Base_Line_Second_Point_Y.Minimum, Math.Min(numericUpDown_Base_Line_Second_Point_Y.Maximum, (decimal)(baseLine.y0 + (double)baseLine.u * ((double)numericUpDown_Base_Line_Second_Point_X.Value - (double)baseLine.x0) / (double)baseLine.v)));
            ReDraw_Image();
            if (checkBox_Spectrum_Angle.Checked)
            {
                numericUpDown_Spectrum_Angle_Deg.Value = (decimal)numericUpDown_Base_Line_Angle_Deg.Value;
                numericUpDown_Spectrum_Angle_Rad.Value = (decimal)numericUpDown_Base_Line_Angle_Rad.Value;
            }
            baseLine_Changing = false;

        }

        private void button_Base_Line_Central_Point_Go_To_Edge_Click(object sender, EventArgs e)
        {
            if (baseLine_Changing) return;
            baseLine_Changing = true;

            set_Base_Line();

            double t = 0;

            if (baseLine.v == 0)
            {
                numericUpDown_Base_Line_Central_Point_Y.Value = 0;
                numericUpDown_Base_Line_Second_Point_Y.Value = dataToShow.Height;
            }
            else if (baseLine.u == 0)
            {
                numericUpDown_Base_Line_Central_Point_X.Value = 0;
                numericUpDown_Base_Line_Second_Point_X.Value = dataToShow.Width;
            }
            else
            {
                t = -baseLine.x0 / baseLine.v;
                if (baseLine.y0 + t * baseLine.u < 0) t = -baseLine.y0 / baseLine.u;
                if (baseLine.y0 + t * baseLine.u > dataToShow.Height) t = (dataToShow.Height - baseLine.y0) / baseLine.u;

                numericUpDown_Base_Line_Central_Point_X.Value = (int)(baseLine.x0 + t * baseLine.v);
                numericUpDown_Base_Line_Central_Point_Y.Value = (int)(baseLine.y0 + t * baseLine.u);

                set_Base_Line();

                t = (dataToShow.Width - baseLine.x0) / baseLine.v;
                if (baseLine.y0 + t * baseLine.u < 0) t = -baseLine.y0 / baseLine.u;
                if (baseLine.y0 + t * baseLine.u > dataToShow.Height) t = (dataToShow.Height - baseLine.y0) / baseLine.u;
                numericUpDown_Base_Line_Second_Point_X.Value = (int)(baseLine.x0 + t * baseLine.v);
                numericUpDown_Base_Line_Second_Point_Y.Value = (int)(baseLine.y0 + t * baseLine.u);

                set_Base_Line();
            }
            ReDraw_Image();

            baseLine_Changing = false;
        }

        private void numericUpDown_Callibration_Zero_Order_ValueChanged(object sender, EventArgs e)
        {
            callibration[0] = (double)numericUpDown_Callibration_Zero_Order.Value;

        }

        private void numericUpDown_Callibration_First_Order_ValueChanged(object sender, EventArgs e)
        {
            callibration[1] = (double)numericUpDown_Callibration_First_Order.Value;

        }

        private void numericUpDown_Callibration_Second_Order_ValueChanged(object sender, EventArgs e)
        {
            callibration[2] = (double)numericUpDown_Callibration_Second_Order.Value;

        }

        private void numericUpDown_Callibration_Third_Order_ValueChanged(object sender, EventArgs e)
        {
            callibration[3] = (double)numericUpDown_Callibration_Third_Order.Value;

        }

        private void button_Callibration_Save_CSV_Click(object sender, EventArgs e)
        {

            String[] data_To_File = new String[0];

            Array.Resize(ref data_To_File, data_To_File.Length + 1);
            data_To_File[data_To_File.Length - 1] = "Base Line Parameters: x0 y0 v u;";
            Array.Resize(ref data_To_File, data_To_File.Length + 1);
            data_To_File[data_To_File.Length - 1] = baseLine.x0.ToString() + ";";
            Array.Resize(ref data_To_File, data_To_File.Length + 1);
            data_To_File[data_To_File.Length - 1] = baseLine.y0.ToString() + ";";
            Array.Resize(ref data_To_File, data_To_File.Length + 1);
            data_To_File[data_To_File.Length - 1] = baseLine.v.ToString() + ";";
            Array.Resize(ref data_To_File, data_To_File.Length + 1);
            data_To_File[data_To_File.Length - 1] = baseLine.u.ToString() + ";";
            Array.Resize(ref data_To_File, data_To_File.Length + 1);
            data_To_File[data_To_File.Length - 1] = "callibration poits x y nm;";
            for (int i = 0; i < callibrationPoints.Length; i++)
            {
                Array.Resize(ref data_To_File, data_To_File.Length + 1);
                data_To_File[data_To_File.Length - 1] = callibrationPoints[i].x.ToString() + ";" + callibrationPoints[i].y.ToString() + ";" + callibrationPoints[i].nm.ToString() + ";";
            }

            string filePath;

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory = textBox_Auto_Save_Path.Text;
                saveFileDialog.FileName = "Callibration_" + Set_Auto_Save_Name();
                saveFileDialog.FileName = saveFileDialog.FileName.Substring(0, saveFileDialog.FileName.Length - 5);
                saveFileDialog.Filter = "CSV files (*.CSV)|*.CSV|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = saveFileDialog.FileName;
                    try
                    {
                        File.WriteAllLines(filePath, data_To_File);
                    }
                    catch (Exception q) { MessageBox.Show(q.Message); };

                }
            }
        }

        private void button_Load_CSV_Click(object sender, EventArgs e)
        {
            string filePath;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = textBox_Auto_Save_Path.Text;
                openFileDialog.Filter = "CSV files (*.CSV)|*.CSV|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = openFileDialog.FileName;

                    String[] data_From_File = new String[0];

                    try
                    {
                        data_From_File = File.ReadAllText(filePath).Split(";");
                        baseLine.x0 = int.Parse(data_From_File[1]);
                        baseLine.y0 = int.Parse(data_From_File[2]);
                        baseLine.v = double.Parse(data_From_File[3]);
                        baseLine.u = double.Parse(data_From_File[4]);

//                        if (baseLine_Changing) return;
                        baseLine_Changing = true;
                        numericUpDown_Base_Line_Central_Point_X.Value = baseLine.x0;
                        numericUpDown_Base_Line_Central_Point_Y.Value = baseLine.y0;
                        numericUpDown_Base_Line_Angle_Rad.Value = (decimal)Math.Acos(baseLine.v);
                        if (baseLine.u < 0) numericUpDown_Base_Line_Angle_Rad.Value = -numericUpDown_Base_Line_Angle_Rad.Value;
                        numericUpDown_Base_Line_Angle_Deg.Value = (decimal)((double)numericUpDown_Base_Line_Angle_Rad.Value * 180 / Math.PI);

                        numericUpDown_Base_Line_Second_Point_X.Value = baseLine.x0 + (int)(1000 * baseLine.v);
                        numericUpDown_Base_Line_Second_Point_Y.Value = Math.Max(numericUpDown_Base_Line_Second_Point_Y.Minimum, Math.Min(numericUpDown_Base_Line_Second_Point_Y.Maximum, (decimal)(baseLine.y0 + (double)baseLine.u * ((double)numericUpDown_Base_Line_Second_Point_X.Value - (double)baseLine.x0) / (double)baseLine.v)));
                        numericUpDown_Base_Line_Angle_Deg.Value = (decimal)((double)numericUpDown_Base_Line_Angle_Rad.Value * 180 / Math.PI);

                        numericUpDown_Base_Line_Second_Point_Y.Value = Math.Max(numericUpDown_Base_Line_Second_Point_Y.Minimum, Math.Min(numericUpDown_Base_Line_Second_Point_Y.Maximum, (decimal)(baseLine.y0 + (double)baseLine.u * ((double)numericUpDown_Base_Line_Second_Point_X.Value - (double)baseLine.x0) / (double)baseLine.v)));

                        if (checkBox_Spectrum_Angle.Checked)
                        {
                            numericUpDown_Spectrum_Angle_Deg.Value = (decimal)numericUpDown_Base_Line_Angle_Deg.Value;
                            numericUpDown_Spectrum_Angle_Rad.Value = (decimal)numericUpDown_Base_Line_Angle_Rad.Value;
                        }
                        baseLine_Changing = false;

                        comboBox_Callibration_Points.Items.Clear();
                        comboBox_Callibration_Points.Items.Add("");
                        comboBox_Callibration_Points.Items.Add("Add");
                        comboBox_Callibration_Points.Items.Add("Delete All");
                        Array.Resize(ref callibrationPoints, 0);
                        comboBox_Callibration_Points.SelectedIndex = 0;

                        int i = 6;

                        while (i < data_From_File.Length - 3)
                        {
                            Array.Resize(ref callibrationPoints, callibrationPoints.Length + 1);
                            callibrationPoints[callibrationPoints.Length - 1].x = int.Parse(data_From_File[i]);
                            callibrationPoints[callibrationPoints.Length - 1].y = int.Parse(data_From_File[i + 1]);
                            callibrationPoints[callibrationPoints.Length - 1].nm = double.Parse(data_From_File[i + 2]);
                            callibrationPoints[callibrationPoints.Length - 1].eV = nm_to_eV / double.Parse(data_From_File[i + 2]);

                            comboBox_Callibration_Points.Items.Add("x = " + callibrationPoints[callibrationPoints.Length - 1].x.ToString()
                                                                + "; y = " + callibrationPoints[callibrationPoints.Length - 1].y.ToString()
                                                                + "; nm = " + callibrationPoints[callibrationPoints.Length - 1].nm.ToString()
                                                                + "; eV = " + callibrationPoints[callibrationPoints.Length - 1].eV.ToString());
                            i += 3;
                        }
                        callibration_Recalc();
                    }
                    catch (Exception q)
                    {
                        MessageBox.Show(q.Message);
                    }
                }
            }
            callibration_Recalc();
        }

        #endregion

        private void checkBox_Show_Callibration_CheckedChanged(object sender, EventArgs e)
        {
            ReDraw_Image();
        }

        private void checkBox_Spectrum_Angle_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_Spectrum_Angle.Checked)
            {
                numericUpDown_Spectrum_Angle_Deg.Value = (decimal)numericUpDown_Base_Line_Angle_Deg.Value;
                numericUpDown_Spectrum_Angle_Rad.Value = (decimal)numericUpDown_Base_Line_Angle_Rad.Value;

                numericUpDown_Spectrum_Angle_Deg.Enabled = false;
                numericUpDown_Spectrum_Angle_Rad.Enabled = false;
            }
            else 
            {
                numericUpDown_Spectrum_Angle_Deg.Enabled = true;
                numericUpDown_Spectrum_Angle_Rad.Enabled = true;
            }
        }

        private void numericUpDown_Spectrum_Angle_Deg_ValueChanged(object sender, EventArgs e)
        {
            numericUpDown_Spectrum_Angle_Rad.Value = (decimal)((double)numericUpDown_Spectrum_Angle_Deg.Value / 180 * Math.PI);

            ReDraw_Image();

        }

        private void numericUpDown_Spectrum_Angle_Rad_ValueChanged(object sender, EventArgs e)
        {
            ReDraw_Image();
        }

        private void numericUpDown_Spectrum_First_Line_X_ValueChanged(object sender, EventArgs e)
        {

            ReDraw_Image();
        }

        private void numericUpDown_Spectrum_First_Line_Y_ValueChanged(object sender, EventArgs e)
        {
            ReDraw_Image();
        }

        private void numericUpDown_Spectrum_First_Line_Width_ValueChanged(object sender, EventArgs e)
        {
            ReDraw_Image();
        }

        private void checkBox_Spectrum_First_Line_Dash_CheckedChanged(object sender, EventArgs e)
        {
            ReDraw_Image();
        }

        private void button_Spectrum_First_Line_Color_Click(object sender, EventArgs e)
        {
            calc_Spectrum();
            colorDialog1 = new ColorDialog();
            colorDialog1.Color = button_Spectrum_First_Line_Color.BackColor;
            colorDialog1.ShowDialog();
            button_Spectrum_First_Line_Color.BackColor = colorDialog1.Color;
            ReDraw_Image();
        }

        private void numericUpDown_Spectrum_Second_Line_X_ValueChanged(object sender, EventArgs e)
        {
            ReDraw_Image();
        }

        private void numericUpDown_Spectrum_Second_Line_Y_ValueChanged(object sender, EventArgs e)
        {
            ReDraw_Image();
        }

        private void numericUpDown_Spectrum_Second_Line_Width_ValueChanged(object sender, EventArgs e)
        {
            calc_Spectrum();
            ReDraw_Image();
        }

        private void checkBox_Spectrum_Second_Line_Dash_CheckedChanged(object sender, EventArgs e)
        {
            ReDraw_Image();
        }

        private void button_Spectrum_Second_Line_Color_Click(object sender, EventArgs e)
        {
            calc_Spectrum();
            colorDialog1 = new ColorDialog();
            colorDialog1.Color = button_Spectrum_Second_Line_Color.BackColor;
            colorDialog1.ShowDialog();
            button_Spectrum_Second_Line_Color.BackColor = colorDialog1.Color;
            ReDraw_Image();
        }

        private void button_Spectrum_Color_Click(object sender, EventArgs e)
        {
            colorDialog1 = new ColorDialog();
            colorDialog1.Color = button_Spectrum_Color.BackColor;
            colorDialog1.ShowDialog();
            button_Spectrum_Color.BackColor = colorDialog1.Color;

            ReDraw_Image();
        }

        private void checkBox_Spectrum_Dash_CheckedChanged(object sender, EventArgs e)
        {
            ReDraw_Image();
        }

        private void numericUpDown_Spectrum_Width_ValueChanged(object sender, EventArgs e)
        {
            ReDraw_Image();
        }

        private void checkBox_Show_Spectrum_CheckedChanged(object sender, EventArgs e)
        {
            checkBox_Show_Spectrum_Copy.Checked = checkBox_Show_Spectrum.Checked;
            ReDraw_Image();
        }

        private void numericUpDown_Spectrum_Ofset_ValueChanged(object sender, EventArgs e)
        {
            ReDraw_Image();
        }

        private void numericUpDown_Spectrum_Scale_ValueChanged(object sender, EventArgs e)
        {
            ReDraw_Image();
        }

        private void checkBox_Show_Spectrum_Copy_CheckedChanged(object sender, EventArgs e)
        {
            checkBox_Show_Spectrum.Checked = checkBox_Show_Spectrum_Copy.Checked;
        }

        private void button_Save_Spectrum_Click(object sender, EventArgs e)
        {

            String[] data_To_File = new String[10 + spectrum.Length / 6];

            data_To_File[0] = "First Line Parameters: x0 y0 v u;";
            data_To_File[1] = firstLine.x0.ToString() + ";";
            data_To_File[2] = firstLine.y0.ToString() + ";";
            data_To_File[3] = firstLine.v.ToString() + ";";
            data_To_File[4] = firstLine.u.ToString() + ";";
            data_To_File[5] = "Second Line Parameters: x0 y0;";
            data_To_File[6] = secondLine.x0.ToString() + ";";
            data_To_File[7] = secondLine.y0.ToString() + ";";
            data_To_File[8] = "Spectrum poits:";
            data_To_File[9] = "Intensity; Number of Points; X on First Line; Y on First Line; nm; eV";

            for (int i = 0; i < data_To_File.Length-10; i++)
            {
                data_To_File[i+10] = spectrum[i, 0].ToString() + ";" + spectrum[i, 1].ToString() + ";" + spectrum[i, 2].ToString() + ";" + spectrum[i, 3].ToString() + ";" + spectrum[i, 4].ToString() + ";" + spectrum[i, 5].ToString() + ";";
            }

            string filePath;

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory = textBox_Auto_Save_Path.Text;
                saveFileDialog.FileName = "Spectrum_" + Set_Auto_Save_Name();
                saveFileDialog.FileName = saveFileDialog.FileName.Substring(0, saveFileDialog.FileName.Length - 5);
                saveFileDialog.Filter = "CSV files (*.CSV)|*.CSV|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = saveFileDialog.FileName;
                    try
                    {
                        File.WriteAllLines(filePath, data_To_File);
                    }
                    catch (Exception q) { MessageBox.Show(q.Message); };

                }
            }
        }

        private void label38_Click(object sender, EventArgs e)
        {

        }

        private void numericUpDown_Callibrations_Size_ValueChanged(object sender, EventArgs e)
        {
            ReDraw_Image();
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void numericUpDown_Callibration_Ticks_ValueChanged(object sender, EventArgs e)
        {
            ReDraw_Image();
        }

        private void button_Callibration_Color_Click(object sender, EventArgs e)
        {
            colorDialog1 = new ColorDialog();
            colorDialog1.Color = button_Callibration_Color.BackColor;
            colorDialog1.ShowDialog();
            button_Callibration_Color.BackColor = colorDialog1.Color;

            ReDraw_Image();
        }

        private void checkBox_Callibration_nm_CheckedChanged(object sender, EventArgs e)
        {
            ReDraw_Image();
        }

        private void checkBox_Callibration_eV_CheckedChanged(object sender, EventArgs e)
        {
            ReDraw_Image();
        }
    }
}