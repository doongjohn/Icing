using System;
using System.Collections.Generic;
using UnityEngine;

namespace Icing
{
    public abstract partial class CSM_Controller<D, S> : MonoBehaviour
        where D : CSM_Data, new()
        where S : CSM_State
    {
        #region Class: Behaviour
        protected abstract class CSM_Behaviour
        {
            public T Clone<T>() where T : CSM_Behaviour
            {
                return MemberwiseClone() as T; // 안에 State들을 바꿀 일이 없기 때문에 DeepCopy 할 필요 없음.
            }

            public virtual void OnBehaviourEnd() { }
            public abstract (CSM_State state, StateAction stateAction) GetCurrent(CSM_State currentState);
        }
        #endregion

        #region Class: Behaviour -> Single
        protected sealed class Single : CSM_Behaviour
        {
            private (CSM_State state, StateAction stateAction, Func<bool> done) data;

            // Ctor
            public Single(CSM_State state, StateAction stateAction, Func<bool> done)
            {
                if (state == null)
                {
                    Debug.LogError($"State is Null!");
                    return;
                }

                data = (state, stateAction, done);
            }

            // Behaviour
            public override (CSM_State state, StateAction stateAction) GetCurrent(CSM_State currentState)
            {
                if (data.done())
                    return default;

                return (data.state, data.stateAction);
            }
        }
        #endregion

        #region Class: Behaviour -> Process
        protected sealed class Process : CSM_Behaviour
        {
            private List<(CSM_State state, StateAction stateAction, Func<bool> done)> data;

            // Ctor
            public Process(params (CSM_State state, StateAction stateAction, Func<bool> done)[] data)
            {
                this.data = new List<(CSM_State state, StateAction stateAction, Func<bool> done)>();

                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i].state == null)
                    {
                        Debug.LogError($"{data[i].state.GetType().Name} is Null!");
                        continue;
                    }

                    this.data.Add(data[i]);
                }
            }

            // Behaviour
            public override (CSM_State state, StateAction stateAction) GetCurrent(CSM_State currentState)
            {
                for (int i = 0; i < data.Count; i++)
                {
                    if (data[i].done())
                        continue;

                    return (data[i].state, data[i].stateAction);
                }

                return default;
            }
        }
        #endregion

        #region Class: Behaviour -> Sequence
        protected sealed class Sequence : CSM_Behaviour
        {
            private List<(CSM_State state, StateAction stateAction, Func<bool> done)> data;
            private int curIndex = 0;
            private bool restart = true;

            // Ctor
            public Sequence(bool restart, params (CSM_State state, StateAction stateAction, Func<bool> done)[] data)
            {
                this.restart = restart;
                this.data = new List<(CSM_State state, StateAction stateAction, Func<bool> done)>();

                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i].state == null)
                    {
                        Debug.LogError($"{data[i].state.GetType().Name} is Null!");
                        continue;
                    }

                    this.data.Add(data[i]);
                }
            }
            public Sequence(bool restart, int repeat, CSM_State state, StateAction stateAction, Func<bool> done)
            {
                if (state == null)
                {
                    Debug.LogError($"{state.GetType().Name} is Null!");
                }

                this.restart = restart;
                data = new List<(CSM_State state, StateAction stateAction, Func<bool> done)>();

                for (int i = 0; i < repeat; i++)
                    data.Add((state, stateAction.Clone(), done));
            }

            // Behaviour
            public void Restart()
            {
                curIndex = 0;
            }
            public override void OnBehaviourEnd()
            {
                if (restart)
                    curIndex = 0;
            }
            public override (CSM_State state, StateAction stateAction) GetCurrent(CSM_State currentState)
            {
                if (data[curIndex].done())
                {
                    if (curIndex != data.Count - 1)
                    {
                        curIndex++;
                    }
                    else
                    {
                        return default;
                    }
                }

                return (data[curIndex].state, data[curIndex].stateAction);
            }
        }
        #endregion

        #region Class: Behaviour -> Choice
        protected sealed class Choice : CSM_Behaviour
        {
            private Dictionary<CSM_State, (StateAction stateAction, Func<CSM_State> getNext)> data;
            private (CSM_State state, StateAction stateAction) defaultData;

            // Ctor
            public Choice(
                (CSM_State state, StateAction stateAction, Func<CSM_State> getNext) defaultData,
                params (CSM_State state, StateAction stateAction, Func<CSM_State> next)[] data)
            {
                this.data = new Dictionary<CSM_State, (StateAction, Func<CSM_State>)>();

                // Check Default Null
                if (defaultData.state == null)
                {
                    Debug.LogError($"{defaultData.state.GetType().Name} is Null!");
                    return;
                }

                // Add Default
                this.defaultData = (defaultData.state, defaultData.stateAction);
                this.data.Add(defaultData.state, (defaultData.stateAction, defaultData.getNext));

                // Add States
                for (int i = 0; i < data.Length; i++)
                {
                    // Check Null
                    if (data[i].state == null)
                    {
                        Debug.LogError($"{data[i].state.GetType().Name} is Null!");
                        continue;
                    }
                    if (this.data.ContainsKey(data[i].state))
                        continue;

                    this.data.Add(data[i].state, (data[i].stateAction, data[i].next));
                }
            }

            // Behaviour
            public override (CSM_State state, StateAction stateAction) GetCurrent(CSM_State currentState)
            {
                if (currentState == null || !data.ContainsKey(currentState))
                    currentState = defaultData.state;

                CSM_State nextState = data[currentState].getNext();

                if (nextState == null)
                    return default;

                return (nextState, data[nextState].stateAction);
            }
        }
        #endregion
    }
}
