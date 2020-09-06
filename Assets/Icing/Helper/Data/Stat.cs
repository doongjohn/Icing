using UnityEngine;

namespace Icing
{
    public struct IntStat
    {
        private bool recalc;
        private int baseValue, value, min, max;
        private IntMinMax modFlat;
        private FloatMinMax modPercent;

        public int BaseValue          // 기본 값
        {
            get => baseValue;
            set { baseValue = Mathf.Clamp(value, min, max); }
        }
        public int BaseMin            // 기본 최솟값
        { get; } 
        public int BaseMax            // 기본 최댓값
        { get; }
        public int Value              // 현재 값
        {
            get
            {
                if (recalc)
                {
                    value = Mathf.Clamp((int)(baseValue * (1 + (modPercent.Value * 0.01f)) + 0.5f) + modFlat.Value, min, max);
                    recalc = false;
                }
                return value;
            }
        }
        public int Min                // 현재 최솟값
        {
            get => min;
            set
            {
                min = Mathf.Min(value, max);
                baseValue = Mathf.Max(baseValue, min);
                recalc = true;
            }
        }
        public int Max                // 현재 최댓값
        {
            get => max;
            set
            {
                max = Mathf.Max(value, min);
                baseValue = Mathf.Min(baseValue, max);
                recalc = true;
            }
        }
        public IntMinMax ModFlat      // 단순히 증감함. 예) ModFlat = 1 -> BaseValue + 1
        {
            get => modFlat;
            set
            {
                modFlat = value;
                recalc = true;
            }
        }
        public FloatMinMax ModPercent // 퍼센트로 증감함. 예) ModPercent = 100 -> BaseValue의 100%(2배)
        {
            get => modPercent;
            set
            {
                modPercent = value;
                recalc = true;
            }
        }
        public bool IsMin => Value == Min;
        public bool IsMax => Value == Max;

        public IntStat(int baseValue, IntMinMax modFlat, FloatMinMax modPercent, int min = int.MinValue, int max = int.MaxValue)
        {
            recalc = true;
            value = 0;
            BaseMin = min;
            BaseMax = max;
            this.baseValue = baseValue;
            this.min = min;
            this.max = max;
            this.modFlat = modFlat;
            this.modPercent = modPercent;
        }
        public void Reset()
        {
            Min = BaseMin;
            Max = BaseMax;
            modFlat.Value = 0;
            modPercent.Value = 0;
        }
        public void ResetMinMax()
        {
            Min = BaseMin;
            Max = BaseMax;
        }
        public void ResetMod()
        {
            modFlat.Value = 0;
            modPercent.Value = 0;
            recalc = true;
        }
    }
    public struct FloatStat
    {
        private bool recalc;
        private float baseValue, value, min, max;
        private FloatMinMax modFlat;
        private FloatMinMax modPercent;

        public float BaseValue
        {
            get => baseValue;
            set { baseValue = Mathf.Clamp(value, min, max); }
        }
        public float BaseMin
        { get; }
        public float BaseMax
        { get; }
        public float Value
        {
            get
            {
                if (recalc)
                {
                    value = Mathf.Clamp((baseValue * (1 + modPercent.Value * 0.01f)) + modFlat.Value, min, max);
                    recalc = false;
                }
                return value;
            }
        }
        public float Min
        {
            get => min;
            set
            {
                min = Mathf.Min(value, max);
                baseValue = Mathf.Max(baseValue, min);
                recalc = true;
            }
        }
        public float Max
        {
            get => max;
            set
            {
                max = Mathf.Max(value, min);
                baseValue = Mathf.Min(baseValue, max);
                recalc = true;
            }
        }
        public FloatMinMax ModFlat
        {
            get => modFlat;
            set
            {
                modFlat = value;
                recalc = true;
            }
        }
        public FloatMinMax ModPercent
        {
            get => modPercent;
            set
            {
                modPercent = value;
                recalc = true;
            }
        }
        public bool IsMin => Value == Min;
        public bool IsMax => Value == Max;

        public FloatStat(float baseValue, FloatMinMax modFlat, FloatMinMax modPercent, float min = float.MinValue, float max = float.MaxValue)
        {
            recalc = true;
            value = 0;
            BaseMin = min;
            BaseMax = max;
            this.baseValue = baseValue;
            this.min = min;
            this.max = max;
            this.modFlat = modFlat;
            this.modPercent = modPercent;
        }
        public void Reset()
        {
            Min = BaseMin;
            Max = BaseMax;
            modFlat.Value = 0;
            modPercent.Value = 0;
        }
        public void ResetMinMax()
        {
            Min = BaseMin;
            Max = BaseMax;
        }
        public void ResetMod()
        {
            modFlat.Value = 0;
            modPercent.Value = 0;
            recalc = true;
        }
    }
}
