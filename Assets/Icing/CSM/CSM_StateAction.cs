using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Icing
{
    public abstract partial class CSM_Controller<D, S> : MonoBehaviour
        where D : CSM_Data, new()
        where S : CSM_State
    {
        protected class StateAction
        {
            public Action Enter
            { get; private set; }
            public Action Exit
            { get; private set; }
            public Action Update
            { get; private set; }
            public Action FixedUpdate
            { get; private set; }

            public StateAction(Action enter = null, Action exit = null, Action update = null, Action fixedUpdate = null)
            {
                Enter = enter;
                Exit = exit;
                Update = update;
                FixedUpdate = fixedUpdate;
            }
            public StateAction Clone()
            {
                return new StateAction()
                {
                    Enter = Enter,
                    Exit = Exit,
                    Update = Update,
                    FixedUpdate = FixedUpdate
                };
            }
        }

        protected static StateAction EMPTY_STATE_ACTION => new StateAction();
    }
}
