using System.Collections.Generic;
using UnityEngine;

namespace Icing
{
    public class BoolCount
    {
        private int count = 0;
        public bool Value => count != 0;

        public void Set(bool value)
        {
            count += value ? 1 : -1;
            count = Mathf.Max(count, 0);
        }
        public void Reset()
        {
            count = 0;
        }
    }
}
