using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Icing
{
    public class TimerManager : SingletonBase<TimerManager>
    {
        private readonly List<ITimer> timers_Update = new List<ITimer>();
        private readonly List<ITimer> timers_LateUpdate = new List<ITimer>();
        private readonly List<ITimer> timers_FixedUpdate = new List<ITimer>();

        private void Update()
        {
            TickTimers(timers_Update);
        }
        private void LateUpdate()
        {
            TickTimers(timers_LateUpdate);
        }
        private void FixedUpdate()
        {
            TickTimers(timers_FixedUpdate);
        }

        public void TickTimers(List<ITimer> timers)
        {
            if (timers.Count == 0)
                return;

            float deltaTime = Time.deltaTime;
            for (int i = timers.Count - 1; i >= 0; i--)
            {
                // Check Game Object
                if (timers[i].Owner == null)
                {
                    timers.RemoveAt(i);
                    continue;
                }

                // Run Timer
                timers[i].Tick(deltaTime);
            }
        }
        public void AddTimer(ITimer data, TimerTickMode mode)
        {
            if (data == null || mode == TimerTickMode.Manual)
                return;

            switch (mode)
            {
                case TimerTickMode.Update:
                    if (!timers_Update.Contains(data))
                        timers_Update.Add(data);
                    break;
                case TimerTickMode.LateUpdate:
                    if (!timers_LateUpdate.Contains(data))
                        timers_LateUpdate.Add(data);
                    break;
                case TimerTickMode.FixedUpdate:
                    if (!timers_FixedUpdate.Contains(data))
                        timers_FixedUpdate.Add(data);
                    break;
            }
        }
        public void RemoveTimer(ITimer data, TimerTickMode mode)
        {
            if (data == null || mode == TimerTickMode.Manual)
                return;

            switch (mode)
            {
                case TimerTickMode.Update:
                    if (timers_Update.Contains(data))
                        timers_Update.Remove(data);
                    break;
                case TimerTickMode.LateUpdate:
                    if (timers_LateUpdate.Contains(data))
                        timers_LateUpdate.Remove(data);
                    break;
                case TimerTickMode.FixedUpdate:
                    if (timers_FixedUpdate.Contains(data))
                        timers_FixedUpdate.Remove(data);
                    break;
            }
        }
    }
}
