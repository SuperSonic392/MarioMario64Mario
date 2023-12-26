using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New CharacterColor", menuName = "ScriptableObjects/CharacterColor", order = 1)]
[Serializable]
public class CharacterColors : ScriptableObject
{
    public Color shirt;
    public Color pants;
    public bool enabled;
}
