using Icing;
using UnityEngine;

public class NPC_Sleep : GSM_State
{
    [SerializeField] private Color sleepColor;

    private NPC npc;
    private SpriteRenderer sr;
    private Color orininalColor;

    private void Awake()
    {
        gameObject.GetComponent(out npc);
        gameObject.GetComponentInChildren(out sr);
        orininalColor = sr.color;
    }

    public override void OnEnter()
    {
        base.OnEnter();

        sr.color = sleepColor;
        Defer(() => sr.color = orininalColor);
    }
    public override void OnFixedUpdate()
    {
        npc.rb2D.velocity = Vector2.zero;
    }
}
