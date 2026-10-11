using UnityEngine;

namespace EscapeGame
{
    /// <summary>
    /// Sonido de pasos: suena un clip cada cierta DISTANCIA recorrida (no cada cierto tiempo),
    /// así corriendo suenan más seguidos y quieto no suena. Va en el objeto raíz "Player".
    /// Si no hay clips asignados no hace nada (no da errores).
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(AudioSource))]
    public class PlayerFootsteps : MonoBehaviour
    {
        [SerializeField] AudioClip[] footstepClips;
        [SerializeField, Range(0f, 1f)] float volume = 0.5f;
        [Tooltip("Metros entre paso y paso caminando.")]
        [SerializeField] float walkStride = 1.9f;
        [Tooltip("Metros entre paso y paso corriendo.")]
        [SerializeField] float sprintStride = 2.6f;
        [SerializeField] float minPitch = 0.9f;
        [SerializeField] float maxPitch = 1.1f;

        PlayerMovement movement;
        AudioSource source;
        float distanceWalked;
        int lastIndex = -1;

        void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        void Update()
        {
            if (footstepClips == null || footstepClips.Length == 0) return;
            if (!GameManager.IsPlaying || !movement.IsGrounded || !movement.IsMoving) return;

            distanceWalked += movement.HorizontalSpeed * Time.deltaTime;
            float stride = movement.IsSprinting ? sprintStride : walkStride;

            if (distanceWalked >= stride)
            {
                distanceWalked = 0f;
                PlayStep();
            }
        }

        void PlayStep()
        {
            // Elegimos un clip distinto al anterior para que no suene repetitivo.
            int index = Random.Range(0, footstepClips.Length);
            if (footstepClips.Length > 1 && index == lastIndex) index = (index + 1) % footstepClips.Length;
            lastIndex = index;

            AudioClip clip = footstepClips[index];
            if (clip == null) return;

            source.pitch = Random.Range(minPitch, maxPitch);
            source.PlayOneShot(clip, volume);
        }
    }
}
