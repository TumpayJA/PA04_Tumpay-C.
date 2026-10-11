using UnityEngine;

namespace EscapeGame
{
    /// <summary>
    /// Lo añade el ObjectPool automáticamente a cada objeto que crea (no hace falta ponerlo a mano).
    /// Cuenta su tiempo de vida y, al terminar, lo devuelve al pool.
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        public GameObject Prefab { get; private set; }
        public bool Pooled { get; private set; }

        ObjectPool pool;
        ParticleSystem[] particles;
        float timer;
        bool active;

        public void Setup(ObjectPool owner, GameObject prefab)
        {
            pool = owner;
            Prefab = prefab;
            particles = GetComponentsInChildren<ParticleSystem>(true); // se guardan una vez: reutilizar no genera basura
        }

        public void Activate(float lifetime, bool pooled)
        {
            Pooled = pooled;
            timer = lifetime;
            active = true;

            gameObject.SetActive(true);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Clear(true);
                particles[i].Play(true);
            }
        }

        void Update()
        {
            if (!active) return;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                active = false;
                pool.Despawn(this);
            }
        }
    }
}
