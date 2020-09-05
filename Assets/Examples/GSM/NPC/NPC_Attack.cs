using Icing;
using UnityEngine;

public class NPC_Attack : GSM_State
{
    [SerializeField] private Color attackColor;

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
        sr.color = attackColor;
    }
    protected override void OnExit()
    {
        base.OnExit();
        sr.color = orininalColor;
    }
    public override void OnFixedUpdate()
    {
        npc.rb2D.velocity = (npc.target.position - transform.position).normalized * 10f;
    }
}
