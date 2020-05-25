using Emgu.CV;
using Emgu.CV.ImgHash;
using Emgu.CV.Structure;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Color = System.Drawing.Color;

namespace MyPaint
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        private Image<Bgr, byte> img;
        private Image<Bgr, byte> img2;
        private Image<Bgr, byte> imgSel;
        private Image<Bgr, byte> imgEffects;
        private bool paint = false;
        private int brushSize = 1;
        private Bgr brushColor = new Bgr(Color.Black);
        private bool brushOn = true;
        private bool selectOn = false;
        private bool select = false;
        private System.Drawing.Point selectPoint;
        private System.Windows.Point endSelectPoint;
        private double grayscaleAlpha = 0.0;
        private Bitmap sepiaImage = null;
        private double sepiaAlpha = 0.0;
        private bool imageChanged = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        public ImageSource ImageSourceFromBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                if (ofd.ShowDialog() == true)
                {
                    img = new Image<Bgr, byte>(ofd.FileName);
                    img2 = img.Clone();
                    imgEffects = img.Clone();
                    imageBox.Source = ImageSourceFromBitmap(img.ToBitmap());
                }
            }
            catch(Exception ex)
            {

            }
        }

        private void brightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            img2 = img + brightnessSlider.Value;
            imgEffects = img2.Clone();
            imageChanged = true;
            imageBox.Source = ImageSourceFromBitmap(img2.ToBitmap());
            //brushSize = (int) brushSizeSlider.Value;
        }

        private void brushSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            brushSize = (int) brushSizeSlider.Value;
        }

        private void dockPanel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            paint = false;
            select = false;
        }

        private void dockPanel_MouseMove(object sender, MouseEventArgs e)
        {

            System.Windows.Point clickPoint = e.GetPosition(dockPanel);
            System.Drawing.Point normalizedPoint = new System.Drawing.Point(0, 0);
            normalizedPoint.X = (int)img.Height * (int)clickPoint.X / (int)dockPanel.ActualHeight - brushSize / 2;
            normalizedPoint.Y = (int)img.Width * (int)clickPoint.Y / (int)dockPanel.ActualWidth - brushSize;
            imgSel = img.Clone();
            //Console.WriteLine("{0} {1} {2} {3}", selectPoint.X, selectPoint.Y, endSelectPoint.X, endSelectPoint.Y);


            if (paint)
            {
                img2.Draw(new System.Drawing.Rectangle(normalizedPoint,
                    new System.Drawing.Size(brushSize, brushSize)), brushColor, -1);
                img = img2.Clone();
                imageBox.Source = ImageSourceFromBitmap(img2.ToBitmap());
                imageChanged = true;
            }

            if (select)
            {
                imageChanged = true;
                System.Drawing.Point point2 = new System.Drawing.Point(0, 0);
                System.Drawing.Point point3 = new System.Drawing.Point(0, 0);
                endSelectPoint.X = normalizedPoint.X;
                endSelectPoint.Y = normalizedPoint.Y;
                if (normalizedPoint.X > selectPoint.X)
                {
                    point2.X = normalizedPoint.X;
                    point2.Y = selectPoint.Y;
                    point3.X = selectPoint.X;
                    point3.Y = normalizedPoint.Y;
                }
                else
                {
                    point2.X = normalizedPoint.X;
                    point2.Y = selectPoint.Y;
                    point3.X = selectPoint.X;
                    point3.Y = normalizedPoint.Y;
                }

                //imgSel.Draw(new LineSegment2D(selectPoint, point2), new Bgr(Color.Black), 1);
                //imgSel.Draw(new LineSegment2D(point2, normalizedPoint), new Bgr(Color.Black), 1);
                //imgSel.Draw(new LineSegment2D(normalizedPoint, point3), new Bgr(Color.Black), 1);
                //imgSel.Draw(new LineSegment2D(point3, selectPoint), new Bgr(Color.Black), 1);
                imgSel = drawDottedLine(imgSel, selectPoint, point2, new Bgr(Color.Black), getLineLength(selectPoint, point2) / 3.5);
                imgSel = drawDottedLine(imgSel, point2, normalizedPoint, new Bgr(Color.Black), getLineLength(point2, normalizedPoint) / 3.5);
                imgSel = drawDottedLine(imgSel, normalizedPoint, point3, new Bgr(Color.Black), getLineLength(normalizedPoint, point3) / 3.5);
                imgSel = drawDottedLine(imgSel, point3, selectPoint, new Bgr(Color.Black), getLineLength(point3, selectPoint) / 3.5);

               // img = img2.Clone();
                imageBox.Source = ImageSourceFromBitmap(imgSel.ToBitmap());
            }
        }

        private double getLineLength(System.Drawing.Point p1, System.Drawing.Point p2)
        {
            return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
        }

        private Image<Bgr, byte> drawDottedLine(Image <Bgr, byte> imgF, System.Drawing.Point p1, System.Drawing.Point p2, Bgr color, double div)
        {
            System.Drawing.Point point1a = new System.Drawing.Point(0, 0);
            System.Drawing.Point point2a = new System.Drawing.Point(0, 0);
            bool draw = true;

            if(p1.Y == p2.Y)
            {
                if(p1.X > p2.X)
                {
                    int dx = p1.X - p2.X;
                    point1a.X = p2.X;
                    point1a.Y = p2.Y;
                    point2a.Y = point1a.Y;
                    
                    while((point1a.X + ((int)Math.Ceiling(dx / (double)div))) < p1.X)
                    {
                        point2a.X = point1a.X + (int)Math.Ceiling(dx / div);
                        if (draw)
                        {
                            imgF.Draw(new LineSegment2D(point1a, point2a), color, 1);
                            draw = false;
                        }
                        else draw = true;
                        point1a.X = point2a.X;
                    }
                }
                else
                {
                    int dx = p2.X - p1.X;
                    point1a.X = p1.X;
                    point1a.Y = p1.Y;
                    point2a.Y = point1a.Y;

                    while ((point1a.X + ((int)Math.Ceiling(dx / (double)div))) < p2.X)
                    {
                        point2a.X = point1a.X + (int) Math.Ceiling(dx / (double)div);
                        if (draw)
                        {
                            imgF.Draw(new LineSegment2D(point1a, point2a), color, 1);
                            draw = false;
                        }
                        else draw = true;
                        point1a.X = point2a.X;
                    }
                }
            }
            else
            if (p1.X == p2.X)
            {
                if (p1.Y > p2.Y)
                {
                    int dy = p1.Y - p2.Y;
                    point1a.X = p2.X;
                    point1a.Y = p2.Y;
                    point2a.X = point1a.X;

                    while ((point1a.Y + ((int)Math.Ceiling(dy / (double)div))) < p1.Y)
                    {
                        point2a.Y = point1a.Y + (int)Math.Ceiling(dy / div);
                        if (draw)
                        {
                            imgF.Draw(new LineSegment2D(point1a, point2a), color, 1);
                            draw = false;
                        }
                        else draw = true;
                        point1a.Y = point2a.Y;
                    }
                }
                else
                {
                    int dy = p2.Y - p1.Y;
                    point1a.X = p1.X;
                    point1a.Y = p1.Y;
                    point2a.X = point1a.X;

                    while ((point1a.Y + ((int)Math.Ceiling(dy / (double)div))) < p2.Y)
                    {
                        point2a.Y = point1a.Y + (int)Math.Ceiling(dy / (double)div);
                        if (draw)
                        {
                            imgF.Draw(new LineSegment2D(point1a, point2a), color, 1);
                            draw = false;
                        }
                        else draw = true;
                        point1a.Y = point2a.Y;
                    }
                }
            }

            return imgF;
        }

        private void dockPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            paint = false;
            select = false;
        }

        private void dockPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (brushOn)
            {
                paint = true;
                System.Windows.Point clickPoint = e.GetPosition(dockPanel);
                System.Drawing.Point normalizedPoint = new System.Drawing.Point(0, 0);
                normalizedPoint.X = (int)img.Height * (int)clickPoint.X / (int)dockPanel.ActualHeight - brushSize;
                normalizedPoint.Y = (int)img.Width * (int)clickPoint.Y / (int)dockPanel.ActualWidth - brushSize;
                img2.Draw(new System.Drawing.Rectangle(normalizedPoint,
                    new System.Drawing.Size(brushSize, brushSize)), brushColor, -1);
                img = img2.Clone();
                imageChanged = true;
                imageBox.Source = ImageSourceFromBitmap(img2.ToBitmap());
            }

            if (selectOn)
            {
                select = true;
                imgSel = img.Clone();
                System.Windows.Point clickPoint = e.GetPosition(dockPanel);
                selectPoint = new System.Drawing.Point(0, 0);
                selectPoint.X = (int)img.Height * (int)clickPoint.X / (int)dockPanel.ActualHeight - brushSize;
                selectPoint.Y = (int)img.Width * (int)clickPoint.Y / (int)dockPanel.ActualWidth - brushSize;
                imageChanged = true;
            }
        }

        private void StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Point pt = e.GetPosition((UIElement)sender);
            int colRange = (int) pt.X;

            if (colRange < 25)
            {
                brushColor = new Bgr(0, 0, 249);
            }else
            if (colRange < 50)
            {
                brushColor = new Bgr(0, 249, 34);
            }else
            if (colRange < 75)
            {
                brushColor = new Bgr(197, 0, 9);
            }else
            if (colRange < 100)
            {
                brushColor = new Bgr(49, 164, 243);
            }else
            if (colRange < 125)
            {
                brushColor = new Bgr(47, 255, 255);
            }else
            if (colRange < 150)
            {
                brushColor = new Bgr(236, 64, 228);
            }else
            if (colRange < 175)
            {
                brushColor = new Bgr(4, 8, 4);
            }
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            brushOn = true;
            selectOn = false;
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            brushOn = false;
            selectOn = true;
        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            if(selectOn)
            {
                Mat crop = imgSel.Mat;
                Matrix<byte> cropMatrix = new Matrix<byte>(crop.Rows, crop.Cols, crop.NumberOfChannels);
                crop.CopyTo(cropMatrix);
                int startX, startY, endX, endY;

                if (endSelectPoint.X < selectPoint.X)
                {
                    startX = (int)endSelectPoint.X;
                    endX = (int)selectPoint.X;
                }
                else
                {
                    startX = (int)selectPoint.X;
                    endX = (int)endSelectPoint.X;
                }

                if (endSelectPoint.Y < selectPoint.Y)
                {
                    startY = (int)endSelectPoint.Y;
                    endY = (int)selectPoint.Y;
                }
                else
                {
                    startY = (int)selectPoint.Y;
                    endY = (int)endSelectPoint.Y;
                }

                Image<Bgr, byte> croppedImage = new Image<Bgr, byte> (endX - startX, endY - startY);

                for (int i = startX; i < endX; i++)
                {
                    for (int j = startY; j < endY; j++)
                    {
                        croppedImage[j - startY, i - startX] = imgSel[j, i];
                    }

                }
                imageBox.Source = ImageSourceFromBitmap(croppedImage.ToBitmap());
                img = croppedImage.Clone();
                img2 = img.Clone();
                imageChanged = true;
                //saveJpeg("myJPEG.jpeg", croppedImage.ToBitmap(), 100);
            }

            selectOn = false;
        }

        private void saveJpeg(string path, Bitmap img, long quality)
        {
            // Encoder parameter for image quality

            EncoderParameter qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

            // Jpeg image codec
            ImageCodecInfo jpegCodec = this.getEncoderInfo("image/jpeg");

            if (jpegCodec == null)
                return;

            EncoderParameters encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = qualityParam;

            img.Save(path, jpegCodec, encoderParams);
        }

        private ImageCodecInfo getEncoderInfo(string mimeType)
        {
            // Get image codecs for all image formats
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            // Find the correct image codec
            for (int i = 0; i < codecs.Length; i++)
                if (codecs[i].MimeType == mimeType)
                    return codecs[i];
            return null;
        }

        private void grayscaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            grayscaleAlpha = grayscaleSlider.Value / 100.0;
            imageChanged = true;
            Image<Gray, byte> grayscaleImage = img.Convert<Gray, Byte>();
            Image<Bgr, byte> grayscaleImageBgr = grayscaleImage.Convert<Bgr, byte>();
            imgEffects = grayscaleImageBgr.AddWeighted(img, grayscaleAlpha, 1 - grayscaleAlpha, 0); ;
            //img2 = img.Clone();
            imageBox.Source = ImageSourceFromBitmap(imgEffects.ToBitmap());
        }

        private void sepiaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            sepiaAlpha = sepiaSlider.Value / 100;

            if (imageChanged || sepiaImage == null)
            {
                sepiaImage = img.ToBitmap();

                for (int i = 0; i < img.Height; i++)
                    for (int j = 0; j < img.Width; j++)
                    {
                        double red = sepiaImage.GetPixel(j, i).R * 0.393 + sepiaImage.GetPixel(j, i).G * 0.769 + sepiaImage.GetPixel(j, i).B * 0.189;
                        double green = sepiaImage.GetPixel(j, i).R * 0.349 + sepiaImage.GetPixel(j, i).G * 0.686 + sepiaImage.GetPixel(j, i).B * 0.168;
                        double blue = sepiaImage.GetPixel(j, i).R * 0.272 + sepiaImage.GetPixel(j, i).G * 0.534 + sepiaImage.GetPixel(j, i).B * 0.131;

                        red = (red > 255) ? 255 : red;
                        green = (green > 255) ? 255 : green;
                        blue = (blue > 255) ? 255 : blue;
                        Color newColor = new Color();
                        newColor = Color.FromArgb((int)red, (int)green, (int)blue);
                        sepiaImage.SetPixel(j, i, newColor);
                    }

                //outputRed = (inputRed * .393) + (inputGreen * .769) + (inputBlue * .189)
                //outputGreen = (inputRed * .349) + (inputGreen * .686) + (inputBlue * .168)


                //outputBlue = (inputRed * .272) + (inputGreen * .534) + (inputBlue * .131)
                imageChanged = false;
            }

            imgEffects = img2.AddWeighted(sepiaImage.ToImage<Bgr, byte> (), 1 - sepiaAlpha, sepiaAlpha, 0);
            //img2 = img.Clone();
            imageBox.Source = ImageSourceFromBitmap(imgEffects.ToBitmap());

        }

        private void ApplyEffect(object sender, RoutedEventArgs e)
        {
            img = imgEffects.Clone();
            img2 = img.Clone();
        }
    }
}
