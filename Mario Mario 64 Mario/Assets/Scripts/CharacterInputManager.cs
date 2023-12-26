using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterInputManager : MonoBehaviour
{
    public PlayerInput input;
    public Transform POV; // the camera/view
    public Vector2 joystickRaw;
    public Vector3 joystick; // joystick relative to POV
    public Vector2 look;
    public Vector2 dPadRaw;
    public Vector3 dPad; // dPad relative to POV
    public playerInput A = new playerInput();
    public playerInput B = new playerInput();
    public playerInput Z = new playerInput();

    private void Update()
    {
    }

    public void CheckInputs()
    {
        if (gameObject.activeInHierarchy)
        {
            joystickRaw = input.actions["Joystick"].ReadValue<Vector2>();
            look = input.actions["Look"].ReadValue<Vector2>();
            //dPadRaw = input.actions["dPad"].ReadValue<Vector2>(); doesn't exist rn

            // Check if the "Attack" action was just pressed or released in the current frame.
            bool attackValue = input.actions["Attack"].ReadValue<float>() > 0;
            B.wasPressedThisTick = input.actions["Attack"].ReadValue<float>() > 0 && !B.pressed;
            B.wasReleasedThisTick = B.pressed && !attackValue;
            B.pressed = attackValue; 

            // Similarly, check "Jump" and "Duck" actions.
            float jumpValue = input.actions["Jump"].ReadValue<float>();
            A.wasPressedThisTick = !A.pressed && jumpValue > 0;
            A.wasReleasedThisTick = A.pressed && jumpValue == 0;
            A.pressed = jumpValue > 0;

            float duckValue = input.actions["Duck"].ReadValue<float>();
            Z.wasPressedThisTick = !Z.pressed && duckValue > 0;
            Z.wasReleasedThisTick = Z.pressed && duckValue == 0;
            Z.pressed = duckValue > 0;

            Vector3 forward = new Vector3(POV.forward.x, 0, POV.forward.z)*15;
            forward.Normalize();

            joystick = POV.forward * joystickRaw.y + POV.right * joystickRaw.x;
            joystick.y = 0f;
            joystick.Normalize();

            dPad = POV.forward * dPadRaw.y + POV.right * dPadRaw.x;
            dPad.y = 0f;
            dPad.Normalize();
        }
    }
}

[Serializable]
public class playerInput
{
    public bool pressed;
    public bool wasPressedThisTick;
    public bool wasReleasedThisTick;
}