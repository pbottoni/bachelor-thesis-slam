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
                //using (var poseFrame = fs.FirstOrDefault(Intel.RealSense.Stream.Pose, Format.SixDOF))
                //    if (poseFrame != null)
                //        q.Enqueue(poseFrame);
                //print("did this work 0?");
                //var fs = frame.As<FrameSet>();
                //print("did this work?");
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

                    //print(angularVelocity[0] + "," + angularVelocity[1] + "," + angularVelocity[2]);
                    //print(linearAcceleration[0] + "," + linearAcceleration[1] + "," + linearAcceleration[2]);
                    tw2 = new StreamWriter(filename2, true);

                    tw2.WriteLine(f.Timestamp * 100 + "," + angularVelocity[0] + "," + angularVelocity[1] + "," +
                        angularVelocity[2] + "," + linearAcceleration[0] + "," + linearAcceleration[1] + "," + linearAcceleration[2]);

                    tw2.Close();
                    amount_of_imu++;
                }
            }
        }
       /* else
        {
            using (var p = f.Profile)
                if (p.Stream == Intel.RealSense.Stream.Pose && p.Format == Format.SixDOF)
                    q.Enqueue(f);
        }*/
    }

    void Update()
    {
        
        /*if (q != null)
        {
            PoseFrame frame;
            if (q.PollForFrame<PoseFrame>(out frame))
                using (frame)
                {
                    frame.CopyTo(pose);

                    // Convert T265 coordinate system to Unity's
                    // see https://realsense.intel.com/how-to-getting-imu-data-from-d435i-and-t265/



                    /* static_.Add(frame.Timestamp * 100);
                    prev_time = frame.Timestamp;
                    
                    var t = pose.translation;
                    t.Set(t.x, t.y, -t.z);

                    var e = pose.rotation.eulerAngles;
                    var r = Quaternion.Euler(-e.x, -e.y, e.z);

                    /* Quaternion rot = pose.rotation;
                    Quaternion rot_inv = Quaternion.Inverse(rot);
                    Quaternion rot_force = new Quaternion(0.0f, 9.81f, 0.0f, 0.0f);
                    Quaternion new_force = (rot * rot_force * rot_inv);
                    
                    //print(pose.acceleration[0] + ", " + pose.acceleration[1] + ", " + pose.acceleration[2]);
                    //print(new_force[0] + ", " + new_force[1] + ", " + new_force[2]);
                    //print((pose.acceleration[0] + new_force[0]) +", " + (pose.acceleration[1] + new_force[1]) + ", "+ (pose.acceleration[2] + new_force[2]));
                    //print(pose.translation);
                    //print(pose.velocity);
                    //print(pose.acceleration);
                    //print(pose.rotation);
                    //print(pose.angular_velocity);
                    //print(pose.angular_velocity );
                    //print(pose.tracker_confidence);
                    //print(pose.mapper_confidence);
                    //print(pose.angular_acceleration);
                    //print(pose.acceleration);
                    //print("");

                    transform.localRotation = r;
                    transform.localPosition = t;
                    /*for (int i = 0; i < 3; i++)
                    {
                        static_.Add(pose.angular_velocity[i]);
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        static_.Add(pose.acceleration[i] + new_force[i]);
                    }
                    

                    /* tw2 = new StreamWriter(filename2, true);

                    tw2.WriteLine(static_[0] + "," + static_[1] + "," + static_[2] + "," +
                        static_[3] + "," + static_[4] + "," + static_[5] + "," + static_[6]);

                    tw2.Close();
                    
                    // static_.Clear();
                    //
                    //print(amount_of_imu);

                }

        }*/
    }

    void OnApplicationQuit()
    {
        //print("done");
        WrtieCSV();
    }

    public void WrtieCSV()
    {

        TextWriter tw = new System.IO.StreamWriter(filename, false);
        tw.WriteLine("#timestamp [ns],w_RS_S_x [rad s^-1],w_RS_S_y [rad s^-1],w_RS_S_z [rad s^-1],a_RS_S_x [m s^-2],a_RS_S_y [m s^-2],a_RS_S_z [m s^-2]");
        tw.Close();
        // TextWriter tw2 = new System.IO.StreamWriter(filename2, false);
        // tw2.WriteLine("#timestamp [ns],w_RS_S_x [rad s^-1],w_RS_S_y [rad s^-1],w_RS_S_z [rad s^-1],a_RS_S_x [m s^-2],a_RS_S_y [m s^-2],a_RS_S_z [m s^-2]");
        //tw2.Close();
        List<double> prev = new List<double>();
        List<double> next = new List<double>();
        var values = File.ReadLines(filename2).Skip(1).First().Split(',');
        //print(values[0]);
        prev = Array.ConvertAll(values, Double.Parse).ToList();
        //print(prev[0]);
        tw = new StreamWriter(filename, true);
        //tw2 = new StreamWriter(filename2, true);
        tw.WriteLine(prev[0] + "0000," + prev[1] + "," + prev[2] + "," +
                prev[3] + "," + (prev[4]) + "," + (prev[5]) + "," + (prev[6]));

        for (int i = 2; i < amount_of_imu; i++)
        {
            values = File.ReadLines(filename2).Skip(i).First().Split(',');
            next = Array.ConvertAll(values, Double.Parse).ToList();
            /* tw.WriteLine(((int)((static_[i * 7] - static_[(i - 1) * 7]) / 4) + static_[(i - 1) * 7]) + "2500," + ((static_[i * 7 + 1] - static_[(i - 1) * 7 + 1]) / 4 + static_[(i - 1) * 7 + 1]) + ","
                 + ((static_[i * 7 + 2] - static_[(i - 1) * 7 + 2]) / 4 + static_[(i - 1) * 7 + 2]) + "," +
             ((static_[i * 7 + 3] - static_[(i - 1) * 7 + 3]) / 4 + static_[(i - 1) * 7 + 3]) + "," + ((static_[i * 7 + 4] - static_[(i - 1) * 7 + 4]) / 4 + static_[(i - 1) * 7 + 4]) +
             "," + ((static_[i * 7 + 5] + static_[(i - 1) * 7 + 5]) / 4 + static_[(i - 1) * 7 + 5]) + "," + ((static_[i * 7 + 6] - static_[(i - 1) * 7 + 6]) / 4 + static_[(i - 1) * 7 + 6]));

             tw.WriteLine(((int)((static_[i * 7] - static_[(i - 1) * 7]) / 2) + static_[(i - 1) * 7]) + "5000," + ((static_[i * 7 + 1] - static_[(i - 1) * 7 + 1]) / 2 + static_[(i - 1) * 7 + 1]) + ","
                 + ((static_[i * 7 + 2] - static_[(i - 1) * 7 + 2]) / 2 + static_[(i - 1) * 7 + 2]) + "," +
             ((static_[i * 7 + 3] - static_[(i - 1) * 7 + 3]) / 2 + static_[(i - 1) * 7 + 3]) + "," + ((static_[i * 7 + 4] - static_[(i - 1) * 7 + 4]) / 2 + static_[(i - 1) * 7 + 4]) +
             "," + ((static_[i * 7 + 5] + static_[(i - 1) * 7 + 5]) / 2 + static_[(i - 1) * 7 + 5]) + "," + ((static_[i * 7 + 6] - static_[(i - 1) * 7 + 6]) / 2 + static_[(i - 1) * 7 + 6]));

             tw.WriteLine(((int)((static_[i * 7] - static_[(i - 1) * 7]) / 4)*3 + static_[(i - 1) * 7]) + "7500," + ((static_[i * 7 + 1] - static_[(i - 1) * 7 + 1]) / 4 *3 + static_[(i - 1) * 7 + 1]) + ","
                  + ((static_[i * 7 + 2] - static_[(i - 1) * 7 + 2]) / 4 * 3 + static_[(i - 1) * 7 + 2]) + "," +
              ((static_[i * 7 + 3] - static_[(i - 1) * 7 + 3]) / 4 * 3 + static_[(i - 1) * 7 + 3]) + "," + ((static_[i * 7 + 4] - static_[(i - 1) * 7 + 4]) / 4 * 3+ static_[(i - 1) * 7 + 4]) +
              "," + ((static_[i * 7 + 5] + static_[(i - 1) * 7 + 5]) / 4 * 3 + static_[(i - 1) * 7 + 5]) + "," + ((static_[i * 7 + 6] - static_[(i - 1) * 7 + 6]) / 4 * 3 + static_[(i - 1) * 7 + 6]));
         */


            /*  int interpolate = 5;
             for(int j = 1; j < interpolate; j++)
              {
                  tw.WriteLine(((int)((static_[i * 7] - static_[(i - 1) * 7]) / interpolate) * j + static_[(i - 1) * 7]) + ""+10000/interpolate *j +"," + ((static_[i * 7 + 3] - static_[(i - 1) * 7 + 3]) / interpolate * j + static_[(i - 1) * 7 + 3]) + ","
                 + ((static_[i * 7 + 1] - static_[(i - 1) * 7 + 1]) / interpolate * j + static_[(i - 1) * 7 + 1]) + "," +
             ((static_[i * 7 + 2] - static_[(i - 1) * 7 + 2]) / interpolate * j + static_[(i - 1) * 7 + 2]) + "," + ((static_[i * 7 + 6] - static_[(i - 1) * 7 + 6]) / interpolate * j + static_[(i - 1) * 7 + 6] -0.4) +
            "," + ((static_[i * 7 + 4] - static_[(i - 1) * 7 + 4]) / interpolate * j + static_[(i - 1) * 7 + 4] -0.1) + "," + ((static_[i * 7 + 5] - static_[(i - 1) * 7 + 5]) / interpolate * j + static_[(i - 1) * 7 + 5]+9.80665));

              }

          }

          tw.WriteLine(static_[i * 7] + "0000," + static_[i * 7 + 3] + "," + static_[i * 7 + 1] + "," +
              static_[i * 7 + 2] + "," + (static_[i * 7 + 6] - 0.4) + "," + (static_[i * 7 + 4] - 0.1)  + "," + (static_[i * 7 + 5] + 9.80665));
          tw2.WriteLine(static_[i * 7] + "0000," + static_[i * 7 + 3] + "," + static_[i * 7 + 1] + "," +
             static_[i * 7 + 2] + "," + static_[i * 7 + 6] + "," + static_[i * 7 + 4] + "," + (static_[i * 7 + 5] + 9.80665));
            */

            /* old working...
             int interpolate = 5;
             for (int j = 1; j < interpolate; j++)
             {
                 tw.WriteLine(((int)((static_[i * 7] - static_[(i - 1) * 7]) / interpolate) * j + static_[(i - 1) * 7]) + "" + 10000 / interpolate * j + "," + ((static_[i * 7 + 1] - static_[(i - 1) * 7 + 1]) / interpolate * j + static_[(i - 1) * 7 + 1]) + ","
                + ((static_[i * 7 + 2] - static_[(i - 1) * 7 + 2]) / interpolate * j + static_[(i - 1) * 7 + 2]) + "," +
            ((static_[i * 7 + 3] - static_[(i - 1) * 7 + 3]) / interpolate * j + static_[(i - 1) * 7 + 3]) + "," + ((static_[i * 7 + 4] - static_[(i - 1) * 7 + 4]) / interpolate * j + static_[(i - 1) * 7 + 4] - 0.1) +
           "," + ((static_[i * 7 + 5] - static_[(i - 1) * 7 + 5]) / interpolate * j + static_[(i - 1) * 7 + 5] + 9.7) + "," + ((static_[i * 7 + 6] - static_[(i - 1) * 7 + 6]) / interpolate * j + static_[(i - 1) * 7 + 6] -0.4));

             }

         }

         tw.WriteLine(static_[i * 7] + "0000," + static_[i * 7 + 1] + "," + static_[i * 7 + 2] + "," +
             static_[i * 7 + 3] + "," + (static_[i * 7 + 4] - 0.1) + "," + (static_[i * 7 + 5] + 9.7) + "," + (static_[i * 7 + 6] - 0.4));
         tw2.WriteLine(static_[i * 7] + "0000," + static_[i * 7 + 1] + "," + static_[i * 7 + 2] + "," +
             static_[i * 7 + 3] + "," + (static_[i * 7 + 4] - 0.1) + "," + (static_[i * 7 + 5] + 9.7) + "," + (static_[i * 7 + 6] - 0.4));

     */
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
            //tw2.WriteLine(static_[i * 7] + "0000," + static_[i * 7 + 1] + "," + static_[i * 7 + 2] + "," +
            //  static_[i * 7 + 3] + "," + (static_[i * 7 + 4]) + "," + (static_[i * 7 + 5]) + "," + (static_[i * 7 + 6]));

        }
        tw.Close();
        //tw2.Close();


    }
}
