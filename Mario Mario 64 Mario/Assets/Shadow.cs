using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shadow : MonoBehaviour //based on how sm64 on the n64 did it
{
    public LayerMask mask;
    public GameObject visual;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.localPosition = Vector3.zero; transform.localRotation = Quaternion.identity;
        RaycastHit hit;
        if(Physics.Raycast(transform.position, Vector3.down, out hit, 20, mask))
        {
            transform.position = hit.point + (hit.normal * 0.01f);
            transform.rotation = Quaternion.LookRotation(hit.normal);
            visual.SetActive(true);
        }
        else
        {
            visual.SetActive(false);
        }
    }
}
