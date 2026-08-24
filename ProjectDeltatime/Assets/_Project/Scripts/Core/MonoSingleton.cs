using UnityEngine;

namespace Deltatime.Core
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T instance;
        private static bool applicationQuitting;

        public static bool HasInstance => instance != null;

        public static T Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                if (applicationQuitting)
                {
                    return null;
                }

                T found = FindFirstObjectByType<T>(FindObjectsInactive.Include);
                if (found == null)
                {
                    GameObject owner = new GameObject(typeof(T).Name);
                    found = owner.AddComponent<T>();
                }

                instance = found;
                return instance;
            }
        }

        protected virtual bool PersistAcrossScenes => true;

        protected virtual void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = (T)this;
            applicationQuitting = false;

            if (PersistAcrossScenes && transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            applicationQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
