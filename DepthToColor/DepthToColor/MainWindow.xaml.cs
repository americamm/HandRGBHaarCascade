﻿using System;
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

        private CascadeClassifier haarColor;
        private CascadeClassifier haarDepth; 
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::


        public MainWindow()
        {
            InitializeComponent();
        }


        //::::::::::::::::::::Call all methods:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            haarColor = new CascadeClassifier(@"C:\Users\America\Documents\HandRGBHaarCascade\Classifiers\1\1256617233-1-haarcascade_hand.xml"); //La compu de escritorio
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
            List<byte[]> kinectArrayBytes = new List<byte[]>(3);
            List<object> return1 = new List<object>(2);
            List<object> return2 = new List<object>(2);
            List<object> return3 = new List<object>(2);

            Image<Bgra, Byte> imagenColor = new Image<Bgra,Byte>(640,480);
            Image<Gray, Byte> imagenDepth = new Image<Gray, Byte>(640,480);
            Image<Bgra, Byte> imagenMapped = new Image<Bgra,Byte>(640,480);

            Image<Gray, Byte> colorDetection = new Image<Gray, Byte>(640, 480);
            Image<Gray, Byte> mappedDetection = new Image<Gray, Byte>(640, 480); 
            Image<Gray, Byte> depthDetection = new Image<Gray, Byte>(640, 480);

            System.Drawing.Rectangle[] HandsColor;
            System.Drawing.Rectangle[] HandsDepth;
            System.Drawing.Rectangle[] HandsBoth; 
             
            
            kinectArrayBytes = PollData();

            imagenColor.Bytes = kinectArrayBytes[0];
            imagenDepth.Bytes = kinectArrayBytes[1];
            imagenMapped.Bytes = kinectArrayBytes[2]; 

            //Preposesing imagen for the detection, remove of noise in the image of depth and covertionsof color image. 
            depthDetection = imagenDepth.SmoothMedian(3);
            colorDetection = imagenColor.Convert<Gray, Byte>();
            mappedDetection = imagenMapped.Convert<Gray, Byte>();

            //Detection of the hand 
            return1 = Detection(haarColor, colorDetection);
            return2 = Detection(haarColor, mappedDetection);
            return3 = Detection(haarDepth, depthDetection); 
            
            //Cast to his respdctive data type 
            HandsColor = (System.Drawing.Rectangle[])return1[0];
            HandsBoth = (System.Drawing.Rectangle[])return2[0];
            HandsDepth = (System.Drawing.Rectangle[])return3[0];
            colorDetection = (Image<Gray,Byte>)return1[1];
            mappedDetection = (Image<Gray, Byte>)return2[1];
            depthDetection = (Image<Gray, Byte>)return3[1]; 

            //Convert to bgra for the display 
            imagenColor = colorDetection.Convert<Bgra, Byte>();
            imagenMapped = mappedDetection.Convert<Bgra, Byte>();

            //Display the images
            colorImage.Source = colorWriteablebitmap(imagenColor);
            DepthAndColorImage.Source = colorWriteablebitmap(imagenMapped);
            depthImage.Source = depthWriteablebitmap(depthDetection); 


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

                                if ((valorDistancia == this.Kinect.DepthStream.UnknownDepth)|| (valorDistancia == this.Kinect.DepthStream.TooFarDepth))
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
                System.Drawing.Rectangle[] hands = haar.DetectMultiScale(frame, 1.4, -1, new System.Drawing.Size(frame.Width / 9, frame.Height / 9), new System.Drawing.Size(frame.Width / 4, frame.Height / 4));

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


        private void handDetection(System.Drawing.Rectangle[] ColorRA, System.Drawing.Rectangle[] DepthRA) 
        {
            List<System.Drawing.Rectangle[]> ListRect = new List<System.Drawing.Rectangle[]>();
            System.Drawing.Rectangle[] ArrayRect = new System.Drawing.Rectangle[2]; 

            foreach (System.Drawing.Rectangle rect1 in ColorRA)
            {
                foreach (System.Drawing.Rectangle rect2 in DepthRA)
                { 
                
                }
            }
        }

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
