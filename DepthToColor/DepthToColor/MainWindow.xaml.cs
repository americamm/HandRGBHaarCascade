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

                            StrideColor = colorFrame.BytesPerPixel * colorFrame.Width;

                            output = new byte[DepthStream.FrameWidth * DepthStream.FrameHeight * outputBytesPerPixel];

                            int outputIndex = 0;

                            ColorCoordinates = new ColorImagePoint[depthFrame.PixelDataLength];
                            depthImagePixel = new DepthImagePixel[]

                            this.Kinect.CoordinateMapper.MapDepthFrameToColorFrame(depthFrame.Format, DepthValues, colorFrame.Format, ColorCoordinates);
 


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
