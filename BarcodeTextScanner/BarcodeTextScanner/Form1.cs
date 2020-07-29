using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using System.Drawing.Imaging;
using System.IO;
using static Emgu.CV.OCR.Tesseract;

namespace BarcodeTextScanner
{
    public partial class Form1 : Form
    {
        private Tesseract _tesseract = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            timer1.Enabled = true;
            _tesseract = new Tesseract();
            _tesseract.Init("tessdata", "eng", OcrEngineMode.Default);
        }

        private void BrowseBtn_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
                LoadImageToPictureBox();
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (!File.Exists(textBox1.Text))
            {
                MessageBox.Show("File does not exist");
                return;
            } 
            LoadImageToPictureBox();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!File.Exists(textBox1.Text))
                return;
            Bitmap fullImage = new Bitmap(textBox1.Text);

            Bitmap fullImageGray = new Bitmap(fullImage.Width, fullImage.Height);

            for (int i = 0; i < fullImage.Width; i++)
            {
                for (int x = 0; x < fullImage.Height; x++)
                {
                    Color oc = fullImage.GetPixel(i, x);
                    int grayScale = (int)((oc.R * 0.3) + (oc.G * 0.59) + (oc.B * 0.11));
                    Color nc = Color.FromArgb(oc.A, grayScale, grayScale, grayScale);
                    fullImageGray.SetPixel(i, x, nc);
                }
            }

            richTextBox1.Text = getImageText(fullImageGray);
        }

        private void LoadImageToPictureBox()
        {
            pictureBox1.Image = new Bitmap(textBox1.Text);
        }

        private string getImageText(Bitmap image)
        {
            List<string> possibleResults = new List<string>();
            Image<Bgr, byte> emguFullImage = new Image<Bgr, byte>(image);

            getPartialImageText(image, possibleResults, emguFullImage, 1);
            getPartialImageText(image, possibleResults, emguFullImage, 2);
            getPartialImageText(image, possibleResults, emguFullImage, 3);

            return GetBestResult(possibleResults);
        }

        private void getPartialImageText(Bitmap image, List<string> possibleResults, Image<Bgr, byte> emguFullImage, int partOfImage)
        {
            int partialWidth = image.Width / partOfImage;
            int partialHeight = image.Height / partOfImage;

            for (int i = 0; i < partOfImage; i++)
            {
                int startX = i * partialWidth;

                for (int j = 0; j < partOfImage; j++)
                {
                    int startY = j * partialHeight;
                    //full row
                    GetPartImageText(possibleResults, emguFullImage, image.Width, partialHeight, 0, startY);
                    // slice
                    GetPartImageText(possibleResults, emguFullImage, partialWidth, partialHeight, startX, startY);
                }
            }
        }

        private void GetPartImageText(List<string> possibleResults, Image<Bgr, byte> emguFullImage, int partialWidth, int partialHeight, int startX, int startY)
        {
            emguFullImage.ROI = new Rectangle(startX, startY, partialWidth, partialHeight);
            _tesseract.SetImage(emguFullImage);
            _tesseract.Recognize();
            string imageText = _tesseract.GetUTF8Text();
            if (string.IsNullOrEmpty(imageText))
                return;
            string[] lines = imageText.Split( new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string cleanLine = CleanLine(line);
                if (!string.IsNullOrEmpty(cleanLine))
                    possibleResults.Add(cleanLine);
            }
        }

        private string CleanLine(string line)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in line)
            {
                if (c >= '0' && c <= '9')
                {
                    sb.Append(c);
                }
            }
            string result = sb.ToString();
            return (result.Length > 1)? result : string.Empty;
        }

        private string GetBestResult(List<string> possibleResults)
        {
            string longestString = string.Empty;
            Dictionary<string, int> commonStrings = new Dictionary<string, int>();

            foreach (var result in possibleResults)
            {
                if (result.Length > longestString.Length)
                    longestString = result;
                if (commonStrings.ContainsKey(result))
                    commonStrings[result]++;
                else
                    commonStrings.Add(result, 1);
            }

            var mostCommon = commonStrings.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
            return (longestString.Contains(mostCommon)) ? longestString : mostCommon;
        }
    }
}