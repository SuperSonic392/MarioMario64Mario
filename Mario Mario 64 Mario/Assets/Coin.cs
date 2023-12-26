using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Respawning")]
    public bool respawns;
    public GameObject visual;
    public float respawnTime;
    private float respawnTimer;
    [Header("Generics")]
    public float radius;
    public GameObject particle; // Prefab for the particle effect
    public float spinspeed;
    //privates
    private bool collected = false; // Keep track of if the coin got collected.
    private GameManager manager; // Reference to the GameManager to access Mario controllers
    void Awake()
    {
        manager = FindObjectOfType<GameManager>(); // Find a GameManager in the scene
    }

    void FixedUpdate()
    {
        if (respawns)
        {

            if (respawnTimer >= 0)
            {
                respawnTimer -= Time.fixedDeltaTime;
            }
            else
            {
                if (collected)
                {
                    collected = false;
                    visual.SetActive(true);
                }
            }
        }
        transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y + (spinspeed * Time.fixedDeltaTime), 0f);

        foreach (MarioController mario in manager.controllers)
        {
            if (collected)
                return;
            float playerHeight = mario.height;
            float playerRadius = mario.radius;

            // Calculate the distance between the coin's center and the nearest point on the Mario's hitbox
            float closestX = Mathf.Clamp(transform.position.x, mario.transform.position.x - playerRadius, mario.transform.position.x + playerRadius);
            float closestY = Mathf.Clamp(transform.position.y, mario.transform.position.y, mario.transform.position.y + playerHeight);
            float closestZ = Mathf.Clamp(transform.position.z, mario.transform.position.z - playerRadius, mario.transform.position.z + playerRadius);

            Vector3 closestPoint = new Vector3(closestX, closestY, closestZ);
            float distance = Vector3.Distance(transform.position, closestPoint);

            // Check if the coin and Mario intersect
            if (distance < radius)
            {
                collected = true;
                CollectCoin(mario);
            }
        }
    }

    void CollectCoin(MarioController target)
    {
        target.coins++;
        target.HealPlayer(1);
        Instantiate(particle, transform.position, Quaternion.Euler(-90, 0, 0));
        if (respawns)
        {
            visual.SetActive(false);
            respawnTimer = respawnTime;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
