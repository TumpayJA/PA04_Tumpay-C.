using UnityEngine;

namespace EscapeGame
{
    /// <summary>
    /// Mirar con el ratón: gira el cuerpo en horizontal y la cámara (cameraPivot) en vertical.
    /// Va en el objeto raíz "Player".
    /// </summary>
    public class PlayerLook : MonoBehaviour
    {
        [Tooltip("Objeto que cabecea arriba/abajo. Normalmente el hijo CameraPivot.")]
        [SerializeField] Transform cameraPivot;
        [Tooltip("Grados por unidad de ratón. Súbelo si la cámara va lenta, bájalo si va muy rápida.")]
        [SerializeField] float sensitivity = 2f;
        [SerializeField] float minPitch = -80f;
        [SerializeField] float maxPitch = 80f;

        float pitch;

        void Awake()
        {
            if (cameraPivot == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null) cameraPivot = cam.transform;
            }
        }

        void Start()
        {
            // Si pruebas el Player suelto (sin GameManager), bloqueamos el cursor aquí.
            if (GameManager.Instance == null)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void Update()
        {
            if (!GameManager.IsPlaying || cameraPivot == null) return;

            Vector2 look = GameInput.LookDelta * sensitivity;

            transform.Rotate(0f, look.x, 0f, Space.Self);
            pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
