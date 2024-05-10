using System;
using UnityEngine;

namespace BA2LW.Utils
{
    /// <summary>
    /// Generic singleton base class for global, single-instance MonoBehaviours.
    /// </summary>
    /// <typeparam name="T">The class type to define</typeparam>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        static T instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<T>();

                    if (instance == null)
                    {
                        instance = new GameObject(
                            $"{typeof(T).Name} [Singleton]",
                            typeof(T)
                        ).GetComponent<T>();
                    }
                }

                DontDestroyOnLoad(instance.gameObject);
                return instance;
            }
        }

        protected virtual void OnInitialize()
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<T>();

                if (instance == null)
                    instance = this as T;

                DontDestroyOnLoad(instance.gameObject);
            }
            else if (instance != this)
                Destroy(instance.gameObject);
        }

        void Awake()
        {
            OnInitialize();
        }

        // Clear the instance field when destroyed.
        protected virtual void OnDestroy() => instance = null;
    }
}
