using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Icing
{
    public class GSM_State : MonoBehaviour
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
        protected virtual void OnExit() { }
        public virtual void OnEnter() { }
        public virtual void OnLateEnter() { }
        public virtual void OnUpdate() { }
        public virtual void OnLateUpdate() { }
        public virtual void OnFixedUpdate() { }
    }
}

