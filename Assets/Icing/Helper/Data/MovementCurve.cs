using System;
using UnityEngine;

namespace Icing
{
    [Serializable]
    public class MovementCurve
    {
        public AnimationCurve curve;
        [HideInInspector] public float curTime;

        public float Value => curve.Evaluate(curTime);
        public float EndTime => curve.keys[curve.length - 1].time;
        public bool IsEnded => curve.keys[curve.length - 1].time <= curTime;
    }
}
