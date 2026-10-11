using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EscapeGame
{
    /// <summary>
    /// Toda la interfaz del nivel: HUD (orbes, tiempo, objetivo, stamina, mensajes, aviso de persecución)
    /// y los paneles de Pausa / Victoria / Derrota con sus botones.
    /// Solo ESCUCHA eventos del GameManager: aquí no hay reglas de juego, únicamente presentación.
    /// Va en el objeto raíz del prefab UI_Canvas.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] GameObject hudRoot;
        [SerializeField] TMP_Text orbsText;
        [SerializeField] TMP_Text timerText;
        [SerializeField] TMP_Text objectiveText;
        [SerializeField] TMP_Text promptText;
        [SerializeField] TMP_Text messageText;
        [SerializeField] TMP_Text chaseWarningText;
        [SerializeField] Slider staminaBar;
        [Tooltip("Imagen roja a pantalla completa que pulsa cuando te persiguen.")]
        [SerializeField] Graphic chaseVignette;

        [Header("Paneles")]
        [SerializeField] GameObject pausePanel;
        [SerializeField] GameObject winPanel;
        [SerializeField] GameObject losePanel;
        [SerializeField] TMP_Text winDetailsText;
        [SerializeField] TMP_Text loseReasonText;

        [Header("Botones")]
        [SerializeField] Button resumeButton;
        [SerializeField] Button[] restartButtons;
        [SerializeField] Button[] menuButtons;

        [Header("Textos")]
        [SerializeField] string objectiveCollect = "Encuentra todos los orbes";
        [SerializeField] string objectiveEscape = "¡Ve a la salida!";

        GameManager gm;
        PlayerMovement playerMovement;
        int lastShownSeconds = -1;
        float messageHideTime;
        float vignetteAlpha;

        void Awake()
        {
            EventSystemGuard.EnsureExists(); // asegura que los botones respondan
        }

        void Start()
        {
            gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogError("[UIManager] No hay GameManager en la escena. Añade el prefab GameSystems.", this);
                enabled = false;
                return;
            }

            playerMovement = FindFirstObjectByType<PlayerMovement>();

            gm.OrbsChanged += OnOrbsChanged;
            gm.ObjectiveCompleted += OnObjectiveCompleted;
            gm.StageChanged += OnStageChanged;
            gm.StateChanged += OnStateChanged;
            gm.ChaseChanged += OnChaseChanged;
            gm.MessageRequested += ShowMessage;
            gm.PromptChanged += SetPrompt;

            WireButtons();

            // Sincronizamos con el estado actual (el GameManager ya arrancó antes que nosotros).
            RefreshOrbsText(Mathf.Min(gm.OrbsCollected, gm.OrbsNeeded), gm.OrbsNeeded);
            SetObjective(gm.ObjectiveComplete);
            OnStateChanged(gm.State);
            OnChaseChanged(gm.IsBeingChased);
            SetPrompt(null);
            if (messageText != null) messageText.gameObject.SetActive(false);
            SetVignetteAlpha(0f);
        }

        void OnDestroy()
        {
            if (gm == null) return;
            gm.OrbsChanged -= OnOrbsChanged;
            gm.ObjectiveCompleted -= OnObjectiveCompleted;
            gm.StageChanged -= OnStageChanged;
            gm.StateChanged -= OnStateChanged;
            gm.ChaseChanged -= OnChaseChanged;
            gm.MessageRequested -= ShowMessage;
            gm.PromptChanged -= SetPrompt;
        }

        void Update()
        {
            if (gm == null) return;

            UpdateTimer();
            UpdateMessageTimeout();
            UpdateVignette();

            if (staminaBar != null && playerMovement != null) staminaBar.value = playerMovement.Stamina01;
        }

        // ---------- Botones ----------

        void WireButtons()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(gm.Resume);
            AddListeners(restartButtons, gm.RestartLevel);
            AddListeners(menuButtons, gm.GoToMainMenu);
        }

        static void AddListeners(Button[] buttons, UnityAction action)
        {
            if (buttons == null) return;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null) buttons[i].onClick.AddListener(action);
            }
        }

        // ---------- Reacciones a eventos del GameManager ----------

        void OnOrbsChanged(int collected, int needed)
        {
            RefreshOrbsText(collected, needed);

            if (collected >= needed) ShowMessage("¡Tienes todos los orbes! Busca la salida.", 3f);
            else ShowMessage("Orbe recogido (" + collected + "/" + needed + ")", 2f);
        }

        void OnObjectiveCompleted()
        {
            SetObjective(true);
        }

        void OnStageChanged(int stage)
        {
            RefreshOrbsText(gm.OrbsCollected, gm.OrbsNeeded);
            SetObjective(gm.ObjectiveComplete);

            string bonus = gm.StageTimeBonus > 0f ? "  (+" + FormatTime(gm.StageTimeBonus) + " de tiempo)" : "";
            ShowMessage("ZONA " + stage + " de " + gm.TotalStages + bonus, 4f);
        }

        void OnStateChanged(GameState state)
        {
            SetActive(pausePanel, state == GameState.Paused);
            SetActive(winPanel, state == GameState.Won);
            SetActive(losePanel, state == GameState.Lost);
            SetActive(hudRoot, state == GameState.Playing || state == GameState.Paused);

            if (state != GameState.Playing) SetPrompt(null);

            if (state == GameState.Won && winDetailsText != null)
                winDetailsText.text = "Orbes: " + gm.TotalOrbsCollected + "/" + gm.TotalOrbsInLevel + "\nTiempo: " + FormatTime(gm.TimeElapsed);

            if (state == GameState.Lost && loseReasonText != null)
                loseReasonText.text = gm.LoseReason;
        }

        void OnChaseChanged(bool chased)
        {
            if (chaseWarningText != null) chaseWarningText.gameObject.SetActive(chased);
        }

        // ---------- HUD ----------

        void RefreshOrbsText(int collected, int needed)
        {
            if (orbsText != null) orbsText.text = "Orbes: " + collected + "/" + needed;
        }

        void SetObjective(bool complete)
        {
            if (objectiveText == null) return;

            string text = complete ? objectiveEscape : objectiveCollect;
            if (gm != null && gm.TotalStages > 1) text = "ZONA " + gm.CurrentStage + "/" + gm.TotalStages + "  ·  " + text;
            objectiveText.text = text;
        }

        void UpdateTimer()
        {
            if (timerText == null) return;

            if (!gm.HasTimeLimit)
            {
                if (lastShownSeconds != -2)
                {
                    timerText.text = "";
                    lastShownSeconds = -2;
                }
                return;
            }

            // Solo reescribimos el texto cuando cambia el segundo (evita crear strings 60 veces por segundo).
            int seconds = Mathf.CeilToInt(gm.TimeRemaining);
            if (seconds == lastShownSeconds) return;

            lastShownSeconds = seconds;
            timerText.text = FormatTime(seconds);
            timerText.color = seconds <= 30 ? new Color(1f, 0.3f, 0.3f) : Color.white;
        }

        void ShowMessage(string text, float seconds)
        {
            if (messageText == null) return;
            messageText.text = text;
            messageText.gameObject.SetActive(true);
            messageHideTime = Time.unscaledTime + seconds;
        }

        void UpdateMessageTimeout()
        {
            if (messageText != null && messageText.gameObject.activeSelf && Time.unscaledTime >= messageHideTime)
                messageText.gameObject.SetActive(false);
        }

        void SetPrompt(string text)
        {
            if (promptText == null) return;

            bool show = !string.IsNullOrEmpty(text);
            if (show) promptText.text = text;
            if (promptText.gameObject.activeSelf != show) promptText.gameObject.SetActive(show);
        }

        void UpdateVignette()
        {
            if (chaseVignette == null) return;

            bool chased = gm.IsBeingChased && gm.State == GameState.Playing;
            float target = chased ? 0.16f + 0.08f * Mathf.Sin(Time.unscaledTime * 8f) : 0f;
            vignetteAlpha = Mathf.MoveTowards(vignetteAlpha, target, Time.unscaledDeltaTime * 0.8f);
            SetVignetteAlpha(vignetteAlpha);
        }

        void SetVignetteAlpha(float alpha)
        {
            if (chaseVignette == null) return;
            Color c = chaseVignette.color;
            c.a = alpha;
            chaseVignette.color = c;
        }

        // ---------- Utilidades ----------

        static void SetActive(GameObject go, bool value)
        {
            if (go != null && go.activeSelf != value) go.SetActive(value);
        }

        static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }
    }
}
