using UnityEngine;

namespace EscapeGame
{
    /// <summary>
    /// Relámpagos (ambientación): cada cierto tiempo la luz de la Luna se enciende fuerte un instante (dos destellos)
    /// y, un poco después, suena un trueno. Va en el objeto "Weather" que crea el menú
    /// Tools > Escape Game > "6 - Agregar clima". Si no asignas el sonido, solo hay destello.
    /// </summary>
    public class LightningFlash : MonoBehaviour
    {
        [Tooltip("Luz que destella. Normalmente la Luna (Directional Light). Si se deja vacía usa la luz principal de la escena.")]
        [SerializeField] Light lightningLight;
        [SerializeField] AudioClip thunderClip;
        [Tooltip("Cada cuántos segundos cae un relámpago (al azar entre estos dos valores).")]
        [SerializeField] Vector2 intervalRange = new Vector2(20f, 45f);
        [SerializeField] float flashIntensity = 3f;
        [SerializeField] Color flashColor = new Color(0.8f, 0.85f, 1f);
        [Tooltip("Segundos entre el relámpago y el trueno.")]
        [SerializeField] Vector2 thunderDelayRange = new Vector2(0.6f, 2.2f);

        float baseIntensity;
        Color baseColor;
        float nextStrikeTimer;
        float flashTimer;
        int flashStep;           // 0 = esperando, 1 = destello, 2 = pausa, 3 = destello, 4 = fin
        bool thunderPending;
        float thunderTimer;

        void Start()
        {
            if (lightningLight == null) lightningLight = RenderSettings.sun;
            if (lightningLight == null)
            {
                Debug.LogWarning("[LightningFlash] No hay luz asignada ni luz principal en la escena.", this);
                enabled = false;
                return;
            }

            baseIntensity = lightningLight.intensity;
            baseColor = lightningLight.color;
            nextStrikeTimer = Random.Range(intervalRange.x, intervalRange.y);
        }

        void OnDisable()
        {
            if (lightningLight != null && baseIntensity > 0f) SetFlash(false);
        }

        void Update()
        {
            if (!GameManager.IsPlaying) return;
            float dt = Time.deltaTime;

            // Trueno retrasado respecto al relámpago
            if (thunderPending)
            {
                thunderTimer -= dt;
                if (thunderTimer <= 0f)
                {
                    thunderPending = false;
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(thunderClip, 1f);
                }
            }

            if (flashStep == 0)
            {
                nextStrikeTimer -= dt;
                if (nextStrikeTimer <= 0f) StartStrike();
                return;
            }

            flashTimer -= dt;
            if (flashTimer > 0f) return;

            flashStep++;
            switch (flashStep)
            {
                case 2: SetFlash(false); flashTimer = 0.07f; break;
                case 3: SetFlash(true); flashTimer = 0.12f; break;
                default:
                    SetFlash(false);
                    flashStep = 0;
                    nextStrikeTimer = Random.Range(intervalRange.x, intervalRange.y);
                    break;
            }
        }

        void StartStrike()
        {
            flashStep = 1;
            flashTimer = 0.09f;
            SetFlash(true);

            thunderPending = true;
            thunderTimer = Random.Range(thunderDelayRange.x, thunderDelayRange.y);
        }

        void SetFlash(bool on)
        {
            lightningLight.intensity = on ? flashIntensity : baseIntensity;
            lightningLight.color = on ? flashColor : baseColor;
        }
    }
}
