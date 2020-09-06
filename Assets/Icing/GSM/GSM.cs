// --------------------------------------------------------
// GSM: Glorified State Machine
// --------------------------------------------------------
// #1. It's a FSM.
// #2. It may throw Stack overflow if there are *TOO MANY* flow.To() calls in one frame.
//     (for most cases it will be fine.)
// #3. StateEx is always executed before GSM_State
// --------------------------------------------------------
// GSM_State
// --------------------------------------------------------
// #1. It's a MonoBehaviour.
// #2. OnExit()
//     -> Runs once when exiting this state.
// #3. OnEnter() 
//     -> Runs once when entering this state.
// #4. OnLateEnter() 
//     -> Same as OnEnter() but runs inside LateUpdate()
// #5. OnUpdate(), OnLateUpdate(), OnFixedUpdate(), 
//     -> Same as MonoBehaviour Update(), LateUpdate(), FixedUpdate(). 
//        But only runs when this state is active.


using System;
using System.Collections.Generic;
using UnityEngine;

namespace Icing
{
    public abstract class GSM_State : MonoBehaviour
    {
        private readonly Stack<Action> deferStack = new Stack<Action>();

        protected void Defer(Action action)
        {
            deferStack.Push(action);
        }
        public void OnExitWithDefer()
        {
            OnExit();
            for (int i = 0; i < deferStack.Count; i++)
                deferStack.Pop()();
        }
        protected virtual void OnExit() { /*Debug.Log($"Exit <- {this.GetType().FullName}");*/ }
        public virtual void OnEnter() { /*Debug.Log($"Enter -> {this.GetType().FullName}");*/ }
        public virtual void OnLateEnter() { }
        public virtual void OnUpdate() { }
        public virtual void OnLateUpdate() { }
        public virtual void OnFixedUpdate() { }
    }

    public abstract class GSM_Controller : MonoBehaviour
    {
        #region StatEx

        public class StateEx
        {
            public bool ResumeConsecutive { get; private set; }
            public Action OnEnter { get; private set; }
            public Action OnExit { get; private set; }
            public Action OnUpdate { get; private set; }
            public Action OnLateUpdate { get; private set; }
            public Action OnFixedUpdate { get; private set; }

            public StateEx(
                bool resumeConsecutive = true,
                Action onEnter = null,
                Action onExit = null,
                Action onUpdate = null,
                Action onLateUpdate = null,
                Action onFixedUpdate = null)
            {
                ResumeConsecutive = resumeConsecutive;
                OnEnter = onEnter;
                OnExit = onExit;
                OnUpdate = onUpdate;
                OnLateUpdate = onLateUpdate;
                OnFixedUpdate = onFixedUpdate;
            }
        }

        #endregion

        #region Bvr

        public abstract class Bvr
        {
            private class BvrID { }

            private readonly BvrID bvrID = new BvrID();
            private readonly List<(Func<bool> condition, Flow flow)> transitions = 
                new List<(Func<bool> condition, Flow flow)>();

            public bool IsWait { get; private set; } = false;

            public Bvr Wait()
            {
                var result = (Bvr)this.MemberwiseClone();
                result.IsWait = true;
                return result;
            }
            public Bvr To(Func<bool> condition, Flow flow)
            {
                var result = (Bvr)this.MemberwiseClone();
                result.transitions.Add((condition, flow));
                return result;
            }

            public virtual void BvrEnter() { }
            public Flow GetTransitionFlow(HashSet<Flow> checkedFlows)
            {
                for (int i = 0; i < transitions.Count; i++)
                {
                    // Avoides stack overflow
                    if (!checkedFlows.Contains(transitions[i].flow) && transitions[i].condition())
                        return transitions[i].flow;
                }
                return null;
            }
            public abstract (StateEx stateEx, GSM_State state)? GetCurState();

            public static bool operator ==(Bvr lhs, Bvr rhs)
            {
                if (lhs is null && rhs is null) return true;
                if (lhs is null || rhs is null) return false;
                return lhs.bvrID == rhs.bvrID;
            }
            public static bool operator !=(Bvr lhs, Bvr rhs)
            {
                if (lhs is null && rhs is null) return false;
                if (lhs is null || rhs is null) return true;
                return lhs.bvrID != rhs.bvrID;
            }
            public override bool Equals(object obj)
            {
                return obj is Bvr bvr &&
                       EqualityComparer<BvrID>.Default.Equals(bvrID, bvr.bvrID) &&
                       EqualityComparer<List<(Func<bool> condition, Flow flow)>>.Default.Equals(transitions, bvr.transitions) &&
                       IsWait == bvr.IsWait;
            }
            public override int GetHashCode()
            {
                int hashCode = 1282332479;
                hashCode = hashCode * -1521134295 + EqualityComparer<BvrID>.Default.GetHashCode(bvrID);
                hashCode = hashCode * -1521134295 + EqualityComparer<List<(Func<bool> condition, Flow flow)>>.Default.GetHashCode(transitions);
                hashCode = hashCode * -1521134295 + IsWait.GetHashCode();
                return hashCode;
            }
        }
        public class BvrSingle : Bvr
        {
            private readonly StateEx stateEx;
            private readonly GSM_State state;
            private readonly Func<bool> isDone;

            public BvrSingle(StateEx stateEx, GSM_State state, Func<bool> isDone)
            {
                this.stateEx = stateEx ?? new StateEx();
                this.state = state;
                this.isDone = isDone;
            }

            public override (StateEx stateEx, GSM_State state)? GetCurState()
            {
                if (isDone())
                    return null;

                return (stateEx, state);
            }
        }
        public class BvrRepeat : Bvr
        {
            private readonly bool restartOnEnter;
            private readonly int repeatCount;
            private int curRepeatCount;
            private readonly StateEx stateEx;
            private readonly GSM_State state;
            private readonly Func<bool> isDone;

            public BvrRepeat(bool restartOnEnter, int repeatCount, StateEx stateEx, GSM_State state, Func<bool> isDone)
            {
                this.restartOnEnter = restartOnEnter;
                this.repeatCount = repeatCount;
                this.curRepeatCount = 0;
                this.stateEx = stateEx ?? new StateEx();
                this.state = state;
                this.isDone = isDone;
            }

            public override void BvrEnter()
            {
                if (restartOnEnter || curRepeatCount == repeatCount)
                    curRepeatCount = 0;
            }
            public override (StateEx stateEx, GSM_State state)? GetCurState()
            {
                if (curRepeatCount == repeatCount)
                    return null;

                if (isDone())
                {
                    curRepeatCount++;
                    return GetCurState();
                }

                return (stateEx, state);
            }
        }
        public class BvrSequence : Bvr
        {
            private readonly bool restartOnEnter;
            private int curIndex;
            private (StateEx stateEx, GSM_State state, Func<bool> isDone)[] stateList;

            public BvrSequence(bool restartOnEnter, params (StateEx stateEx, GSM_State state, Func<bool> isDone)[] stateList)
            {
                this.restartOnEnter = restartOnEnter;
                this.curIndex = 0;
                for (int i = 0; i < stateList.Length; i++)
                    stateList[i].stateEx = stateList[i].stateEx ?? new StateEx();
                this.stateList = stateList;
            }

            public override void BvrEnter()
            {
                if (restartOnEnter || curIndex == stateList.Length)
                    curIndex = 0;
            }
            public override (StateEx stateEx, GSM_State state)? GetCurState()
            {
                if (curIndex == stateList.Length)
                    return null;

                if (stateList[curIndex].isDone())
                {
                    curIndex++;
                    return GetCurState();
                }

                return (stateList[curIndex].stateEx, stateList[curIndex].state);
            }
        }

        #endregion

        #region Flow

        public class Flow
        {
            public class Node
            {
                public Func<bool> condition;
                public bool forceRun;
            }
            public class BvrNode : Node
            {
                public Bvr bvr;
            }
            public class FlowNode : Node
            {
                public Flow flow;
            }

            private List<Node> nodes = new List<Node>();
            public readonly static Node waitingNode = new Node();

            public Flow Do(Func<bool> condition, Bvr bvr)
            {
                nodes.Add(new BvrNode()
                {
                    condition = condition,
                    forceRun = false,
                    bvr = bvr
                });
                return this;
            }
            public Flow ForceDo(Func<bool> condition, Bvr bvr)
            {
                nodes.Add(new BvrNode()
                {
                    condition = condition,
                    forceRun = true,
                    bvr = bvr
                });
                return this;
            }
            public Flow To(Func<bool> condition, Flow flow)
            {
                nodes.Add(new FlowNode()
                {
                    condition = condition,
                    forceRun = false,
                    flow = flow
                });
                return this;
            }
            public Flow ForceTo(Func<bool> condition, Flow flow)
            {
                nodes.Add(new FlowNode()
                {
                    condition = condition,
                    forceRun = true,
                    flow = flow
                });
                return this;
            }

            public Node GetCurNode(HashSet<Flow> checkedFlows, Bvr curBvr)
            {
                Node GetNodeAvoidStackOverflow(int i)
                {
                    if (nodes[i] is FlowNode flowNode)
                    {
                        // Avoides stack overflow
                        if (checkedFlows.Contains(flowNode.flow))
                            return null;

                        if (nodes[i].condition())
                            return nodes[i];
                    }
                    else if (nodes[i].condition())
                    {
                        return nodes[i];
                    }

                    return null;
                }

                // When current Bvr is Waiting
                if (curBvr != null && curBvr.IsWait && curBvr.GetCurState() != null)
                {
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        if (nodes[i].forceRun)
                        {
                            var node = GetNodeAvoidStackOverflow(i);
                            if (node != null)
                                return node;
                        }
                    }
                    return waitingNode;
                }
                // When current Bvr is not Waiting
                else
                {
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        var node = GetNodeAvoidStackOverflow(i);
                        if (node != null)
                            return node;
                    }
                    return null;
                }
            }
        }

        #endregion

        // GSM Default Data
        protected Flow flow_begin = new Flow();
        private StateEx defaultStateEx;
        private GSM_State defaultState;

        // GSM Data
        private HashSet<Flow> checkedFlows = new HashSet<Flow>();
        private bool onLateEnterDone = false;

        public Flow CurFlow { get; private set; }
        public Flow PrevFlow { get; private set; }
        public Bvr CurBvr { get; private set; }
        public Bvr PrevBvr { get; private set; }
        public StateEx CurStateEx { get; private set; }
        public StateEx PrevStateEx { get; private set; }
        public GSM_State CurState { get; private set; }
        public GSM_State PrevState { get; private set; }

        protected void GSM_Init(Flow startingFlow, StateEx defaultStateEx, GSM_State defaultState)
        {
            // Call this method before OnStart()
            this.CurFlow = startingFlow;
            this.defaultStateEx = CurStateEx = defaultStateEx ?? new StateEx();
            this.defaultState = CurState = defaultState;
        }
        protected void GSM_Update()
        {
            void ChangeState(StateEx newStateEx, GSM_State newState)
            {
                if (newStateEx != CurStateEx)
                {
                    PrevStateEx = CurStateEx;
                    CurStateEx = newStateEx;
                    PrevStateEx?.OnExit?.Invoke();
                    CurStateEx.OnEnter?.Invoke();

                    // 현재 또는 다음 StateEx의 ResumeConsecutive가 false일 경우 OnExit->OnEnter을 실행함.
                    if (newState != CurState || (newState == CurState && (!CurStateEx.ResumeConsecutive || !newStateEx.ResumeConsecutive)))
                    {
                        onLateEnterDone = false;
                        PrevState = CurState;
                        CurState = newState;
                        PrevState?.OnExitWithDefer();
                        CurState.OnEnter();
                    }
                }
            }
            void ChangeBvr(Bvr newBvr)
            {
                if (newBvr == CurBvr)
                    return;

                PrevBvr = CurBvr;
                CurBvr = newBvr;
                CurBvr.BvrEnter();
            }
            bool ChangeFlowRecursive(Flow newFlow)
            {
                // How to avoid stack overflow:
                // 한 프레임에 들어갔던 플로우로 다시 가는 건 무시 해야함.
                // flow_a.To(flow_b) -> checkedFlows.Add(flow_a) -> if (!checkedFlows.Contains(flow_b)) -> To flow_b
                // flow_b.To(flow_c) -> checkedFlows.Add(flow_b) -> if (!checkedFlows.Contains(flow_c)) -> To flow_c
                // flow_c.To(flow_d) -> checkedFlows.Add(flow_c) -> if (!checkedFlows.Contains(flow_d)) -> To flow_d
                // flow_d.To(flow_b) -> checkedFlows.Add(flow_d) -> if (!checkedFlows.Contains(flow_b)) -> Infinite loop detected: Stop here

                checkedFlows.Add(CurFlow);
                PrevFlow = CurFlow;
                CurFlow = newFlow;
                return ProcessFlowNode(CurFlow);
            }
            bool ProcessBvr(Bvr bvr)
            {
                var stateData = bvr.GetCurState();

                // When Bvr is done
                if (stateData == null)
                {
                    var transitionFlow = bvr.GetTransitionFlow(checkedFlows);
                    return transitionFlow != null && ChangeFlowRecursive(transitionFlow);
                }
                // When Bvr is not done
                else
                {
                    ChangeState(stateData.Value.stateEx, stateData.Value.state);
                    return true;
                }
            }
            bool ProcessFlowNode(Flow flowToProcess)
            {
                var curFlowNode = flowToProcess.GetCurNode(checkedFlows, CurBvr);

                // When no available node
                if (curFlowNode == null)
                    return false;

                // When waiting
                if (curFlowNode == Flow.waitingNode)
                    return ProcessBvr(CurBvr);

                // When not waiting
                switch (curFlowNode)
                {
                    case Flow.BvrNode bvrNode:
                        ChangeBvr(bvrNode.bvr);
                        return ProcessBvr(CurBvr);
                    case Flow.FlowNode flowNode:
                        return ChangeFlowRecursive(flowNode.flow);
                    default:
                        return false;
                }
            }

            // Remember current frame flows
            // to avoid stack overflow
            checkedFlows.Clear();
            checkedFlows.Add(CurFlow);

            // Check begin flow
            if (ProcessFlowNode(flow_begin))
                return;

            // Check current flow
            if (ProcessFlowNode(CurFlow))
                return;

            // If no Bvr is active, then set to default
            ChangeState(defaultStateEx, defaultState);
        }

        protected void OnStart()
        {
            CurStateEx.OnEnter?.Invoke();
            CurState.OnEnter();
        }
        protected void OnUpdate()
        {
            CurStateEx.OnUpdate?.Invoke();
            CurState.OnUpdate();
        }
        protected void OnLateUpdate()
        {
            if (onLateEnterDone == false)
            {
                onLateEnterDone = true;
                CurState.OnLateEnter();
            }

            CurStateEx.OnLateUpdate?.Invoke();
            CurState.OnLateUpdate();
        }
        protected void OnFixedUpdate()
        {
            CurStateEx.OnFixedUpdate?.Invoke();
            CurState.OnFixedUpdate();
        }
    }
    public class GSM_NormalController : GSM_Controller
    {
        // Normal GSM Controller for Real-time games.
        // It calls GSM_Update every frame.

        protected virtual void Start()
        {
            OnStart();
        }
        protected virtual void Update()
        {
            GSM_Update();
            OnUpdate();
        }
        protected virtual void LateUpdate()
        {
            OnLateUpdate();
        }
        protected virtual void FixedUpdate()
        {
            OnFixedUpdate();
        }
    }
}
