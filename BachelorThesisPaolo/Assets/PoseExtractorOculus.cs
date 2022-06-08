using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PoseExtractorOculus : MonoBehaviour
{
    GameObject centerEye;
    Vector3 headsetPos;
    Quaternion headsetRot;
    string filename = "";
    string filenameTXT = "";


    public List<double> static_= new List<double>();
    public List<string> time_ = new List<string>();
    // Start is called before the first frame update
    void Start()
    {
        print(System.DateTime.Now.ToString("yyyyMMdd_hhmmss"));
        centerEye = GameObject.Find("CenterEyeAnchor");
        print("hello there");
        filename = "C:/Users/pbottoni/Documents/BachelorThesis/TestCSV/test_oculus_" + System.DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".csv";
        filenameTXT = "C:/Users/pbottoni/Documents/BachelorThesis/TestCSV/test_oculus_" + System.DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".txt";

    }


    // Update is called once per frame
    void Update()
    {
        headsetPos = centerEye.transform.position;
        headsetRot = centerEye.transform.rotation;

        for (int i =0; i < 3; i++)
        {
            static_.Add(headsetPos[i]);
        }
        for (int i = 0; i < 4; i++)
        {
            static_.Add(headsetRot[i]);
        }
        time_.Add(Time.time.ToString());

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
            for(int i = 0; i < static_.Count/7; i++)
            {
                tw.WriteLine("Pose," + static_[i * 7] + "," + static_[i * 7 + 1] + "," + static_[i * 7 + 2] + "," +
                    static_[i * 7 + 3] + "," + static_[i * 7 + 4] + "," + static_[i * 7 + 5] + "," + static_[i * 7 + 6]);
           
            }
            tw.Close();

            TextWriter tw2 = new StreamWriter(filenameTXT, false);
            tw2.WriteLine("# time x y z qx qy qz qw");
            tw2.Close();


            tw2 = new StreamWriter(filenameTXT, true);
            for (int i = 0; i < static_.Count / 7; i++)
            {
                tw2.WriteLine(time_[i] + " " + static_[i * 7] + " " + static_[i * 7 + 1] + " " + static_[i * 7 + 2] + " " +
                    static_[i * 7 + 3] + " " + static_[i * 7 + 4] + " " + static_[i * 7 + 5] + " " + static_[i * 7 + 6]);

            }
            tw2.Close();
        }

    }
}
