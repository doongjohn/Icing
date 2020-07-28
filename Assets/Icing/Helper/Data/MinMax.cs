using UnityEngine;

namespace Icing
{
    public struct IntMinMax
    {
        private bool recalc;
        private int value, min, max;

        public int Value
        {
            get
            {
                if (recalc)
                {
                    value = Mathf.Clamp(value, min, max);
                    recalc = false;
                }
                return value;
            }
            set { this.value = Mathf.Clamp(value, min, max); }
        }
        public int Min
        {
            get => min;
            set
            {
                min = Mathf.Min(value, max);
                recalc = true;
            }
        }
        public int Max
        {
            get => max;
            set
            {
                max = Mathf.Max(value, min);
                recalc = true;
            }
        }


        public IntMinMax(int value, int min = int.MinValue, int max = int.MaxValue)
        {
            recalc = true;
            this.value = value;
            this.min = min;
            this.max = max;
        }
    }
    public struct FloatMinMax
    {
        private bool recalc;
        private float value, min, max;

        public float Value
        {
            get
            {
                if (recalc)
                {
                    value = Mathf.Clamp(value, min, max);
                    recalc = false;
                }
                return value;
            }
            set { this.value = Mathf.Clamp(value, min, max); }
        }
        public float Min
        {
            get => min;
            set
            {
                min = Mathf.Min(value, max);
                recalc = true;
            }
        }
        public float Max
        {
            get => max;
            set
            {
                max = Mathf.Max(value, min);
                recalc = true;
            }
        }


        public FloatMinMax(float value, float min = float.NegativeInfinity, float max = float.PositiveInfinity)
        {
            recalc = true;
            this.value = value;
            this.min = min;
            this.max = max;
        }
    }

    public struct Int0Max
    {
        private bool recalc;
        private int value, max;

        public int Value
        {
            get
            {
                if (recalc)
                {
                    value = Mathf.Clamp(value, 0, max);
                    recalc = false;
                }
                return value;
            }
            set { this.value = Mathf.Clamp(value, 0, max); }
        }
        public int Max
        {
            get => max;
            set
            {
                max = Mathf.Max(value, 0);
                recalc = true;
            }
        }


        public Int0Max(int value, int max = int.MaxValue)
        {
            recalc = true;
            this.value = value;
            this.max = max;
        }
    }
    public struct Float0Max
    {
        private bool recalc;
        private float value, max;

        public float Value
        {
            get
            {
                if (recalc)
                {
                    value = Mathf.Clamp(value, 0, max);
                    recalc = false;
                }
                return value;
            }
            set { this.value = Mathf.Clamp(value, 0, max); }
        }
        public float Max
        {
            get => max;
            set
            {
                max = Mathf.Max(value, 0);
                recalc = true;
            }
        }


        public Float0Max(float value, float max = float.PositiveInfinity)
        {
            recalc = true;
            this.value = value;
            this.max = max;
        }
    }
}
