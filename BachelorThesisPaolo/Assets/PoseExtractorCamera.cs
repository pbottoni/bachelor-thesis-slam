using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PoseExtractorCamera : MonoBehaviour
{
    // Start is called before the first frame update
    Vector3 cameraPos;
    Quaternion cameraRot;
    GameObject cameraT265;
    string filename = "";
    public List<double> static_ = new List<double>();


    void Start()
    {
        print(System.DateTime.Now.ToString("yyyyMMdd_hhmmss"));
        //print("hello there");
        cameraT265 = GameObject.Find("Pose"); 
        filename = "C:/Users/pbottoni/Documents/BachelorThesis/TestCSV/test_camera_" + System.DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".csv";
    }

    // Update is called once per frame
    void Update()
    {
        cameraPos = cameraT265.transform.position;
        cameraRot = cameraT265.transform.rotation;

        for (int i = 0; i < 3; i++)
        {
            static_.Add(cameraPos[i]);
        }
        for (int i = 0; i < 4; i++)
        {
            static_.Add(cameraRot[i]);
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
            tw.WriteLine("Stream Type,x,y,z,rx,ry,rz,rw");
            tw.Close();


            tw = new StreamWriter(filename, true);
            for (int i = 0; i < static_.Count / 7; i++)
            {
                tw.WriteLine("Pose," + static_[i * 7] + "," + static_[i * 7 + 1] + "," + static_[i * 7 + 2] + "," +
                    static_[i * 7 + 3] + "," + static_[i * 7 + 4] + "," + static_[i * 7 + 5] + "," + static_[i * 7 + 6]);

            }
            tw.Close();
        }

    }
}

