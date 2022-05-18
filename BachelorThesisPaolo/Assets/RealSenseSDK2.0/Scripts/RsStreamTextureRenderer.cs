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

    public RsFrameProvider Source;

    [System.Serializable]
    public class TextureEvent : UnityEvent<Texture> { }

    public Intel.RealSense.Stream _stream;
    public Format _format;
    public int _streamIndex;

    public FilterMode filterMode = FilterMode.Point;

    protected Texture2D texture;

    public int amound_of_images=0;
    public List<string> images_names = new List<string>();
    public List<int> _width = new List<int>();
    public List<int> _height = new List<int>();

    public string time_name = "";


    public string filename = "";

    [Space]
    public TextureEvent textureBinding;

    FrameQueue q;
    Predicate<Frame> matcher;

    void Start()
    {
        Source.OnStart += OnStartStreaming;
        Source.OnStop += OnStopStreaming;
        time_name= System.DateTime.Now.ToString("yyyyMMdd_hhmmss");
        Directory.CreateDirectory("C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images_" + time_name);
        filename="C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images_" + time_name + "/" + time_name + ".txt";
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
            using (var p = frame.Profile) {
                index = p.Index;
                bool linear = (QualitySettings.activeColorSpace != ColorSpace.Linear) || (p.Stream != Intel.RealSense.Stream.Color && p.Stream != Intel.RealSense.Stream.Infrared);
                texture = new Texture2D(frame.Width, frame.Height, Convert(p.Format), false, linear)
                {
                    //wrapMode = TextureWrapMode.Clamp,
                   // filterMode = filterMode
                };
            }

            textureBinding.Invoke(texture);
        }
       // print("hello world from textureRenderer");
        texture.LoadRawTextureData(frame.Data, frame.Stride * frame.Height);
        int width = texture.width;
        int height = texture.height;
       
        /*print(texture.dimension);
        UnityEngine.Color[] pixels = texture.GetPixels();
        Array.Reverse(pixels);
        for (int row = 0; row < height; ++row)
            Array.Reverse(pixels, row * width, width);
        texture.SetPixels(pixels);*/
        texture.Apply();
        byte[] imag = texture.EncodeToPNG();

        
        File.WriteAllBytes("C:/Users/pbottoni/Documents/BachelorThesis/TestImages/" + _streamIndex.ToString() + "_" + Time.time.ToString() + ".png", imag);
        images_names.Add(_streamIndex.ToString() + "_" + Time.time.ToString() + ".png");
        //File.WriteAllBytes("C:/Users/pbottoni/Documents/BachelorThesis/TestImages/" + _streamIndex.ToString() + "_" + Time.time.ToString() + ".png", frame.ColorFrame);
        print(images_names.Last());
        amound_of_images += 1;
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
        File.WriteAllLines(filename, images_names, Encoding.UTF8);
        for (int i=0;i< amound_of_images; i++)
        {
            print("Start convert");
            print(images_names[i]);
            print(_height[i]);
            print(_width[i]);

            data = File.ReadAllBytes(filePath + images_names[i]);
            tex = new Texture2D(_width[i],_height[i]);
            tex.LoadImage(data);
            UnityEngine.Color[] pixels = tex.GetPixels();
            Array.Reverse(pixels);
            for (int row = 0; row < _height[i]; ++row)
                Array.Reverse(pixels, row * _width[i], _width[i]);
            
          
      
            for (int k = 0; k < _width[i]; k++)
            {
                for (int j = 0; j < _height[i]; j++)
                {
                    //print(pixels[k * _height[i] + j])
                    //System.Drawing.Color myColor = pixels[k * _height[i] + j];
                    //print(pixels[k * _height[i] + j]);
                    //pixels[k * _height[i] + j] = InvertColor(pixels[k * _height[i] + j]);
                    pixels[k * _height[i] + j][3] = 1 - pixels[k * _height[i] + j][3];
                    //print(pixels[k * _height[i] + j]);
                }
            }
           
            tex.SetPixels(pixels);
            tex.Apply();
            imag = tex.EncodeToPNG();

            File.WriteAllBytes("C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images_" + time_name + "/" + images_names[i], imag);
        }
        
    }

    public UnityEngine.Color InvertColor(UnityEngine.Color color) {
        var maxColor = color.maxColorComponent;
        var mycolor =new UnityEngine.Color(color[0],color[1], color[2], 1-color[3]);
     return mycolor;
    }
}
