using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Intel.RealSense;
using UnityEngine;
using System.IO;
using System.Linq;

public class IMU : MonoBehaviour
{
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

    public List<double> static_ = new List<double>();
    string filename = "";
    string filename2 = "";
    string time_name = "";
    TextWriter tw;
    TextWriter tw2;
    public double prev_time = 0;
    public RsFrameProvider Source;
    int amount_of_imu = 0;

    FrameQueue q;

    void Start()
    {   
        Source.OnStart += OnStartStreaming;
        Source.OnStop += OnStopStreaming;
        time_name = System.DateTime.Now.ToString("yyyyMMdd_hhmm");
        Directory.CreateDirectory("C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images" + time_name + "/IMU");
        filename = "C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images" + time_name + "/IMU/data.csv";
        filename2 = "C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images" + time_name + "/IMU/data_notITP.csv";
        tw = new System.IO.StreamWriter(filename, false);
        tw.WriteLine("#timestamp [ns],w_RS_S_x [rad s^-1],w_RS_S_y [rad s^-1],w_RS_S_z [rad s^-1],a_RS_S_x [m s^-2],a_RS_S_y [m s^-2],a_RS_S_z [m s^-2]");
        tw.Close();
        tw2 = new System.IO.StreamWriter(filename2, false);
        tw2.WriteLine("#timestamp [ns],w_RS_S_x [rad s^-1],w_RS_S_y [rad s^-1],w_RS_S_z [rad s^-1],a_RS_S_x [m s^-2],a_RS_S_y [m s^-2],a_RS_S_z [m s^-2]");
        tw2.Close();
        

    }

    private void OnStartStreaming(PipelineProfile profile)
    {
        q = new FrameQueue(1);
        Source.OnNewSample += OnNewSample;
    }


    private void OnStopStreaming()
    {
        Source.OnNewSample -= OnNewSample;

        if (q != null)
        {
            q.Dispose();
            q = null;
        }
    }


    private void OnNewSample(Frame f)
    {
        Vector3 angularVelocity;
        Vector3 linearAcceleration;
        if (f.IsComposite)
        {
            using (var fs = f.As<FrameSet>())
            {
                if (prev_time == f.Timestamp)
                {

                }
                else
                {
                    prev_time = f.Timestamp;
                    using (var gyro = fs.FirstOrDefault<Frame>(Intel.RealSense.Stream.Gyro, Format.MotionXyz32f).DisposeWith(fs))
                        angularVelocity = Marshal.PtrToStructure<Vector3>(gyro.Data);
                    using (var accel = fs.FirstOrDefault<Frame>(Intel.RealSense.Stream.Accel, Format.MotionXyz32f).DisposeWith(fs))
                        linearAcceleration = Marshal.PtrToStructure<Vector3>(accel.Data);

                    tw2 = new StreamWriter(filename2, true);

                    tw2.WriteLine(f.Timestamp * 100 + "," + angularVelocity[0] + "," + angularVelocity[1] + "," +
                        angularVelocity[2] + "," + linearAcceleration[0] + "," + linearAcceleration[1] + "," + linearAcceleration[2]);

                    tw2.Close();
                    amount_of_imu++;
                }
            }
        }
    }

    void Update()
    {
        
    }

    void OnApplicationQuit()
    {
        WrtieCSV();
    }

    public void WrtieCSV()
    {
      
        TextWriter tw = new System.IO.StreamWriter(filename, false);
        tw.WriteLine("#timestamp [ns],w_RS_S_x [rad s^-1],w_RS_S_y [rad s^-1],w_RS_S_z [rad s^-1],a_RS_S_x [m s^-2],a_RS_S_y [m s^-2],a_RS_S_z [m s^-2]");
        tw.Close();
        
        List<double> prev = new List<double>();
        List<double> next = new List<double>();
        var values = File.ReadLines(filename2).Skip(1).First().Split(',');
        
        prev = Array.ConvertAll(values, Double.Parse).ToList();
        
        tw = new StreamWriter(filename, true);
        
        tw.WriteLine(prev[0] + "0000," + prev[1] + "," + prev[2] + "," +
                prev[3] + "," + (prev[4]) + "," + (prev[5]) + "," + (prev[6]));

        for (int i = 2; i < amount_of_imu; i++)
        {
            values = File.ReadLines(filename2).Skip(i).First().Split(',');
            next = Array.ConvertAll(values, Double.Parse).ToList();
           
            int interpolate = 5;
            for (int j = 1; j < interpolate; j++)
            {
                tw.WriteLine(((int)((next[0] - prev[0]) / interpolate) * j + prev[0]) + "" + 10000 / interpolate * j + "," + ((next[1] - prev[1]) / interpolate * j + prev[1]) + ","
               + ((next[2] - prev[2]) / interpolate * j + prev[2]) + "," +
           ((next[3] - prev[3]) / interpolate * j + prev[3]) + "," + ((next[4] - prev[4]) / interpolate * j + prev[4]) +
          "," + ((next[5] - prev[5]) / interpolate * j + prev[5]) + "," + ((next[6] - prev[6]) / interpolate * j + prev[6]));

            }

            prev = next;

            tw.WriteLine(prev[0] + "0000," + prev[1] + "," + prev[2] + "," +
            prev[3] + "," + (prev[4]) + "," + (prev[5]) + "," + (prev[6]));
        }

        tw.Close();

    }
}
