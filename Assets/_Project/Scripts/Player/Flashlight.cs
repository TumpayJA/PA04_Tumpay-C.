using UnityEngine;

namespace EscapeGame
{
    /// <summary>
    /// Linterna: tecla F para encender/apagar. Va en el objeto "Flashlight" (hijo de la cámara) que tiene la Spot Light.
    /// Es la única luz en tiempo real "cara" del juego: el resto del mapa debería usar luces Baked (ver guía de optimización).
    /// </summary>
    public class Flashlight : MonoBehaviour
    {
        [SerializeField] Light lightSource;
        [SerializeField] AudioClip toggleClip;
        [SerializeField] bool startOn = true;

        void Awake()
        {
            if (lightSource == null) lightSource = GetComponentInChildren<Light>();
        }

        void Start()
        {
            if (lightSource != null) lightSource.enabled = startOn;
        }

        void Update()
        {
            if (!GameManager.IsPlaying) return;
            if (GameInput.FlashlightPressed) Toggle();
        }

        public void Toggle()
        {
            if (lightSource == null) return;
            lightSource.enabled = !lightSource.enabled;
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(toggleClip, 0.6f);
        }
    }
}
