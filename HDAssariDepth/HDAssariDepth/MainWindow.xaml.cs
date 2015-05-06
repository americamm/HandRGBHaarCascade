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


namespace HDAssariDepth
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

        private WriteableBitmap colorWBitmap;
        private Int32Rect RectColor;
        private int StrideColor;
        private WriteableBitmap depthWBitmap;
        private Int32Rect RectDepth;
        private int StrideDepth;

        private CascadeClassifier haarColor;
        private CascadeClassifier haarDepth;

        private int contador = 0;
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            haarDepth = new CascadeClassifier(@"C:\Users\America\Documents\HandRGBHaarCascade\Classifiers\cascade.xml"); 
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
            //:::::::::::Variables:::::::::::::::::::::::::::::::::::::::::::::::
            List<byte[]> kinectArrayBytes = new List<byte[]>(2);
            List<object> return1 = new List<object>(2);
            List<object> return2 = new List<object>(2);

            Image<Gray, Byte> imagenDepth = new Image<Gray, Byte>(640, 480);
            Image<Bgra, Byte> imagenMapped = new Image<Bgra, Byte>(640, 480);

            Image<Gray, Byte> colorDetection = new Image<Gray, Byte>(640, 480);
            Image<Gray, Byte> mappedDetection = new Image<Gray, Byte>(640, 480);
            Image<Gray, Byte> depthDetection = new Image<Gray, Byte>(640, 480);

            System.Drawing.Rectangle[] HandsDepth;
            System.Drawing.Rectangle[] HandsBoth;

            List<System.Drawing.Rectangle> ListRectangles; 
            List<Image<Gray, Byte>> listHandColor;
            List<int> indexFrames; 
            //::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
            
            kinectArrayBytes = PollData();

            if (kinectArrayBytes.Count != 0)
            {
                imagenDepth.Bytes = kinectArrayBytes[0];
                imagenMapped.Bytes = kinectArrayBytes[1];

                //Preposesing imagen for the detection, remove of noise in the image of depth and covertionsof color image. 
                depthDetection = imagenDepth.SmoothMedian(3);

                //Detection of the hand 
                //return1 = Detection(haarColor, mappedDetection);
                return2 = Detection(haarDepth, depthDetection);

                //Cast to his respective data type 
                //HandsBoth = (System.Drawing.Rectangle[])return1[0];
                //mappedDetection = (Image<Gray, Byte>)return1[1];
                HandsDepth = (System.Drawing.Rectangle[])return2[0]; 
                depthDetection = (Image<Gray, Byte>)return2[1];

                listHandColor = rectanglesFrameColor(imagenMapped, HandsDepth); //Binary images from the hand
                indexFrames = FindHandColorRoi(listHandColor);

                if (indexFrames.Count != 0)
                {
                    imagenMapped = DrawRoiMappedImage(imagenMapped, HandsDepth, indexFrames);
                    //imagenMapped = mappedDetection.Convert<Bgra, Byte>(); //Convert to bgra for the display 
                }

                //Display the images
                DepthAndColorImage.Source = colorWriteablebitmap(imagenMapped);
                DepthImage.Source = depthWriteablebitmap(depthDetection);
            }//end if  
        }

        private Image<Gray, byte> DrawHandMappedImage()
        {
            throw new NotImplementedException();
        }//end CompositionTarget_Rendering


        private List<byte[]> PollData()
        {
            List<byte[]> ArrayList = new List<byte[]>(2);

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
                    using (ColorImageFrame colorFrame = this.Kinect.ColorStream.OpenNextFrame(100))
                    using (DepthImageFrame depthFrame = this.Kinect.DepthStream.OpenNextFrame(100))
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

                                if ((valorDistancia == this.Kinect.DepthStream.UnknownDepth) || (valorDistancia == this.Kinect.DepthStream.TooFarDepth))
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
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //:::::::::::::Methods to do the detection, using haar features and cascade trining, the classifiers already trained:::::::::::::::::::::::::::::::

        private List<object> Detection(CascadeClassifier haar, Image<Gray, Byte> frame)
        {
            List<object> returnDetection = new List<object>(2);

            if (frame != null)
            {
                System.Drawing.Rectangle[] hands = haar.DetectMultiScale(frame, 1.4, 1, new System.Drawing.Size(frame.Width / 9, frame.Height / 9), new System.Drawing.Size(frame.Width / 4, frame.Height / 4));

                foreach (System.Drawing.Rectangle roi in hands)
                {
                    Gray colorcillo = new Gray(double.MaxValue);
                    frame.Draw(roi, colorcillo, 3);
                }

                returnDetection.Add(hands);
                returnDetection.Add(frame);
            }

            return returnDetection;
        }//finaliza detection()  


        private List<Image<Gray, Byte>> rectanglesFrameColor(Image<Bgra, Byte> frameColor, System.Drawing.Rectangle[] roiArray)
        {
            List<Image<Gray, Byte>> roiColor = new List<Image<Gray, Byte>>();
            Image<Bgra, Byte> imagen; 
            Image<Ycc,Byte> imageYcc;
            Image<Gray, Byte> imageGray; 

            foreach (System.Drawing.Rectangle roi in roiArray)
            {
                imagen = frameColor.Clone();
                imagen.ROI = roi; 
                imageYcc = imagen.Convert<Ycc, Byte>();  

                imageGray = SkinColorSegmentation(imageYcc); 
                roiColor.Add(imageGray);
            }

            return roiColor; 
        }//end rectanglesFrameColor 


        private Image<Gray,Byte> SkinColorSegmentation(Image<Ycc, Byte> FrameYcc)
        {
            int filas = FrameYcc.Height;
            int columnas = FrameYcc.Width; 
            double mediaCr = 149.7692;
            double mediaCb = 114.3846;
            double deCr = 13.80914;
            double deCb = 7.136041;
            
            Image<Gray, Byte> GrayImage = new Image<Gray,Byte>(filas,columnas); 
            byte[, ,] bytesGrayImagen = new byte[filas, columnas, 1];
            byte[, ,] arregloBytes = new byte[filas, columnas, 3]; 
            
            double izqCr = mediaCr - deCr;
            double derCr = mediaCr + deCr;
            double izqCb = mediaCb - deCb;
            double derCb = mediaCb + deCb;
            arregloBytes = FrameYcc.Data; 

            for (int i = 0; i < filas; i++)
            {
                for (int j = 0; j < columnas; j++)
                {
                    if ((izqCr < arregloBytes[i, j, 1]) && (arregloBytes[i, j, 1] < derCr) && (izqCb < arregloBytes[i, j, 2]) && (arregloBytes[i, j, 2] < derCb))
                        bytesGrayImagen[i, j, 0] = 255;
                }
            }

            GrayImage.Data = bytesGrayImagen;

            return GrayImage; 
        }//end SkinColorSegmentation 


        private List<int> FindHandColorRoi(List<Image<Gray, Byte>> ColorRoi)
        { 
            List<int> ListIndex = new List<int>();
            Image<Gray, Byte> ImageComparation; 
            
            int index=0;  
            Gray colorcillo = new Gray(0); 

            foreach (Image<Gray, Byte> imagen in ColorRoi)
            { 
                ImageComparation = new Image<Gray, Byte>(imagen.Width, imagen.Height, colorcillo); 
                if (imagen.Equals(ImageComparation) == false)
                {
                    ListIndex.Add(index); 
                } 
                index++; 
            }

            return ListIndex; 
        }//end 


        private Image<Bgra,Byte> DrawRoiMappedImage(Image<Bgra,Byte> MappedImage, System.Drawing.Rectangle[] RoiArray, List<int> indexArray) 
        {
            Bgra negro = new Bgra(0,0,0,0);
            Bgra verde = new Bgra(0, 255, 0, 0);

            foreach(int index in indexArray) //acomodar es en cada indice son todos los values; 
            {
                MappedImage.Draw(RoiArray[index], negro, 3); 
            }
            foreach (System.Drawing.Rectangle roi in RoiArray)
            {
                MappedImage.Draw(roi, verde, 4);
            }

            return MappedImage;            
        }//end 
 
        //::::::::::::::::::::Stop tyhe sensor:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            Kinect.ColorStream.Disable();
            Kinect.DepthStream.Disable();
            Kinect.Stop();
        }
        //::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: 

    }//end class
}//end namespace 
