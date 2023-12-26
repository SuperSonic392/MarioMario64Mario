using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SkinnedLODManager : MonoBehaviour
{
    private Camera cam;
    public SkinnedMeshRenderer render;
    public List<float> distances;
    public List<Mesh> renderers;
    public Animator anim;
    public List<Avatar> avatars;
    void Update()
    {
        if(cam == null)
        {
            cam = FindObjectOfType<CameraController>().GetComponent<Camera>();
            return;
        }
        float distance = (transform.position - cam.transform.position).magnitude;

        int minListSize = Mathf.Min(distances.Count, renderers.Count, avatars.Count);

        if (anim == null)
        {
            for (int i = 0; i < distances.Count; i++)
            {
                if (distance <= distances[i])
                {
                    render.sharedMesh = renderers[i];
                    break;
                }
            }
            return;
        }
        
        for (int i = 0; i < minListSize; i++)
        {
            if (distance <= distances[i])
            {
                render.sharedMesh = renderers[i];
                anim.avatar = avatars[i];
                break;
            }
        }
    }
}
