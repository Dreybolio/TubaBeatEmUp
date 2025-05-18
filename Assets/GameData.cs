using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class GameData : Singleton<GameData>
{
    [Header("Character Data")]
    public CharacterData[] Characters;
    [SerializeField] private CharacterData[] _unlockByDefault;
    public SaveFileData SaveFile {  get; private set; }

    // DataPaths
    private string SAVEFILE_DATAPATH;
    private string FILENAME = "SAVEDATA.json";

    private void Awake()
    {
#if UNITY_EDITOR
        SAVEFILE_DATAPATH = Path.Combine(Application.dataPath, "Resources");
#else
        SAVEFILE_DATAPATH = Path.Combine(Application.persistentDataPath, "Resources");
#endif
        if (!Directory.Exists(SAVEFILE_DATAPATH))
            Directory.CreateDirectory(SAVEFILE_DATAPATH);

        Load();
    }

    public void Save()
    {
        Debug.LogError("Can't Save! Not yet implemented!");
    }
    public void Load()
    {
        Debug.Log("Loading Save Data");
        try
        {
            string json = File.ReadAllText(Path.Combine(SAVEFILE_DATAPATH, FILENAME));
            SerializableSaveFileData readInfo = JsonConvert.DeserializeObject<SerializableSaveFileData>(json);
            // Convert the serializable form into the normal form

            SaveFileData readSave = new();
            readSave.Characters = new();
            int i = 0;
            foreach (var cha in Characters)
            {
                readSave.Characters.Add(cha, readInfo.CharacterSaves[i]);
                i++;
            }
            SaveFile = readSave;
        }
        catch
        {
            Debug.LogWarning("Could not find a save file! Creating a default one.");
            CreateNewFile();
        }
    }
    public void CreateNewFile()
    {
        SaveFileData newSave = new();
        newSave.Characters = new();
        foreach (var cha in Characters)
        {
            newSave.Characters.Add(cha, new() { level = 1, unlocked = _unlockByDefault.Contains(cha), items = new() });
        }
        SaveFile = newSave;

        // Convert this save data into the serializable form
        SerializableSaveFileData ssfd = new();
        ssfd.CharacterSaves = new();
        ssfd.CharacterSaves.AddRange(SaveFile.Characters.Values);

        // Convert to JSON
        string json = JsonConvert.SerializeObject(ssfd);
        print(json);
        File.WriteAllText(Path.Combine(SAVEFILE_DATAPATH, FILENAME), json);
    }


    ///
    /// Useful getter functions used by other classes
    ///
    public CharacterSaveData GetCharacterSaveData(CharacterData cd)
    {
        if (SaveFile.Characters.TryGetValue(cd, out CharacterSaveData csd))
        {
            return csd;
        }
        else
        {
            Debug.LogError($"Failed to get save data of character {cd}");
            return null;
        }
    }

    public CharacterData[] GetUnlockedCharacters()
    {
        print($"SaveFile Null: {SaveFile == null}");
        print($"Characters Null: {SaveFile.Characters == null}");
        print($"Keys Null: {SaveFile.Characters.Keys == null}");
        return SaveFile.Characters.Keys.Where(c => GetCharacterSaveData(c).unlocked).ToArray();
    }
}


//**
//**    This system is a little weird: here's the explanation. 
//**    Basically, Dictionaries cannot be serializaed into JSON, and we wouldn't want to do that since the Key part of the dict is a ScriptableObject
//**    Since saving ScriptableObject data would be dumb, we assume that the order of the ScriptableObjects won't change and store the CharacterSaveData in a numbered list
//**    When we read this data, we assign the CharacterSaveData back to the CharacterData based on the order of both lists.
//**
//**    Unless otherwise specified, all other data should be identical between the two classes.
//**

public class SaveFileData
{
    public Dictionary<CharacterData, CharacterSaveData> Characters;
}

[Serializable]
public class SerializableSaveFileData
{
    public List<CharacterSaveData> CharacterSaves;
}