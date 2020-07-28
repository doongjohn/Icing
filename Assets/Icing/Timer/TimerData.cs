using System;
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

    public interface I_TimerData
    {
        GameObject User { get; }
        void Tick(float deltaTime);
    }
    public abstract class TimerData_Base<T> : I_TimerData
        where T : TimerData_Base<T>
    {
        private TimerTickMode tickMode;
        private Action onStart;
        private Action onTick;
        private Action onEnd;
        private float curTime = 0f;

        protected abstract float GetEndTime // 타이머의 종료 시간.
        { get; }

        public GameObject User
        { get; private set; }
        public bool IsActive
        { get; private set; } = true;
        public float CurTime // 타이머의 현재 시간.
        {
            get => curTime;
            set
            {
                curTime = Mathf.Clamp(value, 0, GetEndTime);
                IsEnded = value < GetEndTime;
            }
        }
        public bool IsEnded
        { get; private set; }
        public bool IsZero => CurTime == 0;


        public T SetTick(GameObject user, TimerTickMode tickMode = TimerTickMode.LateUpdate)
        {
            User = user;
            this.tickMode = tickMode;
            TimerManager.Inst.RemoveTimer(this, TimerTickMode.Update);
            TimerManager.Inst.RemoveTimer(this, TimerTickMode.LateUpdate);
            TimerManager.Inst.RemoveTimer(this, TimerTickMode.FixedUpdate);
            TimerManager.Inst.AddTimer(this, tickMode);
            return this as T;
        }
        public T SetAction(Action onStart = null, Action onTick = null, Action onEnd = null)
        {
            this.onStart = onStart;
            this.onTick = onTick;
            this.onEnd = onEnd;
            return this as T;
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

            return this as T;
        }
        public T Reset()
        {
            CurTime = 0;
            IsEnded = false;
            return this as T;
        }
        public T ToEnd()
        {
            CurTime = GetEndTime;
            IsEnded = true;
            return this as T;
        }
        public void Tick(float deltaTime)
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
    public class TimerData : TimerData_Base<TimerData>
    {
        public float EndTime = 0;
        protected override float GetEndTime => EndTime;
    }
    public class TimerStat : TimerData_Base<TimerStat>
    {
        public FloatStat EndTime;
        protected override float GetEndTime => EndTime.Value;
    }
}
