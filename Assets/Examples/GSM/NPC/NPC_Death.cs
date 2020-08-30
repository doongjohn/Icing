using Icing;
using UnityEngine;

public class NPC_Death : GSM_State
{
    public override void OnEnter()
    {
        base.OnEnter();
        Destroy(gameObject);
    }
}
