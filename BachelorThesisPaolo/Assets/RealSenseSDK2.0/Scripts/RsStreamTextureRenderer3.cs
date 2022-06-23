using Intel.RealSense;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Unity.Collections;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Linq;
using System.Text;


public class RsStreamTextureRenderer3 : MonoBehaviour
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

    [Space]
    public TextureEvent textureBinding;

    FrameQueue q;
    Predicate<Frame> matcher;

    //added by me
    public ulong amound_of_images = 0;
    public ulong amound_of_images_r = 0;
    public ulong amound_of_images_l = 0;
 
    public string time_name = "";
    public string lOrR = "";
    
    public double prev_time_l = 0;
    public double prev_time_r = 0;

    public string fileIMG = "";
    public string fileIMU = "";
    public string fileTIME = "";
    public string fileTimeNames_l = "";
    public string fileTimeNames_r = "";
    
    int dummy=0;
    //to extract position of oculus
    public GameObject centerEye;
    public Vector3 headsetPos;
    public Quaternion headsetRot;
    public string filenameO = "";
    public string filenameTXTO = "";
    public TextWriter twO;
    public TextWriter tw2O;
    public List<double> static_O = new List<double>();
    public List<string> time_ = new List<string>();

    //to extract position of camera
    Vector3 cameraPos;
    Quaternion cameraRot;
    GameObject cameraT265;
    string filenameC = "";
    string filenameTXTC = "";
    TextWriter twC;
    TextWriter tw2C;
    public List<double> static_C = new List<double>();

    //to extract position of tracker
    public Vector3 pos;
    public Quaternion rot;
    GameObject tracker;
    string filenameT = "";
    string filenameTXTT = "";
    TextWriter twT;
    TextWriter tw2T;
    public List<double> static_T = new List<double>();

    void Start()
    {
        Source.OnStart += OnStartStreaming;
        Source.OnStop += OnStopStreaming;

        //added by me        
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
        
        fileIMG = "C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images" + time_name + "/" + lOrR + "/data.csv";
  
        fileTIME = "C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images" + time_name + "/timestamp.txt";
        fileTimeNames_l = "C:/Users/pbottoni/Documents/BachelorThesis/TestImages/images" + time_name + "/timeName_l.csv";
        fileTimeNames_r = "C:/Users/pbottoni/Documents/BachelorThesis/TestImages/images" + time_name + "/timeName_r.csv";
        
        
        centerEye = GameObject.Find("CenterEyeAnchor");
        filenameO = "C:/Users/pbottoni/Documents/BachelorThesis/TestCSV/test_oculus_" + System.DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".csv";
        filenameTXTO = "C:/Users/pbottoni/Documents/BachelorThesis/TestCSV/test_oculus_" + System.DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".txt";
        twO = new StreamWriter(filenameO, false);
        twO.WriteLine("Stream Type,x,y,z,rx,ry,rz,rw");
        twO.Close();

        cameraT265 = GameObject.Find("Pose");
        filenameC = "C:/Users/pbottoni/Documents/BachelorThesis/TestCSV/test_camera_" + System.DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".csv";
        filenameTXTC = "C:/Users/pbottoni/Documents/BachelorThesis/TestCSV/test_camera_" + System.DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".txt";
        twC = new StreamWriter(filenameC, false);
        twC.WriteLine("Stream Type,x,y,z,rx,ry,rz,rw");
        twC.Close();

        tracker = GameObject.Find("Tracker");
        filenameT = "C:/Users/pbottoni/Documents/BachelorThesis/TestCSV/test_tracker_" + System.DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".csv";
        filenameTXTT = "C:/Users/pbottoni/Documents/BachelorThesis/TestCSV/test_tracker_" + System.DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".txt";
        twT = new StreamWriter(filenameT, false);
        twT.WriteLine("Stream Type,x,y,z,rx,ry,rz,rw");
        twT.Close();
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

        //added by me
         formate();
    }

    protected void OnStopStreaming()
    {
        Source.OnNewSample -= OnNewSample;
        if (q != null)
        {
            q.Dispose();
            q = null;
        }
        
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

    public void OnNewSample(Frame frame)
    {
        Texture2D texture_try = new Texture2D(848, 800,TextureFormat.Alpha8,false,false);
        byte[] letssee;
        try
        {
            if (frame.IsComposite)
            {
                using (var fs = frame.As<FrameSet>())
                using (var f = fs.FirstOrDefault(matcher))
                {
                    Debug.Log("hello");
                    using (var image = fs.FirstOrDefault<Frame>(Intel.RealSense.Stream.Fisheye, Format.Y8).DisposeWith(fs)){
                        letssee = Marshal.PtrToStructure<byte[]>(image.Data);
                        //texture_try.LoadImage(image.Data);
                        print(letssee[0]);         
                        //texture_try.LoadRawTextureData(letssee);
                        //texture_try.Apply();
                        //texture_try=Texture2D.CreateExternalTexture(848, 800, TextureFormat.Alpha8,false,false,image.Data);
                        //File.WriteAllBytes("C:/Users/pbottoni/Documents/BachelorThesis/TestImages/letsHope.png_"+dummy, texture_try.EncodeToPNG());
                        dummy++;
                        print(dummy);

                    }
                    


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

    public void Update()
    {
        
        if (q != null)
        {
            /*VideoFrame frame;
            if (q.PollForFrame<VideoFrame>(out frame))
                using (frame)
                    ProcessFrame(frame);
        */}
    }

    public void ProcessFrame(VideoFrame frame)
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
       

        texture.LoadRawTextureData(frame.Data, frame.Stride * frame.Height);
        texture.Apply();

        //added by me
        uint width = (uint)texture.width;
        uint height = (uint) texture.height;
        UnityEngine.Experimental.Rendering.GraphicsFormat format_ = texture.graphicsFormat;
        System.Byte[] dataTex=texture.GetRawTextureData();

        StreamWriter writer_r = new StreamWriter(fileTimeNames_r, true);
        StreamWriter writer_l = new StreamWriter(fileTimeNames_l, true);
        
        if (lOrR == "right" && frame.Timestamp == prev_time_r)
        {
           
        }
        else if(lOrR == "left" && frame.Timestamp == prev_time_l)
        {
            
        }
        else
        {
            double time = frame.Timestamp;
            
            if (_streamIndex == 1)
            {

                writer_l.Write(_streamIndex.ToString() + "_" + time * 100 + "0000.png\n");
                
                amound_of_images_l += 1;

                
                time_.Add(DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString());

                //for pose oculus
                headsetPos = centerEye.transform.position;
                headsetRot = centerEye.transform.rotation;

                for (int i = 0; i < 3; i++)
                {
                    static_O.Add(headsetPos[i]);
                }
                for (int i = 0; i < 4; i++)
                {
                    static_O.Add(headsetRot[i]);
                }
                time_.Add(Time.time.ToString());

                twO = new StreamWriter(filenameO, true);
                twO.WriteLine("Pose," + static_O[0] + "," + static_O[1] + "," + static_O[2] + "," +
                            static_O[3] + "," + static_O[4] + "," + static_O[5] + "," + static_O[6]);
                twO.Close();

                tw2O = new StreamWriter(filenameTXTO, true);
                tw2O.WriteLine(time_[0] + " " + static_O[0] + " " + static_O[1] + " " + static_O[2] + " " +
                            static_O[3] + " " + static_O[4] + " " + static_O[5] + " " + static_O[6]);
                tw2O.Close();

                static_O.Clear();
                

                //for pose camera
                cameraPos = cameraT265.transform.position;
                cameraRot = cameraT265.transform.rotation;

                for (int i = 0; i < 3; i++)
                {
                    static_C.Add(cameraPos[i]);
                }
                for (int i = 0; i < 4; i++)
                {
                    static_C.Add(cameraRot[i]);
                }
                

                twC = new StreamWriter(filenameC, true);
                twC.WriteLine("Pose," + static_C[0] + "," + static_C[1] + "," + static_C[2] + "," +
                            static_C[3] + "," + static_C[4] + "," + static_C[5] + "," + static_C[6]);
                twC.Close();

                tw2C = new StreamWriter(filenameTXTC, true);
                tw2C.WriteLine(time_[0] + " " + static_C[0] + " " + static_C[1] + " " + static_C[2] + " " +
                            static_C[3] + " " + static_C[4] + " " + static_C[5] + " " + static_C[6]);
                tw2C.Close();

                static_C.Clear();

                //for pose tracker
                pos = tracker.transform.position;
                rot = tracker.transform.rotation;
                

                for (int i = 0; i < 3; i++)
                {
                    static_T.Add(pos[i]);
                }
                for (int i = 0; i < 4; i++)
                {
                    static_T.Add(rot[i]);
                }
                
                twT = new StreamWriter(filenameT, true);
                twT.WriteLine("Pose," + static_T[0] + "," + static_T[1] + "," + static_T[2] + "," +
                            static_T[3] + "," + static_T[4] + "," + static_T[5] + "," + static_T[6]);
                twT.Close();

                tw2T = new StreamWriter(filenameTXTT, true);
                tw2T.WriteLine(time_[0] + " " + static_T[0] + " " + static_T[1] + " " + static_T[2] + " " +
                            static_T[3] + " " + static_T[4] + " " + static_T[5] + " " + static_T[6]);
                tw2T.Close();

                static_T.Clear();
                time_.Clear();
            }
            else
            {
                writer_r.Write(_streamIndex.ToString() + "_" + time * 100 + "0000.png\n");
                amound_of_images_r += 1;
            }

           

            Thread t = new Thread(() => png(width, height, time, dataTex, format_));
            t.Start();
            
            print(amound_of_images_l);
           
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


    public void png(uint width, uint height, double time, System.Byte[] imageBytes, UnityEngine.Experimental.Rendering.GraphicsFormat format_)
    {
        byte[] imag = ImageConversion.EncodeArrayToPNG(imageBytes, format_, width, height);
        File.WriteAllBytes("C:/Users/pbottoni/Documents/BachelorThesis/TestImages/images" + time_name +"/" + _streamIndex.ToString() + "_" + time * 100 + "0000.png", imag);
    }

    public void formate()
    {
        Texture2D tex = new Texture2D(2, 2);
        byte[] data;
        byte[] imag;
        string filePath = "C:/Users/pbottoni/Documents/BachelorThesis/TestImages/images" + time_name+"/";
        ulong lineCount_r = 0;
        using (var reader = File.OpenText(filePath + "timeName_r.csv"))
        {
            while (reader.ReadLine() != null)
            {
                lineCount_r++;
            }
            reader.Close();
        }
        
        amound_of_images = lineCount_r;
        print(amound_of_images);
        
        StreamWriter writer = File.AppendText(fileIMG);
        StreamWriter writer_timestamp;
       
        writer.Write("#timestamp [ns],filename\n");
        writer.Close();
       
        for (int i = 0; i < (int)amound_of_images-2; i++)
        {
            
            writer = File.AppendText(fileIMG);
            writer_timestamp = File.AppendText(fileTIME);
            string img = "";
           
            string imag_save_name = readLineAt(i, filePath + "timeName_l.csv");

            if (_streamIndex == 2)
            {
                img = readLineAt(i, filePath + "timeName_r.csv");
               
            }
            else
            {
                img = imag_save_name;
            }

            
           
            data = File.ReadAllBytes(filePath + img);
           
            FileUtil.DeleteFileOrDirectory(filePath + img);

            
           
            tex.LoadImage(data);

            int height = tex.height;
            int width = tex.width;
            
            UnityEngine.Color[] pixels = tex.GetPixels();
            Array.Reverse(pixels);
            for (int row = 0; row < height; ++row)
                Array.Reverse(pixels,row * width, width);
            if (_streamIndex == 1)
            {
               writer_timestamp.Write(img.Remove(img.Length - 4, 4).Remove(0, 2) + "\n");
            }
            writer.Write(imag_save_name.Remove(imag_save_name.Length - 4, 4).Remove(0, 2) + "," + imag_save_name.Remove(0, 2) + "\n");
            
            
            for (int p = 0; p < pixels.Length; p++)
            {
                float alpha = pixels[p][3];
                pixels[p][0] = alpha;
                pixels[p][1] = alpha;
                pixels[p][2] = alpha;
                pixels[p][3] = 1;
            }
            
            tex.SetPixels(pixels);
            
            tex.Apply();
            
            imag = tex.EncodeToPNG();

            File.WriteAllBytes("C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images" + time_name + "/" + lOrR + "/data/" + imag_save_name.Remove(0, 2), imag);
            
           
            writer.Flush();
            writer_timestamp.Flush();
            writer.Close();
            writer_timestamp.Close();
            writer.Dispose();
            writer_timestamp.Dispose();

           
        }
     }

    public string readLineAt(int i, string path)
    {
        
        int localI = 0;
        using (StreamReader sr = new StreamReader(path))
        {
            string currentLine;

            // currentLine will be null when the StreamReader reaches the end of file
            while ((currentLine = sr.ReadLine()) != null)
            {
                if (localI == i)
                {
                    return currentLine;
                }

                localI++;
         
            }
        }
        return " ";
    }
    
}

