using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.Drawing; 



namespace DepthToColor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //:::::::::::::::Declaration::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private KinectSensor Kinect;
        private ColorImageStream ColorStream;
        private DepthImageStream DepthStream;

        private ColorImagePoint[] ColorCoordinates;
        private DepthImagePixel[] depthImagePixel; 

        private byte[] ColorPixeles;
        private byte[] DepthPixeles;
        private short[] DepthValues;

        private byte[] output; 
        private byte[] mappedValues;

        private WriteableBitmap colorWBitmap;
        private Int32Rect RectColor;
        private int StrideColor;
        private WriteableBitmap outputWBitmap;
        private Int32Rect RectOutput;
        private int StrideOutput; 
        private WriteableBitmap depthWBitmap;
        private Int32Rect RectDepth;
        private int StrideDepth;  
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::


        public MainWindow()
        {
            InitializeComponent();
        }


        //::::::::::::::::::::Call all methods:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FindKinect();
            CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);
        }
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::::::::::::::::::::Get the data from the kinect::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 

        private void FindKinect()
        {
            Kinect = KinectSensor.KinectSensors.FirstOrDefault();

            try
            {
                if (Kinect.Status == KinectStatus.Connected)
                {
                    Kinect.ColorStream.Enable();
                    Kinect.DepthStream.Enable();
                    Kinect.DepthStream.Range = DepthRange.Near;
                    Kinect.Start();
                }
            }
            catch
            {
                MessageBox.Show("El dispositivo Kinect no se encuentra conectado", "Error Kinect");
            }
        } //end FinKinect() 


        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            PollData(); 
        }//end CompositionTarget_Rendering


        private void PollData()
        {
            if (this.Kinect != null)
            {
                this.ColorStream = this.Kinect.ColorStream; 
                this.DepthStream = this.Kinect.DepthStream;
                this.DepthValues = new short[DepthStream.FramePixelDataLength];
                this.DepthPixeles = new byte[DepthStream.FramePixelDataLength];
                this.ColorPixeles = new byte[ColorStream.FramePixelDataLength];
                this.depthImagePixel = new DepthImagePixel[DepthStream.FramePixelDataLength];

                var pixelFormat = PixelFormats.Bgra32;
                var outputBytesPerPixel = pixelFormat.BitsPerPixel / 8;

                try
                {
                    using(ColorImageFrame colorFrame = this.Kinect.ColorStream.OpenNextFrame(100))
                    using(DepthImageFrame depthFrame = this.Kinect.DepthStream.OpenNextFrame(100))
                    {
                        if (colorFrame != null && depthFrame != null)
                        {
                            depthFrame.CopyPixelDataTo(DepthValues);
                            colorFrame.CopyPixelDataTo(ColorPixeles);
                            depthFrame.CopyDepthImagePixelDataTo(depthImagePixel);

                            int index = 0;
                            for (int i = 0; i < depthFrame.PixelDataLength; i++)
                            {
                                int valorDistancia = DepthValues[i] >> 3;

                                if (valorDistancia == this.Kinect.DepthStream.UnknownDepth)
                                {
                                    DepthPixeles[index] = 0;
                                }
                                else if (valorDistancia == this.Kinect.DepthStream.TooFarDepth)
                                {
                                    DepthPixeles[index] = 0;
                                }
                                else
                                {
                                    byte byteDistancia = (byte)(255 - (valorDistancia >> 5));
                                    DepthPixeles[index] = byteDistancia;
                                }
                                index++; //= index + 4; 
                            }


                            StrideColor = colorFrame.BytesPerPixel * colorFrame.Width;

                            output = new byte[DepthStream.FrameWidth * DepthStream.FrameHeight * outputBytesPerPixel];

                            int outputIndex = 0; 
                         
                            ColorCoordinates = new ColorImagePoint[depthFrame.PixelDataLength];
                            //depthImagePixel = new DepthImagePixel[depthFrame.PixelDataLength];

                            this.Kinect.CoordinateMapper.MapDepthFrameToColorFrame(depthFrame.Format, depthImagePixel, colorFrame.Format, ColorCoordinates);

                            for (int depthIndex = 0; depthIndex < depthImagePixel.Length; depthIndex++, outputIndex += outputBytesPerPixel)
                            {
                                ColorImagePoint colorPoint = ColorCoordinates[depthIndex];
                                int colorPixelIndex = (colorPoint.X * colorFrame.BytesPerPixel) + (colorPoint.Y * StrideColor);

                                output[outputIndex] = ColorPixeles[colorPixelIndex + 0];
                                output[outputIndex + 1] = ColorPixeles[colorPixelIndex + 1];
                                output[outputIndex + 2] = ColorPixeles[colorPixelIndex + 2];

                            }

                            this.colorWBitmap = new WriteableBitmap(ColorStream.FrameWidth, ColorStream.FrameHeight, 96, 96, PixelFormats.Bgr32, null);
                            this.RectColor = new Int32Rect(0, 0, ColorStream.FrameWidth, ColorStream.FrameHeight);
                            this.StrideColor = ColorStream.FrameWidth * ColorStream.FrameBytesPerPixel;

                            this.outputWBitmap = new WriteableBitmap(ColorStream.FrameWidth, ColorStream.FrameHeight, 96, 96, PixelFormats.Bgr32, null);
                            this.RectOutput = new Int32Rect(0, 0, ColorStream.FrameWidth, ColorStream.FrameHeight);
                            //this.StrideColor = ColorStream.FrameWidth * ColorStream.FrameBytesPerPixel;

                            this.depthWBitmap = new WriteableBitmap(DepthStream.FrameWidth, DepthStream.FrameHeight, 96, 96, PixelFormats.Gray8, null);
                            this.RectDepth = new Int32Rect(0, 0, DepthStream.FrameWidth, DepthStream.FrameHeight);
                            this.StrideDepth = DepthStream.FrameWidth; 

                            outputWBitmap.WritePixels(RectColor, output, StrideColor, 0);
                            colorWBitmap.WritePixels(RectColor, ColorPixeles, StrideColor, 0);
                            depthWBitmap.WritePixels(RectColor, DepthPixeles, StrideDepth, 0);

                            DepthAndColorImage.Source = outputWBitmap;
                            colorImage.Source = colorWBitmap;
                            depthImage.Source = depthWBitmap; 
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("No se pueden leer los datos del sensor", "Error");
                }
            }

        }//end PollData


        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::::::::::::::::::::Stop tyhe sensor:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {

        } 
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::


    } //end class
}//end namespace
