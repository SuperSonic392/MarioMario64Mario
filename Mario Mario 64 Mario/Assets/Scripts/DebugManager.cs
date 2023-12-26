using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DebugManager : MonoBehaviour
{
    private void Start()
    {
        for (int i = 1; i < Display.displays.Length; i++)
        {
            Display.displays[i].Activate();
        }
    }
    // Update is called once per frame
    void Update()
    {
        if(Input.GetButton("Fire1"))
        {
            SceneManager.LoadScene(0);
        }
    }
}
