using UnityEngine;

namespace EscapeGame
{
    /// <summary>
    /// Agrupa objetos (normalmente los ENEMIGOS) de una zona. Sus hijos solo están activos mientras la partida
    /// está en esa zona: al pasar de zona, los de la anterior desaparecen y los de la nueva aparecen.
    ///
    /// Por qué sirve:
    ///  - Jugabilidad: los enemigos de una zona no te persiguen por todo el mapa ni cruzan puertas cerradas.
    ///  - OPTIMIZACIÓN: los enemigos de otras zonas no ejecutan IA ni NavMesh (se evitan cálculos innecesarios).
    ///
    /// Importante: los ORBES y las PUERTAS no deben ir dentro de un StageGroup (el GameManager los cuenta al iniciar).
    /// El objeto con este script debe quedar siempre activo; solo se encienden/apagan sus hijos.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class StageGroup : MonoBehaviour
    {
        [Tooltip("Zona a la que pertenecen los hijos de este objeto.")]
        [SerializeField] int stage = 1;
        [Tooltip("Marcado: los hijos se apagan al pasar a otra zona. Desmarcado: aparecen en su zona y se quedan.")]
        [SerializeField] bool onlyDuringThisStage = true;

        GameManager gm;

        void Awake()
        {
            gm = GameManager.Instance;
            if (gm != null)
            {
                gm.StageChanged += Apply;
                Apply(gm.CurrentStage);
            }
            else
            {
                Apply(stage); // sin GameManager (probando suelto): todo visible
            }
        }

        void OnDestroy()
        {
            if (gm != null) gm.StageChanged -= Apply;
        }

        void Apply(int currentStage)
        {
            bool active = onlyDuringThisStage ? currentStage == stage : currentStage >= stage;

            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (child.activeSelf != active) child.SetActive(active);
            }
        }
    }
}
