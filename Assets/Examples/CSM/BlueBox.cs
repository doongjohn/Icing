using Icing;
using System;
using UnityEngine;

[Serializable]
public class BlueBox_Data : CSM_Data
{
    public bool myTurn = false;
    public int Health = 10;
    public SpriteRenderer SmallBoxSR;

    public override void Init_Awake(GameObject gameObject)
    {
    }
    public override void Init_Start(GameObject gameObject)
    {
    }
}

public class BlueBox : CSM_Controller<BlueBox_Data, BlueBox_State>
{
    private BlueBox_Idle state_Idle;
    private BlueBox_Hit state_Hit;
    private BlueBox_Sleep state_Sleep;
    private BlueBox_Awake state_Awake;
    private BlueBox_Follow state_Follow;
    private BlueBox_Attack state_Attack;

    protected override void InitCSM()
    {
        #region States
        GetState(ref state_Idle);
        GetState(ref state_Hit);
        GetState(ref state_Awake);
        GetState(ref state_Sleep);
        GetState(ref state_Follow);
        GetState(ref state_Attack);
        SetDefaultState(state_Idle, EMPTY_STATE_ACTION);
        #endregion

        #region Behaviours
        var bvr_Idle = new Single(
            state_Idle,
            EMPTY_STATE_ACTION,
            () => Input.GetKeyDown(KeyCode.Return));

        var bvr_Hit = new Single(
            state_Hit,
            new StateAction(enter: () => Data.Health -= 1),
            () => Input.GetKeyDown(KeyCode.Return));

        var bvr_Sleep = new Sequence(true,
            (state_Sleep,
            EMPTY_STATE_ACTION,
            () => Input.GetKeyDown(KeyCode.Return)),
            (state_Awake,
            EMPTY_STATE_ACTION,
            () => Input.GetKeyDown(KeyCode.Return)));

        var bvr_Follow = new Single(
            state_Follow,
            EMPTY_STATE_ACTION,
            () => Input.GetKeyDown(KeyCode.Return));

        var bvr_Attack = new Single(
            state_Attack,
            EMPTY_STATE_ACTION,
            () => Input.GetKeyDown(KeyCode.Return));
        #endregion

        #region Cases
        var case_NotMyTurn = Case.New(
            () => !Data.myTurn);

        var case_Hit = Case.New(
            () => Input.GetKeyDown(KeyCode.H),
            bvr_Hit);

        var case_Sleep = Case.New(
            () => Input.GetKeyDown(KeyCode.S),
            bvr_Sleep);

        var case_Follow = Case.New(
            () => Input.GetKeyDown(KeyCode.F),
            bvr_Follow);

        var case_Attack = Case.New(
            () => true,
            bvr_Attack);

        var case_LowHealth = Case.New(
            () => Data.Health <= 5);

        var case_NotLowHealth = Case.New(
            () => Data.Health > 5,
            bvr_Idle);
        #endregion

        #region Flow
        var flow_Normal = NewCaseGroup();
        var flow_Angry = NewCaseGroup();
        SetDefaultFlow(flow_Normal);

        Flow_First
            .AddCase(case_Hit.Wait().AlwaysCheck())
            .AddCase(case_NotMyTurn.Resume().AlwaysCheck());

        flow_Normal
            .AddCase(case_LowHealth.AlwaysCheck().SetNextFlow(flow_Angry))
            .AddCase(case_Sleep.Wait())
            .AddCase(case_Follow);

        flow_Angry
            .AddCase(case_NotLowHealth.AlwaysCheck().SetNextFlow(flow_Normal))
            .AddCase(case_Attack.Wait())
            .AddCase(case_Follow);
        #endregion
    }
}
