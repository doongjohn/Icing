using System;
using System.Collections.Generic;
using UnityEditorInternal;
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
        public interface Bvr
        {
            bool IsWait { get; }

            Bvr Wait();
            Bvr To(Func<bool> condition, Flow flow);
            Flow GetTransition();
            void BvrEnter();
            (StateEx stateEx, GSM_State state)? GetCurState();
        }
        public struct BvrSingle : Bvr
        {
            private readonly StateEx stateEx;
            private readonly GSM_State state;
            private readonly Func<bool> isDone;
            private Func<bool> transitionFlowCondition;
            private Flow transitionFlow;

            public bool IsWait { get; private set; }

            public BvrSingle(StateEx stateEx, GSM_State state, Func<bool> isDone)
            {
                this.stateEx = stateEx ?? new StateEx();
                this.state = state;
                this.isDone = isDone;
                this.transitionFlowCondition = null;
                this.transitionFlow = null;
                this.IsWait = false;
            }
            public BvrSingle(BvrSingle bvr)
            {
                this.stateEx = bvr.stateEx;
                this.state = bvr.state;
                this.isDone = bvr.isDone;
                this.transitionFlowCondition = bvr.transitionFlowCondition;
                this.transitionFlow = bvr.transitionFlow;
                this.IsWait = bvr.IsWait;
            }

            public Bvr Wait()
            {
                return new BvrSingle(this) { IsWait = true };
            }
            public Bvr To(Func<bool> condition, Flow flow)
            {
                return new BvrSingle(this)
                {
                    transitionFlowCondition = condition,
                    transitionFlow = flow
                };
            }
            public Flow GetTransition()
            {
                if (transitionFlowCondition != null && transitionFlowCondition())
                    return transitionFlow;
                return null;
            }
            public void BvrEnter() { }
            public (StateEx stateEx, GSM_State state)? GetCurState()
            {
                if (isDone())
                    return null;

                return (stateEx, state);
            }
        }
        public struct BvrRepeat : Bvr
        {
            private bool restartOnEnter;
            private int repeatCount;
            private int curRepeatCount;
            private readonly StateEx stateEx;
            private readonly GSM_State state;
            private readonly Func<bool> isDone;
            private Func<bool> transitionFlowCondition;
            private Flow transitionFlow;

            public bool IsWait { get; private set; }

            public BvrRepeat(bool restartOnEnter, int repeatCount, StateEx stateEx, GSM_State state, Func<bool> isDone)
            {
                this.restartOnEnter = restartOnEnter;
                this.repeatCount = repeatCount;
                this.curRepeatCount = 0;
                this.stateEx = stateEx ?? new StateEx();
                this.state = state;
                this.isDone = isDone;
                this.transitionFlowCondition = null;
                this.transitionFlow = null;
                this.IsWait = false;
            }
            public BvrRepeat(BvrRepeat bvr)
            {
                this.restartOnEnter = bvr.restartOnEnter;
                this.repeatCount = bvr.repeatCount;
                this.curRepeatCount = bvr.curRepeatCount;
                this.stateEx = bvr.stateEx;
                this.state = bvr.state;
                this.isDone = bvr.isDone;
                this.transitionFlowCondition = bvr.transitionFlowCondition;
                this.transitionFlow = bvr.transitionFlow;
                this.IsWait = bvr.IsWait;
            }

            public Bvr Wait()
            {
                return new BvrRepeat(this) { IsWait = true };
            }
            public Bvr To(Func<bool> condition, Flow flow)
            {
                return new BvrRepeat(this)
                {
                    transitionFlowCondition = condition,
                    transitionFlow = flow
                };
            }
            public Flow GetTransition()
            {
                if (transitionFlowCondition != null && transitionFlowCondition())
                    return transitionFlow;
                return null;
            }
            public void BvrEnter()
            {
                if (restartOnEnter || curRepeatCount == repeatCount)
                    curRepeatCount = 0;
            }
            public (StateEx stateEx, GSM_State state)? GetCurState()
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
        public struct BvrSequence : Bvr
        {
            private bool restartOnEnter;
            private int curIndex;
            private (StateEx stateEx, GSM_State state, Func<bool> isDone)[] stateList;
            private Func<bool> transitionFlowCondition;
            private Flow transitionFlow;

            public bool IsWait { get; private set; }

            public BvrSequence(bool restartOnEnter, params (StateEx stateEx, GSM_State state, Func<bool> isDone)[] stateList)
            {
                this.restartOnEnter = restartOnEnter;
                this.curIndex = 0;
                for (int i = 0; i < stateList.Length; i++)
                    stateList[i].stateEx = stateList[i].stateEx ?? new StateEx();
                this.stateList = stateList;
                this.transitionFlowCondition = null;
                this.transitionFlow = null;
                this.IsWait = false;
            }
            public BvrSequence(BvrSequence bvr)
            {
                this.restartOnEnter = bvr.restartOnEnter;
                this.curIndex = bvr.curIndex;
                this.stateList = bvr.stateList;
                this.transitionFlowCondition = bvr.transitionFlowCondition;
                this.transitionFlow = bvr.transitionFlow;
                this.IsWait = bvr.IsWait;
            }

            public Bvr Wait()
            {
                return new BvrSequence(this) { IsWait = true };
            }
            public Bvr To(Func<bool> condition, Flow flow)
            {
                return new BvrSequence(this)
                {
                    transitionFlowCondition = condition,
                    transitionFlow = flow
                };
            }
            public Flow GetTransition()
            {
                if (transitionFlowCondition != null && transitionFlowCondition())
                    return transitionFlow;
                return null;
            }
            public void BvrEnter()
            {
                if (restartOnEnter || curIndex == stateList.Length)
                    curIndex = 0;
            }
            public (StateEx stateEx, GSM_State state)? GetCurState()
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
            public static Node waitingNode = new Node();

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

            public Node GetCurNode(Bvr curBvr)
            {
                if (curBvr != null && curBvr.IsWait && curBvr.GetCurState() != null)
                {
                    for (int i = 0; i < nodes.Count; i++)
                        if (nodes[i].forceRun && nodes[i].condition())
                            return nodes[i];

                    return waitingNode;
                }
                else
                {
                    for (int i = 0; i < nodes.Count; i++)
                        if (nodes[i].condition())
                            return nodes[i];

                    return null;
                }
            }
        }
        #endregion

        // GSM Default Data
        protected Flow flow_begin = new Flow();
        protected StateEx defaultStateEx;
        protected GSM_State defaultState;

        // GSM Data
        public Flow CurFlow { get; private set; }
        public Flow PrevFlow { get; private set; }
        public Bvr CurBvr { get; private set; }
        public Bvr PrevBvr { get; private set; }
        public StateEx CurStateEx { get; private set; }
        public StateEx PrevStateEx { get; private set; }
        public GSM_State CurState { get; private set; }
        public GSM_State PrevState { get; private set; }

        protected virtual void Start()
        {
            CurStateEx.OnEnter?.Invoke();
            CurState.OnEnter();
        }
        protected virtual void Update()
        {
            GSM_Update();
            CurStateEx.OnUpdate?.Invoke();
            CurState.OnUpdate();
        }
        protected virtual void LateUpdate()
        {
            CurStateEx.OnLateUpdate?.Invoke();
            CurState.OnLateUpdate();
        }
        protected virtual void FixedUpdate()
        {
            CurStateEx.OnFixedUpdate?.Invoke();
            CurState.OnFixedUpdate();
        }

        protected void GSM_Init(Flow startingFlow, StateEx defaultStateEx, GSM_State defaultState)
        {
            // Call this method before Start()
            this.CurFlow = startingFlow;
            this.defaultStateEx = CurStateEx = defaultStateEx ?? new StateEx();
            this.defaultState = CurState = defaultState;
        }
        private void GSM_Update()
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
                        PrevState = CurState;
                        CurState = newState;
                        PrevState?.OnExitWithDefer();
                        CurState.OnEnter();
                    }
                }
            }
            void ChangeBvr(Bvr newBvr)
            {
                if (newBvr != CurBvr)
                {
                    PrevBvr = CurBvr;
                    CurBvr = newBvr;
                    CurBvr.BvrEnter();
                }
            }
            bool ProcessBvr(Bvr bvr)
            {
                var stateData = bvr.GetCurState();

                // When Bvr is done
                if (stateData == null)
                {
                    var transitionFlow = bvr.GetTransition();
                    if (transitionFlow != null)
                    {
                        ChangeFlow(transitionFlow);
                        return true;
                    }
                    return false;
                }
                // When Bvr is not done
                else
                {
                    ChangeState(stateData.Value.stateEx, stateData.Value.state);
                    return true;
                }
            }
            void ChangeFlow(Flow newFlow)
            {
                PrevFlow = CurFlow;
                CurFlow = newFlow;
            }
            bool ProcessFlowNode(Flow flowToProcess)
            {
                var curFlowNode = flowToProcess.GetCurNode(CurBvr);

                // When no available node
                if (curFlowNode == null)
                    return false;

                // When waiting
                if (curFlowNode == Flow.waitingNode)
                    return ProcessBvr(CurBvr);

                // When not waiting
                if (curFlowNode is Flow.BvrNode bvrNode)
                {
                    Bvr bvr = bvrNode.bvr;
                    ChangeBvr(bvr);
                    return ProcessBvr(bvr);
                }
                else if (curFlowNode is Flow.FlowNode flowNode)
                {
                    ChangeFlow(flowNode.flow);
                    return true;
                }
                return false;
            }

            // Check begin flow
            if (!ProcessFlowNode(flow_begin))
            {
                // Check current flow
                if (!ProcessFlowNode(CurFlow))
                {
                    // If no bvr is active, then set default state
                    ChangeState(defaultStateEx, defaultState);
                }
            }
        }
    }
}
