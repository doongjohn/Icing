using System;
using System.Collections.Generic;
using UnityEngine;

namespace Icing
{
    /* CSM: Case-Based State Machine
     * -> 상황 기반의 스테이트 머신
     * 
     * ------------------------------------------------------------------------
     * 
     * Case (상황)
     * -> 행동을 하기 위한 어떤 조건.
     * 
     * Behaviour (행동)
     * -> State의 묶음.
     * 
     * State (상태)
     * -> 실제 로직.
     * 
     * ------------------------------------------------------------------------
     * 
     * 기능:
     * 
     * -> Flow_First
     *    항상 CurrentFlow를 검사하기 전 검사되는 Flow.
     *    
     * -> Flow_Last
     *    항상 CurrentFlow를 검사한 이후 검사되는 Flow.
     * -> Case.Wait()
     *    이 Case의 Behaviour가 끝날때까지 다른 Case를 검사하지 않음.
     *    
     * -> Case.AlwaysCheck()
     *    Case.Wait을 무시하고 이 Case를 검사함.
     * 
     * -> Case.SetNextGroup()
     *    이 Case의 Behaviour가 끝나면 현재 CaseGroup을 지정한 것으로 바꿈. (무한 루프가 일어날 수 있으니 주의!)
     * 
     * ------------------------------------------------------------------------
     * 
     * 초기화:
     * 
     * -> States 초기화
     *    1) State를 가져올 때는 GetComponent 대신 GetState(ref state) 함수를 사용해야함.
     * 
     * -> Behaviour 초기화
     *    1) Single: 하나의 State만 필요할 때 사용함.
     *    2) Process: 두개 이상의 State들이 모두 순차적으로 동시에 완수되어야 끝남.
     *    3) Sequence: 두개 이상의 State들이 하나씩 순차적으로 완수되어야 끝남.
     *    4) Choice: 두개 이상의 State들이 조건에 의해 다른 State로 바뀌어야 할 때 사용함.
     * 
     * -> Case 초기화
     *    1) Case를 만들 때는 무조건 Case.New() 함수를 사용해야 함.
     * 
     * -> 초기화 할 때 꼭 실행해야 하는 함수:
     *    - SetDefaultState(state, stateAction)
     *    - SetDefaultFlow(flow)
     * 
     */

    public abstract partial class CSM_Controller<D, S> : MonoBehaviour
        where D : CSM_Data, new()
        where S : CSM_State
    {
        #region Var: CSM
        private bool canRunLateEnter = true;
        #endregion

        #region Prop: Data
        [SerializeField] 
        private D _data = new D();
        public D Data => _data;
        #endregion

        #region Prop: State
        protected readonly CSM_State END_BEHAVIOUR = null;
        protected CSM_State DefaultState
        { get; private set; }
        protected CSM_State PreviousState
        { get; private set; }
        protected CSM_State CurrentState
        { get; private set; }
        protected StateAction DefaultStateAction
        { get; private set; }
        protected StateAction CurrentStateAction
        { get; private set; }
        #endregion

        #region Prop: Behaviour
        protected CSM_Behaviour CurrentBehaviour
        { get; private set; }
        #endregion

        #region Prop: Case
        protected Case CurrentCase
        { get; private set; } = null;
        #endregion

        #region Prop: Flow
        protected Flow Flow_First
        { get; private set; } = new Flow();
        protected Flow Flow_Last
        { get; private set; } = new Flow();
        protected List<Flow> Flows
        { get; private set; } = new List<Flow>();

        protected Flow DefaultFlow
        { get; private set; } = null;
        protected Flow CurrentFlow
        { get; private set; } = null;
        #endregion

        #region Method: Debug Log
        public void SayError(string msg, string fix)
        {
            Debug.LogError($"{this.GetType().FullName} -> [Error]: {msg}\n[Fix]: {fix}");
        }
        #endregion

        #region Method: Unity
        protected virtual void Awake()
        {
            // Init Data
            Data.Init_Awake(gameObject);
        }
        protected virtual void Start()
        {
            // Init Data
            Data.Init_Start(gameObject);

            // Init CSM
            InitCSM();

            // Check Error
            if (DefaultState == null || DefaultStateAction == null)
                SayError("No Default State!", "Call Method -> SetDefaultState(state, stateAction) inside InitCSM()");

            if (DefaultFlow == null)
                SayError("No Default Case Group!", "Call Method -> SetDefaultCaseGroup() inside InitCSM()");

            CurrentStateAction.Enter?.Invoke();
            CurrentState.OnEnter();
            canRunLateEnter = true;
        }
        protected virtual void Update()
        {
            UpdateCSM();
            CurrentState.OnUpdate();
            CurrentStateAction.Update?.Invoke();
        }
        protected virtual void LateUpdate()
        {
            if (canRunLateEnter)
            {
                canRunLateEnter = false;
                CurrentState.OnLateEnter();
            }

            CurrentState.OnLateUpdate();
        }
        protected virtual void FixedUpdate()
        {
            CurrentState.OnFixedUpdate();
            CurrentStateAction.FixedUpdate?.Invoke();
        }
        #endregion

        #region Method: Init CSM
        protected abstract void InitCSM();
        #endregion

        #region Method: Init State
        protected virtual void GetState<TState>(ref TState state)
            where TState : CSM_State
        {
            state = GetComponent<TState>();

            if (state == null)
                SayError("Can't Find State Component!", $"Attatch <{typeof(TState).Name}> Component to this GameObject");

            state.Init(Data);
        }
        protected void SetDefaultState(CSM_State state, StateAction stateAction)
        {
            DefaultState = state;
            CurrentState = state;
            DefaultStateAction = stateAction;
            CurrentStateAction = stateAction;
        }
        #endregion

        #region Method: Init Case
        protected Flow NewCaseGroup()
        {
            var group = new Flow();
            Flows.Add(group);
            return group;
        }
        protected void SetDefaultFlow(Flow caseGroup)
        {
            DefaultFlow = caseGroup;
            CurrentFlow = caseGroup;
        }
        #endregion

        #region Method: Update CSM
        // TODO:
        private (Case @case, CSM_Behaviour behaviour, CSM_State state, StateAction stateAction) GetNext()
        {
            Case GetNextCase(params Flow[] flows)
            {
                for (int i_flow = 0; i_flow < flows.Length; i_flow++)
                {
                    var flow = flows[i_flow];
                    for (int i = 0; i < flow.Cases.Count; i++)
                    {
                        if (((CurrentCase?.IsWait ?? false) && !flow.Cases[i].IsAlwaysCheck) || !flow.Cases[i].Condition())
                            continue;

                        return flow.Cases[i];
                    }
                }
                return null;
            }

            // Get next case
            Case nextCase = GetNextCase(Flow_First, CurrentFlow, Flow_Last);

            // Return next case
            (Case, CSM_Behaviour, CSM_State, StateAction) Next()
            {
                var (behaviour, state, stateAction) = nextCase.GetCurrent(CurrentState);
                return (nextCase, behaviour, state, stateAction);
            }

            // Return next case
            if (nextCase != null && nextCase.Behaviour != null && nextCase.IsAlwaysCheck)
                return Next();

            // Check Wait
            if (CurrentCase != null)
            {
                var current = CurrentCase.GetCurrent(CurrentState);
                if (current.state != null && (CurrentCase?.IsWait ?? false))
                    return (CurrentCase, CurrentBehaviour, CurrentState, CurrentStateAction);
            }

            if (nextCase != null && nextCase.Behaviour == null && nextCase.IsResume)
            {
                return (CurrentCase, CurrentBehaviour, CurrentState, CurrentStateAction);
            }

            // Return next case
            return nextCase == null ? default : Next();
        }
        private void UpdateCSM()
        {
            var next = GetNext();

            if (next.@case != null)
            {
                var nextnext = next.@case.GetCurrent(CurrentState);
                if (nextnext.state == null && next.@case.NextFlow != null)
                {
                    CurrentFlow = next.@case.NextFlow;
                    next = GetNext();
                }
            }

            CurrentCase = next.@case;

            // Change Behaviour
            if (CurrentBehaviour != next.behaviour)
            {
                CurrentBehaviour?.OnBehaviourEnd();
                CurrentBehaviour = next.behaviour;
            }

            // Set Default State
            if (next.state == null)
            {
                next.state = DefaultState;
                next.stateAction = DefaultStateAction;
            }

            // Change State
            bool isStateNew = CurrentState != next.state;
            bool isStateActionNew = CurrentStateAction != next.stateAction;

            if (isStateNew)
            {
                PreviousState = CurrentState;

                canRunLateEnter = true;
                CurrentState?.OnExitWithDefer();
                CurrentState = next.state;
            }
            if (isStateActionNew)
            {
                CurrentStateAction.Exit?.Invoke();
                CurrentStateAction = next.stateAction;
                CurrentStateAction.Enter?.Invoke();
            }
            if (isStateNew)
                CurrentState.OnEnter();
        }
        #endregion
    }
}
