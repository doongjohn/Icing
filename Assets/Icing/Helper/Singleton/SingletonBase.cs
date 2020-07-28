using UnityEngine;

namespace Icing
{
    public abstract class SingletonBase<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Inst { get; private set; }

        protected virtual void Awake()
        {
            Inst = this as T;
        }
    }
}

