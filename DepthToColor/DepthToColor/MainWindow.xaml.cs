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
using Emgu.CV; 
using Emgu.CV.CvEnum; 
using Emgu.CV.Structure; 
using Emgu.Util;



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

        private Image<Bgra, Byte> ImagenColor;
        private Image<Gray, Byte> ImagenDepth;
        private Image<Bgra, Byte> ImagenMappedDepth; 

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
            List<byte[]> kinectArrayBytes = new List<byte[]>();
            byte[] colorArray;
            byte[] depthArray;
            byte[] mappedArray;
            Image<Bgra, Byte> imagenColor = new Image<Bgra,Byte>(640,480);
            Image<Gray, Byte> imagenDepth = new Image<Gray, Byte>(640,480);
            Image<Bgra, Byte> imagenMapped = new Image<Bgra,Byte>(640,480);

            Image<Bgra, Byte> colorDetection = new Image<Bgra, Byte>(640, 480);
            Image<Gray, Byte> colorGray = new Image<Gray, Byte>(640, 480);
            Image<Gray, Byte> grayDetection = new Image<Gray, Byte>(640, 480);
            Image<Gray, Byte> depthDetection = new Image<Gray, Byte>(640, 480); 
            
            kinectArrayBytes = PollData();

            imagenColor.Bytes = kinectArrayBytes[0];
            imagenDepth.Bytes = kinectArrayBytes[1];
            imagenMapped.Bytes = kinectArrayBytes[2]; 


        }//end CompositionTarget_Rendering


        private List<byte[]> PollData()
        {
            List<byte[]> ArrayList = new List<byte[]>(); 

            if (this.Kinect != null)
            {   
                var pixelFormat = PixelFormats.Bgra32;
                var outputBytesPerPixel = pixelFormat.BitsPerPixel / 8;
                
                this.ColorStream = this.Kinect.ColorStream; 
                this.DepthStream = this.Kinect.DepthStream;

                this.DepthValues = new short[DepthStream.FramePixelDataLength];
                this.DepthPixeles = new byte[DepthStream.FramePixelDataLength];
                this.ColorPixeles = new byte[ColorStream.FramePixelDataLength];
                this.output = new byte[DepthStream.FrameWidth * DepthStream.FrameHeight * outputBytesPerPixel];
                this.depthImagePixel = new DepthImagePixel[DepthStream.FramePixelDataLength];
                this.ColorCoordinates = new ColorImagePoint[DepthStream.FramePixelDataLength];
                

                try
                {
                    using(ColorImageFrame colorFrame = this.Kinect.ColorStream.OpenNextFrame(100))
                    using(DepthImageFrame depthFrame = this.Kinect.DepthStream.OpenNextFrame(100))
                    {
                        if (colorFrame != null && depthFrame != null)
                        {
                            StrideColor = colorFrame.BytesPerPixel * colorFrame.Width;
                            int outputIndex = 0; 


                            depthFrame.CopyPixelDataTo(DepthValues);
                            colorFrame.CopyPixelDataTo(ColorPixeles);
                            depthFrame.CopyDepthImagePixelDataTo(depthImagePixel);


                            for (int i = 0; i < depthFrame.PixelDataLength; i++)
                            {
                                int valorDistancia = DepthValues[i] >> 3;

                                if ((valorDistancia == this.Kinect.DepthStream.UnknownDepth)) /*|| (valorDistancia == this.Kinect.DepthStream.TooFarDepth))*/
                                    DepthPixeles[i] = 0;
                                else
                                {
                                    byte byteDistancia = (byte)(255 - (valorDistancia >> 5));
                                    DepthPixeles[i] = byteDistancia;
                                }
                            } 


                            Kinect.CoordinateMapper.MapDepthFrameToColorFrame(depthFrame.Format, depthImagePixel, colorFrame.Format, ColorCoordinates);


                            for (int depthIndex = 0; depthIndex < depthImagePixel.Length; depthIndex++, outputIndex += outputBytesPerPixel)
                            {
                                ColorImagePoint colorPoint = ColorCoordinates[depthIndex];
                                int colorPixelIndex = (colorPoint.X * colorFrame.BytesPerPixel) + (colorPoint.Y * StrideColor);

                                output[outputIndex] = ColorPixeles[colorPixelIndex + 0];
                                output[outputIndex + 1] = ColorPixeles[colorPixelIndex + 1];
                                output[outputIndex + 2] = ColorPixeles[colorPixelIndex + 2];
                            }

                            ArrayList.Add(ColorPixeles);
                            ArrayList.Add(DepthPixeles);
                            ArrayList.Add(output);
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("No se pueden leer los datos del sensor", "Error");
                }
            }

            return ArrayList; 
        }//end PollData


        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::::::::::::::::::::Stop tyhe sensor:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            Kinect.ColorStream.Disable();
            Kinect.DepthStream.Disable();
            Kinect.Stop();
        } 
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::


    } //end class
}//end namespace
