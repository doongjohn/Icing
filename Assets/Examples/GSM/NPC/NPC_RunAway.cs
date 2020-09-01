using Icing;
using UnityEngine;

public class NPC_RunAway : GSM_State
{
    private NPC npc;

    private void Awake()
    {
        gameObject.GetComponent(out npc);
    }

    public override void OnFixedUpdate()
    {
        npc.rb2D.velocity = (transform.position - npc.target.position).normalized * 10f;
    }
}
