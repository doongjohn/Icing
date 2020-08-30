using Icing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC_RunAway : GSM_State
{
    private Rigidbody2D rb2D;
    private Transform target;

    private void Awake()
    {
        gameObject.GetComponent(ref rb2D);
        target = GameObject.Find("Target").transform;
    }

    public override void OnFixedUpdate()
    {
        rb2D.velocity = (transform.position - target.position).normalized * 10f;
    }
}
