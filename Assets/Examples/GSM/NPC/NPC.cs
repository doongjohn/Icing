using Icing;
using UnityEngine;

public class NPC : GSM_Controller
{
    private Transform target;
    private float targetDist;
    private int health = 10;

    protected void Awake()
    {
        target = GameObject.Find("Target").transform;

        // Get states
        gameObject.GetComponent(out NPC_Death state_death);
        gameObject.GetComponent(out NPC_Idle state_idle);
        gameObject.GetComponent(out NPC_Follow state_follow);
        gameObject.GetComponent(out NPC_RunAway state_runAway);

        // Init Bvr
        var bvr_Death = new BvrSingle(
            stateEx: new StateEx(),
            state: state_death,
            isDone: () => false
        );
        var bvr_follow = new BvrSingle(
            stateEx: new StateEx(),
            state: state_follow,
            isDone: () => targetDist <= 5f
        );
        var bvr_runAway = new BvrSingle(
            stateEx: new StateEx(),
            state: state_runAway,
            isDone: () => targetDist >= 3f
        );
        var bvr_attack = new BvrSingle(
            stateEx: new StateEx(),
            state: state_follow,
            isDone: () => targetDist <= 0.5f
        );

        // Init Flow
        var flow_normal = new Flow();
        var flow_angry = new Flow();

        // Define Flow Logic
        // "flow_begin" will be always checked before checking the current flow.
        flow_begin
            .ForceDo(() => health <= 0, bvr_Death);

        flow_normal
            .To(() => Input.GetKeyDown(KeyCode.Return) && !bvr_attack.IsFinished, flow_angry)
            .Do(() => targetDist < 3f, bvr_runAway)
            .Do(() => targetDist > 5f, bvr_follow);

        flow_angry
            .Do(() => true, bvr_attack.Wait()
                .To(() => true, flow_normal));

        // Init GSM
        GSM_Init(
            startingFlow: flow_normal,
            defaultStateEx: new StateEx(),
            defaultState: state_idle
        );
    }
    protected override void Update()
    {
        base.Update();
        targetDist = Vector2.Distance(target.position, transform.position);
    }
}
