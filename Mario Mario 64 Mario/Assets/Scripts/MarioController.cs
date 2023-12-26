using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.InputSystem;

public class MarioController : MonoBehaviourPunCallbacks, IPunObservable
{
    /// <summary> Bug List:
    /// seam clip: there's holes in the wall detection that allow the player to slip into pointed corners.
    /// virtical seam clip: I don't know how to articulate this, but you can get to a point where neither the floor or wall detect anything. similar setup to the seam clip
    /// seam bonk boost: bonking during a seam clip sends you flying into the sky.
    /// why is it all related to seams?
    /// </summary>

    [Header("Misc")]
    public int coins;
    public float health = 8;
    public float groundedTime;
    public CharacterData data;
    public PhotonView view;
    public float acceleration = 30f;
    public float deceleration = 40f;
    public bool canTurnMidair = true;
    public GameObject[] particals;
    private Vector3 Horizontal = new Vector3(1, 0, 1);
    private Vector3 Negate = new Vector3(1, -1, 1);
    public Transform cameraTransform;
    public bool grounded = true; // Initially grounded
    private RaycastHit groundHit; // Store the ground hit information
    public float turningSpeed;
    public enum CharacterState
    {
        Walk,
        LowHP,
        Stop,
        Stopped,
        Stoop,
        SideFlip,
        BackFlip,
        AirKick,
        LongJump,
        Bonk,
        WallSlide,
        WallJump,
        GroundPoundStart,
        GroundPound,
        GroundPoundEnd,
        Dive,
        DiveRollout, //leftover from the sm64 style dive
        Door,
        Swim,
        WarpOut,
        WarpIn,
        DoubleJump,
        TripleJump,
        GroundPoundJump,
        Slide
    }
    public CharacterState currentState = CharacterState.Walk;
    [Header("Ground Movement")]
    public float turnSpeed = 10f;
    public float maxGroundVelocity;
    public float groundSpeed = 0f;

    [Header("Air Movement")]
    public Vector3 airVelocity;
    public float maxAirVelocity;

    [Header("Animation")]
    public Animator anim;
    public Transform model;
    public AnimationCurve runningTilt;
    public AnimationCurve runningAnimationSpeed;
    private bool wasGrounded = true; // Variable to keep track of the previous grounded state
    public float height;
    public float radius;
    public bool wall;
    public Vector3 wallangle; // deg
    public LayerMask layerMask;
    public Quaternion targetDirection;
    public float skidSpeed;
    public CharacterInputManager input;
    public SkinnedMeshRenderer[] renderers;
    public GameObject cameraTarget;
    public float incline;
    public Vector3 slopeNormal;

    public Color shirtColor;
    public Color pantsColor;

    public TMP_Text nametag;

    public IngamePlayerListItem template;
    public IngamePlayerListItem myListItem;
    public IngamePlayerList playerlist;

    #region Pun Serialization
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(airVelocity);
            stream.SendNext(transform.rotation.eulerAngles);
            stream.SendNext(groundSpeed);
            stream.SendNext((int)currentState);
            stream.SendNext((string)anim.GetCurrentAnimatorClipInfo(0)[0].clip.name); //flawed
            stream.SendNext(input.joystick);
            stream.SendNext(input.joystickRaw);
            stream.SendNext(myListItem);
            stream.SendNext(input.Z.pressed);
            stream.SendNext(input.B.pressed);
            stream.SendNext(input.A.pressed);
            stream.SendNext(health);
            stream.SendNext(shirtColor.r);
            stream.SendNext(shirtColor.g);
            stream.SendNext(shirtColor.b);
            stream.SendNext(pantsColor.r);
            stream.SendNext(pantsColor.g);
            stream.SendNext(pantsColor.b);
            stream.SendNext(sm.color.enabled);
        }
        else
        {
            transform.position = (Vector3)stream.ReceiveNext();
            airVelocity = (Vector3)stream.ReceiveNext();
            transform.rotation = Quaternion.Euler((Vector3)stream.ReceiveNext());
            groundSpeed = (float)stream.ReceiveNext();
            currentState = (CharacterState)stream.ReceiveNext();
            anim.Play((string)stream.ReceiveNext()); //flawed
            input.joystick = (Vector3)stream.ReceiveNext();
            input.joystickRaw = (Vector2)stream.ReceiveNext();
            myListItem = (IngamePlayerListItem)stream.ReceiveNext();
            input.Z.pressed = (bool)stream.ReceiveNext();
            input.B.pressed = (bool)stream.ReceiveNext();
            input.A.pressed = (bool)stream.ReceiveNext();
            health = (float)stream.ReceiveNext();
            float shirtr = (float)stream.ReceiveNext();
            if(shirtr != shirtColor.r)
            {
                shirtColor.r = shirtr;
                shirtColor.g = (float)stream.ReceiveNext();
                shirtColor.b = (float)stream.ReceiveNext();
                pantsColor.r = (float)stream.ReceiveNext();
                pantsColor.g = (float)stream.ReceiveNext();
                pantsColor.b = (float)stream.ReceiveNext();
                foreach (SkinnedMeshRenderer renderer in renderers)
                {
                    foreach (Material mat in renderer.materials)
                    {
                        mat.SetColor("_Shirt", shirtColor);
                        mat.SetColor("_Overalls", pantsColor);
                        mat.SetFloat("_colorMul", shirtr == 0 ? 0 : 1); //default color uses black
                    }
                }
            }
            else
            { //compensate
                stream.ReceiveNext();
                stream.ReceiveNext();
                stream.ReceiveNext();
                stream.ReceiveNext();
                stream.ReceiveNext();
            }
        }
    }
    #endregion
    SaveManager sm;
    private AudioSource audioSource;
    private Vector3 camTargetOrigin;
    private void Start()
    {
        camTargetOrigin = cameraTarget.transform.localPosition;
        audioSource = GetComponent<AudioSource>();
        sm = FindObjectOfType<SaveManager>();
        nametag.text = view.Owner.NickName;
        if (!PhotonNetwork.OfflineMode)
        {
            if (view.IsMine)
            {
                shirtColor = sm.color.shirt;
                pantsColor = sm.color.pants;
                nametag.enabled = false;
            }
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                foreach (Material mat in renderer.materials)
                {
                    mat.SetColor("_Shirt", shirtColor);
                    mat.SetColor("_Overalls", pantsColor);
                    mat.SetFloat("_colorMul", sm.color.enabled ? 1 : 0);
                }
            }
        }
        view.ObservedComponents.Add(this);
    }
    private void FixedUpdate()
    {
        turningSpeed = 0;
        if(grounded)
        {
            groundedTime += Time.fixedDeltaTime;
        }
        else
        {
            groundedTime = 0;
        }
        cameraTarget.transform.localPosition = camTargetOrigin;
        anim.SetFloat("Vertical Speed", airVelocity.y);
        anim.SetFloat("Health", health);
        anim.SetBool("Grounded", grounded);
        anim.SetInteger("State", (int)currentState);
        if (view.IsMine)
        {
            input.CheckInputs();
        }
        else
        {
            audioSource.spatialBlend = 1;
        }
        switch (currentState)
        {
            case CharacterState.Walk:
                WalkState();
                break;
            case CharacterState.Stop:
                SkidState();
                break;
            case CharacterState.Stopped:
                SkiddedState();
                break;
            case CharacterState.Stoop:
                StoopState();
                break;
            case CharacterState.SideFlip:
                SlideFlipState(false);
                break;
            case CharacterState.WarpOut:
                WarpOutState();
                break;
            case CharacterState.WarpIn:
                WarpInState();
                break;
            case CharacterState.LongJump:
                LongJumpState();
                break;
            case CharacterState.Bonk:
                BonkState();
                break;
            case CharacterState.BackFlip:
                SlideFlipState(true);
                break;
            case CharacterState.WallSlide:
                WallSlideState();
                break;
            case CharacterState.AirKick:
                AirKickState();
                break;
            case CharacterState.GroundPoundStart:
                GroundPoundStartState();
                break;
            case CharacterState.GroundPound:
                GroundPoundState();
                break;
            case CharacterState.GroundPoundEnd:
                GroundPoundEndState();
                break;
            case CharacterState.Dive:
                DiveState();
                break;
            case CharacterState.DiveRollout:
                WalkState();
                break;
            case CharacterState.Door:
                DoorState();
                break;
            case CharacterState.Swim:
                SwimState();
                break;
            case CharacterState.LowHP:
                WalkState();
                break;
            case CharacterState.WallJump: 
                WallJumpState();
                break;
            case CharacterState.DoubleJump:
                DoubleJumpState();
                break;
            case CharacterState.TripleJump:
                DoubleJumpState();
                break;
            case CharacterState.GroundPoundJump:
                model.transform.rotation = Quaternion.Euler(0, airVelocity.y / 20 * 360, 0);
                SlideFlipState(false); 
                break;
            case CharacterState.Slide:
                SlopeState();
                break;
        }
        if (transform.position.y <= -250 && currentState != CharacterState.WarpOut)
        {
            currentState = CharacterState.WarpOut;
            airVelocity = Vector3.zero;
            timer = 1f;
        }
        if(airVelocity.y < -30)
        {
            airVelocity.y = -30;
        }
        wall = false;
        cameraTarget.transform.rotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
        VolumeData vol = GameManager.Instance.CheckVolume(Volume.VolumeType.Water, transform.position);
        if(vol != null)
        {
            if (transform.position.y <= vol.surface - (height / 2) && (currentState != CharacterState.Swim))
            {
                airVelocity = airVelocity / 2;
                anim.SetTrigger("Wat");
                currentState = CharacterState.Swim;
                current = vol.current;
                density = vol.density;
                currentVel = Vector3.zero;
            }
        }
        if (GameManager.Instance.CheckVolume(Volume.VolumeType.Gas, transform.position+(Vector3.up * (height/2))) != null)
        {
            health -= Time.fixedDeltaTime;
        }
        if(health <= 0)
        {
            health = 8;
            transform.position = Vector3.zero;
        }
        health = Mathf.Min(health, 8);
        if (grounded)
        {
            if (currentState == CharacterState.WarpIn || currentState == CharacterState.WarpOut || currentState == CharacterState.Door)
                return;
            transform.position += Vector3.down * 0.1f;
        }
    }
    public void SlopeState()
    {
        grounded = CheckGrounded();
        if (CheckWall(0))
        {
            Bonk();
        }
        airVelocity.y = 0;
        airVelocity += multiplyVector3(slopeNormal, Negate) * 0.5f;
        airVelocity += input.joystick * 0.25f;
        if (input.A.wasPressedThisTick)
        {
            airVelocity.y = 15;
            grounded = false;
            wasGrounded = false;
        }
        Move(airVelocity * Time.fixedDeltaTime + (Vector3.down * 0.1f));
        if(!grounded)
        {
            currentState = CharacterState.Walk;
        }
    }
    public void DoubleJumpState()
    {
        grounded = CheckGrounded();
        if (grounded)
        {
            airVelocity.y = 0;
            if(input.Z.pressed)
            {
                currentState = CharacterState.Stoop;
                StoopState();
                return;
            }
            groundSpeed = multiplyVector3(airVelocity, Horizontal).magnitude;
            transform.rotation = quaternion.LookRotation(multiplyVector3(airVelocity, Horizontal), Vector3.up);
            if (currentState == CharacterState.TripleJump)
            {
                currentState = CharacterState.Walk;
                if (input.joystick.magnitude == 0)
                {
                    groundSpeed = 0;
                    playVoice("TripleLand");
                }
                Move(transform.forward * groundSpeed * Time.fixedDeltaTime);
                groundedTime = 999;
                return;
            }
            if(groundedTime > 0.15)
            {
                currentState = CharacterState.Walk;
            }
            else
            {
                if(groundSpeed < 2)
                {
                    if (CheckJump())
                    {
                        currentState = CharacterState.Walk;
                    }
                }
                else
                {
                    if (input.A.wasPressedThisTick)
                    {
                        currentState = CharacterState.TripleJump;
                        playVoice("3Jump");
                        airVelocity.y = 20;
                        grounded = false;
                    }
                }
            }
            Move(transform.forward * groundSpeed * Time.fixedDeltaTime);
            CheckWall(0);
        }
        else
        {
            if (CheckCeil())
            {
                airVelocity.y = -1f;
            }
            airVelocity += Physics.gravity * Time.fixedDeltaTime;
            if (input.A.wasReleasedThisTick && airVelocity.y > 5)
            {
                airVelocity.y = 5;
            }
            model.localRotation = Quaternion.identity;
            // Convert air velocity to ground speed
            groundSpeed = new Vector3(airVelocity.x, 0, airVelocity.z).magnitude;

            airVelocity += input.joystick * 25f * Time.fixedDeltaTime;
            Vector2 tmp = new Vector2(airVelocity.x, airVelocity.z);
            // Ensure that the magnitude of airVelocity does not exceed maxAirVelocity
            if (tmp.magnitude > maxAirVelocity)
            {
                Vector3 dec = -airVelocity.normalized * (30f);
                dec.y = 0;
                airVelocity += dec * Time.fixedDeltaTime;
            }

            // Apply air velocity to the position directly
            Move(airVelocity * Time.fixedDeltaTime);
            CheckWall(0);
            if (wall)
            {
                if (airVelocity.y < 0)
                {
                    transform.rotation = Quaternion.LookRotation(-wallangle, Vector3.up);
                    transform.rotation = quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
                    currentState = CharacterState.WallSlide;
                }
            }
            else
            {
                if (canTurnMidair)
                {
                    if (input.joystick.magnitude > 0)
                    {
                        targetDirection = Quaternion.LookRotation(input.joystick.normalized);
                    }
                    transform.rotation = Quaternion.Lerp(targetDirection, transform.rotation, 0.5f);
                }
            }
            CheckGroundPound();
            CheckAirKick();
        }
        anim.SetBool("Grounded", grounded);
        anim.SetFloat("Hstick", input.joystickRaw.magnitude);
        wasGrounded = grounded;
    }
    public void WallJumpState()
    {
        if (CheckCeil())
        {
            airVelocity.y = -1f;
        }
        CheckWall(0);
        if (wall && airVelocity.y < 5)
        {
            transform.rotation = Quaternion.LookRotation(-wallangle, Vector3.up);
            transform.rotation = quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            currentState = CharacterState.WallSlide;
        }
        //calculate the joystick input relative to the camera direction
        Vector3 inputDirection = input.joystick;
        airVelocity += Physics.gravity * Time.fixedDeltaTime;
        model.localRotation = Quaternion.identity;
        // Convert air velocity to ground speed
        groundSpeed = new Vector3(airVelocity.x, 0, airVelocity.z).magnitude;

        airVelocity += inputDirection * 10f * Time.fixedDeltaTime;
        Vector2 tmp = new Vector2(airVelocity.x, airVelocity.z);
        // Ensure that the magnitude of airVelocity does not exceed maxAirVelocity
        if (tmp.magnitude > maxAirVelocity)
        {
            Vector3 dec = -airVelocity.normalized * (20f);
            dec.y = 0;
            airVelocity += dec * Time.fixedDeltaTime;
        }
        Move(airVelocity * Time.fixedDeltaTime);
        transform.rotation = Quaternion.LookRotation(multiplyVector3(airVelocity, Horizontal), Vector3.up);
        grounded = CheckGrounded();
        if (grounded)
        {
            currentState = CharacterState.Walk;
            if (new Vector2(airVelocity.x, airVelocity.z).magnitude > 0)
            {
                transform.rotation = Quaternion.LookRotation(airVelocity.normalized);
                transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            }
        }
        CheckGroundPound();
        CheckAirKick();
    }
    private Vector3 current;
    private float density;
    private Vector3 currentVel;
    public void SwimState()
    {
        model.localRotation = Quaternion.identity;
        Vector3 curr = current;
        curr.y = 0;
        currentVel += current * Time.fixedDeltaTime;
        currentVel.y = 0;
        if(currentVel.magnitude > curr.magnitude)
        {
            currentVel = curr;
        }
        airVelocity.y += current.y * Time.fixedDeltaTime;
        Move(currentVel * Time.fixedDeltaTime);
        anim.SetFloat("Horizontal Speed", groundSpeed);
        if (grounded)
        {
            airVelocity.y = 0;
            CheckWall(height * 0.45f);
            // Calculate the target rotation based on the input direction
            Quaternion targetRotation = Quaternion.identity;
            if (input.joystick != Vector3.zero)
                targetRotation = Quaternion.LookRotation(input.joystick);

            float angle = Vector3.Angle(input.joystick, transform.forward);
            // Calculate the acceleration and deceleration values based on the input direction and angle
            float currentAcceleration = 0f;
            float currentmaxspeed = 5;

            if (input.joystick.magnitude > 0f)
            {
                if (groundSpeed < currentmaxspeed)
                {
                    if (angle < 90f)
                    {
                        currentAcceleration = acceleration / density;
                    }
                    else
                    {
                        currentAcceleration = -deceleration;
                        if (groundSpeed < 0)
                        {
                            currentAcceleration = -currentAcceleration;
                        }
                    }
                }
                else
                {
                    currentAcceleration = -deceleration;
                    if (groundSpeed < 0)
                    {
                        currentAcceleration = -currentAcceleration;
                    }
                }
            }
            else
            {
                // Decelerate when there's no input
                currentAcceleration = -5;
                if (groundSpeed < 0)
                {
                    currentAcceleration = -currentAcceleration;
                }
            }
            // Calculate the scaled turn speed based on the current ground speed
            float scaledTurnSpeed = turnSpeed / density / Mathf.Max(1f, groundSpeed);
            if (input.joystick != Vector3.zero)
                targetRotation = Quaternion.LookRotation(input.joystick);
            // Smoothly rotate towards the target rotation using the scaled turn speed
            if (input.joystick.magnitude > 0)
            {
                if (groundSpeed == 0)
                {
                    transform.rotation = targetRotation;
                }
                else
                {
                    turningSpeed = Quaternion.Slerp(transform.rotation, targetRotation, scaledTurnSpeed * Time.fixedDeltaTime).eulerAngles.y - transform.rotation.eulerAngles.y;
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, scaledTurnSpeed * Time.fixedDeltaTime);
                }
            }
            if (Mathf.Abs(groundSpeed) < 0.1 && input.joystick.magnitude == 0)
            {
                currentAcceleration = 0;
                groundSpeed = 0;
            }
            targetDirection = transform.rotation;
            groundSpeed += currentAcceleration * Time.fixedDeltaTime;
            Move(transform.forward * groundSpeed * Time.fixedDeltaTime);
            grounded = CheckGrounded(); 
            if (!grounded)
            {
                airVelocity = transform.forward * groundSpeed;
            }
            if (input.A.wasPressedThisTick)
            {
                airVelocity = transform.forward * groundSpeed;
                airVelocity.y = 5;
                grounded = false;
                anim.SetTrigger("Swim");
                playSound("Swim");
            }
        }
        else
        {
            airVelocity += input.joystick * (acceleration / (density/3)) * Time.fixedDeltaTime;
            Vector2 tmp = new Vector2(airVelocity.x, airVelocity.z);
            // Ensure that the magnitude of airVelocity does not exceed currentMaxVel
            float currentMaxVel = 8;
            if (tmp.magnitude > currentMaxVel)
            {
                Vector3 dec = -airVelocity.normalized * (30f);
                dec.y = 0;
                airVelocity += dec * Time.fixedDeltaTime;
            }
            airVelocity += Physics.gravity / density * Time.fixedDeltaTime;
            if (airVelocity.y < -15)
            {
                airVelocity.y = -15;
            }
            Move(airVelocity * Time.fixedDeltaTime);
            if (input.A.wasPressedThisTick)
            {
                airVelocity.y += 24 / density;
                airVelocity.y = Mathf.Min(airVelocity.y, 23 / density);
                anim.SetTrigger("Swim");
                playSound("Swim");
            }
            if (CheckCeil())
            {
                airVelocity.y = -airVelocity.y;
            }
            CheckWall(0);
            if (canTurnMidair)
            {
                if (input.joystickRaw.magnitude > 0)
                {
                    targetDirection = Quaternion.LookRotation(input.joystick.normalized);
                }
                transform.rotation = Quaternion.Lerp(targetDirection, transform.rotation, 0.5f);
            }
            grounded = CheckGrounded();
            if (grounded)
            {
                airVelocity.y = 0;
                groundSpeed = airVelocity.magnitude;
                if(multiplyVector3(airVelocity, Horizontal).magnitude != 0)
                {
                    transform.rotation = Quaternion.LookRotation(airVelocity.normalized);
                    transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
                }
            }
        }
        VolumeData vol = GameManager.Instance.CheckVolume(Volume.VolumeType.Water, transform.position + (Vector3.up * height / 2));
        if (vol == null)
        {
            airVelocity = transform.forward * 5;
            airVelocity.y = 12;
            currentState = CharacterState.Walk;
            Vector3 pos = transform.position;
            pos.y = vol.surface;
            transform.position = pos;
            anim.SetTrigger("Jump"); //play the jump animation so we don't swim midair
        }
        anim.SetInteger("State", (int)currentState);
        anim.SetBool("Grounded", grounded);
    }

    public bool doorPush; //whether we push the door or pull it
    public float doorTimer;
    public Transform doorCamPos;
    public void DoorState()
    {
        cameraTarget.transform.position = doorCamPos.position;// + (Vector3.up * 0.75f);
        Vector3 pos = cameraTarget.transform.localPosition;
        pos.y = camTargetOrigin.y;
        cameraTarget.transform.localPosition = pos;
        anim.SetBool("Grounded", doorPush); //repurposing grounded for what animation we should play
        doorTimer -= Time.fixedDeltaTime;
        if(doorTimer <= 0)
        {
            transform.position = warppos; //another repurposed variable
            currentState = CharacterState.Walk;
            airVelocity = Vector3.zero;
            groundSpeed = 0;
            anim.SetBool("Grounded", true); //make sure we don't play the landing animation
            cameraTransform.GetComponent<CameraController>().enabled = true;
            cameraTarget.transform.localPosition = camTargetOrigin;

        }
        anim.SetInteger("State", (int)currentState);
    }
    public void DiveState()
    {
        if (timer == 0)
        {
            if (input.joystick.magnitude > 0)
            {
                transform.rotation = Quaternion.LookRotation(input.joystick.normalized);
            }
            airVelocity = transform.forward * maxAirVelocity*1.2f;
            airVelocity.y = 10;
        }
        timer += Time.fixedDeltaTime;
        if (grounded)
        {
            currentState = CharacterState.Walk;
            if(input.joystick.magnitude == 0)
            {
                groundSpeed = 0;
            }
            Move(transform.forward * groundSpeed * Time.fixedDeltaTime);
        }
        else
        {
            if(airVelocity.magnitude > 0)
            {
                transform.rotation = quaternion.LookRotation(airVelocity, Vector3.up);
                transform.rotation = quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            }
            model.transform.rotation = quaternion.LookRotation(new Vector3(airVelocity.x, airVelocity.y/2, airVelocity.z), Vector3.up);
            groundSpeed = multiplyVector3(airVelocity, Horizontal).magnitude;
            airVelocity += Physics.gravity * Time.fixedDeltaTime;
            Move(airVelocity * Time.fixedDeltaTime);
            if (CheckCeil())
            {
                airVelocity.y = -1f;
            }
            grounded = CheckGrounded();
            if (grounded)
            {
                transform.rotation = Quaternion.LookRotation(airVelocity.normalized);
                transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            }
            airVelocity += input.joystick * 15f * Time.fixedDeltaTime;
            Vector2 tmp = new Vector2(airVelocity.x, airVelocity.z);
            // Ensure that the magnitude of airVelocity does not exceed maxAirVelocity
            if (tmp.magnitude > maxAirVelocity * 1.2f)
            {
                Vector3 dec = -airVelocity.normalized * (30f);
                dec.y = 0;
                airVelocity += dec * Time.fixedDeltaTime;
            }
        }
        if (CheckWall(0))
        {
            Bonk();
        }
    }
    public void GroundPoundEndState()
    {
        timer += Time.fixedDeltaTime;
        if(timer >= 0.25f)
        {
            currentState = CharacterState.Walk;
        }
        if (input.A.wasPressedThisTick)
        {
            currentState = CharacterState.GroundPoundJump;
            airVelocity.y = 20; //groundpound jump
            playVoice("3Jump");
            timer = 0f;
            grounded = false;
        }
    }
    public void GroundPoundState()
    {
        airVelocity.y = -30;
        Move(airVelocity*1.25f * Time.fixedDeltaTime);
        if (CheckGrounded())
        {
            timer = 0f;
            currentState = CharacterState.GroundPoundEnd;
            playSound("Hiped");
            groundSpeed = 0;
        }
        if (input.B.wasPressedThisTick)
        {
            timer = 0f;
            currentState = CharacterState.Dive;
            playVoice("Dive");
        }
    }
    public void GroundPoundStartState()
    {
        if(timer == 0)
        {
            airVelocity.y = 5f;
        }
        timer += Time.fixedDeltaTime;
        airVelocity.y -= 15f * Time.fixedDeltaTime;
        if(airVelocity.y <= 0 && timer >= 0.25f)
        {
            currentState = CharacterState.GroundPound;
        }
        if(airVelocity.y > 0)
            Move(airVelocity * Time.fixedDeltaTime);
        if (input.B.wasPressedThisTick)
        {
            timer = 0f;
            currentState = CharacterState.Dive;
            playVoice("Dive");
        }
    }
    public void AirKickState()
    {
        if (CheckCeil())
        {
            airVelocity.y = -1f;
        }
        CheckWall(0);
        if (wall && airVelocity.y < 0)
        {
            transform.rotation = Quaternion.LookRotation(-wallangle, Vector3.up);
            transform.rotation = quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            currentState = CharacterState.WallSlide;
        }
        //calculate the joystick input relative to the camera direction
        Vector3 inputDirection = input.joystick;
        airVelocity += Physics.gravity * Time.fixedDeltaTime;
        model.localRotation = Quaternion.identity;
        // Convert air velocity to ground speed
        groundSpeed = new Vector3(airVelocity.x, 0, airVelocity.z).magnitude;

        airVelocity += inputDirection * 15f * Time.fixedDeltaTime;
        Vector2 tmp = new Vector2(airVelocity.x, airVelocity.z);
        // Ensure that the magnitude of airVelocity does not exceed maxAirVelocity
        if (tmp.magnitude > maxAirVelocity)
        {
            Vector3 dec = -airVelocity.normalized * (18f);
            dec.y = 0;
            airVelocity += dec * Time.fixedDeltaTime;
        }
        Move(airVelocity * Time.fixedDeltaTime);
        grounded = CheckGrounded();
        if (grounded)
        {
            currentState = CharacterState.Walk;
            if (new Vector2(airVelocity.x, airVelocity.z).magnitude > 0)
            {
                transform.rotation = Quaternion.LookRotation(airVelocity.normalized);
                transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            }
        }
        CheckGroundPound();
    }

    public Vector3 warppos;
    float timer = 1f;
    public void WallSlideState()
    {
        airVelocity = Vector3.up * airVelocity.y;
        airVelocity += Physics.gravity / 4 * Time.fixedDeltaTime;
        if(airVelocity.y < -15)
        {
            airVelocity.y = -15;
        }
        float angle = Vector3.Angle(input.joystick, transform.forward);
        if (angle > 90f)
        {
            currentState = CharacterState.Walk;
            airVelocity = input.joystick;
        }
        RaycastHit hit;
        Physics.Raycast(transform.position + (Vector3.up * height/2), transform.forward, out hit, Mathf.Infinity, layerMask);
        Move(airVelocity * Time.fixedDeltaTime + (-hit.normal * 0.25f));
        CheckWall(0);
        if (hit.collider) { transform.rotation = Quaternion.LookRotation(-hit.normal, Vector3.up); } else
        {
            transform.rotation = Quaternion.Euler(input.joystick);
            Debug.Log(hit.normal);
        }
        
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        grounded = CheckGrounded();
        if (grounded || !wall)
        {
            currentState = CharacterState.Walk;
            WalkState();
            return;
        }
        if(input.A.wasPressedThisTick)
        {
            currentState = CharacterState.WallJump;
            playVoice("Jump" + UnityEngine.Random.Range(1, 3));
            if (hit.collider)
            {
                transform.rotation = Quaternion.LookRotation(hit.normal, Vector3.up);
            }
            else
            {
                transform.rotation = Quaternion.Euler(-input.joystick);
            }
            airVelocity = transform.forward * maxAirVelocity * 0.75f;
            airVelocity.y = 15;
        }
    }
    public void BonkState()
    {
        model.transform.localRotation = quaternion.identity;
        timer -= Time.fixedDeltaTime;
        if (timer <= 0f && airVelocity.y <= 0)
        {
            currentState = CharacterState.Walk;
        }
        Vector3 velHor = multiplyVector3(airVelocity, Horizontal);
        if (grounded)
        {
            grounded = CheckGrounded();
            groundSpeed += 15 * Time.fixedDeltaTime;
            if(groundSpeed >= 0)
            {
                currentState = CharacterState.Walk; //todo: add recover state
            }
            airVelocity = transform.forward * groundSpeed;
            Move(airVelocity * Time.fixedDeltaTime);
        }
        else
        {
            grounded = CheckGrounded();
            if (grounded)
            {
                groundSpeed = -velHor.magnitude;
                playVoice("BonkLand");
            }
            airVelocity += Physics.gravity * Time.fixedDeltaTime;
            Move(airVelocity * Time.fixedDeltaTime);
        }
        anim.SetBool("Grounded", grounded);
        CheckCeil();
        CheckWall(0);
        if (wall && !grounded)
        {
            airVelocity = Vector3.Reflect(airVelocity.normalized * 5, wallangle) + (Vector3.up * airVelocity.y) ;
            transform.rotation = Quaternion.LookRotation(-airVelocity.normalized);
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        }
    }
    public void Bonk()
    {
        playVoice("Hurt");
        airVelocity = Vector3.Reflect(airVelocity.normalized * 5, wallangle);
        currentState = CharacterState.Bonk;
        airVelocity.y = 5;
        grounded = false;
        Move(airVelocity * Time.fixedDeltaTime);
        transform.rotation = Quaternion.LookRotation(-airVelocity.normalized);
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        timer = 1;
    }
    public void LongJumpState()
    {
        
        //calculate the joystick input relative to the camera direction
        Vector3 inputDirection = input.joystick;
        airVelocity += Physics.gravity/1.25f * Time.fixedDeltaTime;
        model.localRotation = Quaternion.identity;
        // Convert air velocity to ground speed
        groundSpeed = new Vector3(airVelocity.x, 0, airVelocity.z).magnitude;
        airVelocity += inputDirection * 25f * Time.fixedDeltaTime;
        Vector2 tmp = new Vector2(airVelocity.x, airVelocity.z);
        // Ensure that the magnitude of airVelocity does not exceed maxAirVelocity
        if (tmp.magnitude > maxAirVelocity*1.5f)
        {
            Vector3 dec = -airVelocity.normalized * (30f);
            dec.y = 0;
            airVelocity += dec * Time.fixedDeltaTime;
        }
        Move(airVelocity * Time.fixedDeltaTime);
        grounded = CheckGrounded();
        if (grounded)
        {
            currentState = CharacterState.Stoop;
            if (new Vector2(airVelocity.x, airVelocity.z).magnitude > 0)
            {
                transform.rotation = Quaternion.LookRotation(airVelocity.normalized);
                transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            }
        }
        if (CheckCeil())
        {
            airVelocity.y = -1f;
        }
        if (CheckWall(0))
        {
            Bonk();
            return;
        }
        anim.SetBool("Grounded", grounded);
        CheckAirKick();
    }
    public void Warp(Vector3 target)
    {
        timer = 1f;
        currentState = CharacterState.WarpOut;
        warppos = target;
    }
    public void WarpOutState()
    {
        anim.speed = 0;
        grounded = true;
        model.localRotation = Quaternion.identity;
        timer -= Time.fixedDeltaTime;
        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                mat.SetFloat("_alpha", timer*2-1f);
            }
        }
        if (timer <= 0)
        {
            timer = 1f;
            transform.position = warppos;
            currentState = CharacterState.WarpIn;
        }
    }
    public void WarpInState()
    {
        anim.speed = 0;
        grounded = true;
        timer -= Time.fixedDeltaTime;
        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                mat.SetFloat("_alpha", 1-(timer*2));
            }
        }
        if (timer <= 0)
        {
            currentState = CharacterState.Walk;
            anim.speed = 1;
        }
    }
    public void StoopState()
    {
        airVelocity.y = 0;
        anim.SetFloat("Horizontal Speed", groundSpeed);
        anim.SetBool("Grounded", true);
        anim.SetInteger("State", 4);
        bool cramped = CheckCeil();
        grounded = true;
        if(!CheckWall(0) || (!input.Z.pressed && !cramped))
        {
            currentState = CharacterState.Walk;
        }
        float currentAcceleration = -deceleration;
        if (groundSpeed < 0)
        {
            currentAcceleration = -currentAcceleration;
        }
        // Calculate the scaled turn speed based on the current ground speed
        float scaledTurnSpeed = turnSpeed / Mathf.Max(1f, groundSpeed);
        Quaternion targetRotation = Quaternion.identity;
        if (input.joystick != Vector3.zero)
            targetRotation = Quaternion.LookRotation(input.joystick);
        // Smoothly rotate towards the target rotation using the scaled turn speed
        if (input.joystick.magnitude > 0)
        {
            if (groundSpeed == 0)
            {
                //transform.rotation = targetRotation;
            }
            else
            {
                turningSpeed = Quaternion.Slerp(transform.rotation, targetRotation, scaledTurnSpeed * Time.fixedDeltaTime).eulerAngles.y - transform.rotation.eulerAngles.y;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, scaledTurnSpeed * Time.fixedDeltaTime);
            }
        }
        if (Mathf.Abs(groundSpeed) < 0.1)
        {
            currentAcceleration = 0;
            if (cramped)
            {
                groundSpeed = 1f;
            }
            else
            {
                groundSpeed = 0;
            }
        }
        else
        {
            Instantiate(particals[0], transform.position, Quaternion.Euler(-90, 0, 0));
            if (CheckWall(0) && groundSpeed > 1)
            {
                Bonk();
                return;
            }
        }
        if (input.A.wasPressedThisTick)
        {
            if(groundSpeed >= skidSpeed)
            {
                if(groundSpeed < maxGroundVelocity*2)
                    groundSpeed *= 1.2f;
                airVelocity = transform.forward * groundSpeed;
                airVelocity.y = 12;
                playVoice("Broad");
                currentState = CharacterState.LongJump;
            }
            else
            {
                groundSpeed = -5f;
                airVelocity = transform.forward * groundSpeed;
                airVelocity.y = 20;
                currentState = CharacterState.BackFlip;
                playVoice("Jump" + UnityEngine.Random.Range(1, 3));
            }
        }
        // Adjust the current speed towards the target speed
        groundSpeed += currentAcceleration * Time.fixedDeltaTime;
        model.localRotation = Quaternion.identity;
        Move(transform.forward * groundSpeed * Time.fixedDeltaTime);
    }
    public void SlideFlipState(bool back)
    {
        if (CheckCeil())
        {
            airVelocity.y = -1f;
        }
        CheckWall(0);
        if (wall && airVelocity.y < 0)
        {
            transform.rotation = Quaternion.LookRotation(-wallangle, Vector3.up);
            transform.rotation = quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            currentState = CharacterState.WallSlide;
        }
        //calculate the joystick input relative to the camera direction
        Vector3 inputDirection = input.joystick;
        airVelocity += Physics.gravity * Time.fixedDeltaTime;
        model.localRotation = Quaternion.identity;
        // Convert air velocity to ground speed
        groundSpeed = new Vector3(airVelocity.x, 0, airVelocity.z).magnitude;

        airVelocity += inputDirection * 15f * Time.fixedDeltaTime;
        Vector2 tmp = new Vector2(airVelocity.x, airVelocity.z);
        // Ensure that the magnitude of airVelocity does not exceed maxAirVelocity
        if (tmp.magnitude > maxAirVelocity)
        {
            Vector3 dec = -airVelocity.normalized * (18f);
            dec.y = 0;
            airVelocity += dec * Time.fixedDeltaTime;
        }
        Move(airVelocity * Time.fixedDeltaTime);
        grounded = CheckGrounded();
        if (grounded)
        {
            if (back && input.joystickRaw.magnitude < 0.75f)
            {
                airVelocity = Vector3.zero;
            }
            currentState = CharacterState.Walk;
            if (new Vector2(airVelocity.x, airVelocity.z).magnitude > 0)
            {
                transform.rotation = Quaternion.LookRotation(airVelocity.normalized);
                transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            }
        }
        CheckGroundPound();
        CheckAirKick();
    }
    public void SkiddedState()
    {
        model.localRotation = Quaternion.identity;
        grounded = CheckGrounded();
        if(!grounded)
        {
            RaycastHit hit;
            Physics.Raycast(transform.position, Vector3.down, out hit, 1f);
            if (hit.collider != null)
            {
                transform.position = hit.point;
                grounded = true;
            }
            else
            {
                currentState = CharacterState.Walk;
            }
        }
        float angle = Vector3.Angle(input.joystick, transform.forward);
        airVelocity = new Vector3(transform.forward.x * groundSpeed, 0, transform.forward.z * groundSpeed);
        CheckWall(0);
        CheckWall(height * 0.45f);
        groundSpeed -= deceleration * 2 * Time.fixedDeltaTime;
        if (CheckSideflip())
        {
            Move(airVelocity * Time.deltaTime);
            return;
        }
        Move(transform.forward * groundSpeed * Time.fixedDeltaTime);
        float scaledTurnSpeed = turnSpeed / Mathf.Max(1f, -groundSpeed);
        Quaternion targetRotation = Quaternion.identity;
        if (input.joystick != Vector3.zero)
            targetRotation = Quaternion.LookRotation(-input.joystick);
        // Smoothly rotate towards the target rotation using the scaled turn speed
        if (input.joystick.magnitude > 0)
        {
            turningSpeed = Quaternion.Slerp(transform.rotation, targetRotation, scaledTurnSpeed * Time.fixedDeltaTime).eulerAngles.y - transform.rotation.eulerAngles.y;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, scaledTurnSpeed * Time.fixedDeltaTime);
        }
        targetDirection = transform.rotation;
        if (groundSpeed <= -maxGroundVelocity || angle < 90f || input.Z.pressed)
        {
            currentState = CharacterState.Walk;
            transform.rotation = Quaternion.LookRotation(-transform.forward);
            groundSpeed = -groundSpeed;
        }
    }
    public void SkidState()
    {
        
        float angle = Vector3.Angle(input.joystick, transform.forward);
        if (angle < 90f)
        {
            currentState = CharacterState.Walk;
        }
        model.localRotation = Quaternion.identity;
        anim.SetInteger("State", 2);
        grounded = CheckGrounded();

        if (!grounded)
        {
            RaycastHit hit;
            Physics.Raycast(transform.position, Vector3.down, out hit, 1f);
            if (hit.collider != null)
            {
                transform.position = hit.point;
                grounded = true;
            }
            else
            {
                currentState = CharacterState.Walk;
            }
        }
        airVelocity = new Vector3(transform.forward.x * groundSpeed, 0, transform.forward.z * groundSpeed);
        Instantiate(particals[0], transform.position, Quaternion.Euler(-90, 0, 0));
        if(CheckWall(0))
        {
            Bonk();
            return;
        }
        CheckWall(height * 0.45f);
        CheckSideflip();
        groundSpeed -= deceleration * 4 * Time.fixedDeltaTime;
        Move(transform.forward * groundSpeed * Time.fixedDeltaTime);
        if (groundSpeed < 0)
        {
            currentState = CharacterState.Stopped;
            transform.rotation = Quaternion.LookRotation(-input.joystick.normalized);
        }
    }
    public void WalkState()
    {
        grounded = CheckGrounded();
        if(multiplyVector3(slopeNormal, Horizontal).magnitude > 0.5 && grounded)
        {
            currentState = CharacterState.Slide;
        }
        if(grounded && groundSpeed == 0 && health <= 2) 
        {
            currentState = CharacterState.LowHP;
        }
        else if(currentState == CharacterState.LowHP)
        {
            currentState = CharacterState.Walk;
        }

        // Calculate the joystick input relative to the camera direction
        Vector3 inputDirection = input.joystick;
        if (!grounded)
        {
            if (wasGrounded)
            {
                RaycastHit hit;
                Physics.Raycast(transform.position, Vector3.down, out hit, 1f);
                if (hit.collider != null)
                {
                    transform.position = hit.point;
                    grounded = true;
                }
            }
        }
        if (grounded)
        {
            if (input.Z.pressed)
            {
                currentState = CharacterState.Stoop;
                StoopState();
                anim.SetBool("Grounded", true);
                return;
            }
            CheckWall(height*0.45f);
            // Calculate the target rotation based on the input direction
            Quaternion targetRotation = Quaternion.identity;
            if (inputDirection != Vector3.zero)
                targetRotation = Quaternion.LookRotation(inputDirection);

            if (!wasGrounded)
            {
                // Invoke the OnLanded event if needed

                // Face in the direction of air velocity when landing
                if (new Vector2(airVelocity.x, airVelocity.z).magnitude > 0)
                {
                    transform.rotation = Quaternion.LookRotation(airVelocity.normalized);
                    transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
                }
            }
            float angle = Vector3.Angle(inputDirection, transform.forward);
            // Calculate the acceleration and deceleration values based on the input direction and angle
            float currentAcceleration = 0f;
            float currentmaxspeed = maxGroundVelocity;
            if(input.joystickRaw.magnitude < 0.99f)
            {
                currentmaxspeed = currentmaxspeed / 2;
            }
            if (input.joystickRaw.magnitude < 0.2f)
            {
                currentmaxspeed = currentmaxspeed / 2;
            }

            if (inputDirection.magnitude > 0f)
            {
                if (groundSpeed < currentmaxspeed)
                {
                    if (angle < 90f)
                    {
                        currentAcceleration = acceleration;
                    }
                    else
                    {
                        currentAcceleration = -deceleration * 2;
                        if (groundSpeed < 0)
                        {
                            currentAcceleration = -currentAcceleration;
                        }
                        if (groundSpeed > skidSpeed && (angle > 120) && input.joystickRaw.magnitude > 0.99f)
                        {
                            currentState = CharacterState.Stop;
                            transform.rotation = Quaternion.LookRotation(-inputDirection.normalized);
                            Move(transform.forward * groundSpeed * Time.fixedDeltaTime);
                            return;
                        }
                    }
                }
                else
                {
                    currentAcceleration = -deceleration;
                    if (groundSpeed < 0)
                    {
                        currentAcceleration = -currentAcceleration;
                    }
                    if (groundSpeed > skidSpeed && (angle > 145))
                    {
                        currentState = CharacterState.Stop;
                        transform.rotation = Quaternion.LookRotation(-inputDirection.normalized);
                        Move(transform.forward * groundSpeed * Time.fixedDeltaTime);
                        return;
                    }
                }
            }
            else
            {
                // Decelerate when there's no input
                currentAcceleration = -deceleration;
                if (groundSpeed < 0)
                {
                    currentAcceleration = -currentAcceleration;
                }
            }
            // Calculate the scaled turn speed based on the current ground speed
            float scaledTurnSpeed = turnSpeed / Mathf.Max(1f, groundSpeed);

            // Smoothly rotate towards the target rotation using the scaled turn speed
            if (inputDirection.magnitude > 0)
            {
                if (groundSpeed == 0)
                {
                    transform.rotation = targetRotation;
                    groundSpeed = .25f;
                }
                else
                {
                    turningSpeed = Quaternion.Slerp(transform.rotation, targetRotation, scaledTurnSpeed * Time.fixedDeltaTime).eulerAngles.y - transform.rotation.eulerAngles.y;
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, scaledTurnSpeed * Time.fixedDeltaTime);
                }
            }
            if (Mathf.Abs(groundSpeed) < 0.1 && inputDirection.magnitude == 0)
            {
                currentAcceleration = 0;
                groundSpeed = 0;
            }

            // Adjust the current speed towards the target speed
            groundSpeed += currentAcceleration * Time.fixedDeltaTime;

            if (Mathf.Abs(groundSpeed) > 0)
            {
                if (inputDirection.magnitude > 0)
                    anim.SetFloat("Horizontal Speed", runningAnimationSpeed.Evaluate(Mathf.Abs(groundSpeed / maxGroundVelocity)) * 4);
                else
                {
                    anim.SetFloat("Horizontal Speed", Mathf.Abs(groundSpeed / maxGroundVelocity) * 4);
                }
            }
            else
            {
                anim.SetFloat("Horizontal Speed", 0);
            }
            if (anim.GetCurrentAnimatorStateInfo(0).IsName("Run"))
                model.localRotation = Quaternion.Euler(runningTilt.Evaluate(groundSpeed / maxGroundVelocity) * 35 - incline + 90, 0, 0);
            else
                model.localRotation = Quaternion.identity;
            if (input.joystickRaw.magnitude < 0.99f)
            {
                currentmaxspeed = currentmaxspeed / 2;
                anim.SetFloat("Horizontal Speed", groundSpeed/2);
            }
            Move(transform.forward * groundSpeed * Time.fixedDeltaTime);
            airVelocity = new Vector3(transform.forward.x * groundSpeed, 0, transform.forward.z * groundSpeed);
            CheckJump();
            targetDirection = transform.rotation;
            CheckWall(0);
        }
        else
        {
            if (CheckCeil())
            {
                airVelocity.y = -1f;
            }
            airVelocity += Physics.gravity * Time.fixedDeltaTime;
            if(input.A.wasReleasedThisTick && airVelocity.y > 5)
            {
                airVelocity.y = 5;
            }
            model.localRotation = Quaternion.identity;
            // Convert air velocity to ground speed
            groundSpeed = new Vector3(airVelocity.x, 0, airVelocity.z).magnitude;

            airVelocity += inputDirection * 25f * Time.fixedDeltaTime;
            Vector2 tmp = new Vector2(airVelocity.x, airVelocity.z);
            // Ensure that the magnitude of airVelocity does not exceed maxAirVelocity
            if (tmp.magnitude > maxAirVelocity)
            {
                Vector3 dec = -airVelocity.normalized * (30f);
                dec.y = 0;
                airVelocity += dec * Time.fixedDeltaTime;
            }
            
            // Apply air velocity to the position directly
            Move(airVelocity * Time.fixedDeltaTime);
            CheckWall(0);
            if (wall)
            {
                if(airVelocity.y < 0)
                {
                    transform.rotation = Quaternion.LookRotation(-wallangle, Vector3.up);
                    transform.rotation = quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
                    currentState = CharacterState.WallSlide;
                }
            }
            else
            {
                if (canTurnMidair)
                {
                    if (inputDirection.magnitude > 0)
                    {
                        targetDirection = Quaternion.LookRotation(inputDirection.normalized);
                    }
                    transform.rotation = Quaternion.Lerp(targetDirection, transform.rotation, 0.5f);
                }
            }
            CheckGroundPound();
            CheckAirKick();
        }
        anim.SetBool("Grounded", grounded);
        anim.SetFloat("Hstick", input.joystickRaw.magnitude);
        wasGrounded = grounded;
    }
    public bool CheckAirKick()
    {
        if (input.B.wasPressedThisTick) //kick
        {
            airVelocity = airVelocity * 0.5f;
            airVelocity.y = 8;
            currentState = CharacterState.AirKick;
            playVoice("Kick" + UnityEngine.Random.Range(1, 3));
            return true;
        }
        return false;
    }
    public bool CheckGroundPound()
    {
        if (input.Z.wasPressedThisTick)
        {
            airVelocity = Vector3.zero;
            timer = 0;
            currentState = CharacterState.GroundPoundStart;
            return true;
        }
        return false;
    }
    public bool CheckJump()
    {
        if (input.A.wasPressedThisTick)
        {
            if(currentState == CharacterState.Walk && groundedTime < 0.15f)
            {
                airVelocity.y = 17.5f;
                currentState = CharacterState.DoubleJump;
                playVoice("2Jump" + UnityEngine.Random.Range(1, 3));
            }
            else
            {
                airVelocity.y = 15;
                playVoice("Jump" + UnityEngine.Random.Range(1, 3));
            }
            groundSpeed = maxGroundVelocity;
            grounded = false;
            anim.SetTrigger("Jump");
            anim.SetBool("Grounded", false);
            return true;
        }
        return false;
    }
    public bool CheckSideflip()
    {
        if (input.A.wasPressedThisTick)
        {
            transform.rotation = Quaternion.LookRotation(-transform.forward);
            groundSpeed = maxAirVelocity/3;
            airVelocity = new Vector3(transform.forward.x * groundSpeed, 20, transform.forward.z * groundSpeed);
            grounded = false;
            currentState = CharacterState.SideFlip;
            anim.SetBool("Grounded", false);
            playVoice("Jump" + UnityEngine.Random.Range(1, 3));
            return true;
        }
        return false;
    }
    public bool CheckGrounded()
    {
        RaycastHit hit;
        float currentheight = height * transform.localScale.y;
        Physics.Raycast(transform.position + (Vector3.up * currentheight / 2), Vector3.down, out hit, (currentheight / 2f - (airVelocity.y * transform.localScale.y * Time.fixedDeltaTime)), layerMask);
        if (hit.collider)
        {
            Vector3 pos = transform.position;
            incline = Vector3.Angle(hit.normal, transform.forward);
            slopeNormal = hit.normal;
            pos.y = hit.point.y;
            transform.position = pos;
            return true;
        }
        return false;
    }
    public Vector3 MultiplyVector3(Vector3 vector1, Vector3 vector2)
    {
        Vector3 retur = new Vector3();
        retur.x = vector1.x * vector2.x;
        retur.y = vector1.y * vector2.y;
        retur.z = vector1.z * vector2.z;

        return retur;
    }
    public bool CheckCeil()
    {
        RaycastHit hit;
        float currentheight = height * transform.localScale.y;
        Physics.Raycast(transform.position + (Vector3.up * currentheight / 2), Vector3.up, out hit, (currentheight / 2f + (airVelocity.y * transform.localScale.y * Time.fixedDeltaTime)), layerMask);
        if (hit.collider)
        {
            Vector3 pos = transform.position;
            pos.y = hit.point.y - currentheight;
            transform.position = pos;
            return true;
        }
        return false;
    }
    public bool CheckWall(float off)
    {
        // Define the number of rays to cast in a circle.
        int numRays = 4;
        if (GameManager.Instance.localPlayer == this)
        {
            numRays = 500; 
        }
        else
        {
            numRays = 50; 
        }
        float currentrad = radius * transform.localScale.x;

        // Calculate the angle increment between each ray.
        float angleIncrement = 360f / numRays;

        // Calculate the position at the center of the character's hitbox.
        Vector3 center = transform.position + Vector3.up * (height / 2) + (Vector3.up * off);

        for (int i = 0; i < numRays; i++)
        {
            center = transform.position + Vector3.up * (height / 2) + (Vector3.up * off);
            float angle = i * angleIncrement;
            Vector3 rayDirection = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            RaycastHit hit;

            Debug.DrawLine(center, center + (rayDirection * currentrad), Color.green);

            if (Physics.Raycast(center, rayDirection, out hit, currentrad, layerMask))
            {
                wall = true;
                Vector3 collisionNormal = hit.normal;
                collisionNormal.y = 0;

                RaycastHit hit2;
                if (Physics.Raycast(center, -collisionNormal, out hit2, currentrad, layerMask))
                {

                    // Calculate the desired position by moving along the collision normal.
                    Vector3 newPosition = hit2.point + (collisionNormal * currentrad);

                    // Calculate the horizontal movement direction.
                    Vector3 movementDirection = newPosition - transform.position;

                    // Debug lines to visualize the raycasts and collision handling.
                    Debug.DrawLine(center, center + hit.normal, Color.blue); // Collision normal

                    // Move the character horizontally to the new position.
                    transform.position = new Vector3(newPosition.x, transform.position.y, newPosition.z);
                    wallangle = hit.normal;
                    wall = true;
                }
            }
        }
        return wall; // Return true if wall collision detected, otherwise false
    }
    public void CheckWallLast(float off)
    {
        // Define the number of rays to cast in a circle.
        int numRays = 600; // You can adjust this value for more or fewer rays.

        // Calculate the angle increment between each ray.
        float angleIncrement = 360f / numRays;

        // Calculate the position at the center of the character's hitbox.
        Vector3 center = transform.position + Vector3.up * (height / 2) + (Vector3.up * off);

        for (int i = 0; i < numRays; i++)
        {
            center = transform.position + Vector3.up * (height / 2) + (Vector3.up * off);
            float angle = i * angleIncrement;
            Vector3 rayDirection = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            RaycastHit hit;

            Debug.DrawLine(center, center + (rayDirection * radius), Color.yellow);

            if (Physics.Raycast(center, rayDirection, out hit, radius, layerMask))
            {
                Vector3 collisionNormal = hit.normal;
                collisionNormal.y = 0;

                RaycastHit hit2;
                if (Physics.Raycast(center, -collisionNormal, out hit2, radius, layerMask))
                {
                    wallangle = hit.normal;

                    // Calculate the desired position by moving along the collision normal.
                    Vector3 newPosition = hit2.point + (collisionNormal * radius);

                    // Calculate the horizontal movement direction.
                    Vector3 movementDirection = newPosition - transform.position;

                    // Debug lines to visualize the raycasts and collision handling.
                    Debug.DrawRay(center, rayDirection * radius, Color.red); // Raycast direction
                    Debug.DrawRay(center, hit.normal, Color.blue); // Collision normal
                    Debug.DrawRay(center, movementDirection, Color.green); // Movement direction

                    // Move the character horizontally to the new position.
                    transform.position = new Vector3(newPosition.x, transform.position.y, newPosition.z);
                    wall = true;
                }
            }
        }
    }

    public void Partical(int id)
    {
        if (id == 0 && groundSpeed > skidSpeed) return;

        Instantiate(particals[id], transform.position, Quaternion.Euler(-90, 0, 0));
    }

    public void FootStep(int sfx)
    {
        playSound("Step");
        if (GameManager.Instance.CheckVolume(Volume.VolumeType.Water, transform.position) != null)
        {
            Instantiate(particals[2], transform.position, Quaternion.Euler(-90, 0, 0));
        }
    }

    public void Move(Vector3 amount)
    {
        transform.position += multiplyVector3(amount, transform.localScale);
    }

    public Vector3 multiplyVector3(Vector3 vec1, Vector3 vec2)
    {
        Vector3 result = vec1;
        result.x = vec1.x * vec2.x;
        result.y = vec1.y * vec2.y;
        result.z = vec1.z * vec2.z;
        return result;
    }

    public void playSound(string clip)
    {
        photonView.RPC("playSFX", RpcTarget.All, "Sounds/Players/Sounds/" + clip);
    }

    public void playVoice(string clip)
    {
        if(view.IsMine)
            photonView.RPC("playSFX", RpcTarget.All, "Sounds/Players/Voices/" + data.voicesFolder + "/" + clip);
    }

    [PunRPC]
    public void playSFX(string clip)
    {
        if (view.IsMine)
            audioSource.PlayOneShot(Resources.Load<AudioClip>(clip));
    }

    public void Die()
    {
        photonView.RPC("setHealth", RpcTarget.All, 0);
    }

    [PunRPC]
    public void setHealth(int a)
    {
        health = a;
    }

    public void HealPlayer(int amount)
    {
        health += amount;
    }
    private void OnDisable()
    {
        anim.speed = 0;
    }

    private void OnEnable()
    {
        anim.speed = 1;
    }
}