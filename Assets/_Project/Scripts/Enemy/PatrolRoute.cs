using UnityEngine;

namespace EscapeGame
{
    /// <summary>
    /// Ruta de patrulla. Los HIJOS de este objeto son los waypoints, en orden (el último vuelve al primero).
    /// Mueve los hijos en la escena para cambiar la ruta; se dibujan numerados en la Scene View.
    /// Asigna este objeto al campo "Route" del EnemyAI.
    /// </summary>
    public class PatrolRoute : MonoBehaviour
    {
        [SerializeField] Color gizmoColor = new Color(1f, 0.55f, 0.1f);

        public int Count { get { return transform.childCount; } }

        public Transform GetPoint(int index)
        {
            return transform.GetChild(index);
        }

        void OnDrawGizmos()
        {
            int n = transform.childCount;
            Gizmos.color = gizmoColor;

            for (int i = 0; i < n; i++)
            {
                Vector3 p = transform.GetChild(i).position + Vector3.up * 0.15f;
                Vector3 next = transform.GetChild((i + 1) % n).position + Vector3.up * 0.15f;

                Gizmos.DrawSphere(p, 0.25f);
                Gizmos.DrawLine(p, next);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(p + Vector3.up * 0.6f, (i + 1).ToString());
#endif
            }
        }
    }
}
