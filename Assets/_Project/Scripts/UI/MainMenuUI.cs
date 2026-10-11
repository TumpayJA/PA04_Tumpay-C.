using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EscapeGame
{
    /// <summary>
    /// Menú principal: Jugar, Controles y Salir. También reproduce la música del menú.
    /// Va en el objeto raíz del Canvas de la escena MainMenu.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] string levelScene = "Level01";

        [Header("Paneles")]
        [SerializeField] GameObject mainPanel;
        [SerializeField] GameObject controlsPanel;

        [Header("Botones")]
        [SerializeField] Button playButton;
        [SerializeField] Button controlsButton;
        [SerializeField] Button backButton;
        [SerializeField] Button quitButton;

        [Header("Audio")]
        [SerializeField] AudioClip menuMusic;
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.35f;
        [SerializeField] AudioClip clickClip;

        AudioSource source;

        void Awake()
        {
            EventSystemGuard.EnsureExists(); // asegura que los botones respondan

            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = musicVolume;
            source.clip = menuMusic;
            if (menuMusic != null) source.Play();
        }

        void Start()
        {
            // Por si venimos de una partida pausada o terminada.
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (playButton != null) playButton.onClick.AddListener(Play);
            if (controlsButton != null) controlsButton.onClick.AddListener(() => ShowControls(true));
            if (backButton != null) backButton.onClick.AddListener(() => ShowControls(false));
            if (quitButton != null) quitButton.onClick.AddListener(Quit);

            ShowControls(false);
        }

        public void Play()
        {
            PlayClick();
            SceneManager.LoadScene(levelScene);
        }

        public void ShowControls(bool show)
        {
            if (show) PlayClick();
            if (mainPanel != null) mainPanel.SetActive(!show);
            if (controlsPanel != null) controlsPanel.SetActive(show);
        }

        public void Quit()
        {
            PlayClick();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void PlayClick()
        {
            if (clickClip != null) source.PlayOneShot(clickClip);
        }
    }
}
