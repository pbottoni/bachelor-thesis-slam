using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Text;
using System.IO;
using System;

public class Overlap : MonoBehaviour
{
	//Gameobjects
    public GameObject headset;
    public GameObject tracker1;
    public GameObject tracker2;
    public GameObject tracker3;
	
	//Track Tracker
	public bool trackerEnabled;
	//public bool oneTrackerOnly;
	
	//Input strings
	public string TestName;
	public string HeadsetName;
	
	//Input variables, might not work as expected depending on Unity refresh rate
	//Is dependent on the headset connected, Focus 3 has 90 hz refresh rate, test for other devices as well
	//Maybe change in case the trackers and headset have different refresh rate, but should be per image frame
	public int NrFramesContinuously;
	public int NrFramesSpecialPoint;
	public int nrOfSpecialSamples;
	
	//Time variables
	private string now;
	private string nowWrite;
	
	//Write strings of single lines for the HMD
    string[] rowDataTempCont = new string[9];
    string[] rowDataTempSpec = new string[9];
	
	//Write strings of single lines for the trackers
    string[] rowDataTempContTracker = new string[21];
    string[] rowDataTempSpecTracker = new string[21];
	
	//Private booleans to know if it should track data or not
	private bool trackContinuously;
	private bool trackSpecial;
	
	//Nr of samples already taken in current special location
	private int specialPointsSampled = 0;
	//Nr of images since last sample point taken
	private int framesPasedSpec = 0;
	private int framesPasedCont = 0;

	//Store the whole tables before writing to file 
    private List<string[]> rowDataCont = new List<string[]>();
	private List<string[]> rowDataSpec = new List<string[]>();
    private List<string[]> rowDataContTracker = new List<string[]>();
	private List<string[]> rowDataSpecTracker = new List<string[]>();


    void Awake()
    {
					
		//Use experiment start time in file name
        nowWrite = DateTime.Now.ToString("yyyyMMdd\\THHmm");
		
		
		//Creating First row of titles manually for HMDs
        //Continuous
        rowDataTempCont[0] = "Device";
        rowDataTempCont[1] = "TestName";
        rowDataTempCont[2] = "Date";
        rowDataTempCont[3] = "X-Position";
        rowDataTempCont[4] = "Y-Position";
        rowDataTempCont[5] = "Z-Position";
        rowDataTempCont[6] = "X-Rotation";
        rowDataTempCont[7] = "Y-Rotation";
        rowDataTempCont[8] = "Z-Rotation";
        rowDataCont.Add(rowDataTempCont);
		
		//Special
        rowDataTempSpec[0] = "Device";
        rowDataTempSpec[1] = "TestName";
        rowDataTempSpec[2] = "Date";
        rowDataTempSpec[3] = "X-Position";
        rowDataTempSpec[4] = "Y-Position";
        rowDataTempSpec[5] = "Z-Position";
        rowDataTempSpec[6] = "X-Rotation";
        rowDataTempSpec[7] = "Y-Rotation";
        rowDataTempSpec[8] = "Z-Rotation";
        rowDataSpec.Add(rowDataTempSpec);
		
		
		if(trackerEnabled){
			//Creating First row of titles manually for Trackers
			//Continuous
			rowDataTempContTracker[0] = "Device";
			rowDataTempContTracker[1] = "TestName";
			rowDataTempContTracker[2] = "Date";
			rowDataTempContTracker[3] = "X-Position1";
			rowDataTempContTracker[4] = "Y-Position1";
			rowDataTempContTracker[5] = "Z-Position1";
			rowDataTempContTracker[6] = "X-Rotation1";
			rowDataTempContTracker[7] = "Y-Rotation1";
			rowDataTempContTracker[8] = "Z-Rotation2";
			rowDataTempContTracker[9] = "X-Position2";
			rowDataTempContTracker[10] = "Y-Position2";
			rowDataTempContTracker[11] = "Z-Position2";
			rowDataTempContTracker[12] = "X-Rotation2";
			rowDataTempContTracker[13] = "Y-Rotation2";
			rowDataTempContTracker[14] = "Z-Rotation2";
			rowDataTempContTracker[15] = "X-Position3";
			rowDataTempContTracker[16] = "Y-Position3";
			rowDataTempContTracker[17] = "Z-Position3";
			rowDataTempContTracker[18] = "X-Rotation3";
			rowDataTempContTracker[19] = "Y-Rotation3";
			rowDataTempContTracker[20] = "Z-Rotation3";
			rowDataContTracker.Add(rowDataTempContTracker);
			
			//Special
			rowDataTempSpecTracker[0] = "Device";
			rowDataTempSpecTracker[1] = "TestName";
			rowDataTempSpecTracker[2] = "Date";
			rowDataTempSpecTracker[3] = "X-Position1";
			rowDataTempSpecTracker[4] = "Y-Position1";
			rowDataTempSpecTracker[5] = "Z-Position1";
			rowDataTempSpecTracker[6] = "X-Rotation1";
			rowDataTempSpecTracker[7] = "Y-Rotation1";
			rowDataTempSpecTracker[8] = "Z-Rotation2";
			rowDataTempSpecTracker[9] = "X-Position2";
			rowDataTempSpecTracker[10] = "Y-Position2";
			rowDataTempSpecTracker[11] = "Z-Position2";
			rowDataTempSpecTracker[12] = "X-Rotation2";
			rowDataTempSpecTracker[13] = "Y-Rotation2";
			rowDataTempSpecTracker[14] = "Z-Rotation2";
			rowDataTempSpecTracker[15] = "X-Position3";
			rowDataTempSpecTracker[16] = "Y-Position3";
			rowDataTempSpecTracker[17] = "Z-Position3";
			rowDataTempSpecTracker[18] = "X-Rotation3";
			rowDataTempSpecTracker[19] = "Y-Rotation3";
			rowDataTempSpecTracker[20] = "Z-Rotation3";
			rowDataSpecTracker.Add(rowDataTempSpecTracker);
		}

		
		

		framesPasedCont = NrFramesContinuously;
    }

	//Start special data collection
    public void startSpecial(InputAction.CallbackContext context)
    {
        if (context.performed && !trackSpecial)
        {
			trackSpecial = true;
			Debug.Log("keep still");
			framesPasedSpec = NrFramesSpecialPoint;
			specialPointsSampled = 0;
        }  
    }
	
	//start data collection continuously
	public void startContinuously(InputAction.CallbackContext context)
    {
        if (context.performed && !trackContinuously)
        {
			trackContinuously = true;
			Debug.Log("start tracking continuously");
			framesPasedSpec = NrFramesSpecialPoint;
			specialPointsSampled = 0;
        } 
    }
	
	
    // Start is called before the first frame update
    void Start()
    {

        
    }
	
	//Save all the collected data points
	public void save(InputAction.CallbackContext context)
    {
		
        if (context.performed)
        {
			
			//Save HMD Logs
			//Save special Log
			Debug.Log("Saving Special Data");
			trackSpecial = false;
			trackContinuously = false;
			string[][] output = new string[rowDataSpec.Count][];

			for(int i = 0; i < output.Length; i++){
				output[i] = rowDataSpec[i];
			}

			int     length         = output.GetLength(0);
			string     delimiter     = ",";

			StringBuilder sb = new StringBuilder();
        
			for (int index = 0; index < length; index++)
				sb.AppendLine(string.Join(delimiter, output[index]));
		
			string filePath = Application.dataPath +"/CSV/"+ nowWrite + TestName + HeadsetName +".Special.csv";
			StreamWriter outStream = System.IO.File.CreateText(filePath);
			outStream.WriteLine(sb);
			outStream.Close();
			
			//Save continuous Log
			Debug.Log("Saving Continuous Data");
			output = new string[rowDataCont.Count][];

			for(int i = 0; i < output.Length; i++){
				output[i] = rowDataCont[i];
			}

			length         = output.GetLength(0);
			delimiter     = ",";

			sb = new StringBuilder();
        
			for (int index = 0; index < length; index++)
				sb.AppendLine(string.Join(delimiter, output[index]));
		
			filePath = Application.dataPath +"/CSV/"+ nowWrite + TestName + HeadsetName +".Continuous.csv";
			outStream = System.IO.File.CreateText(filePath);
			outStream.WriteLine(sb);
			outStream.Close();
			
			
			if(trackerEnabled){
				//Save Tracker Logs
				//Save special Log
				Debug.Log("Saving Special Tracker Data");
				trackSpecial = false;
				trackContinuously = false;
				output = new string[rowDataSpecTracker.Count][];

				for(int i = 0; i < output.Length; i++){
					output[i] = rowDataSpecTracker[i];
				}

				length         = output.GetLength(0);
				delimiter     = ",";

				sb = new StringBuilder();
			
				for (int index = 0; index < length; index++)
					sb.AppendLine(string.Join(delimiter, output[index]));
			
				filePath = Application.dataPath +"/CSV/"+ nowWrite + TestName + "Tracker.Special.csv";
				outStream = System.IO.File.CreateText(filePath);
				outStream.WriteLine(sb);
				outStream.Close();
				
				//Save continuous Log
				Debug.Log("Saving Continuous Tracker Data");
				output = new string[rowDataContTracker.Count][];

				for(int i = 0; i < output.Length; i++){
					output[i] = rowDataContTracker[i];
				}

				length         = output.GetLength(0);
				delimiter     = ",";

				sb = new StringBuilder();
			
				for (int index = 0; index < length; index++)
					sb.AppendLine(string.Join(delimiter, output[index]));
			
				filePath = Application.dataPath +"/CSV/"+ nowWrite + TestName + "Tracker.Continuous.csv";
				outStream = System.IO.File.CreateText(filePath);
				outStream.WriteLine(sb);
				outStream.Close();
			}
			
			Debug.Log("Finished Saving");
		}
		
	}
	
	//Add location data to continuous file buffer
	void addLocationCont(){
		rowDataTempCont = new string[9];
        rowDataTempCont[0] = HeadsetName; // name
        rowDataTempCont[1] = TestName; // ID
        rowDataTempCont[2] = now;
        rowDataTempCont[3] = headset.transform.position.x.ToString();
        rowDataTempCont[4] = headset.transform.position.y.ToString();
        rowDataTempCont[5] = headset.transform.position.z.ToString();
        rowDataTempCont[6] = headset.transform.rotation.x.ToString();
		rowDataTempCont[7] = headset.transform.rotation.y.ToString();
        rowDataTempCont[8] = headset.transform.rotation.z.ToString();
        rowDataCont.Add(rowDataTempCont);
	}
	
	//Add location data to special file buffer
	void addLocationSpecial(){
		rowDataTempSpec = new string[9];
        rowDataTempSpec[0] = HeadsetName; // name
        rowDataTempSpec[1] = TestName; // ID
        rowDataTempSpec[2] = now;
        rowDataTempSpec[3] = headset.transform.position.x.ToString();
        rowDataTempSpec[4] = headset.transform.position.y.ToString();
        rowDataTempSpec[5] = headset.transform.position.z.ToString();
        rowDataTempSpec[6] = headset.transform.rotation.x.ToString();
		rowDataTempSpec[7] = headset.transform.rotation.y.ToString();
        rowDataTempSpec[8] = headset.transform.rotation.z.ToString();
        rowDataSpec.Add(rowDataTempSpec);
	}

	//Add location data to continuous file buffer
	void addLocationContTracker(){
		rowDataTempContTracker = new string[21];
        rowDataTempContTracker[0] = "Trackers"; // name
        rowDataTempContTracker[1] = TestName; // ID
        rowDataTempContTracker[2] = now;
        rowDataTempContTracker[3] = tracker1.transform.position.x.ToString();
        rowDataTempContTracker[4] = tracker1.transform.position.y.ToString();
        rowDataTempContTracker[5] = tracker1.transform.position.z.ToString();
        rowDataTempContTracker[6] = tracker1.transform.rotation.x.ToString();
		rowDataTempContTracker[7] = tracker1.transform.rotation.y.ToString();
        rowDataTempContTracker[8] = tracker1.transform.rotation.z.ToString();
        rowDataTempContTracker[9] = tracker2.transform.position.x.ToString();
        rowDataTempContTracker[10] = tracker2.transform.position.y.ToString();
        rowDataTempContTracker[11] = tracker2.transform.position.z.ToString();
        rowDataTempContTracker[12] = tracker2.transform.rotation.x.ToString();
		rowDataTempContTracker[13] = tracker2.transform.rotation.y.ToString();
        rowDataTempContTracker[14] = tracker2.transform.rotation.z.ToString();
        rowDataTempContTracker[15] = tracker3.transform.position.x.ToString();
        rowDataTempContTracker[16] = tracker3.transform.position.y.ToString();
        rowDataTempContTracker[17] = tracker3.transform.position.z.ToString();
        rowDataTempContTracker[18] = tracker3.transform.rotation.x.ToString();
		rowDataTempContTracker[19] = tracker3.transform.rotation.y.ToString();
        rowDataTempContTracker[20] = tracker3.transform.rotation.z.ToString();
        rowDataContTracker.Add(rowDataTempContTracker);
	}
	
	//Add location data to special file buffer
	void addLocationSpecialTracker(){
		rowDataTempSpecTracker = new string[21];
        rowDataTempSpecTracker[0] = "Trackers"; // name
        rowDataTempSpecTracker[1] = TestName; // ID
        rowDataTempSpecTracker[2] = now;
        rowDataTempSpecTracker[3] = tracker1.transform.position.x.ToString();
        rowDataTempSpecTracker[4] = tracker1.transform.position.y.ToString();
        rowDataTempSpecTracker[5] = tracker1.transform.position.z.ToString();
        rowDataTempSpecTracker[6] = tracker1.transform.rotation.x.ToString();
		rowDataTempSpecTracker[7] = tracker1.transform.rotation.y.ToString();
        rowDataTempSpecTracker[8] = tracker1.transform.rotation.z.ToString();
        rowDataTempSpecTracker[9] = tracker2.transform.position.x.ToString();
        rowDataTempSpecTracker[10] = tracker2.transform.position.y.ToString();
        rowDataTempSpecTracker[11] = tracker2.transform.position.z.ToString();
        rowDataTempSpecTracker[12] = tracker2.transform.rotation.x.ToString();
		rowDataTempSpecTracker[13] = tracker2.transform.rotation.y.ToString();
        rowDataTempSpecTracker[14] = tracker2.transform.rotation.z.ToString();
        rowDataTempSpecTracker[15] = tracker3.transform.position.x.ToString();
        rowDataTempSpecTracker[16] = tracker3.transform.position.y.ToString();
        rowDataTempSpecTracker[17] = tracker3.transform.position.z.ToString();
        rowDataTempSpecTracker[18] = tracker3.transform.rotation.x.ToString();
		rowDataTempSpecTracker[19] = tracker3.transform.rotation.y.ToString();
        rowDataTempSpecTracker[20] = tracker3.transform.rotation.z.ToString();
        rowDataSpecTracker.Add(rowDataTempSpecTracker);
	}

    // Update is called once per frame
    void Update()
    {
		//Get the current time
        now = DateTime.Now.ToString("yyyy-MM-dd\\THH:mm:ss");
		
		//Check if I do special sampling here
		if(trackSpecial == true){
			//Check there are still special samples left
			if(nrOfSpecialSamples - specialPointsSampled > 0){
				//Check if it is a refresh where I should take a sample
				if(framesPasedSpec == NrFramesSpecialPoint){
					addLocationSpecial();
					if(trackerEnabled){
						addLocationSpecialTracker();
					}
					specialPointsSampled++;
					framesPasedSpec = 0;
				}
				framesPasedSpec++;
			}
			else{
				trackSpecial = false;
				Debug.Log("Move B*tch!");
			}
		}
		
		//Check if continuous updates are enabled
		if(trackContinuously){
			//Check if it is time for a normal sample	
			if(framesPasedCont == NrFramesContinuously){
				//Debug.Log("Continuous Measuring Point");
				addLocationCont();
				if(trackerEnabled){
					addLocationContTracker();
				}
				framesPasedCont = 0;
			}   
			framesPasedCont++;
		}


    }
}
