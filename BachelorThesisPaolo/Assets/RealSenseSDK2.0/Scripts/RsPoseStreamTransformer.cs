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
    string time_name="";

    public RsFrameProvider Source;

    FrameQueue q;

    void Start()
    {
        Source.OnStart += OnStartStreaming;
        Source.OnStop += OnStopStreaming;
        time_name = System.DateTime.Now.ToString("yyyyMMdd_hhmm");
        Directory.CreateDirectory("C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images" + time_name + "/IMU");
        filename = "C:/Users/pbottoni/Documents/BachelorThesis/TestImages_nice/images" + time_name + "/IMU/data.txt";
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
                    for (int i = 0; i < 3; i++)
                    {
                        static_.Add(pose.angular_acceleration[i]);
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        static_.Add(pose.acceleration[i]);
                    }
                    

                    //print(pose.angular_acceleration);
                    //print(pose.acceleration);
                    transform.localRotation = r;
                    transform.localPosition = t;
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


            tw = new StreamWriter(filename, true);
            for (int i = 0; i < static_.Count / 7; i++)
            {
                if (i != 0)
                {
                    tw.WriteLine(((int)((static_[i * 7] - static_[(i - 1) * 7]) / 4) + static_[(i - 1) * 7]) + "2500," + ((static_[i * 7 + 1] - static_[(i - 1) * 7 + 1]) / 4 + static_[(i - 1) * 7 + 1]) + ","
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
                }

                tw.WriteLine(static_[i * 7] + "0000," + static_[i * 7 + 1] + "," + static_[i * 7 + 2] + "," +
                    static_[i * 7 + 3] + "," + static_[i * 7 + 4] + "," + static_[i * 7 + 5]+"," + static_[i*7+6]);
               
            }
            tw.Close();
        }

    }
}
