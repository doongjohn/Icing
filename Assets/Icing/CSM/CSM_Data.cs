using UnityEngine;

namespace Icing
{
    [System.Serializable]
    public abstract class CSM_Data
    {
        public abstract void Init_Awake(GameObject gameObject);
        public abstract void Init_Start(GameObject gameObject);
    }
}
