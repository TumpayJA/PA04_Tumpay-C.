using UnityEngine;

namespace EscapeGame
{
    /// <summary>
    /// Zona al otro lado de una puerta. Si el jugador entra y la puerta está abierta:
    ///   - si la puerta es la salida FINAL  -> GANA;
    ///   - si no                              -> pasa a la SIGUIENTE ZONA.
    /// Necesita un Collider con "Is Trigger". Va en el hijo "ExitZone" del prefab ExitDoor.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ExitZone : MonoBehaviour
    {
        [Tooltip("Puerta a la que pertenece. Si se deja vacío, entrar basta para ganar.")]
        [SerializeField] ExitDoor door;

        void Reset()
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (door != null && !door.IsOpen) return;

            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            // Solo cuenta si la partida está en la zona de esta puerta (evita avanzar dos veces al volver a pisarla).
            if (door != null && gm.CurrentStage != door.Stage) return;

            if (door == null || door.IsFinal) gm.Win();
            else gm.AdvanceStage();
        }
    }
}
