using Icing;
using UnityEngine;

public class NPC : GSM_Controller
{
    private float targetDist;
    private int health = 5;

    [HideInInspector] public Rigidbody2D rb2D;
    [HideInInspector] public Transform target;

    protected void Awake()
    {
        // Get other things
        gameObject.GetComponent(out rb2D);
        target = GameObject.Find("Target").transform;

        // Get states
        gameObject.GetComponent(out NPC_Death state_death);
        gameObject.GetComponent(out NPC_Idle state_idle);
        gameObject.GetComponent(out NPC_Attack state_attack);
        gameObject.GetComponent(out NPC_Sleep state_sleep);
        gameObject.GetComponent(out NPC_Follow state_follow);
        gameObject.GetComponent(out NPC_RunAway state_runAway);

        // Init Bvr
        var bvr_Death = new BvrSingle(
            stateEx: null,
            state: state_death,
            isDone: () => false
        );
        var bvr_follow = new BvrSingle(
            stateEx: null,
            state: state_follow,
            isDone: () => targetDist <= 5f
        );
        var bvr_runAway = new BvrSingle(
            stateEx: null,
            state: state_runAway,
            isDone: () => targetDist >= 3f
        );
        var bvr_attack = new BvrSequence(
            restartOnEnter: true,
            (
                stateEx: null,
                state: state_attack,
                isDone: () => targetDist <= 0.5f 
            ),
            (
                stateEx: null,
                state: state_sleep,
                isDone: () => targetDist >= 3f
            )
        );

        // Init Flow
        var flow_normal = new Flow();
        var flow_angry = new Flow();

        // Define Flow Logic
        flow_begin
            .ForceDo(() => health <= 0, bvr_Death);

        flow_normal
            .To(() => Input.GetKeyDown(KeyCode.Return), flow_angry)
            .Do(() => targetDist < 3f, bvr_runAway)
            .Do(() => targetDist > 5f, bvr_follow);

        flow_angry
            .Do(() => true, bvr_attack.Wait()
                .To(() => true, flow_normal));

        // Init GSM
        GSM_Init(
            startingFlow: flow_normal,
            defaultStateEx: null,
            defaultState: state_idle
        );
    }
    protected override void Update()
    {
        targetDist = Vector2.Distance(target.position, transform.position);
        base.Update();
    }
    private void OnMouseDown()
    {
        health--;
    }
}
