using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using System.Collections.ObjectModel;
using UnityEngine.Events;
using Valve.VR.InteractionSystem;
using System.IO;
using Newtonsoft.Json;
using System;
using System.Linq;

public class Attemp_1 : MonoBehaviour
{
    //Declares the FingerCurls array and Hand pointer object used later in the code.
    public float[] FingerCurls;
    public Hand LeftHandPointer;

    //The path to the training file.
    public string DatasetFilePath;

    // A list containing the pattern names.
    public Dictionary<List<string>, int> PatternNames;

    //the example file manager
    public DatasetManager DSM;

    //the current pattern name/identifier of the program.
    public string CurrentIdentifier;

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

        //sets the path to the local dataset file.
        DatasetFilePath = Application.dataPath + "/DatasetTest.txt";

        // instantizes a new DatasetManager object.
        DSM = new DatasetManager(DatasetFilePath);
    }

    //The "Update" function is called once every frame, simmilar to a game loop in other languages.
    void Update()
    {
        //Points to the "fingerCurls" array contained in the "Hand" script.
        //The array contains 5 float values ranging from 0 to 1 that describe how bent each finger is, 1 being completly bent and 0 being completely strait.
        //The array is indexed from 0 to 4, 0 being the thumb and 4 being the pinky.
        //the "if" statmant makes sure that the LeftHandPointer in Awake and Has a skeleton to access, as to not raise a NullExeption error to try and access an empty object.
        if (LeftHandPointer.HasSkeleton())
        {
            FingerCurls = LeftHandPointer.skeleton.fingerCurls;
        }
        
        //the if statement is responsible for recording the current curls to the training file under the current idetifier.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PrintFingerCurls();
            DSM.SaveDataset(new DatasetEntry(FingerCurls, CurrentIdentifier));       
            Debug.Log(DSM.DataEntryClassGuess(FingerCurls, 1));
        }
    }


    /// <summary>
    /// the DatasetManger class Is used as a tool to access and manage the Dataset file.
    /// </summary>
    [JsonObject]
    public class DatasetManager
    {
        //A dict containing a string as it's key, which represents the identifier and a List of DatasetEntry objects that represent the diffrent Patterns for a certain identifier.
        public List<DatasetEntry> DatasetEntries;

        //Again the filepath of the training file.
        public string DatasetFilePath;

        //class constuctor
        public DatasetManager(string _DatasetFilePath)
        {
            //initiated the DatasetEntries dict that will class and store the DatasetEntries for the training file.
            DatasetEntries = new List<DatasetEntry>();      

            //Sets the DatasetFilePath
            this.DatasetFilePath = _DatasetFilePath;

            //Put the Current training file to the DatasetEntries dict that was just created.
            string JsonFile = File.ReadAllText(_DatasetFilePath);
            DatasetEntries = JsonConvert.DeserializeObject<List<DatasetEntry>>(JsonFile) ?? new List<DatasetEntry>();
        }

        /// <summary>
        /// Saves the Current Dataset to the Dataset file.
        /// </summary>
        public void SaveDataset(DatasetEntry CurrentEntry)
        {
            //creates a temporary data save (may be changed in the future)<RFT>.
            DatasetEntry TempSave = new DatasetEntry(CurrentEntry.DataSnapshot, CurrentEntry.Identifier);

            //The Current file save.
            string JsonFile = File.ReadAllText(DatasetFilePath);

            //Retrive the Current file save and put it in the DatasetEntries list (Overcomes the overwriting all datasnapshots bug).
            DatasetEntries = JsonConvert.DeserializeObject<List<DatasetEntry>>(JsonFile) == null ? new List<DatasetEntry>() : JsonConvert.DeserializeObject<List<DatasetEntry>>(JsonFile);

            //Adds the Current DatasetEntry to the DatasetEntries list.
            DatasetEntries.Add(TempSave);

            //Overwrites the current file with the new list.
            File.WriteAllText(DatasetFilePath, JsonConvert.SerializeObject(DatasetEntries, Formatting.Indented));
        }

        //Try1 of pattern Recognition using the KNN algorithm
        public string DataEntryClassGuess(float[] CurrentCurls, int K)
        {
            //create a list to store tuples, each containg a DatasetEntry object and the "distance" of the Entry to the unclassified curls.
            List<Tuple<DatasetEntry, float>> Distances = new List<Tuple<DatasetEntry, float>>();

            //Loops over all the DatasetEntry objects in the File/DatasetEntries list.
            foreach (DatasetEntry Entry in DatasetEntries)
            {
                //sets the current distance to 0.
                float Distance = 0;

                //Loops over each finger from the current DatasetEntry.
                for(int i = 0; i < Entry.DataSnapshot.Length; i++)
                {
                    //Calculate the sqtdistance from the current finger value to the DatasetEntry current value.
                    Distance += (float)Mathf.Pow(CurrentCurls[i] - Entry.DataSnapshot[i], 2);
                }
                //Turns that into a normal distance, important seince most values of the Distances  will be below 1.
                Distance = (float)Mathf.Sqrt(Distance);

                //Adds the Distance to the Distances list with the current DatasetEntry.
                Distances.Add(new Tuple<DatasetEntry, float>(Entry, Distance));
            }

            //sort the Distances list by distance value asending.
            Distances.Sort((x, y) => x.Item2.CompareTo(y.Item2));

            //Create a dictionary containg a string (Pattern class) as its key and an int (counter) as its value.
            Dictionary<string, int> PatternNamesAndCounter = new Dictionary<string, int>();

            //Loops over the K nearest DatasetEntries to the unclassified Entry.
            for (int i = 0; i < K; i++)
            {
                //If there was already a DatasetEntry from the class in Distances[i], incrament the counter at the key "class".
                if (PatternNamesAndCounter.ContainsKey(Distances[i].Item1.Identifier))
                {
                    PatternNamesAndCounter[Distances[i].Item1.Identifier]++;
                }

                //If not, Add a key value pair, the key being the current class and the value being 1.
                else
                {
                    PatternNamesAndCounter.Add(Distances[i].Item1.Identifier, 1);
                }                
            }
            string ClassGuess = PatternNamesAndCounter.FirstOrDefault(x => x.Value == PatternNamesAndCounter.Values.Max()).Key;
            //add here saving the Entry with the New class.

            //return the class that was most common from the K nearest neighbors to the unclassed entrty.
            //that class has a decent chance of being the currect class.
            return ClassGuess;
        }
    }


    /// <summary>
    /// The DatasetEntry class acts as a wrapper to the FingerCurls array data snapshots for the Pattern recognition.
    /// </summary>
    [JsonObject]
    public class DatasetEntry
    {
        //A float array containg the fingerCurls values
        public float[] DataSnapshot 
        { get; set; }

        //which class it belongs to
        public string Identifier
        { get; set; }

        public DatasetEntry(float[] _DataSnapshot, string _Identifier)
        {
            this.DataSnapshot = _DataSnapshot;
            this.Identifier = _Identifier;
        }
    }
  
    /// <summary>
    /// Prints the current curl value of each finger in the debug log.
    /// </summary>
    public void PrintFingerCurls()
    {
        //outputs each finger's name and Current Curl value [0,1].
        for (int i = 0; i < FingerCurls.Length; i++)
        {
            Debug.Log(FingerNames[i] + " " + FingerCurls[i]);
        }
    }
}
