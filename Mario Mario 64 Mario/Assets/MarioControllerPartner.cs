#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MarioController))]
public class MarioControllerPartner : Editor
{
    public override void OnInspectorGUI()
    {
        MarioController mario = (MarioController)target;
        if (GUILayout.Button("Die"))
        {
            mario.Die();
        }
        DrawDefaultInspector();
    }
}
#endif