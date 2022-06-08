using Intel.RealSense;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Unity.Collections;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Linq;
using System.Text;


public class RsStreamTextureRenderer2 : MonoBehaviour
{
    private static TextureFormat Convert(Format lrsFormat)
    {
        switch (lrsFormat)
        {
            case Format.Z16: return TextureFormat.R16;
            case Format.Disparity16: return TextureFormat.R16;
            case Format.Rgb8: return TextureFormat.RGB24;
            case Format.Rgba8: return TextureFormat.RGBA32;
            case Format.Bgra8: return TextureFormat.BGRA32;
            case Format.Y8: return TextureFormat.Alpha8;
            case Format.Y16: return TextureFormat.R16;
            case Format.Raw16: return TextureFormat.R16;
            case Format.Raw8: return TextureFormat.Alpha8;
            case Format.Disparity32: return TextureFormat.RFloat;
            case Format.Yuyv:
            case Format.Bgr8:
            case Format.Raw10:
            case Format.Xyz32f:
            case Format.Uyvy:
            case Format.MotionRaw:
            case Format.MotionXyz32f:
            case Format.GpioRaw:
            case Format.Any:
            default:
                throw new ArgumentException(string.Format("librealsense format: {0}, is not supported by Unity", lrsFormat));
        }
    }

    private static int BPP(TextureFormat format)
    {
        switch (format)
        {
            case TextureFormat.ARGB32:
            case TextureFormat.BGRA32:
            case TextureFormat.RGBA32:
                return 32;
            case TextureFormat.RGB24:
                return 24;
            case TextureFormat.R16:
                return 16;
            case TextureFormat.R8:
            case TextureFormat.Alpha8:
                return 8;
            default:
                throw new ArgumentException("unsupported format {0}", format.ToString());

        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class RsPose
    {
        public Vector3 translation;
        public Vector3 velocity;
        public Vector3 acceleration;
        public Quaternion rotation;
        public Vector3 angular_velocity;
        public Vector3 angular_acceleration;
        public int tracker_confidence;
        public int mapper_confidence;
    }
    RsPose pose = new RsPose();

    public RsFrameProvider Source;

    [System.Serializable]
    public class TextureEvent : UnityEvent<Texture> { }

    public Intel.RealSense.Stream _stream;
    public Format _format;
    public int _streamIndex;

    public FilterMode filterMode = FilterMode.Point;

    protected Texture2D texture;



    public int amound_of_images = 0;
    public int amound_of_images_r = 0;
    public int amound_of_images_l = 0;
    //public static List<string> images_names_r = new List<string>();
    //public static List<string> images_names_l = new List<string>();
    //public List<uint> _width = new List<uint>();
    //public List<uint> _height = new List<uint>();
    //public List<double> imu_ = new List<double>();
    
    //public List<Texture2D> tex_ = new List<Texture2D>();
    public string time_name = "";
    public string lOrR = "";
    
    public double prev_time_l = 0;
    public double prev_time_r = 0;

    public string fileIMG = "";
    public string fileIMU = "";
    public string fileTIME = "";
    public string fileTimeNames_l = "";
    public string fileTimeNames_r = "";
    [Space]
    public TextureEvent textureBinding;

    FrameQueue q;
    Predicate<Frame> matcher;

    void Start()
    {
        Source.OnStart += OnStartStreaming;
        Source.OnStop += OnStopStreaming;

        if (_streamIndex == 2)
        {
            lOrR = "right";
        }
        else
        {
            lOrR = "left";
        }

        time_name = System.DateTime.Now.ToString("yyyyMMdd_hhmm");
        Directory.CreateDirectory("C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images" + time_name + "/" + lOrR + "/data");
        Directory.CreateDirectory("C:/Users/pbottoni/Documents/BachelorThesis/TestImages/images" + time_name);
        //Directory.CreateDirectory("C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images" + time_name + "/IMU");
        fileIMG = "C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images" + time_name + "/" + lOrR + "/data.csv";
        //fileIMU = "C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images" + time_name + "/IMU/data.csv";
        fileTIME = "C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images" + time_name + "/timestamp.txt";
        fileTimeNames_l = "C:/Users/pbottoni/Documents/BachelorThesis/TestImages/images" + time_name + "/timeName_l.csv";
        fileTimeNames_r = "C:/Users/pbottoni/Documents/BachelorThesis/TestImages/images" + time_name + "/timeName_r.csv";

        
    }

    void OnDestroy()
    {
        if (texture != null)
        {
            Destroy(texture);
            texture = null;
        }

        if (q != null)
        {
            q.Dispose();
        }
    }

    protected void OnStopStreaming()
    {
        Source.OnNewSample -= OnNewSample;
        if (q != null)
        {
            q.Dispose();
            q = null;
        }
        formate();
    }

    public void OnStartStreaming(PipelineProfile activeProfile)
    {
        q = new FrameQueue(1);
        matcher = new Predicate<Frame>(Matches);
        Source.OnNewSample += OnNewSample;
    }

    private bool Matches(Frame f)
    {
        using (var p = f.Profile)
            return p.Stream == _stream && p.Format == _format && (p.Index == _streamIndex || _streamIndex == -1);
    }

    void OnNewSample(Frame frame)
    {
        try
        {
            if (frame.IsComposite)
            {
                using (var fs = frame.As<FrameSet>())
                using (var f = fs.FirstOrDefault(matcher))
                {
                    if (f != null)
                        q.Enqueue(f);
                    return;
                } 
            }

            if (!matcher(frame))
                return;

            using (frame)
            {
                q.Enqueue(frame);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            // throw;
        }

    }

    bool HasTextureConflict(VideoFrame vf)
    {
        return !texture ||
            texture.width != vf.Width ||
            texture.height != vf.Height ||
            BPP(texture.format) != vf.BitsPerPixel;
    }

    protected void Update()
    {
        if (q != null)
        {
            VideoFrame frame;
            if (q.PollForFrame<VideoFrame>(out frame))
                using (frame)
                    ProcessFrame(frame);

            /* PoseFrame frame2;
             if (q.PollForFrame<PoseFrame>(out frame2))
                 using (frame2)
                 {
                     frame2.CopyTo(pose);
                     for (int i = 0; i < 3; i++)
                     {
                         imu_.Add(pose.angular_acceleration[i]);
                     }

                     for (int i = 0; i < 3; i++)
                     {
                         imu_.Add(pose.acceleration[i]);
                     }
                 }*/
        }
    }

    private void ProcessFrame(VideoFrame frame)
    {

        if (HasTextureConflict(frame))
        {
            if (texture != null)
            {
                Destroy(texture);
            }
            int index;
            using (var p = frame.Profile)
            {
                index = p.Index;
                bool linear = (QualitySettings.activeColorSpace != ColorSpace.Linear) || (p.Stream != Intel.RealSense.Stream.Color && p.Stream != Intel.RealSense.Stream.Infrared);
                texture = new Texture2D(frame.Width, frame.Height, Convert(p.Format), false, linear)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = filterMode
                };
            }

            textureBinding.Invoke(texture);
        }
        // print("hello world from textureRenderer");
        texture.LoadRawTextureData(frame.Data, frame.Stride * frame.Height);
        /*print(texture.dimension);
        UnityEngine.Color[] pixels = texture.GetPixels();
        Array.Reverse(pixels);
        for (int row = 0; row < height; ++row)
            Array.Reverse(pixels, row * width, width);
        texture.SetPixels(pixels);*/
        texture.Apply();
        uint width = (uint)texture.width;
        uint height = (uint) texture.height;
        UnityEngine.Experimental.Rendering.GraphicsFormat format_ = texture.graphicsFormat;
        System.Byte[] dataTex=texture.GetRawTextureData();

        // StartCoroutine(WaitToAvoidOverwritting());
        /* if (lOrR == "right")
         {
             print((frame.Timestamp - prev_time_r)+" r");
             prev_time_r = frame.Timestamp;
             print(frame.Timestamp);
         }
         else
         {
             print((frame.Timestamp - prev_time_l) + " l");
             prev_time_l = frame.Timestamp;
         }
         */
        // while((Time.time - prev_time) < 0.05)
        //{

        // }
        StreamWriter writer_r = new StreamWriter(fileTimeNames_r, true);
        StreamWriter writer_l = new StreamWriter(fileTimeNames_l, true);
        //prev_time = Time.time;
        if (lOrR == "right" && frame.Timestamp == prev_time_r)
        {
           // print((frame.Timestamp - prev_time_r) + " r");
        }
        else if(lOrR == "left" && frame.Timestamp == prev_time_l)
        {
            //print((frame.Timestamp - prev_time_l) + " l");
        }
        else
        {
            double time = frame.Timestamp;
            
            if (_streamIndex == 1)
            {

                writer_l.Write(_streamIndex.ToString() + "_" + time * 100 + "0000.png\n");
                
                amound_of_images_l += 1;
                
            }
            else
            {
                writer_r.Write(_streamIndex.ToString() + "_" + time * 100 + "0000.png\n");
                amound_of_images_r += 1;
            }

            

            Thread t = new Thread(() => png(width, height, time, dataTex, format_));
            t.Start();
            //Texture2D copyTexture = new Texture2D(texture.width, texture.height);
            //copyTexture.SetPixels(texture.GetPixels());
            //copyTexture.Apply();
            //tex_.Add(copyTexture);
            // print(frame.DataSize);
            //print(texture);
            // byte[] arr = new byte[frame.DataSize];
            //Marshal.Copy(frame.Data, arr, 0, frame.DataSize);
            //byte[] imag = ImageConversion.EncodeArrayToPNG(arr, texture.graphicsFormat, (uint)frame.Width, (uint)frame.Height);

            //ImageDescription info = new ImageDescription(frame.Width, frame.Height, frame.Stride, frame.Width, frame.Height, TextureFormat.Alpha8, frame.DataSize);


            //File.WriteAllBytes("C:/Users/pbottoni/Documents/BachelorThesis/TestImages/" + _streamIndex.ToString() + "_" + frame.Timestamp * 100 + "0000.png",  bytes.ToArray());
            // StartCoroutine(png(width,height,frame));

            
            //File.WriteAllBytes("C:/Users/pbottoni/Documents/BachelorThesis/TestImages/" + _streamIndex.ToString() + "_" + Time.time.ToString() + ".png", frame.ColorFrame);
            //print(amound_of_images);
            print(amound_of_images_l);
            //print(amound_of_images_r);


            //_height.Add(height);
            //_width.Add(width);
            if (lOrR == "right")
            {
                prev_time_r = frame.Timestamp;
            }
            else
            {
                prev_time_l = frame.Timestamp;
            }
            
        }
        writer_l.Close();
        writer_r.Close();
    }

   /*IEnumerator WaitToAvoidOverwritting()
    {
        yield return new WaitForSeconds(1f);
    }*/

    public void png(uint width, uint height, double time, System.Byte[] imageBytes, UnityEngine.Experimental.Rendering.GraphicsFormat format_)
    {
        //NativeArray<byte> imageBytes2 = imageBytes;
       
        byte[] imag = ImageConversion.EncodeArrayToPNG(imageBytes, format_, width, height);
        File.WriteAllBytes("C:/Users/pbottoni/Documents/BachelorThesis/TestImages/images" + time_name +"/" + _streamIndex.ToString() + "_" + time * 100 + "0000.png", imag);

       // yield return null;
    }

    public void formate()
    {
        // print(images_names);
        Texture2D tex = null;
        byte[] data;
        byte[] imag;
        string filePath = "C:/Users/pbottoni/Documents/BachelorThesis/TestImages/images" + time_name+"/";
        amound_of_images = Mathf.Max(amound_of_images_l, amound_of_images_r);
        StreamWriter writer = new StreamWriter(fileIMG, true);
        StreamWriter writer_timestamp = new StreamWriter(fileTIME, true);
        print(amound_of_images);
        //print(amound_of_images_l);
        //print(amound_of_images_r);
        writer.Write("#timestamp [ns],filename\n");
        //using (new avForThread(0, amound_of_images, 4, delegate (int start, int end)
        //  {
        //Parallel.For(0,amound_of_images,i=>
        for (int i = 0; i < amound_of_images-1; i++)
        {
            //print("Start convert");
            //print(images_names[i]);
            //print(_height[i]);
            //print(_width[i]);
            string img = "";
            //imag = tex_[i].EncodeToPNG();

            //File.WriteAllBytes("C:/Users/pbottoni/Documents/BachelorThesis/TestImages/" +img , imag);
            string imag_save_name= File.ReadLines(filePath + "timeName_l.csv").Skip(i).First();
            
            if (_streamIndex == 2)
            {
                img = File.ReadLines(filePath + "timeName_r.csv").Skip(i).First();
            }
            else
            {
                img = imag_save_name;
            }

            

            data = File.ReadAllBytes(filePath + img);
            //print(data.Length);

            tex = new Texture2D(2,2);
           
            tex.LoadImage(data);

            int height = tex.height;
            int width = tex.width;
            print(width + ", " + height);
            UnityEngine.Color[] pixels = tex.GetPixels();
            Array.Reverse(pixels);
            for (int row = 0; row < height; ++row)
                Array.Reverse(pixels,row * width, width);
            if (_streamIndex == 1)
            {
                writer_timestamp.Write(img.Remove(img.Length - 4, 4).Remove(0, 2) + "\n");
            }
            writer.Write(imag_save_name.Remove(imag_save_name.Length - 4, 4).Remove(0, 2) + "," + imag_save_name.Remove(0, 2) + "\n");
            print(imag_save_name.Remove(imag_save_name.Length - 4, 4).Remove(0, 2) + "," + imag_save_name.Remove(0, 2) + "\n");
            //uint width = _width[i];
            //uint height = _height[i];
            for (int p = 0; p < pixels.Length; p++)
            {
                float alpha = pixels[p][3];
                pixels[p][0] = alpha;
                pixels[p][1] = alpha;
                pixels[p][2] = alpha;
                pixels[p][3] = 1;
            }
            //print(pixels[5 * _height[50] + 80][0]);

            print(img);
            tex.SetPixels(pixels);
            print(img);
            tex.Apply();
            print(img);
            imag = tex.EncodeToPNG();

            File.WriteAllBytes("C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images" + time_name + "/" + lOrR + "/data/" + imag_save_name.Remove(0, 2), imag);
            print(img);
            /* System.Byte[] dataTex = tex.GetRawTextureData();
            UnityEngine.Experimental.Rendering.GraphicsFormat format_ = tex.graphicsFormat;
            string imageName = images_names[i];
            // StartCoroutine(WaitToAvoidOverwritting());
            Thread t = new Thread(() => png2(width, height, imageName, dataTex, format_));
            t.Start();
        */
        }
        //});

        writer.Close();
        writer_timestamp.Close();

        /* if (imu_.Count > 0 && _streamIndex == 1)
         {
             TextWriter tw = new System.IO.StreamWriter(fileIMU, false);
             tw.Write("#timestamp [ns],w_RS_S_x [rad s^-1],w_RS_S_y [rad s^-1],w_RS_S_z [rad s^-1],a_RS_S_x [m s^-2],a_RS_S_y [m s^-2],a_RS_S_z [m s^-2] \n");
             tw.Close();


             tw = new StreamWriter(fileIMU, true);
             for (int i = 0; i < imu_.Count / 6; i++)
             {
                 string img = images_names[i];
                 tw.Write(img.Remove(img.Length - 4, 4).Remove(0, 2) + ","+imu_[i * 6] + "," + imu_[i * 6 + 1] + "," + imu_[i * 6 + 2] + "," +
                     imu_[i * 6 + 3] + "," + imu_[i * 6 + 4] + "," + imu_[i * 6 + 5]+"\n");

             }
             tw.Close();
         }*/
    }

    public UnityEngine.Color InvertColor(UnityEngine.Color color)
    {
        var maxColor = color.maxColorComponent;
        var mycolor = new UnityEngine.Color(color[0], color[1], color[2], 1 - color[3]);
        return mycolor;
    }

    public void png2(uint width, uint height, string imageName, System.Byte[] imageBytes, UnityEngine.Experimental.Rendering.GraphicsFormat format_)
    {
        //NativeArray<byte> imageBytes2 = imageBytes;
        byte[] imag = ImageConversion.EncodeArrayToPNG(imageBytes, format_, width, height);
        File.WriteAllBytes("C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images" + time_name + "/" + lOrR + "/data/" + imageName.Remove(0, 2), imag);

        // yield return null;
    }
}
