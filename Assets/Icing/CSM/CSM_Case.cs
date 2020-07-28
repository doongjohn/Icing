using System;
using System.Collections.Generic;
using UnityEngine;

namespace Icing
{
    public abstract partial class CSM_Controller<D, S> : MonoBehaviour
        where D : CSM_Data, new()
        where S : CSM_State
    {
        protected class Flow
        {
            public List<Case> Cases
            { get; private set; } = new List<Case>();

            public Flow AddCase(Case @case)
            {
                Cases.Add(@case);
                return this;
            }
        }

        protected class Case
        {
            public Func<bool> Condition
            { get; private set; }
            public bool IsWait
            { get; private set; } = false;
            public bool IsAlwaysCheck
            { get; private set; } = false;
            public bool IsResume
            { get; private set; } = false;

            public CSM_Behaviour Behaviour
            { get; private set; } = null;
            public Flow NextFlow
            { get; private set; } = null;

            public static Case New<T>(Func<bool> condition, T behaviour) where T : CSM_Behaviour
            {
                return new Case()
                {
                    Condition = condition,
                    Behaviour = behaviour.Clone<T>()
                };
            }
            public static Case New(Func<bool> condition)
            {
                return new Case()
                {
                    Condition = condition,
                    Behaviour = null
                };
            }
            public Case Clone()
            {
                return this.MemberwiseClone() as Case;
            }

            public Case Not()
            {
                var clone = this.Clone();
                clone.Condition = () => !Condition();
                return clone;
            }
            public Case Wait()
            {
                var clone = this.Clone();
                clone.IsWait = true;
                return clone;
            }
            public Case AlwaysCheck()
            {
                var clone = this.Clone();
                clone.IsAlwaysCheck = true;
                return clone;
            }
            public Case Resume()
            {
                var clone = this.Clone();
                clone.IsResume = true;
                return clone;
            }
            public Case SetNextFlow(Flow flow)
            {
                var clone = this.Clone();
                clone.NextFlow = flow;
                return clone;
            }

            public (CSM_Behaviour behaviour, CSM_State state, StateAction stateAction) GetCurrent(CSM_State currentState)
            {
                if (Behaviour == null)
                    return default;

                var (state, stateAction) = Behaviour.GetCurrent(currentState);
                return (Behaviour, state, stateAction);
            }
        }
    }
}
