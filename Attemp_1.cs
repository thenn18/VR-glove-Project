using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using System.Collections.ObjectModel;
using UnityEngine.Events;
using Valve.VR.InteractionSystem;
using System.IO;

public class Attemp_1 : MonoBehaviour
{
    //Declares the FingerCurls array and Hand Array used later in the code.
    public float[] FingerCurls;
    public Hand LeftHandPointer;

    public string DataFileName = "C:/Users/nn18/Desktop/vrGloves/DatasetJsonFileForVRGloveProject.txt";
    public List<string> PatternNames;
    int CurrentPaternIdentifyer = 0;
    JsonFileManager JSFM;

    //Indexes of each finger.
    const int THUMB = 0;
    const int INDEX_FINGER = 1;
    const int MIDDLE_FINGER = 2;
    const int RING_FINGER = 3;
    const int PINKY = 4;
    //Array containing each finger name in the apropriate index.
    private string[] FingerNames = { "THUMB", "INDEX_FINGER", "MIDDLE_FINGER", "RING_FINGER", "PINKY" };

    //the Code in the "Start" function runs once at the start of play.
    void Start()
    {
        //Creates a pointer called "LeftHandPointer" that points to the "Hand" script on the "LeftHand" object in unity.
        LeftHandPointer = GetComponent<Hand>();
        
        JSFM = new JsonFileManager();
    }

    //The "Update" function is called once every frame, simmilar to a game loop in other languages.
    void Update()
    {
        //Points to the "fingerCurls" array contained in the "Hand" script.
        //The array contains 5 float values ranging from 0 to 1 that describe how bent each finger is, 1 being completly bent and 0 being completely strait.
        //The array is indexed from 0 to 4, 0 being the thumb and 4 being the pinky.
        if (LeftHandPointer.HasSkeleton())
        {
            FingerCurls = LeftHandPointer.skeleton.fingerCurls;
        }
        
        //input system
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PrintFingerCurls();
            AppendJson();
        }
    }

    public class JsonFileManager
    {
        public string DataFileName = "DatasetJsonFileForVRGloveProject.txt";
        public string CurrentFileContent;

        public JsonFileManager()
        {
            this.CurrentFileContent = File.ReadAllText(DataFileName);
        }

        public void AddEntryToJsonFile(DatasetEntry dataset)
        {
            string json = JsonUtility.ToJson(dataset);
            File.AppendAllText(DataFileName, json);
        }

    }

    public class DatasetEntry
    {
        public int PatternIndentifyerCode { get; set; }
        public float[] DataSnapshot { get; set; }

        public DatasetEntry(int PatternIndentifyerCode, float[] DataSnapshot)
        {
            this.PatternIndentifyerCode = PatternIndentifyerCode;
            this.DataSnapshot = DataSnapshot;
        }
    }

    public void PrintFingerCurls()
    {
        //outputs each finger's name and Current Curl value [0,1].
        for (int i = 0; i < FingerCurls.Length; i++)
        {
            Debug.Log(FingerNames[i] + " " + FingerCurls[i]);
        }
    }
    
    public void AppendJson()
    {
        string _json = JsonUtility.ToJson(FingerCurls);

        File.AppendAllText(Application.dataPath + "/DatasetTest.txt", FingerCurls.ToString());
    }
}
