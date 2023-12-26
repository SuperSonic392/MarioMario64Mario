using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public int character;
    public TMP_Dropdown characterDropdown;
    public TMP_Dropdown colorDropdown;
    private SaveManager saveManager;
    private void Start()
    {
        saveManager = FindObjectOfType<SaveManager>();
        characterDropdown.ClearOptions();
        List<string> strig = new List<string>();
        foreach (GameObject obj in saveManager.characters)
        {
            strig.Add(obj.name);
        }
        characterDropdown.AddOptions(strig);
        colorDropdown.ClearOptions();
        strig = new List<string>();
        foreach (CharacterColors col in saveManager.colors)
        {
            strig.Add(col.name);
        }
        colorDropdown.AddOptions(strig);
    }
    public void SetCharacter(int character)
    {
        this.character = character;
        saveManager.SetCharacter(character);
    }
    public void SetColor(int color)
    {
        saveManager.SetColor(color);
    }
}
