using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;

public class DialogueController : MonoBehaviour
{
    private List<NPCID> IDs = new List<NPCID>();
    private Dictionary<NPCID, List<DialogueInfo>> AllDialogue = new Dictionary<NPCID, List<DialogueInfo>>();
    string[][] fileContentFormatted;
    string path;
    // Start is called before the first frame update
    void Start()
    {
        LoadFile("NPCDialogue");
    }

    // Converts the contents of the Dialogue CSV file into data that the method GetDialogue can use
    // Additionally, loads the view counts for each piece of dialogue
    private void LoadFile(string fileName)
    {
        // This generates a file path to the file name passed as a parameter, then using the given path to read the contents of the CSV file
        path = Path.Combine(Application.persistentDataPath, fileName + ".csv");
        string[] fileContents = File.ReadAllLines(path);

        // Initialising the fileContentFormatted 2D array, with the length of the fileContents array, corresponding to the CSV file
        // Allows the program to get specific cells from the CSV file
        fileContentFormatted = new string[fileContents.Length][];

        // Splits the first line of file contents using the string via split to create an array, then appending it the the fileContentFormatted 2D array to add a first row
        // This process is repeated for all lines of the file, creating a complete formatted 2D array
        for (int i = 0; i < fileContents.Length; i++)
        {
            string current = fileContents[i];
            fileContentFormatted[i] = current.Split(new[] { ',' }, StringSplitOptions.None);
        }

        // This dictionary will store all NPC dialogue sorted by different moods
        Dictionary<NPCID, List<DialogueInfo>> MoodDictionary = new Dictionary<NPCID, List<DialogueInfo>>();
        // This keeps a record of all the used moods by the different dialogue options
        List<Mood> MoodsUsed = new List<Mood>();
        // This for loop sorts all the dialogue into mood categories, storing them in the MoodDictionary
        // It also keeps a record of the moods used in the MoodsUsed list

        // fileContentFormatted format:
        // fileContentFormatted[row][column]

        // column = 0 = NPC ID
        // column = 1 = Mood
        // column = 2 = dialogue
        // column = 3 = view count

        for (int i = 0; i < fileContentFormatted.Length; i++)
        {
            // This tries to read the NPC ID from the CSV file. If it is blank, it skips the row
            string ID = fileContentFormatted[i][0];
            if (ID == string.Empty)
            {
                continue;
            }
            // If we get here, there is a valid row to read. We are loading the mood and the piece of dialogue and storing them in variables
            string mood = fileContentFormatted[i][1];
            string dialogue = fileContentFormatted[i][2];

            // Views are optional to the file, so we need to check if the file has a views column for this row 
            // This is done by seeing if the length of the row is less than 4, which would mean there is not a views column, so the view count is 0
            int views;
            if (fileContentFormatted[i].Length < 4)
            {
                views = 0;
            }
            else // Else, the views value is equal to the stored value at the views column
            {
                views = int.Parse(fileContentFormatted[i][3]);
            }

            // Variable to store the mood of this piece of dialogue
            Mood currentMood;

            // This if statement allows the mood system to know what mood this piece of dialogue is
            if (mood == "Happy")
            {
                currentMood = Mood.Happy;
            }
            else if (mood == "Sad")
            {
                currentMood = Mood.Sad;
            }
            // Else, the mood must be angry
            else
            {
                currentMood = Mood.Angry;
            }

            // Create an NPC ID based off the ID loaded from the file and the current mood
            NPCID id = new NPCID(int.Parse(ID), currentMood);

            // First of all, we create a new instance of a DialogueInfo class and set it to all the data we loaded from the CSV file
            DialogueInfo newDialogue = new DialogueInfo(id, dialogue, currentMood, views, i);

            // We check to see if the dictionary contains a key which is equal to the key we created from the NPC's ID and the line of dialogue's mood
            // If the key exists, then we add this new dialogue to the list in the dictionary
            if (MoodDictionary.ContainsKey(id))
            {
                MoodDictionary[id].Add(newDialogue);
            }
            else // If the key doesn't exist, then we add the new item to the dictionary using the created id as a key, and intialise a new list containing the dialogue
            {
                MoodDictionary.Add(id, new List<DialogueInfo> { newDialogue });
                IDs.Add(id);
            }

            // Sets AllDialogue to the MoodDictionary
            AllDialogue = MoodDictionary;
        }

    }


    /// <summary>
    /// This method randomly chooses a piece of dialogue from the given ID, where the ID denotes the NPC and its mood
    /// </summary>
    /// <param name="ID"> The ID represents the NPC and its mood</param>
    /// <returns>Returns a line of dialogue for the TextCycle script to use</returns>
    public string GetDialogue(NPCID ID)
    {
        List<DialogueInfo> dialogues = AllDialogue[ID];
        DialogueInfo dialogue = dialogues[UnityEngine.Random.Range(0, dialogues.Count)];
        dialogue.views++;
        return dialogue.views + ". " + dialogue.text;
    }

    // This method is called when the gameobject is destroyed.
    // When this happens, the dialogue is converted back into a CSV format and saved back into the file it was loaded from
    private void OnDestroy()
    {
        // We iterate over all the ID's in the ID list to access all pieces of dialogue in the AllDialogue dictionary
        for (int i = 0; i < IDs.Count; i++)
        {
            // We get the list of dialogue from AllDialogue, given the ID
            List<DialogueInfo> currentDialogue = AllDialogue[IDs[i]];
            // We iterate over the currentDialogue list
            for (int k = 0; k < currentDialogue.Count; k++)
            {
                // We get the piece of dialogue at index k
                DialogueInfo current = currentDialogue[k];
                // We get its line of origin (row) in the CSV file
                int LineOfOrigin = current.LineOfOrigin;
                // We access this row and check its length to see if it has a views column, since the views column is optional
                // If the row length is less than 4, there is no views column and we need to add one
                if (fileContentFormatted[LineOfOrigin].Length < 4)
                {
                    // We initialise a new line for the CSV file equal to the length of the current line + 1 so that it has a 4th column
                    string[] line = new string[fileContentFormatted[LineOfOrigin].Length + 1];
                    // We copy over all the data from the existing 3-column row
                    for (int j = 0; j < fileContentFormatted[LineOfOrigin].Length; j++)
                    {
                        line[j] = fileContentFormatted[LineOfOrigin][j];
                    }
                    // We set the view count in column 4 to the view count value in the program
                    line[3] = current.views.ToString();
                    // We overwrite the line in the fileContentFormatted 2D array
                    fileContentFormatted[LineOfOrigin] = line;
                }
                else // Sets the view count in the 4th column of the row to the view count value
                {
                    fileContentFormatted[LineOfOrigin][3] = current.views.ToString();
                }
            }
        }
        // This gets the length of the first dimension of fileContentFormatted
        int length = fileContentFormatted.GetLength(0);
        // We create a new StringBuilder to reassemble the CSV file
        StringBuilder stringbuilder = new StringBuilder();
        for (int i = 0; i < length; i++)
        {
            // We use string.join to combine a row into a single string seperated by commas, then we append that string to the stringbuilder
            stringbuilder.AppendLine(string.Join(",", fileContentFormatted[i]));
        }

        // We write back the stringbuilder to the CSV file
        File.WriteAllText(path, stringbuilder.ToString());
    }
}

// Contains information about a line of dialogue, ie, which NPC it links to, what mood it is relevant to, how many times its been viewed and the actual line of dialogue
public class DialogueInfo
{
    public NPCID ID; // ID comprising the mood of the dialogue and the NPC's ID
    public int LineOfOrigin; // This is what row in the CSV file this line of dialogue came from, this is used for saving back the views to the file
    public Mood mood; // What mood this piece of dialogue is
    public string text; // The line of dialogue itself
    public int views; // How many times the line of dialogue has been viewed

    public DialogueInfo(NPCID id, string text, Mood mood, int views, int LineOfOrigin)
    {
        ID = id;
        this.mood = mood;
        this.text = text;
        this.views = views;
        this.LineOfOrigin = LineOfOrigin;
    }
}

public enum Mood
{
    Happy,
    Sad,
    Angry
}

// This struct encapsulates an NPC ID and mood to correspond to a set of dialogue
// e.g 'NPC = 1' 'Mood = Happy' will return the list of 'Happy' dialogue for 'NPC 1' from the AllDialogue Dictionary
[Serializable]
public struct NPCID
{
    public int NPC;
    public Mood Mood;

    public NPCID(int npc, Mood mood)
    {
        NPC = npc;
        Mood = mood;
    }
}