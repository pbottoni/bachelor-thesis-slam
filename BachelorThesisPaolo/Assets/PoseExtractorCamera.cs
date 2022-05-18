using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PoseExtractorCamera : MonoBehaviour
{
    // Start is called before the first frame update
    Vector3 cameraPos;
    Vector3 cameraRot;
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
        cameraRot = cameraT265.transform.eulerAngles;

        for (int i = 0; i < 3; i++)
        {
            static_.Add(cameraPos[i]);
        }
        for (int i = 0; i < 3; i++)
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

