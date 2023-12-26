using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelRoom : MonoBehaviour
{
    private GameManager gameManager;
    private Bounds bounds;
    private MeshRenderer meshRenderer;
    private SkinnedMeshRenderer skinnedMeshRenderer;
    public int area;
    void Start()
    {

        gameManager = FindObjectOfType<GameManager>();
        meshRenderer = GetComponent<MeshRenderer>();
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();

        if (skinnedMeshRenderer)
        {
            bounds = skinnedMeshRenderer.bounds;
        }
        else if (meshRenderer)
        {
            bounds = meshRenderer.bounds;
        }
    }

    void Update()
    {
        // Check if the player's position is inside the bounds of the room.
        if (bounds.Contains(gameManager.localPlayer.transform.position))
        {
            gameManager.area = area;
            // Enable the mesh renderer when a player is inside.
            if (meshRenderer)
            {
                meshRenderer.enabled = true;
            }
            else if (skinnedMeshRenderer)
            {
                skinnedMeshRenderer.enabled = true;
            }
            // End the loop if a player is inside the room.
            return;
        }
        // If no player is inside, disable the mesh renderer.
        if (meshRenderer)
        {
            meshRenderer.enabled = false;
        }
        else if (skinnedMeshRenderer)
        {
            skinnedMeshRenderer.enabled = false;
        }
    }
}
