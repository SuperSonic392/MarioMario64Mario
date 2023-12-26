using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // This may happen if the GameManager is accessed before it's created.
                // You might want to handle this situation by creating the GameManager here.
                _instance = FindObjectOfType<GameManager>();
            }
            return _instance;
        }
    }
    public CharacterData[] characters;
    public GameObject character;
    public CameraController cam;
    public Canvas playerlist;
    public IngamePlayerListItem template;
    public MarioController[] controllers;
    public MarioController localPlayer;
    public Image health;
    public Sprite[] icons;
    public int area;
    public AudioSource[] areaAudio;
    void Start()
    {
        SaveManager sm = FindObjectOfType<SaveManager>();
        if(sm != null)
        {
            character = sm.character;
        }
        else
        {
            PhotonNetwork.OfflineMode = true; //loaded from editor
            PhotonNetwork.CreateRoom("");
            Debug.Log("Offline Mode");
        }
        SpawnPlayer();
    }
    private void Update()
    {
        for (int i = 0; i < areaAudio.Length; i++)
        {
            areaAudio[i].enabled = i == area;
        }
        if (playerlist != null)
        {
            playerlist.gameObject.SetActive(Keyboard.current.tabKey.isPressed);
        }
        HandleHealth();
    }
    public void HandleHealth()
    {
        health.sprite = icons[Mathf.CeilToInt(localPlayer.health)];
    }
    private float timer;
    private void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;
        if (timer > 2)
        {
            controllers = FindObjectsOfType<MarioController>();
            timer = 0;
        }
    }
    public void SpawnPlayer()
    {
        localPlayer = PhotonNetwork.Instantiate("Prefabs/Player/" + character.name, Vector3.zero, Quaternion.identity).GetComponent<MarioController>();
        CameraController camera = Instantiate(cam);
        camera.target = localPlayer.cameraTarget.transform;
        camera.con = localPlayer;
        localPlayer.cameraTransform = camera.transform;
        localPlayer.GetComponent<CharacterInputManager>().POV = localPlayer.cameraTarget.transform;
        camera.input = localPlayer.input;
    }

    public VolumeData CheckVolume(Volume.VolumeType type, Vector3 position)
    {
        Volume[] volumes = FindObjectsOfType<Volume>();

        foreach (Volume vol in volumes)
        {
            if (vol.type == type && IsPointInsideVolume(position, vol))
            {
                return new VolumeData
                {
                    density = vol.density,
                    size = vol.size,
                    surface = vol.transform.position.y + (vol.size.y / 2),
                    current = vol.current
                };
            }
        }

        return null;
    }

    private bool IsPointInsideVolume(Vector3 point, Volume volume)
    {
        Vector3 volumeCenter = volume.transform.position;
        Vector3 volumeSize = volume.size;

        bool isInside = Mathf.Abs(point.x - volumeCenter.x) <= volumeSize.x / 2f &&
                        Mathf.Abs(point.y - volumeCenter.y) <= volumeSize.y / 2f &&
                        Mathf.Abs(point.z - volumeCenter.z) <= volumeSize.z / 2f;

        return isInside;
    }
}
