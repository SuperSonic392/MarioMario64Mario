using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class CosmicMario : MonoBehaviour
{
    public MarioController target;
    private Animator anim;
    public GameObject model;
    public int delay;
    public List<Vector3> poss;
    public List<quaternion> rots;
    public List<string> anims;
    public List<float> spds;
    void Start()
    {
        anim = GetComponent<Animator>();
    }
    void FixedUpdate()
    {
        if(target != null)
        {
            poss.Add(target.transform.position);
            rots.Add(target.model.transform.rotation);
            anims.Add(target.anim.GetCurrentAnimatorClipInfo(0)[0].clip.name);
            spds.Add(target.anim.GetFloat("Horizontal Speed"));
            if(poss.Count >= delay)
            {
                anim.Play(anims[0], 0);
                anims.RemoveAt(0);
                transform.position = poss[0];
                model.transform.rotation = rots[0];
                poss.RemoveAt(0);
                rots.RemoveAt(0);
                anim.SetFloat("Horizontal Speed", spds[0]);
                spds.RemoveAt(0);
            }
        }
    }
}
