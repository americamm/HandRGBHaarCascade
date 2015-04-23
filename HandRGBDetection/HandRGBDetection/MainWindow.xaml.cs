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
        private DepthImageStream DepthStream;
        private byte[] DepthImagenPixeles;
        private short[] DepthValoresStream;
        private Image<Gray, Byte> depthFrameKinect;
        private CascadeClassifier haar1;
        private bool moverK = false;
        private bool grabar = false;
        private string path;
        private int index;
        private int numeroGrabaciones = 50;
        private string numeroManos;
        private string tipoIluminacion;
        private string nameClassifier = "1"; 
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
            Image<Gray, Byte> imagenClasificar;
            Image<Gray, Byte> imageHaar1;
            Image<Gray, Byte> imageMedianF3;

            Image<Gray, Byte> imageHaar1NoNoise3;


            imagenClasificar = PollDepth();

            imageMedianF3 = removeNoise(imagenClasificar, 3);
 

            //Call detection an save the image with the result of the classifier.
            imageHaar1 = Detection(haar1, imagenClasificar);
            imageHaar1NoNoise3 = Detection(haar1, imageMedianF3);


            //Display the result of the classifier, so the bytes of the imagen
            //are converted in a wriablebitmap.  
            imageKinect.Source = imagetoWriteablebitmap(imageHaar1);
            imageMedianFilter.Source = imagetoWriteablebitmap(imageHaar1NoNoise3);

            if ((index < (numeroGrabaciones)) && grabar)
            {
                guardaimagen(imageHaar1, path, index, "Noise");
                guardaimagen(imageHaar1NoNoise3, path, index, "noNoise");

                index++;
            }

            if (index == numeroGrabaciones)
            {
                iluminacion.IsEnabled = true;
                noiluminacion.IsEnabled = true;
            }

        } //fin CompositionTarget_Rendering()  


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
                            frame.CopyPixelDataTo(this.DepthValoresStream);

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

                            depthFrameKinect.Bytes = DepthImagenPixeles; //The bytes are converted to a Imagen(Emgu). This to work with the functions of opencv. 
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("No se pueden leer los datos del sensor", "Error");
                }
            }

            return depthFrameKinect;
        }//fin PollDepth()


        //:::::::::::::Fin de los metodos para manipular los datos del Kinect::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 
        //:::::::::::::Methods to do the detection, using haar features and cascade trining, the classifiers already trained::::::::::::

        private Image<Gray, Byte> Detection(CascadeClassifier haar, Image<Gray, Byte> frameDepth)
        {
            if (frameDepth != null)
            {
                System.Drawing.Rectangle[] hands = haar.DetectMultiScale(frameDepth, 1.4, 0, new System.Drawing.Size(frameDepth.Width / 9, frameDepth.Height / 9), new System.Drawing.Size(frameDepth.Width / 4, frameDepth.Height / 4));

                foreach (System.Drawing.Rectangle roi in hands)
                {
                    Gray colorcillo = new Gray(double.MaxValue);
                    frameDepth.Draw(roi, colorcillo, 3);
                }
            }

            return frameDepth;
        }//finaliza detection()


        //:::::::::::::Method to convert a byte[] to a writeablebitmap::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private WriteableBitmap imagetoWriteablebitmap(Image<Gray, Byte> frameHand)
        {
            byte[] imagenPixels = new byte[DepthStream.FrameWidth * DepthStream.FrameHeight];

            this.ImagenWriteablebitmap = new WriteableBitmap(DepthStream.FrameWidth, DepthStream.FrameHeight, 96, 96, PixelFormats.Gray8, null);
            this.WriteablebitmapRect = new Int32Rect(0, 0, DepthStream.FrameWidth, DepthStream.FrameHeight);
            this.WriteablebitmapStride = DepthStream.FrameWidth;

            imagenPixels = frameHand.Bytes;
            ImagenWriteablebitmap.WritePixels(WriteablebitmapRect, imagenPixels, WriteablebitmapStride, 0);

            return ImagenWriteablebitmap;
        }//end 


        //::::::::::::Method to remove the noise, using median filters::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private Image<Gray, Byte> removeNoise(Image<Gray, Byte> imagenKinet, int sizeWindow)
        {
            Image<Gray, Byte> imagenSinRuido;

            imagenSinRuido = imagenKinet.SmoothMedian(sizeWindow);

            return imagenSinRuido;
        }//endremoveNoise 


        //:::::::::::::Method to saves the images with tha detection ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

        private void guardaimagen(Image<Gray, Byte> imagen, string path, int i, string ruido)
        {
            //path ejemplo "C:\imagenClassifiersWitoutNoise\Ilumination\twoHands\Noise\";
            imagen.Save(path + ruido + @"\" + i.ToString() + ".png");
        }//end


        private void botonGrabar_Click(object sender, RoutedEventArgs e)
        {
            path = @"C:\checkDetectionRGB\" + nameClassifier + @"\" + tipoIluminacion + @"\" + numeroManos + @"\";
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
