using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public GameObject character;
    public CharacterColors color;
    public List<GameObject> characters;
    public List<CharacterColors> colors;
    void Start()
    {
        DontDestroyOnLoad(this);
    }
    public void SetColor(int id)
    {
        color = colors[id];
    }
    public void SetCharacter(int id)
    {
        character = characters[id];
    }
}
