using Icing;
using UnityEngine;

public class NPC_Follow : GSM_State
{
    private NPC npc;

    private void Awake()
    {
        gameObject.GetComponent(out npc);
    }

    public override void OnFixedUpdate()
    {
        npc.rb2D.velocity = (npc.target.position - transform.position).normalized * 10f;
    }
}
