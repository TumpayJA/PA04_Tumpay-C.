using UnityEngine;

namespace EscapeGame
{
    /// <summary>
    /// Atajos para PROBAR rápido (completar el objetivo, ganar, perder, saltar de zona, congelar al enemigo...).
    /// Solo funcionan en el Editor y en Builds con "Development Build" marcado (Debug.isDebugBuild).
    /// En el Build final el script se elimina solo: no hay trampas para el jugador.
    ///
    ///   F1  recoge los orbes de la zona actual      F2  gana al instante      F4  pierde al instante
    ///   F5  salta a la siguiente zona (te lleva al otro lado de la puerta)
    ///   F6  lanza 60 efectos (prueba del Object Pool + Profiler)
    ///   F7  congela / descongela a los enemigos      F8  invulnerable (el enemigo no te atrapa)
    /// </summary>
    public class DebugTools : MonoBehaviour
    {
        bool enemiesFrozen;
        GUIStyle style;

        void Awake()
        {
            if (!Debug.isDebugBuild) Destroy(this);
        }

        void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            if (GameInput.FunctionKeyDown(1)) CollectCurrentStageOrbs(gm);
            if (GameInput.FunctionKeyDown(2)) gm.Win();
            if (GameInput.FunctionKeyDown(4)) gm.Lose("Derrota de prueba (F4).");
            if (GameInput.FunctionKeyDown(5)) SkipToNextStage(gm);

            if (GameInput.FunctionKeyDown(6) && ObjectPool.Instance != null && gm.Player != null)
                ObjectPool.Instance.DebugBurst(60, gm.Player.position + gm.Player.forward * 4f, 4f);

            if (GameInput.FunctionKeyDown(7)) ToggleEnemies();

            if (GameInput.FunctionKeyDown(8))
            {
                gm.DebugInvulnerable = !gm.DebugInvulnerable;
                gm.ShowMessage("Invulnerable: " + (gm.DebugInvulnerable ? "SÍ" : "NO"), 1.5f);
            }
        }

        void CollectCurrentStageOrbs(GameManager gm)
        {
            Collectible[] orbs = FindObjectsByType<Collectible>(FindObjectsSortMode.None);
            for (int i = 0; i < orbs.Length; i++)
            {
                if (orbs[i].Stage == gm.CurrentStage) orbs[i].Collect();
            }
        }

        /// <summary>Teletransporta al jugador a 5 m al otro lado de la puerta de la zona actual y avanza de zona.</summary>
        void SkipToNextStage(GameManager gm)
        {
            if (gm.CurrentStage >= gm.TotalStages || gm.Player == null) return;

            ExitDoor[] doors = FindObjectsByType<ExitDoor>(FindObjectsSortMode.None);
            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i].Stage != gm.CurrentStage) continue;

                Vector3 target = doors[i].transform.position + doors[i].transform.forward * 5f + Vector3.up * 0.1f;
                CharacterController controller = gm.Player.GetComponent<CharacterController>();
                if (controller != null) controller.enabled = false; // un CharacterController no se puede teletransportar activo
                gm.Player.position = target;
                if (controller != null) controller.enabled = true;
                break;
            }

            gm.AdvanceStage();
        }

        void ToggleEnemies()
        {
            enemiesFrozen = !enemiesFrozen;
            EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++) enemies[i].SetFrozen(enemiesFrozen);

            if (GameManager.Instance != null)
                GameManager.Instance.ShowMessage("Enemigos " + (enemiesFrozen ? "congelados" : "activos"), 1.5f);
        }

        void OnGUI()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label);
                style.fontSize = 12;
                style.alignment = TextAnchor.UpperRight;
                style.normal.textColor = new Color(1f, 1f, 1f, 0.55f);
            }

            Rect rect = new Rect(Screen.width - 560f, Screen.height - 24f, 550f, 20f);
            GUI.Label(rect, "[DEBUG] F1 orbes  F2 ganar  F4 perder  F5 sig. zona  F6 FX  F7 congelar  F8 invulnerable  F3 FPS", style);
        }
    }
}
