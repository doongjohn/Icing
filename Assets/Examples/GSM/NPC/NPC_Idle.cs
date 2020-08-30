using Icing;
using UnityEngine;

public class NPC_Idle : GSM_State
{
    private Rigidbody2D rb2D;

    private void Awake()
    {
        gameObject.GetComponent(out rb2D);
    }

    public override void OnFixedUpdate()
    {
        rb2D.velocity = Vector2.zero;
    }
}
