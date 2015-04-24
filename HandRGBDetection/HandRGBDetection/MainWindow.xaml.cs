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



namespace HandRGBDetection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //::::::::::::::Variables:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private KinectSensor Kinect;
        private WriteableBitmap ImagenWriteablebitmap;
        private Int32Rect WriteablebitmapRect;
        private int WriteablebitmapStride;
        private ColorImageStream ColorStream; 
        private byte[] ColorImagenPixeles;
        private Image<Bgra, Byte> colorFrameKinect;
        private CascadeClassifier haar1;
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


        //::::::::::::Constructor:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        public MainWindow()
        {
            InitializeComponent();
        } 
        //::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 


        //:::::::::::::Call Methods::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            haar1 = new CascadeClassifier(@"C:\Users\America\Documents\HandRGBHaarCascade\Classifiers\1\1256617233-1-haarcascade_hand.xml"); //La compu de escritorio
            //haar1 = new CascadeClassifier(@"C:\Users\America\Documents\HandRGBHaarCascade\Classifiers\2\palm.xml"); //La compu de escritorio
            //haar1 = new CascadeClassifier(@"C:\Users\America\Documents\HandRGBHaarCascade\Classifiers\3\palm.xml"); //La compu de escritorio
            //haar1 = new CascadeClassifier(@"C:\Users\America\Documents\HandRGBHaarCascade\Classifiers\4\palm.xml"); //La compu de escritorio

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
                    //Kinect.DepthStream.Enable();
                    //Kinect.DepthStream.Range = DepthRange.Near;
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
            Image<Bgra, Byte> imagenClasificar;
            Image<Bgra, Byte> imageHaar1;
            //Image<Bgra, Byte> imageMedianF3;
            //Image<Bgra, Byte> imageHaar1NoNoise3;


            imagenClasificar = PollColor();
            //imageMedianF3 = removeNoise(imagenClasificar, 3);
 

            //Call detection an save the image with the result of the classifier.
            imageHaar1 = Detection(haar1, imagenClasificar);
            //imageHaar1NoNoise3 = Detection(haar1, imageMedianF3);


            //Display the result of the classifier, so the bytes of the imagen
            //are converted in a wriablebitmap.  
            imageKinect.Source = imagetoWriteablebitmap(imageHaar1);
            //imageMedianFilter.Source = imagetoWriteablebitmap(imageHaar1NoNoise3);

            if ((index < (numeroGrabaciones)) && grabar)
            {
                guardaimagen(imageHaar1, path, index, "Noise");
                //guardaimagen(imageHaar1NoNoise3, path, index, "noNoise");

                index++;
            }

            if (index == numeroGrabaciones)
            {
                simple.IsEnabled = true;
                complejo.IsEnabled = true; 
                iluminacion.IsEnabled = true;
                noiluminacion.IsEnabled = true;
            }

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
                    MessageBox.Show("No se pueden leer los datos del sensor", "Error");
                }
            }

            return colorFrameKinect;
        }//fin PollColor()


        //:::::::::::::Fin de los metodos para manipular los datos del Kinect::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 
        //:::::::::::::Methods to do the detection, using haar features and cascade trining, the classifiers already trained::::::::::::

        private Image<Bgra, Byte> Detection(CascadeClassifier haar, Image<Bgra, Byte> frame)
        {
            Image<Gray, Byte> frameGrayscale;

            frameGrayscale = frame.Convert<Gray, Byte>(); 

            if (frameGrayscale != null)
            {
                System.Drawing.Rectangle[] hands = haar.DetectMultiScale(frameGrayscale, 1.4, 0, new System.Drawing.Size(frameGrayscale.Width / 8, frameGrayscale.Height / 8), new System.Drawing.Size(frameGrayscale.Width /3, frameGrayscale.Height / 3));

                foreach (System.Drawing.Rectangle roi in hands)
                {
                    Gray colorcillo = new Gray(double.MaxValue);
                    frameGrayscale.Draw(roi, colorcillo, 3);
                }
            }

            return frame = frameGrayscale.Convert<Bgra, Byte>(); 
        }//finaliza detection()


        //:::::::::::::Method to convert a byte[] to a writeablebitmap::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private WriteableBitmap imagetoWriteablebitmap(Image<Bgra, Byte> frameHand)
        {
            byte[] imagenPixels = new byte[ColorStream.FramePixelDataLength];

            this.ImagenWriteablebitmap = new WriteableBitmap(ColorStream.FrameWidth, ColorStream.FrameHeight, 96, 96, PixelFormats.Bgr32, null);
            this.WriteablebitmapRect = new Int32Rect(0, 0, ColorStream.FrameWidth, ColorStream.FrameHeight);
            this.WriteablebitmapStride = ColorStream.FrameWidth* ColorStream.FrameBytesPerPixel;

            imagenPixels = frameHand.Bytes;
            ImagenWriteablebitmap.WritePixels(WriteablebitmapRect, imagenPixels, WriteablebitmapStride, 0);

            return ImagenWriteablebitmap;
        }//end 


        //::::::::::::Method to remove the noise, using median filters::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private Image<Bgra, Byte> removeNoise(Image<Bgra, Byte> imagenKinet, int sizeWindow)
        {
            Image<Bgra, Byte> imagenSinRuido;

            imagenSinRuido = imagenKinet.SmoothMedian(sizeWindow);

            return imagenSinRuido;
        }//endremoveNoise 


        //:::::::::::::Method to saves the images with tha detection ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

        private void guardaimagen(Image<Bgra, Byte> imagen, string path, int i, string ruido)
        {
            //path ejemplo "C:\imagenClassifiersWitoutNoise\Ilumination\twoHands\Noise\";
            imagen.Save(path + ruido + @"\" + i.ToString() + ".png");
        }//end


        private void botonGrabar_Click(object sender, RoutedEventArgs e)
        {
            path = @"C:\checkDetectionRGB\" + background + @"\" + nameClassifier + @"\" + tipoIluminacion + @"\" + numeroManos + @"\";
            grabar = true;
            index = 0; 
        }//end


        private void iluminacion_Checked(object sender, RoutedEventArgs e)
        {
            tipoIluminacion = "Ilumination";

            noiluminacion.IsEnabled = false;
            unaMano.IsEnabled = true;
            dosManos.IsEnabled = true; 
        }//end 


        private void noiluminacion_Checked(object sender, RoutedEventArgs e)
        {
            tipoIluminacion = "noIlumination";

            iluminacion.IsEnabled = false;
            unaMano.IsEnabled = true;
            dosManos.IsEnabled = true; 
        }//end        
        
        
        private void unaMano_Checked(object sender, RoutedEventArgs e)
        {
            numeroManos = "1";
            dosManos.IsEnabled = false; 
        }


        private void dosManos_Checked(object sender, RoutedEventArgs e)
        {
            numeroManos = "2";
            unaMano.IsEnabled = false; 
        }        
        
        
        private void simple_Checked(object sender, RoutedEventArgs e)
        {
            background = "Simple";

            complejo.IsEnabled = false; 
            noiluminacion.IsEnabled = true;
            iluminacion.IsEnabled = true;  
        }


        private void complejo_Checked(object sender, RoutedEventArgs e)
        {
            background = "Complex";

            simple.IsEnabled = false; 
            noiluminacion.IsEnabled = true;
            iluminacion.IsEnabled = true; 
        }

        //::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 


        //:::::::::::::Move the angle of the tilt of the kinect:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 
        private void anguloSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (moverK)
                Kinect.ElevationAngle = (int)anguloSlider.Value;
        }//end


        private void moverKinect_Checked(object sender, RoutedEventArgs e)
        {
            moverK = true;
            anguloSlider.Value = (double)Kinect.ElevationAngle;
            anguloSlider.IsEnabled = true;
        }//end
        //::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 


        //::::::::::::::Turn it off the kinect::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 
        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            Kinect.Stop();
        }


        //::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 
    

    }//end class
}//end namespace
