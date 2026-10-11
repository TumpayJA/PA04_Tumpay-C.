using UnityEngine;

namespace EscapeGame
{
    /// <summary>
    /// Orbe coleccionable. Gira y flota; al tocarlo el jugador suma 1 al objetivo,
    /// suena un sonido y sale un efecto de partículas (reutilizado desde el ObjectPool).
    /// Necesita un Collider con "Is Trigger" activado (el prefab Orb ya lo trae).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Collectible : MonoBehaviour
    {
        [Header("Zona")]
        [Tooltip("A qué zona pertenece este orbe (1, 2, ...). Hay que recoger todos los de la zona actual para abrir su puerta.")]
        [SerializeField] int stage = 1;

        [Header("Animación")]
        [Tooltip("Hijo que gira y flota. Si se deja vacío se anima este mismo objeto.")]
        [SerializeField] Transform visual;
        [SerializeField] float rotateSpeed = 90f;
        [SerializeField] float bobHeight = 0.12f;
        [SerializeField] float bobSpeed = 2f;

        [Header("Feedback")]
        [SerializeField] AudioClip pickupClip;
        [SerializeField, Range(0f, 1f)] float pickupVolume = 1f;
        [Tooltip("Prefab de partículas que sale al recogerlo (se reproduce desde el ObjectPool).")]
        [SerializeField] GameObject pickupEffectPrefab;

        public int Stage { get { return stage; } }

        bool collected;
        Vector3 visualStart;
        float phase;

        void Reset()
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        void Awake()
        {
            if (visual == null) visual = transform;
            visualStart = visual.localPosition;
            phase = Random.value * Mathf.PI * 2f;
        }

        void Update()
        {
            if (collected) return;

            visual.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
            visual.localPosition = visualStart + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + phase) * bobHeight);
        }

        void OnTriggerEnter(Collider other)
        {
            if (collected || !other.CompareTag("Player")) return;
            Collect();
        }

        /// <summary>Recoge el orbe (también lo usa DebugTools para probar rápido).</summary>
        public void Collect()
        {
            if (collected) return;
            collected = true;

            if (GameManager.Instance != null) GameManager.Instance.CollectOrb(stage);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySfxAt(pickupClip, transform.position, pickupVolume);

            if (pickupEffectPrefab != null)
            {
                if (ObjectPool.Instance != null)
                {
                    ObjectPool.Instance.Spawn(pickupEffectPrefab, visual.position, Quaternion.identity);
                }
                else
                {
                    // Sin pool en la escena: plan B (menos eficiente, pero funciona).
                    GameObject fx = Instantiate(pickupEffectPrefab, visual.position, Quaternion.identity);
                    Destroy(fx, 3f);
                }
            }

            gameObject.SetActive(false);
        }
    }
}
