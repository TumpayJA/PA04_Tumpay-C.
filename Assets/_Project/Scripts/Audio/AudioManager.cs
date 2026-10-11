using UnityEngine;

namespace EscapeGame
{
    /// <summary>
    /// Todo el sonido del juego en un solo lugar: música, ambiente, latido de tensión, efectos y sonidos de victoria/derrota.
    /// Crea sus propios AudioSources, así que solo hay que asignar los clips.
    /// Si un clip no está asignado, simplemente no suena (no da errores). Va en el prefab GameSystems.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Música y ambiente (se repiten en bucle)")]
        [SerializeField] AudioClip musicClip;
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.3f;
        [SerializeField] AudioClip ambientClip;
        [SerializeField, Range(0f, 1f)] float ambientVolume = 0.5f;

        [Header("Tensión (latido que sube cuando el enemigo se acerca)")]
        [SerializeField] AudioClip heartbeatClip;
        [SerializeField, Range(0f, 1f)] float maxHeartbeatVolume = 0.8f;

        [Header("Resultado")]
        [SerializeField] AudioClip winClip;
        [SerializeField] AudioClip loseClip;

        [Header("Efectos")]
        [SerializeField, Range(0f, 1f)] float sfxVolume = 1f;

        AudioSource music;
        AudioSource ambient;
        AudioSource heartbeat;
        AudioSource sfx;
        GameManager gm;

        float tensionReport;
        float tensionCurrent;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            music = CreateSource("Music", true, musicClip, musicVolume);
            ambient = CreateSource("Ambient", true, ambientClip, ambientVolume);
            heartbeat = CreateSource("Heartbeat", true, heartbeatClip, 0f);
            sfx = CreateSource("SFX", false, null, 1f);
        }

        void Start()
        {
            gm = GameManager.Instance;
            if (gm != null) gm.StateChanged += OnStateChanged;
        }

        void OnDestroy()
        {
            if (gm != null) gm.StateChanged -= OnStateChanged;
            if (Instance == this) Instance = null;
        }

        void LateUpdate()
        {
            // Los enemigos "reportan" su nivel de tensión cada fotograma; nos quedamos con el mayor y lo suavizamos.
            tensionCurrent = Mathf.MoveTowards(tensionCurrent, tensionReport, Time.unscaledDeltaTime * 1.5f);
            tensionReport = 0f;

            if (heartbeat != null && heartbeat.clip != null)
            {
                heartbeat.volume = tensionCurrent * maxHeartbeatVolume;
                heartbeat.pitch = Mathf.Lerp(0.9f, 1.3f, tensionCurrent);
            }
        }

        /// <summary>0 = tranquilo, 1 = el enemigo está encima. Lo llama EnemyAI.</summary>
        public void ReportTension(float value)
        {
            tensionReport = Mathf.Max(tensionReport, value);
        }

        /// <summary>Efecto 2D (se oye igual en todas partes): clics, recoger, linterna...</summary>
        public void PlaySfx(AudioClip clip, float volume)
        {
            if (clip == null || sfx == null) return;
            sfx.PlayOneShot(clip, volume * sfxVolume);
        }

        /// <summary>Efecto 3D en una posición del mundo (puerta, orbe...).</summary>
        public void PlaySfxAt(AudioClip clip, Vector3 position, float volume)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, volume * sfxVolume);
        }

        void OnStateChanged(GameState state)
        {
            if (state == GameState.Won || state == GameState.Lost)
            {
                if (heartbeat != null) heartbeat.Stop();
                if (music != null) music.volume = musicVolume * 0.3f;
                PlaySfx(state == GameState.Won ? winClip : loseClip, 1f);
            }
        }

        AudioSource CreateSource(string sourceName, bool loop, AudioClip clip, float volume)
        {
            GameObject child = new GameObject("Audio_" + sourceName);
            child.transform.SetParent(transform, false);

            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.volume = volume;
            source.clip = clip;

            if (clip != null && loop) source.Play();
            return source;
        }
    }
}
