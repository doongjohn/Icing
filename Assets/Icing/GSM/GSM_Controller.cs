using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;


namespace Icing
{

    public class GSM_Controller : MonoBehaviour
    {
        protected Flow curFlow;
        protected Flow prevFlow;
        protected Bvr curBvr;
        protected Bvr prevBvr;
        protected StateEx curStateEx;
        protected StateEx prevStateEx;
        protected GSM_State curState;
        protected GSM_State prevState;

        // Default
        protected Flow beginFlow = new Flow();
        protected GSM_State defaultState;
        protected StateEx defaultStateEx;


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

            public BvrSingle(StateEx stateEx, GSM_State state, Func<bool> isDone)
            {
                this.stateEx = stateEx;
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
                transitionFlowCondition = null;
                transitionFlow = null;
                IsWait = false;
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

            public Node GetCurNode(Bvr curBvr)
            {
                Node node;
                for (int i = 0; i < nodes.Count; i++)
                {
                    node = nodes[i];
                    if (curBvr?.IsWait ?? false)
                    {
                        if (node.forceRun && node.condition())
                            return node;
                    }
                    else
                    {
                        if (node.condition())
                            return node;
                    }
                }
                return null;
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


        protected virtual void Start()
        {
            GSM_Start();
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

        protected void SetStartFlow(Flow flow)
        {
            curFlow = flow;
        }
        protected void SetDefaultState(StateEx stateEx, GSM_State state)
        {
            defaultStateEx = stateEx;
            defaultState = state;
            curStateEx = stateEx;
            curState = state;
        }

        private void GSM_Start()
        {
            curStateEx.OnEnter?.Invoke();
            curState.OnEnter();
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
                if (curFlowNode == null)
                    return false;

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
                        {
                            return ChangeFlow(transitionFlow);
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
                else if (curFlowNode is Flow.FlowNode flowNode)
                {
                    return ChangeFlow(flowNode.flow);
                }
                return false;
            }

            // Check begin flow
            if (!ProcessFlowNode(beginFlow))
            {
                // Check current flow
                if (!ProcessFlowNode(curFlow))
                {
                    // If no bvr is active, then set default state
                    ChangeState(defaultStateEx, defaultState);
                }
            }
        }
        private void OnUpdate()
        {
            curStateEx?.OnUpdate?.Invoke();
            curState?.OnUpdate();
        }
        private void OnLateUpdate()
        {
            curStateEx?.OnLateUpdate?.Invoke();
            curState?.OnLateUpdate();
        }
        private void OnFixedUpdate()
        {
            curStateEx?.OnFixedUpdate?.Invoke();
            curState?.OnFixedUpdate();
        }
    }
}
