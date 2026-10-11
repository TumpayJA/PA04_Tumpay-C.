using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace EscapeGame
{
    /// <summary>
    /// Garantiza que la escena tenga un EventSystem con el módulo de entrada CORRECTO.
    /// Sin esto los botones de la interfaz no responden. Es el error más común al usar el Input System nuevo:
    /// el módulo antiguo (StandaloneInputModule) no funciona con él. Lo llaman UIManager y MainMenuUI al iniciar,
    /// así que no tienes que crear ni arreglar el EventSystem a mano.
    /// </summary>
    public static class EventSystemGuard
    {
        public static void EnsureExists()
        {
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject go = new GameObject("EventSystem");
                eventSystem = go.AddComponent<EventSystem>();
            }

            FixInputModule(eventSystem.gameObject);
        }

        static void FixInputModule(GameObject go)
        {
#if ENABLE_INPUT_SYSTEM
            // Input System nuevo: se necesita InputSystemUIInputModule.
            StandaloneInputModule oldModule = go.GetComponent<StandaloneInputModule>();
            if (oldModule != null)
            {
                oldModule.enabled = false;
                Object.Destroy(oldModule);
            }

            if (go.GetComponent<InputSystemUIInputModule>() == null)
            {
                InputSystemUIInputModule module = go.AddComponent<InputSystemUIInputModule>();

                // Asigna las acciones por defecto de la UI (clic, mover, enviar...). Por reflexión para
                // no depender de la versión exacta del paquete.
                System.Reflection.MethodInfo assign = typeof(InputSystemUIInputModule).GetMethod("AssignDefaultActions");
                if (assign != null) assign.Invoke(module, null);
            }
#else
            // Input Manager antiguo: basta con el módulo clásico.
            if (go.GetComponent<BaseInputModule>() == null) go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
