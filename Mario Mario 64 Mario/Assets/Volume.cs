using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Volume : MonoBehaviour
{
    public enum VolumeType
    {
        Water, 
        Lava, //bounce up with ass on fire
        Poison, //instant death
        Gas, //mario takes slow damage
    }
    public VolumeType type;
    public Vector3 size;
    public float density;
    public Vector3 current; //a velocity to push objects with
    private void Awake() //failsafe density values
    {
        if(density <= 0)
        {
            switch (type)
            {
                case VolumeType.Water:
                    density = 3f; //water is surprisingly dense in this.
                    break;
                case VolumeType.Lava:
                    density = 2.5f; //how much damage it deals
                    break;
                case VolumeType.Poison:
                    density = 1f;
                    break;
                case VolumeType.Gas:
                    density = 1f; //how much damage it deals
                    break;
            }
        }
    }

    private void OnDrawGizmos()
    {
        switch (type)
        {
            case VolumeType.Water:
                Gizmos.color = Color.blue;
                break;
            case VolumeType.Lava:
                Gizmos.color = Color.red;
                break;
            case VolumeType.Poison:
                Gizmos.color = Color.magenta;
                break;
            case VolumeType.Gas:
                Gizmos.color = Color.yellow;
                break;
        }
        Gizmos.DrawWireCube(transform.position, size);
    }
}

public class VolumeData
{
    public float surface;
    public Vector3 size;
    public float density;
    public Vector3 current;
}