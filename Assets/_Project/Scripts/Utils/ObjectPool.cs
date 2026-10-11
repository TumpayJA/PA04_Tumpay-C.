using System.Collections.Generic;
using UnityEngine;

namespace EscapeGame
{
    /// <summary>
    /// OPTIMIZACIÓN: Object Pooling.
    /// En vez de crear (Instantiate) y destruir (Destroy) un efecto cada vez que se necesita, se crean unos
    /// cuantos al inicio, se desactivan, y se REUTILIZAN. Así se evita el trabajo extra de crear objetos y,
    /// sobre todo, la basura (GC.Alloc) que provoca tirones.
    ///
    /// Uso: ObjectPool.Instance.Spawn(prefab, posicion, rotacion);   // se devuelve solo cuando termina su vida.
    /// Va en el prefab GameSystems. Para comparar con y sin pool desmarca "Use Pooling" y mira el Profiler.
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance { get; private set; }

        [System.Serializable]
        public class Entry
        {
            public GameObject prefab;
            [Tooltip("Cuántas copias se crean (desactivadas) al empezar.")]
            public int prewarm = 6;
            [Tooltip("Segundos que dura el efecto antes de volver al pool.")]
            public float lifetime = 2f;
        }

        [SerializeField] List<Entry> entries = new List<Entry>();
        [Tooltip("Desmárcalo para comparar en el Profiler: usará Instantiate/Destroy en lugar del pool.")]
        [SerializeField] bool usePooling = true;

        readonly Dictionary<GameObject, Queue<PooledObject>> free = new Dictionary<GameObject, Queue<PooledObject>>();
        readonly Dictionary<GameObject, float> lifetimes = new Dictionary<GameObject, float>();

        /// <summary>Cuántos objetos se han creado en total (con pool debería quedarse casi fijo).</summary>
        public int TotalCreated { get; private set; }
        public bool UsePooling { get { return usePooling; } }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry == null || entry.prefab == null) continue;

                lifetimes[entry.prefab] = entry.lifetime;

                if (usePooling)
                {
                    Queue<PooledObject> queue = GetQueue(entry.prefab);
                    for (int n = 0; n < entry.prewarm; n++) queue.Enqueue(CreateInstance(entry.prefab));
                }
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Saca un objeto del pool (o lo crea si no queda ninguno libre) y lo activa en esa posición.</summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            float lifetime;
            if (!lifetimes.TryGetValue(prefab, out lifetime)) lifetime = 2f;

            PooledObject pooled;
            if (usePooling)
            {
                Queue<PooledObject> queue = GetQueue(prefab);
                pooled = queue.Count > 0 ? queue.Dequeue() : CreateInstance(prefab);
            }
            else
            {
                pooled = CreateInstance(prefab); // modo "sin pool", solo para comparar
            }

            pooled.transform.SetPositionAndRotation(position, rotation);
            pooled.Activate(lifetime, usePooling);
            return pooled.gameObject;
        }

        /// <summary>Lo llama PooledObject cuando termina su vida.</summary>
        public void Despawn(PooledObject pooled)
        {
            if (!pooled.Pooled)
            {
                Destroy(pooled.gameObject);
                return;
            }

            pooled.gameObject.SetActive(false);
            GetQueue(pooled.Prefab).Enqueue(pooled);
        }

        /// <summary>Solo para pruebas: lanza muchos efectos de golpe (lo usa DebugTools con F6).</summary>
        public void DebugBurst(int count, Vector3 center, float radius)
        {
            GameObject prefab = null;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].prefab != null)
                {
                    prefab = entries[i].prefab;
                    break;
                }
            }
            if (prefab == null) return;

            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Random.insideUnitCircle * radius;
                Spawn(prefab, center + new Vector3(offset.x, 1f, offset.y), Quaternion.identity);
            }
        }

        PooledObject CreateInstance(GameObject prefab)
        {
            GameObject go = Instantiate(prefab, transform);
            go.SetActive(false);

            PooledObject pooled = go.GetComponent<PooledObject>();
            if (pooled == null) pooled = go.AddComponent<PooledObject>();
            pooled.Setup(this, prefab);

            TotalCreated++;
            return pooled;
        }

        Queue<PooledObject> GetQueue(GameObject prefab)
        {
            Queue<PooledObject> queue;
            if (!free.TryGetValue(prefab, out queue))
            {
                queue = new Queue<PooledObject>();
                free[prefab] = queue;
            }
            return queue;
        }
    }
}
