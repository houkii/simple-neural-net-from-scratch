using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;

namespace WindowsFormsApplication
{
    public partial class Main : Form
    {
        Point lastPoint = Point.Empty;
        private Graphics g;
        private Point p = Point.Empty;
        private Pen pioro;

        Point lastPoint2 = Point.Empty;
        private Graphics g2;
        private Point p2 = Point.Empty;
        private Pen pioro2;

        int _on = 0;
        int _loading = 0;
        int _learning = 0;
        int _drawing = 0;

        string sSign;
        double dOutput = 0;
        

        private class Network
        {
            public struct InputLayer
            {
                public double Value;
                public double[] Weights;
            }

            public struct OutputLayer
            {
                public double InputSum;
                public double Output;
                public double Error;
                public double Target;
                public string Value;
            }

            public double learningRate = 0.1;

            public int ImageSize = 0;
            public int InputNum = 0;
            public int OutputNum = 0;

            public InputLayer[] inputLayer = null;
            public OutputLayer[] outputLayer = null;

            public double[] errors = null;

            public int currIter = 0;
            public int maxIter = 100;

            public void Initialize(int inputSize, int outputSize)
            {
                InputNum = inputSize;
                OutputNum = outputSize;
                inputLayer = new Network.InputLayer[inputSize];
                outputLayer = new Network.OutputLayer[outputSize];
 
                Random random = new Random();

                for (int i = 0; i < InputNum; ++i)
                {
                    inputLayer[i].Weights = new double[OutputNum];

                    for (int j = 0; j < OutputNum; ++j)
                    {
                        inputLayer[i].Weights[j] = random.Next(1, 3) / 100.0;
                    }
                }
            }

            public bool TrainNetwork(double[][] inputs, double[][] outputs)
            {
                double currentError = 0.0, maximumError = 0.01;

                currIter = 0;

                // error table
                errors = new double[maxIter];

                do
                {
                    currentError = 0;

                    for (int i = 0; i < inputs.Length; ++i)
                    {
                        CalculateOutput(inputs[i], outputLayer[i].Value);
                        BackPropagation();

                        currentError += GetError();
                    }

                    errors[currIter] = currentError;

                    ++currIter;
                }
                while (currentError > maximumError && currIter < maxIter);

                if (currIter <= maxIter)
                {
                    return true;
                }

                return false;
            }

            public void CalculateOutput(double[] pattern, string output)
            {
                for (int i = 0; i < pattern.Length; i++)
                {
                    inputLayer[i].Value = pattern[i];
                }

                for (int i = 0; i < OutputNum; i++)
                {
                    double total = 0.0;

                    for (int j = 0; j < InputNum; j++)
                    {
                        total += inputLayer[j].Value * inputLayer[j].Weights[i];       //weight * input 
                    }

                    outputLayer[i].InputSum = total;
                    outputLayer[i].Output = Activation(total);
                    outputLayer[i].Target = outputLayer[i].Value.CompareTo(output) == 0 ? 1.0 : 0.0;
                    outputLayer[i].Error = (outputLayer[i].Target - outputLayer[i].Output) * (outputLayer[i].Output) * (1 - outputLayer[i].Output); //f'(x)
                }
            }

            public void BackPropagation()
            {
                for (int j = 0; j < OutputNum; j++)
                {
                    for (int i = 0; i < InputNum; i++)
                    {
                        inputLayer[i].Weights[j] += learningRate * (outputLayer[j].Error) * inputLayer[i].Value;            //backprop
                    }
                }
            }

            public double GetError()
            {
                double total = 0.0;

                for (int j = 0; j < OutputNum; j++)
                {
                    total += Math.Pow((outputLayer[j].Target - outputLayer[j].Output), 2.0) / 2.0;		//f(x)
                }

                return total;
            }

            public double Activation(double x)
            {
                return (1.0 / (1.0 + Math.Exp(-x)));
            }

            public void Recognize(double[] Input)
            {
                for (int i = 0; i < InputNum; i++)
                {
                    inputLayer[i].Value = Input[i];
                }
                                                              
                for (int i = 0; i < OutputNum; i++)                         
                {
                    double total = 0.0;

                    for (int j = 0; j < InputNum; j++)
                    {
                        total += inputLayer[j].Value * inputLayer[j].Weights[i];    
                    }
                    outputLayer[i].InputSum = total;
                    outputLayer[i].Output = Activation(total);
                }
            }
        }

        private Network network = new Network();

        public Main()
        {
            InitializeComponent();
         
            //drawing to recognize
            pictureBox2.Image = new Bitmap(112, 112);
            g = Graphics.FromImage(pictureBox2.Image);
            pioro = new Pen(Color.Black);
            pioro.Width = trackBar1.Value;
            g.SmoothingMode = SmoothingMode.AntiAlias;
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            pictureBox1.Load(openFileDialog1.FileName);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _loading++;
            openFileDialog1.ShowDialog();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string ErrorString = null;
            string[] files;
            if (_on != 0)
            {
                files = Directory.GetFiles(folderBrowserDialog1.SelectedPath, "*.bmp");
                listView1.Items.Clear();
                int imageSize = Bitmap.FromFile(files[0]).Width;

                // input layer
                network.Initialize(imageSize * imageSize, files.Length);

                double[][] inputs = new double[files.Length][];
                double[][] outputs = new double[files.Length][];

                for (int i = 0; i < files.Length; ++i)
                {
                    inputs[i] = new double[imageSize * imageSize];

                    Bitmap image = new Bitmap(files[i]);

                    for (int x = 0; x < imageSize; ++x)
                    {
                        for (int y = 0; y < imageSize; ++y)
                        {
                            Color pixel = image.GetPixel(x, y);
                            inputs[i][x * imageSize + y] = (1.0 - (pixel.R / 255.0 + pixel.G / 255.0 + pixel.B / 255.0) / 3.0) < 0.5 ? 0.0 : 1.0;
                        }
                    }

                    outputs[i] = new double[files.Length];

                    for (int j = 0; j < files.Length; ++j)
                    {
                        outputs[i][j] = i == j ? 1.0 : 0.0;
                    }

                    FileInfo info = new FileInfo(files[i]);
                    network.outputLayer[i].Value = info.Name.Replace(".bmp", "");
                }
                // train
                network.TrainNetwork(inputs, outputs);
            }
            else
            {
                _on++;
                folderBrowserDialog1.ShowDialog();
            }


            for (int i = 0; i < network.currIter; ++i)
            {
                ListViewItem item = listView1.Items.Add(i.ToString());
                item.SubItems.Add(network.errors[i].ToString("#0.000000"));
                ErrorString += network.errors[i].ToString("#0.000000") + " ";
            }
            System.IO.File.WriteAllText(@"D:\path.txt", ErrorString);
            _learning++;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (_on != 0)
            {
                if (_loading != 0)
                {
                    int imageSize = pictureBox1.Image.Width;

                    double[] sample = new double[imageSize * imageSize];

                    Bitmap image = new Bitmap(pictureBox1.ImageLocation);

                    for (int x = 0; x < imageSize; ++x)
                    {
                        for (int y = 0; y < imageSize; ++y)
                        {
                            Color pixel = image.GetPixel(x, y);

                            sample[x * imageSize + y] = (1.0 - (pixel.R / 255.0 + pixel.G / 255.0 + pixel.B / 255.0) / 3.0) < 0.5 ? 0.0 : 1.0;
                        }
                    }
                    network.Recognize(sample);


                    // show output
                    listView2.Items.Clear();


                    if (_learning != 0)
                    {
                        for (int i = 0; i < network.outputLayer.Length; ++i)
                        {
                            ListViewItem item = listView2.Items.Add(network.outputLayer[i].Value);
                            item.SubItems.Add(network.outputLayer[i].Output.ToString("#0.000000"));
                            var temp = (int)(network.outputLayer[i].Output * 100);
                            item.SubItems.Add(temp.ToString());

                          //wygrany
                            if (dOutput < network.outputLayer[i].Output)
                            {
                                dOutput = network.outputLayer[i].Output;
                                sSign = network.outputLayer[i].Value;
                                textBox3.Text = sSign;
                            }
                        }
                    }
                    else { MessageBox.Show("teach"); }
                }
                else
                {
                    _loading++;
                    openFileDialog1.ShowDialog();
                }
                }
            else
            {
                _on++;
                folderBrowserDialog1.ShowDialog();
            }
            dOutput = 0;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            _on++;
            folderBrowserDialog1.ShowDialog();
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                network.maxIter = int.Parse(textBox1.Text);
            }
            catch
            {
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                network.learningRate = double.Parse(textBox2.Text);
            }
            catch
            {
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
           
        }

        private void button5_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "bmp|*.bmp";
            dialog.ShowDialog();
            if (dialog.FileName != "")
                pictureBox2.Image.Save(dialog.FileName);

            //pictureBox2.Load(dialog.FileName);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog2 = new SaveFileDialog();
            dialog2.Filter = "bmp|*.bmp";
            dialog2.ShowDialog();    
        }


        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            Bitmap image = null;
            int imageSize = pictureBox2.Image.Width;

            double[] sample = new double[imageSize * imageSize];

            image = new Bitmap(pictureBox2.Image);

            for (int x = 0; x < imageSize; ++x)
            {
                for (int y = 0; y < imageSize; ++y)
                {
                    Color pixel = image.GetPixel(x, y);

                    sample[x * imageSize + y] = (1.0 - (pixel.R / 255.0 + pixel.G / 255.0 + pixel.B / 255.0) / 3.0) < 0.5 ? 0.0 : 1.0;
                }
            }

            network.Recognize(sample);

            // show output
            listView2.Items.Clear();

            if (_learning != 0)
            {
                if (_drawing != 0)
                {
                    for (int i = 0; i < network.outputLayer.Length; ++i)
                    {
                        ListViewItem item = listView2.Items.Add(network.outputLayer[i].Value);
                        item.SubItems.Add(network.outputLayer[i].Output.ToString("#0.000000"));
                        var temp2 = (int)(network.outputLayer[i].Output * 100);
                        item.SubItems.Add(temp2.ToString());

                        if (dOutput < network.outputLayer[i].Output)
                        {
                            dOutput = network.outputLayer[i].Output;
                            sSign = network.outputLayer[i].Value;
                            textBox3.Text = sSign;
                        }
                    }
                }
                else
                {
                MessageBox.Show("field is empty");
                }
            }
            else {
                if (_on == 0)
                {
                    _on++;
                    folderBrowserDialog1.ShowDialog();
                    MessageBox.Show("teach the net first");
                }
                else
                {
                    MessageBox.Show("teach the net first");
                }
            }

            dOutput = 0;
        }


        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
 
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void clearButton_Click(object sender, EventArgs e)
        {

            if (pictureBox2.Image != null)
            {
                pictureBox2.Image = new Bitmap(112, 112);
                g = Graphics.FromImage(pictureBox2.Image);
                pioro = new Pen(Color.Black);
                pioro.Width = trackBar1.Value;
                g.SmoothingMode = SmoothingMode.AntiAlias;
            }
        }

        private void clearButton2_Click(object sender, EventArgs e)
        {

        }


        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click_1(object sender, EventArgs e)
        {

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            _drawing = 1;
        }

        private void clearButton_Click_1(object sender, EventArgs e)
        {
            g.Clear(Color.White);
            pictureBox2.Refresh();
        }

        private void clearButton2_Click_1(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                p = e.Location;
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
               g.DrawLine(pioro, p, e.Location);
               p = e.Location;
               pictureBox2.Refresh();
            }
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel2_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                p2 = e.Location;
        }

        private void panel2_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                g2.DrawLine(pioro2, p2, e.Location);
                p2 = e.Location;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            
        }

        private void tabPage2_Click(object sender, EventArgs e)
        {

        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            pioro.Width = trackBar1.Value;
            pioro.DashOffset = 1;
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void tabPage3_Click(object sender, EventArgs e)
        {

        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }
    }
}
