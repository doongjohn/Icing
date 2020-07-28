using System;
using System.Collections.Generic;
using UnityEngine;

namespace Icing
{
    public abstract class CSM_State : MonoBehaviour
    {
        private Stack<Action> deferStack = new Stack<Action>();

        public abstract void Init(CSM_Data data);

        public void OnExitWithDefer()
        {
            OnExit();
            for (int i = 0; i < deferStack.Count; i++)
                deferStack.Pop()();
        }
        protected void Defer(Action action)
        {
            deferStack.Push(action);
        }

        public virtual void OnEnter() { }
        public virtual void OnLateEnter() { }
        protected virtual void OnExit() { }
        public virtual void OnUpdate() { }
        public virtual void OnLateUpdate() { }
        public virtual void OnFixedUpdate() { }
    }
}
