using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PoseExtractorOculus : MonoBehaviour
{
    GameObject centerEye;
    Vector3 headsetPos;
    Vector3 headsetRot;
    string filename = "";
   

    public List<double> static_= new List<double>();
    // Start is called before the first frame update
    void Start()
    {
        print(System.DateTime.Now.ToString("yyyyMMdd_hhmmss"));
        centerEye = GameObject.Find("CenterEyeAnchor");
        print("hello there");
        filename = "C:/Users/pbottoni/Documents/BachelorThesis/TestCSV/test_oculus_" + System.DateTime.Now.ToString("yyyyMMdd_hhmmss")+".csv";
    }


    // Update is called once per frame
    void Update()
    {
        headsetPos = centerEye.transform.position;
        headsetRot = centerEye.transform.eulerAngles;
    
        for(int i =0; i < 3; i++)
        {
            static_.Add(headsetPos[i]);
        }
        for (int i = 0; i < 3; i++)
        {
            static_.Add(headsetRot[i]);
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
            for(int i = 0; i < static_.Count/6; i++)
            {
                tw.WriteLine("Pose,"+static_[i * 6] + "," + static_[i * 6 + 1] + "," + static_[i * 6 + 2] + "," +
                    static_[i * 6 + 3] + "," + static_[i * 6 + 4] + "," + static_[i * 6 + 5]);
           
            }
            tw.Close();
        }

    }
}
