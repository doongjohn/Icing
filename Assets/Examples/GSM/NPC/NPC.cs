using Icing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC : GSM_Controller
{
    private Transform target;

    private NPC_Idle state_idle;
    private NPC_Follow state_follow;
    private NPC_RunAway state_runAway;

    protected void Awake()
    {
        target = GameObject.Find("Target").transform;

        gameObject.GetComponent(ref state_idle);
        gameObject.GetComponent(ref state_follow);
        gameObject.GetComponent(ref state_runAway);

        var bvr_idle = new BvrSingle(
            stateEx: new StateEx(),
            state: state_idle,
            isDone: () => false
        );
        var bvr_follow = new BvrSingle(
            stateEx: new StateEx(),
            state: state_follow,
            isDone: () => Vector2.Distance(target.position, transform.position) <= 5f
        );
        var bvr_runAway = new BvrSingle(
            stateEx: new StateEx(),
            state: state_runAway,
            isDone: () => Vector2.Distance(target.position, transform.position) >= 3f
        );
        var bvr_attack = new BvrSingle(
            stateEx: new StateEx(),
            state: state_follow,
            isDone: () => false
        );

        var flow_normal = new Flow();
        var flow_angry = new Flow();

        flow_normal
            .To(() => Input.GetKeyDown(KeyCode.Return), flow_angry)
            .Do(() => Vector2.Distance(target.position, transform.position) < 3f, bvr_runAway)
            .Do(() => Vector2.Distance(target.position, transform.position) > 5f, bvr_follow);

        flow_angry
            .To(() => Input.GetKeyDown(KeyCode.Space), flow_normal)
            .Do(() => true, bvr_attack);

        SetStartFlow(flow_normal);
        SetDefaultState(new StateEx(), state_idle);
    }
}
