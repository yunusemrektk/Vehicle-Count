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
using Emgu.CV.Structure;
using AForge.Vision.Motion;
using Emgu.CV.CvEnum;
using System.Threading;
using Emgu.CV.Util;
using OpenTK.Graphics.OpenGL;
using Emgu.CV.VideoSurveillance;
using Emgu.CV.Cvb;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using Emgu.CV.UI;

namespace Vehicle_Count
{
  
    public partial class Form2 : Form
    {
        #region
        Capture cap = new Capture();
        Mat frame = new Mat();
        private bool captureOn;
        Mat grayframe = new Mat();
        int count = 0;
        int IDCount = 0;
        MotionDetector detector;
        CascadeClassifier carCl = new CascadeClassifier("C:\\Users\\YUNUS EMRE\\Desktop\\cars.xml");
        private Image<Bgr, byte> currentframe = null;
     
        BackgroundSubtractorMOG2 sub = new BackgroundSubtractorMOG2();
        CvBlobDetector detect = new CvBlobDetector();
        CvBlobs blobs = new CvBlobs();
        CvTracks tracks = new CvTracks();

        int px1, px2, py1, py2;

        #endregion
        public Form2()
        {
            InitializeComponent();
            detector = new MotionDetector(new TwoFramesDifferenceDetector(), new MotionAreaHighlighting());
           
        }

        private void mp4_play()
        {

            cap = new Capture(textBox2.Text);
            cap.QueryFrame();

            cap.ImageGrabbed += ProcessFrameMP4;
            cap.Start();

        }

        private void ProcessFrameMP4(object sender, EventArgs e)
        {
             DetectionwithSubtracterMP4();
        }

        private void rtsp_play()
        {
            if (!String.IsNullOrEmpty(textBox1.Text))
            {
                
                cap = new Capture(textBox1.Text);
                cap.QueryFrame();

                cap.ImageGrabbed += ProcessFrameRTSP;
                cap.Start();
            }
        }

        private void ProcessFrameRTSP(object sender, EventArgs e)
        {

            DetectionwithSubtracterRTSP();
            
        }
        private void DetectionwithSubtracterRTSP()
        {

            if (cap != null)
            {
                Point px = new Point(px1, px2);
                Point py = new Point(py1, py2);
                cap.Retrieve(frame, 0);
                currentframe = frame.ToImage<Bgr, byte>();

                
                Mat mask = new Mat();
                sub.Apply(currentframe, mask);
                detect.Detect(mask.ToImage<Gray, byte>(), blobs);                
                blobs.FilterByArea(10, int.MaxValue);
                tracks.Update(blobs, 100.0, 1, 10);
                
                Image<Bgr, byte> result = new Image<Bgr, byte>(currentframe.Size);
                using (Image<Gray, Byte> blobMask = detect.DrawBlobsMask(blobs))
                {
                    frame.CopyTo(result, blobMask);
                }
                CvInvoke.Line(currentframe, px, py, new MCvScalar(0, 0, 255), 1);
                foreach (KeyValuePair<uint, CvTrack> pair in tracks)
                {
                    if (pair.Value.Inactive == 0) //only draw the active tracks.
                    {

                       int cx = Convert.ToInt32(pair.Value.Centroid.X);
                       int cy = Convert.ToInt32(pair.Value.Centroid.Y);

                        CvBlob b = blobs[pair.Value.BlobLabel];
                        Bgr color = detect.MeanColor(b, frame.ToImage<Bgr, Byte>());
                        // result.Draw(pair.Key.ToString(), pair.Value.BoundingBox.Location, FontFace.HersheySimplex, 0.5, color);
                        currentframe.Draw(pair.Value.BoundingBox, new Bgr(0, 0, 255), 1);
                        //Point[] contour = b.GetContour();
                        // result.Draw(contour, new Bgr(0, 0, 255), 1);
                        
                        Point center = new Point(cx, cy);
                        CvInvoke.Circle(currentframe, center , 1, new MCvScalar(255, 0, 0), 2);
                        if (center.Y <= px.Y + 2 && center.Y > py.Y - 1 && center.X <=py.X && center.X > px.X)
                        {
                            count++;
                            IDCount++;
                            CvInvoke.Line(currentframe, px, py, new MCvScalar(0, 255, 0), 2);
                           
                            //Json Logger
                            Logs log = new Logs()
                            {
                                Date = DateTime.Now.ToString("dd-mm-yyyy-hh-mm-ss"),
                                Id = IDCount
                            };
                            string strResultJson = JsonConvert.SerializeObject(log);
                            File.AppendAllText(@"log.json", strResultJson+Environment.NewLine);


                        }
                    }
                }


                CvInvoke.PutText(currentframe, "Count :" + count.ToString(), new Point(10, 25),FontFace.HersheySimplex, 1,new MCvScalar(255, 0,0), 2, LineType.AntiAlias);
                //Frame Rate
                //double framerate = cap.GetCaptureProperty(CapProp.Fps);
                //Thread.Sleep((int)(1000.0 / framerate));
               // pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                pictureBox.Image = result.Bitmap;
             
                Thread th = new Thread(currentframepicBoxRtsp);
                th.Start();
            }
        }
        private void DetectionwithSubtracterMP4()
        {

            if (cap != null)
            {
                Point px = new Point(px1, px2);
                Point py = new Point(py1, py2);
                cap.Retrieve(frame, 0);
                currentframe = frame.ToImage<Bgr, byte>();
                

                Mat mask = new Mat();
                sub.Apply(currentframe, mask);
                detect.Detect(mask.ToImage<Gray, byte>(), blobs);
                blobs.FilterByArea(500, 20000);
                tracks.Update(blobs, 20.0, 1, 10);

                Image<Bgr, byte> result = new Image<Bgr, byte>(currentframe.Size);
                using (Image<Gray, Byte> blobMask = detect.DrawBlobsMask(blobs))
                {
                    frame.CopyTo(result, blobMask);
                }
                CvInvoke.Line(currentframe, px, py, new MCvScalar(0, 0, 255), 2);
                foreach (KeyValuePair<uint, CvTrack> pair in tracks)
                {
                    if (pair.Value.Inactive == 0) //only draw the active tracks.
                    {

                        int cx = Convert.ToInt32(pair.Value.Centroid.X);
                        int cy = Convert.ToInt32(pair.Value.Centroid.Y);

                        CvBlob b = blobs[pair.Value.BlobLabel];
                        Bgr color = detect.MeanColor(b, frame.ToImage<Bgr, Byte>());
                        // result.Draw(pair.Key.ToString(), pair.Value.BoundingBox.Location, FontFace.HersheySimplex, 0.5, color);
                        currentframe.Draw(pair.Value.BoundingBox, new Bgr(0, 0, 255), 1);
                        //Point[] contour = b.GetContour();
                        // result.Draw(contour, new Bgr(0, 0, 255), 1);

                        Point center = new Point(cx, cy);
                        CvInvoke.Circle(currentframe, center, 1, new MCvScalar(255, 0, 0), 2);
                        if (center.Y <= px.Y + 2 && center.Y > py.Y - 2 && center.X <= py.X && center.X > px.X)
                        {
                            count++;
                            IDCount++;
                            CvInvoke.Line(currentframe, px, py, new MCvScalar(0, 255, 0), 2);

                            //Json Logger
                            Logs log = new Logs()
                            {
                                Date = DateTime.Now.ToString("dd-mm-yyyy-hh-mm-ss"),
                                Id = IDCount
                            };
                            string strResultJson = JsonConvert.SerializeObject(log);
                            File.AppendAllText(@"log.json", strResultJson + Environment.NewLine);


                        }
                    }
                }


                CvInvoke.PutText(currentframe, "Count :" + count.ToString(), new Point(10, 25), FontFace.HersheySimplex, 1, new MCvScalar(255, 0, 0), 2, LineType.AntiAlias);
                //Frame Rate
                 double framerate = cap.GetCaptureProperty(CapProp.Fps);
                Thread.Sleep((int)(1000.0 / framerate));
                pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                pictureBox.Image = result.Bitmap;

                Thread th = new Thread(currentframepictureBox);
                th.Start();
            }
        }
        private void DetectionwithMotion()
        {


            if (cap != null)
            {
                Point px = new Point(50, 250);
                Point py = new Point(500, 250);
                cap.Retrieve(frame, 0);
                currentframe = frame.ToImage<Bgr, byte>();


                CvInvoke.CvtColor(currentframe, grayframe, ColorConversion.Bgr2Gray);
                CvInvoke.EqualizeHist(grayframe, grayframe);
                CvInvoke.GaussianBlur(grayframe, grayframe, new Size(13, 13), 1.5);
                CvInvoke.Threshold(grayframe, grayframe, 10, 255, ThresholdType.BinaryInv);

                CvInvoke.Line(currentframe, px, py, new MCvScalar(0, 0, 255), 3);



                VectorOfVectorOfPoint cnt = new VectorOfVectorOfPoint();
                Mat hier = new Mat();
                CvInvoke.FindContours(grayframe, cnt, hier, RetrType.External, ChainApproxMethod.ChainApproxTc89L1);
                CvInvoke.DrawContours(currentframe, cnt, -1, new MCvScalar(0, 0, 255));

                
            }

          //  detector.ProcessFrame(currentframe.Bitmap);
            
            //Frame Rate
            double framerate = cap.GetCaptureProperty(CapProp.Fps);
            Thread.Sleep((int)(1000.0 / framerate));
            
            pictureBox1.Image = currentframe.Bitmap;
            Thread th = new Thread(currentframepictureBox);
            th.Start();

        }
        private void DetectionwithCascade()
        {

            if (cap != null)
            {
                Point px = new Point(px1, px2);
                Point py = new Point(py1, py2);
                cap.Retrieve(frame, 0);
                currentframe = frame.ToImage<Bgr, byte>().Resize(frame.Width,frame.Height,Inter.Cubic);

                CvInvoke.CvtColor(currentframe, grayframe, ColorConversion.Bgr2Gray);
                CvInvoke.EqualizeHist(grayframe, grayframe);
                CvInvoke.Line(currentframe, px, py, new MCvScalar(0, 0, 255), 3);
                Rectangle[] cars = carCl.DetectMultiScale(grayframe, 1.1, 6, Size.Empty, Size.Empty);

                if (cars.Length > 0)
                {
                    foreach (var car in cars)
                    {
                        int cx = car.Width / 2;
                        int cy = car.Height / 2;

                        CvInvoke.Rectangle(currentframe, car, new Bgr(Color.Red).MCvScalar, 2);
                        Point center = new Point(car.X + cx, car.Y + cy);
                        CvInvoke.Circle(currentframe, center, 1, new MCvScalar(255, 0, 0), 3);
                        if (center.Y <= px.Y  && center.Y >= py.Y )
                        {
                            
                            count++;
                            CvInvoke.Line(currentframe, px, py, new MCvScalar(0, 255, 0), 3);
                            Console.WriteLine($"count : {count}");
                        }
                    }
                }
                //Frame Rate
                //   double framerate = cap.GetCaptureProperty(CapProp.Fps);
                // Thread.Sleep((int)(1000.0 / framerate));
               // pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                pictureBox1.Image = currentframe.Bitmap;
                
               // Thread th = new Thread(currentframepictureBox);
               // th.Start();

            }

        }
      
        
        private void button3_Click(object sender, EventArgs e)
        {
            SetLocation();
            mp4_play();
            
        }
        void SetLocation()
        {
            px1 = Convert.ToInt32(textBoxx1.Text);
            px2 = Convert.ToInt32(textBoxx2.Text);
            py1 = Convert.ToInt32(textBoxy1.Text);
            py2 = Convert.ToInt32(textBoxy2.Text);
        }
        private void button4_Click(object sender, EventArgs e)
        {
            cap.Stop();
            count = 0;

            
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            textBoxx1.Text = Convert.ToString(150);
            textBoxx2.Text = Convert.ToString(200);
            textBoxy1.Text = Convert.ToString(500);
            textBoxy2.Text = Convert.ToString(200);

            textBox1.Text = "rtsp://170.93.143.139/rtplive/470011e600ef003a004ee33696235daa";
            textBox2.Text = "C:\\Users\\YUNUS EMRE\\Desktop\\video3.mp4";

        }

        private void button5_Click(object sender, EventArgs e)
        {
            SetLocation();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Reset();
        }
        void Reset()
        {
            count = 0;
            pictureBox1.Image = null;
            pictureBox.Image = null;
        }
        void currentframepicBoxRtsp()
        {
            pictureBox1.Image = currentframe.Bitmap;
        }


        void currentframepictureBox()
        {
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.Image = currentframe.Bitmap;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            SetLocation();
            rtsp_play();
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            cap.Stop();
            count = 0;
        }
    }
   
}
