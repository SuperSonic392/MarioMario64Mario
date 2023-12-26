using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Unity.Mathematics;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public MarioController con;
    public float distance = 5.0f;
    public float mouseSensitivity = 100.0f;
    public float controllerSensitivity = 2.0f; // Adjust this value for controller sensitivity
    public float maxYAngle = 80.0f;
    public float zoomSpeed = 2.0f;
    public float minDistance = 1.0f;
    public float maxDistance = 10.0f;

    private float xRotation = 0.0f;
    private float yRotation = 0.0f;
    public LayerMask obstacleMask;
    public bool canzoom;
    public GameObject waterEffect;
    public float aggression;
    public float aggressionTimer;

    public bool mouseLock = true;

    public CharacterInputManager input;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (input.input.currentControlScheme == "Gamepad")
        {
            yRotation = Quaternion.LookRotation(transform.position - target.position, Vector3.up).eulerAngles.y + 180;
        }

        if (Mouse.current.middleButton.wasPressedThisFrame) //soinc utopia style mouse locking
        {
            mouseLock = !mouseLock; //toggle mouselock
            if (mouseLock)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
            }
        }
        if (mouseLock)
        {
            float controllerX;
            float controllerY;
            if (input.input.devices[0].name == "Keyboard")
            {
                controllerX = input.look.x * mouseSensitivity;
                controllerY = input.look.y * mouseSensitivity;
            }
            else
            {
                controllerX = input.look.x * controllerSensitivity;
                controllerY = input.look.y * controllerSensitivity;
            }

            // Update the Y rotation based on the input
            yRotation += controllerX;
            // Calculate the X rotation based on the input and clamp it within the specified range
            xRotation -= controllerY;


            xRotation = Mathf.Clamp(xRotation, -maxYAngle, maxYAngle);
        }
        // Create a Quaternion based on the X and Y rotations
        Quaternion rotation = Quaternion.Euler(xRotation, yRotation, 0f);

        // Zoom in/out with the scroll wheel
        if (canzoom)
        {
            float scrollWheel = Mouse.current.scroll.ReadValue().y;
            distance -= scrollWheel * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);// Calculate the desired camera position
        }

        Vector3 desiredCameraPosition = target.position - rotation * Vector3.forward * distance;

        // Perform a raycast from the target towards the desired camera position
        RaycastHit hit;
        if (Physics.Raycast(target.position, desiredCameraPosition - target.position, out hit, distance, obstacleMask))
        {
            // If the ray hits an obstacle, set the camera position to the hit point
            transform.position = hit.point + (transform.forward * 0.1f);
        }
        else
        {
            // If the ray doesn't hit an obstacle, set the camera position to the desired position
            transform.position = desiredCameraPosition;
        }
        // Set the camera rotation
        transform.rotation = rotation;
    }
}