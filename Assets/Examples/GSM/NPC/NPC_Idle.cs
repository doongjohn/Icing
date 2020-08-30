using Icing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC_Idle : GSM_State
{
    private Rigidbody2D rb2D;

    private void Awake()
    {
        gameObject.GetComponent(ref rb2D);
    }

    public override void OnEnter()
    {
        base.OnEnter();
        rb2D.velocity = Vector2.zero;
    }
}
