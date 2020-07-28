using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Icing
{
    public class TimerManager : SingletonBase<TimerManager>
    {
        private readonly List<I_TimerData> timers_Update = new List<I_TimerData>();
        private readonly List<I_TimerData> timers_LateUpdate = new List<I_TimerData>();
        private readonly List<I_TimerData> timers_FixedUpdate = new List<I_TimerData>();


        private void Update()
        {
            TickTimers(timers_Update, Time.deltaTime);
        }
        private void LateUpdate()
        {
            TickTimers(timers_LateUpdate, Time.deltaTime);
        }
        private void FixedUpdate()
        {
            TickTimers(timers_FixedUpdate, Time.fixedDeltaTime);
        }

        public void TickTimers(List<I_TimerData> timers, float deltaTime)
        {
            if (timers.Count == 0)
                return;

            for (int i = timers.Count - 1; i >= 0; i--)
            {
                // Check Game Object
                if (timers[i].User == null)
                {
                    timers.RemoveAt(i);
                    continue;
                }

                // Run Timer
                timers[i].Tick(deltaTime);
            }
        }
        public void AddTimer(I_TimerData data, TimerTickMode mode)
        {
            if (data == null || mode == TimerTickMode.Manual)
                return;

            switch (mode)
            {
                case TimerTickMode.Update:
                    if (!timers_Update.Contains(data)) timers_Update.Add(data);
                    break;
                case TimerTickMode.LateUpdate:
                    if (!timers_LateUpdate.Contains(data)) timers_LateUpdate.Add(data);
                    break;
                case TimerTickMode.FixedUpdate:
                    if (!timers_FixedUpdate.Contains(data)) timers_FixedUpdate.Add(data);
                    break;
            }
        }
        public void RemoveTimer(I_TimerData data, TimerTickMode mode)
        {
            if (data == null || mode == TimerTickMode.Manual)
                return;

            switch (mode)
            {
                case TimerTickMode.Update:
                    if (timers_Update.Contains(data)) timers_Update.Remove(data);
                    break;
                case TimerTickMode.LateUpdate:
                    if (timers_LateUpdate.Contains(data)) timers_LateUpdate.Remove(data);
                    break;
                case TimerTickMode.FixedUpdate:
                    if (timers_FixedUpdate.Contains(data)) timers_FixedUpdate.Remove(data);
                    break;
            }
        }
    }
}
