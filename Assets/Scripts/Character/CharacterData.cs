using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="CharacterData", menuName ="ScriptableObjects/CharacterData", order = 1)]
[Serializable]
public class CharacterData : ScriptableObject
{
    public new string name;
    public string description;
    public Character spawnablePrefab;
    public CharacterModel modelPrefab;
    public Sprite icon;
}

[Serializable]
public class CharacterSaveData
{
    public bool unlocked;
    public int level;
    public List<string> items; // TODO: Make Item Class
}