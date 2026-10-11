using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EscapeGame
{
    public enum GameState { Playing, Paused, Won, Lost }

    /// <summary>
    /// Cerebro de las reglas del juego: estado (jugando / pausa / victoria / derrota), ZONAS (etapas),
    /// orbes recogidos, tiempo restante y cambio de escenas. NO toca la interfaz ni el audio: avisa con
    /// eventos y quien quiera reaccionar (UIManager, AudioManager, ExitDoor, StageGroup...) se suscribe.
    /// Va en el prefab GameSystems.
    ///
    /// ZONAS: cada Collectible y cada ExitDoor tiene un número de zona (1, 2, ...). En la zona actual hay que
    /// recoger TODOS sus orbes para desbloquear su puerta; al cruzarla se pasa a la siguiente zona. La puerta
    /// de la última zona es la salida final y, al cruzarla, se gana.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        /// <summary>true si se está jugando (o si no hay GameManager: sirve para probar un prefab suelto).</summary>
        public static bool IsPlaying
        {
            get { return Instance == null || Instance.State == GameState.Playing; }
        }

        [Header("Tiempo")]
        [Tooltip("Segundos para escapar. 0 = sin límite de tiempo.")]
        [SerializeField] float timeLimit = 900f;
        [Tooltip("Segundos extra que se suman al pasar a la siguiente zona (solo si hay límite de tiempo).")]
        [SerializeField] float stageTimeBonus = 120f;

        [Header("Escenas")]
        [SerializeField] string mainMenuScene = "MainMenu";

        public GameState State { get; private set; }
        public string LoseReason { get; private set; }
        public Transform Player { get; private set; }
        public bool DebugInvulnerable { get; set; }

        // Zona actual
        public int CurrentStage { get; private set; }
        public int TotalStages { get; private set; }
        public int OrbsCollected { get; private set; }   // en la zona actual
        public int OrbsNeeded { get; private set; }      // en la zona actual

        // Totales de toda la partida
        public int TotalOrbsCollected { get; private set; }
        public int TotalOrbsInLevel { get; private set; }
        public float TimeRemaining { get; private set; }
        public float TimeElapsed { get; private set; }

        public bool HasTimeLimit { get { return timeLimit > 0f; } }
        public bool ObjectiveComplete { get { return OrbsCollected >= OrbsNeeded; } }
        public bool IsBeingChased { get { return chasers.Count > 0; } }

        // Eventos (patrón Observer): los demás sistemas se suscriben a lo que les interesa.
        public event Action<int, int> OrbsChanged;           // (recogidos, necesarios) de la zona actual
        public event Action ObjectiveCompleted;              // ya tienes todos los orbes de la zona actual
        public event Action<int> StageChanged;               // nueva zona actual
        public event Action<GameState> StateChanged;
        public event Action<bool> ChaseChanged;              // true = al menos un enemigo te persigue
        public event Action<string, float> MessageRequested; // (texto, segundos)
        public event Action<string> PromptChanged;           // texto de interacción ("[E] Abrir"), null = ocultar

        readonly HashSet<EnemyAI> chasers = new HashSet<EnemyAI>();
        int[] orbsPerStage;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[GameManager] Hay más de un GameManager en la escena. Se elimina el duplicado.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) Player = playerObject.transform;
            else Debug.LogError("[GameManager] No hay ningún objeto con el tag 'Player'. Pon el prefab Player en la escena.");

            CountStagesAndOrbs();

            TimeRemaining = timeLimit;
            SetState(GameState.Playing);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Cuenta cuántas zonas hay (la mayor numeración de orbes y puertas) y cuántos orbes tiene cada una.</summary>
        void CountStagesAndOrbs()
        {
            Collectible[] orbs = FindObjectsByType<Collectible>(FindObjectsSortMode.None);
            ExitDoor[] doors = FindObjectsByType<ExitDoor>(FindObjectsSortMode.None);

            int maxStage = 1;
            for (int i = 0; i < orbs.Length; i++) maxStage = Mathf.Max(maxStage, orbs[i].Stage);
            for (int i = 0; i < doors.Length; i++) maxStage = Mathf.Max(maxStage, doors[i].Stage);

            TotalStages = maxStage;
            orbsPerStage = new int[maxStage + 1];
            for (int i = 0; i < orbs.Length; i++) orbsPerStage[Mathf.Clamp(orbs[i].Stage, 1, maxStage)]++;

            TotalOrbsInLevel = orbs.Length;
            CurrentStage = 1;
            OrbsNeeded = orbsPerStage[1];

            if (orbs.Length == 0) Debug.LogWarning("[GameManager] No hay orbes en la escena: la salida estará abierta desde el inicio.");
        }

        void Update()
        {
            if (GameInput.PausePressed)
            {
                if (State == GameState.Playing) Pause();
                else if (State == GameState.Paused) Resume();
            }

            if (State != GameState.Playing) return;

            TimeElapsed += Time.deltaTime;

            if (HasTimeLimit)
            {
                TimeRemaining -= Time.deltaTime;
                if (TimeRemaining <= 0f)
                {
                    TimeRemaining = 0f;
                    Lose("Se acabó el tiempo.");
                }
            }
        }

        // ---------- Objetivo y zonas ----------

        /// <summary>Lo llama el orbe al recogerse. Los orbes de otra zona no cuentan.</summary>
        public void CollectOrb(int stage)
        {
            if (State != GameState.Playing || stage != CurrentStage) return;

            OrbsCollected++;
            TotalOrbsCollected++;
            bool justCompleted = OrbsCollected == OrbsNeeded;

            if (OrbsChanged != null) OrbsChanged(Mathf.Min(OrbsCollected, OrbsNeeded), OrbsNeeded);
            if (justCompleted && ObjectiveCompleted != null) ObjectiveCompleted();
        }

        /// <summary>Lo llama ExitZone al cruzar una puerta que NO es la final: pasa a la siguiente zona.</summary>
        public void AdvanceStage()
        {
            if (State != GameState.Playing || CurrentStage >= TotalStages) return;

            CurrentStage++;
            OrbsCollected = 0;
            OrbsNeeded = orbsPerStage[CurrentStage];
            if (HasTimeLimit) TimeRemaining += stageTimeBonus;

            if (StageChanged != null) StageChanged(CurrentStage);
            if (OrbsNeeded == 0 && ObjectiveCompleted != null) ObjectiveCompleted(); // zona sin orbes: puerta abierta
        }

        /// <summary>Segundos de bonus que da pasar de zona (para mostrarlos en la interfaz).</summary>
        public float StageTimeBonus { get { return HasTimeLimit ? stageTimeBonus : 0f; } }

        // ---------- Estados ----------

        public void Pause()
        {
            if (State == GameState.Playing) SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State == GameState.Paused) SetState(GameState.Playing);
        }

        public void Win()
        {
            if (State != GameState.Playing) return;
            SetState(GameState.Won);
        }

        public void Lose(string reason)
        {
            if (State != GameState.Playing) return;
            LoseReason = reason;
            SetState(GameState.Lost);
        }

        /// <summary>Lo llama el enemigo cuando alcanza al jugador.</summary>
        public void PlayerCaught()
        {
            if (DebugInvulnerable) return;
            Lose("¡El monstruo te atrapó!");
        }

        void SetState(GameState newState)
        {
            State = newState;
            bool playing = newState == GameState.Playing;

            Time.timeScale = playing ? 1f : 0f;
            Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !playing;

            if (StateChanged != null) StateChanged(newState);
        }

        // ---------- Escenas ----------

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuScene);
        }

        // ---------- Avisos para la interfaz ----------

        public void ShowMessage(string text, float seconds)
        {
            if (MessageRequested != null) MessageRequested(text, seconds);
        }

        public void SetPrompt(string text)
        {
            if (PromptChanged != null) PromptChanged(text);
        }

        /// <summary>Los enemigos avisan cuando empiezan o dejan de perseguirte.</summary>
        public void ReportChase(EnemyAI enemy, bool chasing)
        {
            bool before = chasers.Count > 0;
            if (chasing) chasers.Add(enemy);
            else chasers.Remove(enemy);
            bool after = chasers.Count > 0;

            if (before != after && ChaseChanged != null) ChaseChanged(after);
        }
    }
}
