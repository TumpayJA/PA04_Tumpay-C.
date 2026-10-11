using UnityEngine;

namespace EscapeGame
{
    /// <summary>
    /// Puerta de salida de una zona. Empieza bloqueada (luz roja). Cuando el jugador junta todos los orbes de SU zona
    /// se pone verde; con el jugador cerca, la tecla E la abre. Después hay que cruzarla hasta el ExitZone:
    /// si no es la última zona se pasa a la siguiente; si es la última, se gana.
    /// La puerta se abre hacia donde apunta la flecha AZUL (eje Z) del prefab.
    /// Necesita un Collider con "Is Trigger" (zona de interacción alrededor de la puerta).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ExitDoor : MonoBehaviour
    {
        [Header("Zona")]
        [Tooltip("A qué zona pertenece esta puerta. La puerta de la zona 1 lleva a la 2; la de la última zona es la salida final.")]
        [SerializeField] int stage = 1;

        [Header("Partes")]
        [Tooltip("Bisagra: el objeto que gira al abrir. La hoja de la puerta es hija suya.")]
        [SerializeField] Transform hinge;
        [Tooltip("Colliders que bloquean el paso (la hoja). Se desactivan al abrir.")]
        [SerializeField] Collider[] blockingColliders;
        [SerializeField] Light statusLight;
        [SerializeField] Renderer statusRenderer;
        [SerializeField] Material lockedMaterial;
        [SerializeField] Material unlockedMaterial;

        [Header("Apertura")]
        [SerializeField] float openAngle = 100f;
        [SerializeField] float openDuration = 1.6f;

        [Header("Colores de la luz")]
        [SerializeField] Color lockedColor = new Color(1f, 0.15f, 0.1f);
        [SerializeField] Color unlockedColor = new Color(0.25f, 1f, 0.45f);

        [Header("Audio")]
        [SerializeField] AudioClip unlockClip;
        [SerializeField] AudioClip openClip;
        [SerializeField] AudioClip lockedClip;

        public bool IsOpen { get; private set; }
        public int Stage { get { return stage; } }

        /// <summary>true si esta puerta es la salida final (la de la última zona).</summary>
        public bool IsFinal { get { return gm == null || stage >= gm.TotalStages; } }

        GameManager gm;
        bool playerInRange;
        bool opening;
        float openProgress;
        Quaternion closedRotation;
        Quaternion openRotation;

        // Si no hay GameManager (probando el prefab suelto) la puerta se considera desbloqueada.
        bool IsUnlocked
        {
            get { return gm == null || gm.CurrentStage > stage || (gm.CurrentStage == stage && gm.ObjectiveComplete); }
        }
        bool IsSealed { get { return gm != null && gm.CurrentStage < stage; } }   // la zona de esta puerta aún no empieza
        int MissingOrbs { get { return gm == null ? 0 : Mathf.Max(0, gm.OrbsNeeded - gm.OrbsCollected); } }

        void Start()
        {
            gm = GameManager.Instance;

            if (hinge != null)
            {
                closedRotation = hinge.localRotation;
                // Giramos hacia -Y para que la hoja se abra hacia +Z (hacia "afuera").
                openRotation = closedRotation * Quaternion.Euler(0f, -openAngle, 0f);
            }

            if (gm != null)
            {
                gm.OrbsChanged += OnOrbsChanged;
                gm.ObjectiveCompleted += OnObjectiveCompleted;
            }

            ApplyVisuals(IsUnlocked);
        }

        void OnDestroy()
        {
            if (gm != null)
            {
                gm.OrbsChanged -= OnOrbsChanged;
                gm.ObjectiveCompleted -= OnObjectiveCompleted;
            }
        }

        void Update()
        {
            if (opening) AnimateOpening();

            if (!playerInRange || IsOpen || !GameManager.IsPlaying) return;
            if (GameInput.InteractPressed) TryOpen();
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            playerInRange = true;
            RefreshPrompt();
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            playerInRange = false;
            SetPrompt(null);
        }

        void TryOpen()
        {
            if (!IsUnlocked)
            {
                PlaySfx(lockedClip);
                if (gm != null)
                {
                    if (IsSealed) gm.ShowMessage("Esta puerta está sellada.", 2.5f);
                    else gm.ShowMessage("Necesitas " + MissingOrbs + " orbe(s) más para abrir la salida.", 2.5f);
                }
                return;
            }

            IsOpen = true;
            opening = true;
            openProgress = 0f;
            PlaySfx(openClip);

            if (blockingColliders != null)
            {
                for (int i = 0; i < blockingColliders.Length; i++)
                {
                    if (blockingColliders[i] != null) blockingColliders[i].enabled = false;
                }
            }

            SetPrompt(null);
            if (gm != null) gm.ShowMessage(IsFinal ? "¡La salida final está abierta! Corre." : "¡Puerta abierta! Cruza para pasar a la siguiente zona.", 3f);
        }

        void AnimateOpening()
        {
            if (hinge == null)
            {
                opening = false;
                return;
            }

            openProgress = Mathf.Clamp01(openProgress + Time.deltaTime / Mathf.Max(0.01f, openDuration));
            float t = Mathf.SmoothStep(0f, 1f, openProgress);
            hinge.localRotation = Quaternion.Slerp(closedRotation, openRotation, t);

            if (openProgress >= 1f) opening = false;
        }

        void OnOrbsChanged(int collected, int needed)
        {
            if (playerInRange) RefreshPrompt();
        }

        void OnObjectiveCompleted()
        {
            if (gm != null && gm.CurrentStage != stage) return; // el objetivo completado es de otra zona
            ApplyVisuals(true);
            PlaySfx(unlockClip);
            if (playerInRange) RefreshPrompt();
        }

        void RefreshPrompt()
        {
            if (!playerInRange || IsOpen)
            {
                SetPrompt(null);
                return;
            }

            if (IsSealed) SetPrompt("Esta puerta está sellada.");
            else if (IsUnlocked) SetPrompt("[E] Abrir la puerta");
            else SetPrompt("La salida está cerrada. Faltan " + MissingOrbs + " orbe(s).");
        }

        void ApplyVisuals(bool unlocked)
        {
            if (statusLight != null) statusLight.color = unlocked ? unlockedColor : lockedColor;

            if (statusRenderer != null)
            {
                Material m = unlocked ? unlockedMaterial : lockedMaterial;
                if (m != null) statusRenderer.sharedMaterial = m;
            }
        }

        void SetPrompt(string text)
        {
            if (gm != null) gm.SetPrompt(text);
        }

        void PlaySfx(AudioClip clip)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySfxAt(clip, transform.position, 1f);
        }
    }
}
