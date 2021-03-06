﻿using System;
using UnityEngine;


namespace Icing
{
    public enum TimerTickMode
    {
        Update,
        LateUpdate,
        FixedUpdate,
        Manual
    }
    public interface ITimer
    {
        GameObject Owner { get; }
        void Tick(float deltaTime);
    }
    public abstract class TimerBase<T> : ITimer
        where T : TimerBase<T>
    {
        protected TimerTickMode tickMode;
        protected Action onStart = null;
        protected Action onTick = null;
        protected Action onEnd = null;
        protected float curTime = 0f;

        public GameObject Owner
        { get; protected set; }
        public bool IsActive
        { get; protected set; } = true;
        public float CurTime
        {
            get => curTime;
            set
            {
                curTime = Mathf.Clamp(value, 0, GetEndTime);
                IsEnded = value < GetEndTime;
            }
        }
        protected abstract float GetEndTime
        { get; }
        public bool IsEnded
        { get; protected set; } = false;
        public bool IsZero => CurTime == 0;

        public TimerBase(GameObject owner, TimerTickMode tickMode = TimerTickMode.LateUpdate)
        {
            Owner = owner;
            TimerManager.Inst.AddTimer(this, tickMode);
            this.tickMode = tickMode;
        }

        public T SetTickMode(TimerTickMode tickMode)
        {
            TimerManager.Inst.RemoveTimer(this, this.tickMode);
            TimerManager.Inst.AddTimer(this, tickMode);
            this.tickMode = tickMode;
            return (T)this;
        }
        public T SetActive(bool active)
        {
            IsActive = active;

            if (tickMode == TimerTickMode.Manual)
                return this as T;

            if (active == false)
                TimerManager.Inst.RemoveTimer(this, tickMode);
            else
                TimerManager.Inst.AddTimer(this, tickMode);

            return (T)this;
        }
        public T OnStart(Action onStart)
        {
            this.onStart = onStart;
            return (T)this;
        }
        public T OnTick(Action onTick)
        {
            this.onTick = onTick;
            return (T)this;
        }
        public T OnEnd(Action onEnd)
        {
            this.onEnd = onEnd;
            return (T)this;
        }

        public T Reset()
        {
            CurTime = 0;
            IsEnded = false;
            return (T)this;
        }
        public T ToEnd()
        {
            CurTime = GetEndTime;
            IsEnded = true;
            return (T)this;
        }

        void ITimer.Tick(float deltaTime)
        {
            if (!IsActive || IsEnded)
                return;

            if (CurTime == 0)
                onStart?.Invoke();

            CurTime += deltaTime;
            onTick?.Invoke();

            if (CurTime >= GetEndTime)
            {
                CurTime = GetEndTime;
                IsEnded = true;
                onEnd?.Invoke();
            }
        }
    }

    [Serializable]
    public class Timer : TimerBase<Timer>
    {
        public float EndTime = 0;
        protected override float GetEndTime => EndTime;

        public Timer(GameObject owner, TimerTickMode tickMode = TimerTickMode.LateUpdate)
            : base(owner, tickMode) { }
    }

    public class TimerStat : TimerBase<TimerStat>
    {
        public FloatStat EndTime;
        protected override float GetEndTime => EndTime.Value;

        public TimerStat(GameObject owner, TimerTickMode tickMode = TimerTickMode.LateUpdate)
            : base(owner, tickMode) { }
    }
}
