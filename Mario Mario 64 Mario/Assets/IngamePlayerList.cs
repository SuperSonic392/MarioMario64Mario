using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class IngamePlayerList : MonoBehaviour
{
    public IngamePlayerListItem template;
    public List<GameObject> currentList;
    private GameManager gameManager;
    // Start is called before the first frame update
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        UpdateList();
    }
    int timer;
    // Update is called once per frame
    void FixedUpdate()
    {
        timer++;
        if(timer >= 10)
        {
            timer = 0;
            UpdateList();
        }
    }

    private void UpdateList()
    {
        if (template != null)
        {
            foreach(GameObject go in currentList)
            {
                Destroy(go);
            }
            currentList = new List<GameObject>();
            MarioController[] marios = gameManager.controllers;
            foreach (MarioController mario in marios)
            {
                IngamePlayerListItem item = Instantiate(template, template.transform.parent);
                currentList.Add(item.gameObject);
                item.icon.sprite = mario.data.UiImage;
                item.nickname.text = mario.view.Owner.NickName;
                item.gameObject.SetActive(true);
            }
        }
    }
}
