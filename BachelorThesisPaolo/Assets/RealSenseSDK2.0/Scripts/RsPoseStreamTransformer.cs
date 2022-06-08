using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Intel.RealSense;
using UnityEngine;
using System.IO;

public class RsPoseStreamTransformer : MonoBehaviour
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
    string time_name ="";

    public RsFrameProvider Source;

    FrameQueue q;

    void Start()
    {
        Source.OnStart += OnStartStreaming;
        Source.OnStop += OnStopStreaming;
        time_name = System.DateTime.Now.ToString("yyyyMMdd_hhmm");
        Directory.CreateDirectory("C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images" + time_name + "/IMU");
        filename = "C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images" + time_name + "/IMU/data.csv";
        filename2 = "C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images" + time_name + "/IMU/data_notITP.csv";
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
        if (f.IsComposite)
        {
            using (var fs = f.As<FrameSet>())
            using (var poseFrame = fs.FirstOrDefault(Intel.RealSense.Stream.Pose, Format.SixDOF))
                if (poseFrame != null)
                    q.Enqueue(poseFrame);
        }
        else
        {
            using (var p = f.Profile)
                if (p.Stream == Intel.RealSense.Stream.Pose && p.Format == Format.SixDOF)
                    q.Enqueue(f);
        }
    }

    void Update()
    {
        if (q != null)
        {
            PoseFrame frame;
            if (q.PollForFrame<PoseFrame>(out frame))
                using (frame)
                {
                    frame.CopyTo(pose);


                    // Convert T265 coordinate system to Unity's
                    // see https://realsense.intel.com/how-to-getting-imu-data-from-d435i-and-t265/
                    static_.Add(frame.Timestamp*100);
                  
                    
                    var t = pose.translation;
                    t.Set(t.x, t.y, -t.z);

                    var e = pose.rotation.eulerAngles;
                    var r = Quaternion.Euler(-e.x, -e.y, e.z);

                    Quaternion rot = pose.rotation;
                    Quaternion rot_inv = Quaternion.Inverse(rot);
                    Quaternion rot_force = new Quaternion(0.0f, 9.81f, 0.0f, 0.0f);
                    Quaternion new_force = (rot * rot_force * rot_inv);
                    print((new_force[0]-pose.acceleration[0])+", " + (new_force[1] - pose.acceleration[1]) + ", "+ (new_force[2] - pose.acceleration[2]));
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
                    for (int i = 0; i < 3; i++)
                    {
                        static_.Add(pose.angular_velocity[i]);
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        static_.Add(new_force[i] - pose.acceleration[i]);
                    }
                }

        }
    }

    void OnApplicationQuit()
    {
        print("done");
        WrtieCSV();
    }

    public void WrtieCSV()
    {
        if (static_.Count > 0)
        {
            TextWriter tw = new System.IO.StreamWriter(filename, false);
            tw.WriteLine("#timestamp [ns],w_RS_S_x [rad s^-1],w_RS_S_y [rad s^-1],w_RS_S_z [rad s^-1],a_RS_S_x [m s^-2],a_RS_S_y [m s^-2],a_RS_S_z [m s^-2]");
            tw.Close();
            TextWriter tw2 = new System.IO.StreamWriter(filename2, false);
            tw2.WriteLine("#timestamp [ns],w_RS_S_x [rad s^-1],w_RS_S_y [rad s^-1],w_RS_S_z [rad s^-1],a_RS_S_x [m s^-2],a_RS_S_y [m s^-2],a_RS_S_z [m s^-2]");
            tw2.Close();


            tw = new StreamWriter(filename, true);
            tw2 = new StreamWriter(filename2, true);

            for (int i = 0; i < static_.Count / 7; i++)
            {
                if (i != 0)
                {
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
                        tw.WriteLine(((int)((static_[i * 7] - static_[(i - 1) * 7]) / interpolate) * j + static_[(i - 1) * 7]) + "" + 10000 / interpolate * j + "," + ((static_[i * 7 + 1] - static_[(i - 1) * 7 + 1]) / interpolate * j + static_[(i - 1) * 7 + 1]) + ","
                       + ((static_[i * 7 + 2] - static_[(i - 1) * 7 + 2]) / interpolate * j + static_[(i - 1) * 7 + 2]) + "," +
                   ((static_[i * 7 + 3] - static_[(i - 1) * 7 + 3]) / interpolate * j + static_[(i - 1) * 7 + 3]) + "," + ((static_[i * 7 + 4] - static_[(i - 1) * 7 + 4]) / interpolate * j + static_[(i - 1) * 7 + 4]) +
                  "," + ((static_[i * 7 + 5] - static_[(i - 1) * 7 + 5]) / interpolate * j + static_[(i - 1) * 7 + 5] ) + "," + ((static_[i * 7 + 6] - static_[(i - 1) * 7 + 6]) / interpolate * j + static_[(i - 1) * 7 + 6]));

                    }

                }

                tw.WriteLine(static_[i * 7] + "0000," + static_[i * 7 + 1] + "," + static_[i * 7 + 2] + "," +
                    static_[i * 7 + 3] + "," + (static_[i * 7 + 4]) + "," + (static_[i * 7 + 5]) + "," + (static_[i * 7 + 6] ));
                tw2.WriteLine(static_[i * 7] + "0000," + static_[i * 7 + 1] + "," + static_[i * 7 + 2] + "," +
                    static_[i * 7 + 3] + "," + (static_[i * 7 + 4]) + "," + (static_[i * 7 + 5]) + "," + (static_[i * 7 + 6]));

            }
            tw.Close();
            tw2.Close();
        }

    }
}
