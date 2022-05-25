using Intel.RealSense;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Linq;
using System.Text;


public class RsStreamTextureRenderer : MonoBehaviour
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

    int frameRate = 25;

    public int amound_of_images=0;
    public static List<string> images_names = new List<string>();
    public List<int> _width = new List<int>();
    public List<int> _height = new List<int>();
    public List<double> imu_ = new List<double>();
    public List<Texture2D> tex_ = new List<Texture2D>();
    public string time_name = "";
    public string lOrR = "";

    public string fileIMG = "";
    public string fileIMU = "";
    public string fileTIME = "";
    [Space]
    public TextureEvent textureBinding;

    FrameQueue q;
    Predicate<Frame> matcher;

    void Start()
    {
        Source.OnStart += OnStartStreaming;
        Source.OnStop += OnStopStreaming;
        Time.captureFramerate = frameRate;
        if (_streamIndex == 2)
        {
            lOrR = "right";
        }
        else
        {
            lOrR = "left";
        }
        
        time_name= System.DateTime.Now.ToString("yyyyMMdd_hhmm");
        Directory.CreateDirectory("C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images" + time_name+"/"+lOrR+"/data");
        //Directory.CreateDirectory("C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images" + time_name + "/IMU");
        fileIMG = "C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images" + time_name + "/" + lOrR+"/data.csv";
        //fileIMU = "C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images" + time_name + "/IMU/data.csv";
        fileTIME = "C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images" + time_name + "/timestamp.txt";
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

    protected void LateUpdate()
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
        Time.captureFramerate = frameRate;
        if (HasTextureConflict(frame))
        {
            if (texture != null)
            {
                Destroy(texture);
            }
            int index;
            using (var p = frame.Profile) {
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
        Texture2D copyTexture = new Texture2D(texture.width, texture.height);
        copyTexture.SetPixels(texture.GetPixels());
        copyTexture.Apply();
        tex_.Add(copyTexture);
        //byte[] imag = texture.EncodeToPNG();
       // print(frame.DataSize);
        //print(texture);
       // byte[] arr = new byte[frame.DataSize];
        //Marshal.Copy(frame.Data, arr, 0, frame.DataSize);
        //byte[] imag = ImageConversion.EncodeArrayToPNG(arr, texture.graphicsFormat, (uint)frame.Width, (uint)frame.Height);
            
        //ImageDescription info = new ImageDescription(frame.Width, frame.Height, frame.Stride, frame.Width, frame.Height, TextureFormat.Alpha8, frame.DataSize);
        
        
        //File.WriteAllBytes("C:/Users/pbottoni/Documents/BachelorThesis/TestImages/" + _streamIndex.ToString() + "_" + frame.Timestamp * 100 + "0000.png", imag);
       

        if (_streamIndex == 1)
        {
            images_names.Add(_streamIndex.ToString() + "_" + frame.Timestamp * 100 + "0000.png");
        }
        //File.WriteAllBytes("C:/Users/pbottoni/Documents/BachelorThesis/TestImages/" + _streamIndex.ToString() + "_" + Time.time.ToString() + ".png", frame.ColorFrame);
        //print(images_names.Last());
        amound_of_images += 1;
        int width = texture.width;
        int height = texture.height;
        _height.Add(height);
        _width.Add(width);
        
    }

    public void formate()
    {
        print(images_names);
        Texture2D tex = null;
        byte[] data;
        byte[] imag;
        string filePath = "C:/Users/pbottoni/Documents/BachelorThesis/TestImages/";

        StreamWriter writer = new StreamWriter(fileIMG, true);
        StreamWriter writer_timestamp = new StreamWriter(fileTIME, true);
       
        writer.Write("#timestamp [ns],filename\n");

        for (int i=0;i< amound_of_images; i++)
        {
            print("Start convert");
            print(images_names[i]);
            print(_height[i]);
            print(_width[i]);
            string img = images_names[i];
            //imag = tex_[i].EncodeToPNG();
            
            //File.WriteAllBytes("C:/Users/pbottoni/Documents/BachelorThesis/TestImages/" +img , imag);


           
            //data = File.ReadAllBytes(filePath + img);
            //print(data.Length);
            
            //tex = new Texture2D(_width[i],_height[i]);
            //tex.LoadImage(data);
            UnityEngine.Color[] pixels = tex_[i].GetPixels();
            Array.Reverse(pixels);
            for (int row = 0; row < _height[i]; ++row)
                Array.Reverse(pixels, row * _width[i], _width[i]);
            if (_streamIndex == 1)
            {
                writer_timestamp.Write(img.Remove(img.Length - 4, 4).Remove(0, 2) + "\n");
            }
            writer.Write(img.Remove(img.Length - 4, 4).Remove(0, 2) + "," + img.Remove(0, 2) + "\n");
            for (int k = 0; k < _width[i]; k++)
            {
                for (int j = 0; j < _height[i]; j++)
                {
                    //print(pixels[k * _height[i] + j])
                    //System.Drawing.Color myColor = pixels[k * _height[i] + j];
                    //print(pixels[k * _height[i] + j]);
                    //pixels[k * _height[i] + j] = InvertColor(pixels[k * _height[i] + j]);
                    
                    float alpha = pixels[k * _height[i] + j][3];
                    pixels[k * _height[i] + j][0] = alpha;
                    pixels[k * _height[i] + j][1] = alpha;
                    pixels[k * _height[i] + j][2] = alpha;
                    pixels[k * _height[i] + j][3] = 1;
                    //print(pixels[k * _height[i] + j]);
                }
            }
            //print(pixels[5 * _height[50] + 80][0]);


            tex_[i].SetPixels(pixels);
            tex_[i].Apply();
            imag = tex_[i].EncodeToPNG();

            File.WriteAllBytes("C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images" + time_name + "/"+ lOrR+"/data/" + images_names[i].Remove(0, 2), imag);
        }
        
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

    public UnityEngine.Color InvertColor(UnityEngine.Color color) {
        var maxColor = color.maxColorComponent;
        var mycolor =new UnityEngine.Color(color[0],color[1], color[2], 1-color[3]);
     return mycolor;
    }
}
