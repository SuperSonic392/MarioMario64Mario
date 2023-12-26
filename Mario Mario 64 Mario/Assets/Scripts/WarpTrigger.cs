using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarpTrigger : MonoBehaviour
{
    public float radius;
    public Vector3 target;
    public MarioController[] controllers;
    private GameManager gameManager;
    private void Awake()
    {
        controllers = FindObjectsOfType<MarioController>();
        gameManager = FindObjectOfType<GameManager>();
    }
    // Update is called once per frame
    float timer;
    void Update()
    {
        controllers = gameManager.controllers;
        foreach(var controller in controllers)
        {
            if((controller.transform.position - transform.position).magnitude < radius && controller.currentState != MarioController.CharacterState.WarpOut && controller.groundSpeed == 0)
            {
                controller.Warp(target);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
        Gizmos.DrawWireSphere(target, 1f);
    }
}
