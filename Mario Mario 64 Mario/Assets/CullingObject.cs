using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CullingObject : MonoBehaviour
{
    public float radius = 300;
    public MarioController[] controllers;
    private GameManager gameManager;
    private MeshRenderer meshRenderer;
    private SkinnedMeshRenderer skinnedMeshRenderer;
    private void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
        meshRenderer = GetComponent<MeshRenderer>();
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
    }
    void Update()
    {
        if ((gameManager.localPlayer.transform.position - transform.position).magnitude < radius)
        {
            if (meshRenderer)
            {
                meshRenderer.enabled = true;
            }
            else if (skinnedMeshRenderer)
            {
                skinnedMeshRenderer.enabled = true;
            }
            return;
        }
        if (meshRenderer)
        {
            meshRenderer.enabled = false;
        }
        else if (skinnedMeshRenderer)
        {
            skinnedMeshRenderer.enabled = false;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
