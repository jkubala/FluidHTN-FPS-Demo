using UnityEngine;

namespace FPSDemo.Utils
{
    public class PooledGameObject : MonoBehaviour
    {
        [SerializeField] private GameObjectPooler pool;
        [Tooltip("If <= 0 lifetime isn't used to auto-return pooled game object")][SerializeField] 
        private float lifetime = 0;

        private float age = 0.0f;

        private void Update()
        {
            if (pool == null)
            {
                return;
            }

            if (lifetime > 0)
            {
                age += Time.deltaTime;
                if (age >= lifetime)
                {
                    ReturnToPool();
                }
            }
        }

        public void SetPool(GameObjectPooler pool)
        {
            this.pool = pool;
        }

        public void ReturnToPool()
        {
            if (pool == null)
            {
                return;
            }

            pool.ReturnInstance(gameObject);
            age = 0.0f;
        }
    }
}