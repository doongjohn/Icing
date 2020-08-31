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
        public interface Bvr
        {
            bool IsWait { get; }
            bool IsFinished { get; }
            Bvr Wait();
            Bvr To(Func<bool> condition, Flow flow);
            Flow GetTransition();
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
            public bool IsFinished => isDone();

            public BvrSingle(StateEx stateEx, GSM_State state, Func<bool> isDone)
            {
                this.stateEx = stateEx != null ? new StateEx() : stateEx;
                this.state = state;
                this.isDone = isDone;
                transitionFlowCondition = null;
                transitionFlow = null;
                IsWait = false;
            }
            public BvrSingle(BvrSingle bvr)
            {
                stateEx = bvr.stateEx;
                state = bvr.state;
                isDone = bvr.isDone;
                transitionFlowCondition = bvr.transitionFlowCondition;
                transitionFlow = bvr.transitionFlow;
                IsWait = bvr.IsWait;
            }

            public Bvr Wait()
            {
                return new BvrSingle(this) { IsWait = true };
            }
            public Bvr To(Func<bool> condition, Flow flow)
            {
                transitionFlowCondition = condition;
                transitionFlow = flow;
                return this;
            }
            public Flow GetTransition()
            {
                if (transitionFlowCondition != null && transitionFlowCondition())
                    return transitionFlow;
                return null;
            }
            public (StateEx stateEx, GSM_State state)? GetCurState()
            {
                if (isDone())
                    return null;

                return (stateEx, state);
            }
        }
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
        }


        // GSM Data
        protected Flow      curFlow,    prevFlow;
        protected Bvr       curBvr,     prevBvr;
        protected StateEx   curStateEx, prevStateEx;
        protected GSM_State curState,   prevState;

        // Default
        protected Flow flow_begin = new Flow();
        protected StateEx defaultStateEx;
        protected GSM_State defaultState;

        protected virtual void Start()
        {
            curStateEx.OnEnter?.Invoke();
            curState.OnEnter();
        }
        protected virtual void Update()
        {
            GSM_Update();
            curStateEx.OnUpdate?.Invoke();
            curState.OnUpdate();
        }
        protected virtual void LateUpdate()
        {
            curStateEx.OnLateUpdate?.Invoke();
            curState.OnLateUpdate();
        }
        protected virtual void FixedUpdate()
        {
            curStateEx.OnFixedUpdate?.Invoke();
            curState.OnFixedUpdate();
        }

        protected void GSM_Init(Flow startingFlow, StateEx defaultStateEx, GSM_State defaultState)
        {
            // Call this method before Start()
            this.curFlow = startingFlow;
            this.defaultStateEx = curStateEx = defaultStateEx == null ? new StateEx() : defaultStateEx;
            this.defaultState = curState = defaultState;
        }
        private void GSM_Update()
        {
            void ChangeState(StateEx newStateEx, GSM_State newState)
            {
                if (newStateEx != curStateEx)
                {
                    prevStateEx = curStateEx;
                    curStateEx = newStateEx;
                    prevStateEx?.OnExit?.Invoke();
                    curStateEx.OnEnter?.Invoke();

                    if (newState != curState || (newState == curState && !curStateEx.ResumeConsecutive))
                    {
                        prevState = curState;
                        curState = newState;
                        prevState?.OnExitWithDefer();
                        curState.OnEnter();
                    }
                }
            }
            void ChangeBvr(Bvr newBvr)
            {
                if (newBvr != curBvr)
                {
                    prevBvr = curBvr;
                    curBvr = newBvr;
                }
            }
            bool ChangeFlow(Flow newFlow)
            {
                prevFlow = curFlow;
                curFlow = newFlow;

                // NOTE:
                // ※ 스택 오버플로우 주의!!!
                return ProcessFlowNode(curFlow);
            }
            bool ProcessFlowNode(Flow flowToProcess)
            {
                var curFlowNode = flowToProcess.GetCurNode(curBvr);

                // When no available node
                if (curFlowNode == null)
                    return false;

                // When waiting
                if (curFlowNode == Flow.waitingNode)
                    return true;

                // When not waiting
                if (curFlowNode is Flow.BvrNode bvrNode)
                {
                    var bvr = bvrNode.bvr;
                    ChangeBvr(bvr);

                    var stateData = bvr.GetCurState();

                    // When Bvr is done
                    if (stateData == null)
                    {
                        var transitionFlow = bvr.GetTransition();
                        if (transitionFlow != null)
                            return ChangeFlow(transitionFlow);
                        return false;
                    }
                    // When Bvr is not done
                    else
                    {
                        ChangeState(stateData.Value.stateEx, stateData.Value.state);
                        return true;
                    }
                }
                else if (curFlowNode is Flow.FlowNode flowNode)
                {
                    return ChangeFlow(flowNode.flow);
                }
                return false;
            }

            // Check begin flow
            if (!ProcessFlowNode(flow_begin))
            {
                // Check current flow
                if (!ProcessFlowNode(curFlow))
                {
                    // If no bvr is active, then set default state
                    ChangeState(defaultStateEx, defaultState);
                }
            }
        }
    }
}
