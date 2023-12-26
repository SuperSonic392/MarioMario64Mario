using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New CharacterData", menuName = "ScriptableObjects/CharacterData", order = 1)]
[Serializable]
public class CharacterData : ScriptableObject
{
    public GameObject prefab;
    public Sprite UiImage;
    public string voicesFolder;
}
