using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HTC.UnityPlugin.Utility;
using HTC.UnityPlugin.Vive;


public class PoseExtractorTracker : MonoBehaviour
{
    // Start is called before the first frame update
    public Vector3 pos;
    public Quaternion rot;
    GameObject tracker;
    string filename = "";

    void Start()
    {
        tracker = GameObject.Find("Tracker1");
        filename = "C:/Users/pbottoni/Documents/BachelorThesis/TestCSV/test_tracker_" + System.DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".csv";
    }

    // Update is called once per frame
    void Update()
    {
        // pos = VivePose.GetPoseEx(TrackerRole.Tracker1).pos;

        //rot = VivePose.GetPoseEx(TrackerRole.Tracker1).rot;
        //print(pos);
        //print(rot);
        pos = tracker.transform.position;
        rot = tracker.transform.rotation;
        print(pos);
    }
}
