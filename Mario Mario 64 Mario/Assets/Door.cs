using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    public Vector3 hitbox;
    private GameManager gameManager;
    private bool warp;
    private Vector3 destination;
    public Animator anim;
    public AudioSource opSource;
    public AudioSource clSource;
    private float closetimer = -392;
    private bool op;
    public bool camFollowAnyway;

    // Finding references
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>(); // Find a gamemanager
        WarpTrigger warptr = GetComponent<WarpTrigger>();
        if(warptr != null)
        {
            warp = true;
            destination = warptr.target;
            Destroy(warptr);
            warptr = null;
        }
        RaycastHit hit;
        Physics.Raycast(transform.position, Vector3.down, out hit, 2);
        if (hit.collider)
        {
            transform.position = hit.point + (Vector3.up * (hitbox.y/2));
        }
    }

    void FixedUpdate()
    {
        closetimer -= Time.fixedDeltaTime;
        if (closetimer < 0 && op == true)
        {
            op = false;
            clSource.Play();
        }
        foreach (MarioController mario in gameManager.controllers)
        {
            if (mario.currentState == MarioController.CharacterState.WarpOut) return;
            if (mario.currentState == MarioController.CharacterState.WarpIn) return;
            if (mario.currentState == MarioController.CharacterState.Door) return;
            if (!mario.grounded) return;

            // Calculate the door's position and rotation
            Vector3 doorPosition = transform.position;
            Quaternion doorRotation = transform.rotation;

            // Calculate the hitbox transformation based on door rotation
            Matrix4x4 doorMatrix = Matrix4x4.TRS(doorPosition, doorRotation, Vector3.one);
            Vector3 doorForward = doorMatrix.MultiplyVector(Vector3.forward);
            Vector3 doorUp = doorMatrix.MultiplyVector(Vector3.up);

            // Calculate Mario's position
            Vector3 marioPosition = mario.transform.position;
            float marioRadius = mario.radius;
            float marioHeight = mario.height;

            // Calculate the door's hitbox bounds
            Vector3 doorHalfSize = hitbox / 2f;
            Bounds doorBounds = new Bounds(doorPosition, hitbox);

            // Expand the door hitbox to account for Mario's radius
            doorBounds.Expand(marioRadius * 2f);

            // Check if any part of Mario's cylinder is inside the expanded door hitbox
            if (doorBounds.Contains(marioPosition))
            {
                opSource.Play();
                closetimer = 1.3f;
                op = true;
                // Check if Mario is in front of the door
                Vector3 toMario = marioPosition - doorPosition;
                float angle = Vector3.Angle(doorForward, toMario);
                mario.doorTimer = 1.6f;
                if (warp && !camFollowAnyway)
                {

                    mario.cameraTransform.GetComponent<CameraController>().enabled = false;
                    mario.transform.position = transform.position - (Vector3.up * (hitbox.y / 2));
                }
                else
                {
                    mario.transform.position = transform.position - (Vector3.up * (hitbox.y / 2));
                }
                mario.currentState = MarioController.CharacterState.Door;
                mario.model.localRotation = Quaternion.identity;
                if (angle < 90.0f) // Assuming "in front" means within a 90-degree angle
                {
                    RaycastHit hit;
                    Physics.Raycast(transform.position + (doorForward * -2.8f), Vector3.down, out hit, 2);
                    if (hit.collider)
                    {
                        mario.warppos = hit.point;
                    }
                    else
                    {
                        mario.warppos = transform.position + (doorForward * -2.8f) - (Vector3.up * hitbox.y / 2);
                    }
                    mario.doorPush = true;
                    anim.SetTrigger("DoorPush");
                    mario.transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + 180, transform.rotation.eulerAngles.z);
                }
                else
                {
                    RaycastHit hit;
                    Physics.Raycast(transform.position + (doorForward * 2.8f), Vector3.down, out hit, 2);
                    if (hit.collider)
                    {
                        mario.warppos = hit.point;
                    }
                    else
                    {
                        mario.warppos = transform.position + (doorForward * 2.8f) - (Vector3.up * hitbox.y/2);
                    }
                    mario.doorPush = false;
                    anim.SetTrigger("DoorPull");
                    mario.transform.rotation = transform.rotation;
                }
                if (warp)
                {
                    mario.warppos = destination;
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue; // Hitboxes

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.DrawWireCube(Vector3.zero, hitbox);

        // Restore the old transformation matrix
        Gizmos.matrix = oldMatrix;

        Gizmos.color = Color.green; // End positions
        Gizmos.DrawWireSphere(transform.position + (transform.forward * 2.8f), 0.25f);
        Gizmos.DrawWireSphere(transform.position + (transform.forward * -2.8f), 0.25f);
    }
}
