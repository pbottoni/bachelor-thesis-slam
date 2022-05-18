using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


public class PoseExtractorTracker : MonoBehaviour
{
    // Start is called before the first frame update
    public Vector3 pos;
    public Vector3 rot;
    GameObject tracker;
    string filename = "";
    public List<double> static_ = new List<double>();

    void Start()
    {
        tracker = GameObject.Find("Tracker");
        filename = "C:/Users/pbottoni/Documents/BachelorThesis/TestCSV/test_tracker_" + System.DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".csv";
    }

    // Update is called once per frame
    void Update()
    {
        // pos = VivePose.GetPoseEx(TrackerRole.Tracker1).pos;

        //rot = VivePose.GetPoseEx(TrackerRole.Tracker1).rot;
        
        pos = tracker.transform.position;
        rot = tracker.transform.eulerAngles;
        //print(pos);
        //print(rot);

        for (int i = 0; i < 3; i++)
        {
            static_.Add(pos[i]);
        }
        for (int i = 0; i < 3; i++)
        {
            static_.Add(rot[i]);
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
            TextWriter tw = new StreamWriter(filename, false);
            tw.WriteLine("Stream Type, x,y,z,rx,ry,rz");
            tw.Close();


            tw = new StreamWriter(filename, true);
            for (int i = 0; i < static_.Count / 6; i++)
            {
                tw.WriteLine("Pose," + static_[i * 6] + "," + static_[i * 6 + 1] + "," + static_[i * 6 + 2] + "," +
                    static_[i * 6 + 3] + "," + static_[i * 6 + 4] + "," + static_[i * 6 + 5] );

            }
            tw.Close();
        }

    }
}
