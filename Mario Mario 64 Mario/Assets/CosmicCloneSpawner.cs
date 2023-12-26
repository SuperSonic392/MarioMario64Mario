using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CosmicCloneSpawner : MonoBehaviour
{
    public Vector3 size;
    public int amount;
    public int intervul;
    public GameObject prefab;
    void Update()
    {
        foreach(MarioController con in GameManager.Instance.controllers)
        {
            if (IsPointInsideTrigger(con.transform.position))
            {
                SpawnC(con);
                enabled = false;
            }
        }
    }
    private int inter;
    private void SpawnC(MarioController con)
    {
        inter = 0;
        for (int i = 0; i < amount; i++)
        {
            inter += intervul;
            CosmicMario cosm = Instantiate(prefab).GetComponent<CosmicMario>();
            cosm.target = con;
            cosm.delay = inter;
        }
    }

    private bool IsPointInsideTrigger(Vector3 point)
    {
        Vector3 Center = transform.position;
        Vector3 Size = size;

        bool isInside = Mathf.Abs(point.x - Center.x) <= Size.x / 2f &&
                        Mathf.Abs(point.y - Center.y) <= Size.y / 2f &&
                        Mathf.Abs(point.z - Center.z) <= Size.z / 2f;

        return isInside;
    }

    private void OnDrawGizmos()
    {
        if (!enabled) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, size);
    }
}
