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
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.Util;
using System.IO;
using System.Drawing; 


namespace HandDetectionFusion
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //::::::::::::::Variables:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private KinectSensor Kinect;
        private ColorImageStream ColorStream;
        private byte[] ColorImagenPixeles;
        private DepthImageStream DepthStream;
        private byte[] DepthImagenPixeles; 
        private short[] DepthValoresStream;
        private byte[] pixelesDepthRGB;

        private WriteableBitmap colorWBitmap; 
        private Int32Rect RectColor;
        private int StrideColor; 
        private WriteableBitmap depthWBitmap;  
        private Int32Rect RectDepth; 
        private int StrideDepth;

        private ColorImagePoint[] mappedDepthPoints;
        private DepthImagePixel[] depthPixels;

        private Image<Bgra, Byte> colorFrameKinect;
        private Image<Gray, Byte> depthFrameKinect;
        //private Image<Bgra, Byte> mappedDepthRGB; 

        private CascadeClassifier haarColor;
        private CascadeClassifier haarDepth; 
        
        private bool moverK = false;
        private bool grabar = false;
        private string path;
        private int index;
        private int numeroGrabaciones = 80;
        private string numeroManos;
        private string tipoIluminacion;
        private string background;
        private string nameClassifier = "5";
        //:::::::::::::fin variables:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::


        //::::::::::::Constructor::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        public MainWindow()
        {
            InitializeComponent();
        }
        //::::::::::::Constructor:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 
        

        //::::::::::::Call methods:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            haarColor = new CascadeClassifier(@"C:\Users\America\Documents\HandRGBHaarCascade\Classifiers\1\1256617233-1-haarcascade_hand.xml"); //La compu de escritorio
            haarDepth = new CascadeClassifier(@"C:\Users\America\Documents\HandRGBHaarCascade\Classifiers\cascade.xml");
            EncuentraInicializaKinect();
            CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);
        } 
        //::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 


        //:::::::::::::Enseguida estan los metodos para desplegar los datos de profundidad de Kinect:::::::::::::::::::::::::::::::::::
        private void EncuentraInicializaKinect()
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
        } //fin EncuentraKinect()   


        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            Image<Bgra, Byte> imagenColor;
            Image<Gray, Byte> imagenDepth; 
            Image<Bgra, Byte> colorDetection;
            Image<Gray, Byte> colorGray;
            Image<Gray, Byte> grayDetection; 
            Image<Gray, Byte> depthDetection; 

            imagenColor = PollColor();
            imagenDepth = PollDepth(); 

            //Call detection an save the image with the result of the classifier. 
            colorGray = imagenColor.Convert<Gray,Byte>(); 
            depthDetection = Detection(haarDepth,imagenDepth);
            grayDetection = Detection(haarColor,colorGray);
            colorDetection = grayDetection.Convert<Bgra, Byte>(); 

            //Display the result of the classifier, so the bytes of the imagen
            //are converted in a wriablebitmap.   
            colorStreamKinect.Source = colorWriteablebitmap(colorDetection);
            depthStreamKinect.Source = depthWriteablebitmap(depthDetection); 
            //imageKinect.Source = imagetoWriteablebitmap(imageHaar1);
            //imageMedianFilter.Source = imagetoWriteablebitmap(imageHaar1NoNoise3);

            /*if ((index < (numeroGrabaciones)) && grabar)
            {
                //guardaimagen(imageHaar1, path, index, "Noise");
                //guardaimagen(imageHaar1NoNoise3, path, index, "noNoise");

                index++;
            }

            if (index == numeroGrabaciones)
            {
                simple.IsEnabled = true;
                complejo.IsEnabled = true;
                iluminacion.IsEnabled = true;
                noiluminacion.IsEnabled = true;
            }*/

        } //fin CompositionTarget_Rendering()  


        private Image<Bgra, Byte> PollColor()
        {
            if (this.Kinect != null)
            {
                this.ColorStream = this.Kinect.ColorStream;
                this.ColorImagenPixeles = new byte[ColorStream.FramePixelDataLength];
                this.colorFrameKinect = new Image<Bgra, Byte>(ColorStream.FrameWidth, ColorStream.FrameHeight);

                try
                {
                    using (ColorImageFrame frame = this.Kinect.ColorStream.OpenNextFrame(100))
                    {
                        if (frame != null)
                        {
                            frame.CopyPixelDataTo(this.ColorImagenPixeles);
                            colorFrameKinect.Bytes = ColorImagenPixeles; //The bytes are converted to a Imagen(Emgu). This to work with the functions of opencv. 
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("No se pueden leer los datos de color del sensor", "Error");
                }
            }

            return colorFrameKinect;
        }//fin PollColor()


        private Image<Gray, Byte> PollDepth()
        { 
            if (this.Kinect != null)
            {
                this.DepthStream = this.Kinect.DepthStream;
                this.DepthValoresStream = new short[DepthStream.FramePixelDataLength];
                this.DepthImagenPixeles = new byte[DepthStream.FramePixelDataLength];
                this.depthFrameKinect = new Image<Gray, Byte>(DepthStream.FrameWidth, DepthStream.FrameHeight);

                try
                {
                    using (DepthImageFrame frame = this.Kinect.DepthStream.OpenNextFrame(100))
                    {
                        if (frame != null) 
                        {
                            mappedDepthPoints = new ColorImagePoint[frame.PixelDataLength];
                            depthPixels = new DepthImagePixel[frame.PixelDataLength];
                            pixelesDepthRGB = new byte[frame.PixelDataLength];
                            
                            frame.CopyPixelDataTo(this.DepthValoresStream);
                            frame.CopyDepthImagePixelDataTo(depthPixels);

                            int index = 0;
                            for (int i = 0; i < frame.PixelDataLength; i++)
                            {
                                int valorDistancia = DepthValoresStream[i] >> 3;

                                if (valorDistancia == this.Kinect.DepthStream.UnknownDepth)
                                {
                                    DepthImagenPixeles[index] = 0;
                                }
                                else if (valorDistancia == this.Kinect.DepthStream.TooFarDepth)
                                {
                                    DepthImagenPixeles[index] = 0;
                                }
                                else
                                {
                                    byte byteDistancia = (byte)(255 - (valorDistancia >> 5));
                                    DepthImagenPixeles[index] = byteDistancia;
                                }
                                index++; //= index + 4; 

                                
                            }

                            Kinect.CoordinateMapper.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, depthPixels, ColorImageFormat.RgbResolution640x480Fps30, mappedDepthPoints);

                            mappedDepthPoints.Cast(byte).CopyTo(pixelesDepthRGB); 

                            depthFrameKinect.Bytes = DepthImagenPixeles; //The bytes are converted to a Imagen(Emgu). This to work with the functions of opencv. 
                            depthFrameKinect.SmoothMedian(3); 
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("No se pueden leer los datos de profundidad del sensor", "Error");
                }
            }

            return depthFrameKinect;
        }//fin PollDepth() 


        private WriteableBitmap colorWriteablebitmap(Image<Bgra, Byte> frameHand)
        {
            byte[] imagenPixels = new byte[ColorStream.FramePixelDataLength];

            this.colorWBitmap = new WriteableBitmap(ColorStream.FrameWidth, ColorStream.FrameHeight, 96, 96, PixelFormats.Bgr32, null);
            this.RectColor = new Int32Rect(0, 0, ColorStream.FrameWidth, ColorStream.FrameHeight);
            this.StrideColor = ColorStream.FrameWidth * ColorStream.FrameBytesPerPixel;

            imagenPixels = frameHand.Bytes;
            colorWBitmap.WritePixels(RectColor, imagenPixels, StrideColor, 0);

            return colorWBitmap;
        }//end colorWriteablebitmap 


        private WriteableBitmap depthWriteablebitmap(Image<Gray, Byte> frameHand)
        {
            byte[] imagenPixels = new byte[DepthStream.FrameWidth * DepthStream.FrameHeight];

            this.depthWBitmap = new WriteableBitmap(DepthStream.FrameWidth, DepthStream.FrameHeight, 96, 96, PixelFormats.Gray8, null);
            this.RectDepth = new Int32Rect(0, 0, DepthStream.FrameWidth, DepthStream.FrameHeight);
            this.StrideDepth = DepthStream.FrameWidth;

            imagenPixels = frameHand.Bytes;
            depthWBitmap.WritePixels(RectDepth, imagenPixels, StrideDepth, 0);

            return depthWBitmap;
        }//end depthwriteablebitmap; 

        //:::::::::::::Fin de los metodos para manipular los datos del Kinect::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 
        
        
        //:::::::::::::Methods to do the detection, using haar features and cascade trining, the classifiers already trained::::::::::::
        private Image<Gray, Byte> Detection(CascadeClassifier haar, Image<Gray, Byte> frame)
        {
            if (frame != null)
            {
                System.Drawing.Rectangle[] hands = haar.DetectMultiScale(frame, 1.4, -1, new System.Drawing.Size(frame.Width / 9, frame.Height / 9), new System.Drawing.Size(frame.Width / 4, frame.Height / 4));

                foreach (System.Drawing.Rectangle roi in hands)
                {
                    Gray colorcillo = new Gray(double.MaxValue);
                    frame.Draw(roi, colorcillo, 3);
                }
            }

            return frame;
        }//finaliza detection() 



        //::::::::::::Turn it off the kinect:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            this.Kinect.Stop(); 
        } 
        
    }//end class
}//end namespace
