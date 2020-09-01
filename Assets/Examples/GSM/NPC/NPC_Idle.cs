using Icing;
using UnityEngine;

public class NPC_Idle : GSM_State
{
    private NPC npc;

    private void Awake()
    {
        gameObject.GetComponent(out npc);
    }

    public override void OnFixedUpdate()
    {
        npc.rb2D.velocity = Vector2.zero;
    }
}
