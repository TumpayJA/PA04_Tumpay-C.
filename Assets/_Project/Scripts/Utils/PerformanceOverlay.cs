using Unity.Profiling;
using UnityEngine;

namespace EscapeGame
{
    /// <summary>
    /// Panel de rendimiento para medir la optimización "antes y después" (también en el Build).
    /// F3 lo muestra u oculta. Muestra FPS, tiempo por fotograma, memoria y (si el sistema lo permite)
    /// draw calls, SetPass calls y triángulos.
    /// Nota: dibujar el panel también consume un poco; ocúltalo (F3) cuando midas con el Profiler.
    /// </summary>
    public class PerformanceOverlay : MonoBehaviour
    {
        [SerializeField] bool visibleAtStart = false;
        [SerializeField] float refreshSeconds = 0.5f;

        bool visible;
        float timer;
        int frames;
        float accumulatedTime;
        string text = "";
        GUIStyle style;

        ProfilerRecorder drawCalls;
        ProfilerRecorder setPassCalls;
        ProfilerRecorder triangles;

        void OnEnable()
        {
            visible = visibleAtStart;
            drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            setPassCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        }

        void OnDisable()
        {
            if (drawCalls.Valid) drawCalls.Dispose();
            if (setPassCalls.Valid) setPassCalls.Dispose();
            if (triangles.Valid) triangles.Dispose();
        }

        void Update()
        {
            if (GameInput.PerformanceTogglePressed) visible = !visible;
            if (!visible) return;

            frames++;
            accumulatedTime += Time.unscaledDeltaTime;
            timer += Time.unscaledDeltaTime;

            if (timer >= refreshSeconds)
            {
                float averageFrame = accumulatedTime / Mathf.Max(1, frames);
                float fps = averageFrame > 0f ? 1f / averageFrame : 0f;
                float memoryMb = System.GC.GetTotalMemory(false) / 1048576f;

                text = "FPS: " + fps.ToString("0") + "   (" + (averageFrame * 1000f).ToString("0.0") + " ms)\n" +
                       "Draw calls: " + Format(drawCalls) + "\n" +
                       "SetPass calls: " + Format(setPassCalls) + "\n" +
                       "Triángulos: " + Format(triangles) + "\n" +
                       "Memoria (managed): " + memoryMb.ToString("0.0") + " MB\n" +
                       "[F3] ocultar";

                frames = 0;
                accumulatedTime = 0f;
                timer = 0f;
            }
        }

        void OnGUI()
        {
            if (!visible) return;

            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label);
                style.fontSize = 15;
                style.normal.textColor = Color.white;
            }

            GUI.Box(new Rect(10f, 10f, 320f, 135f), GUIContent.none);
            GUI.Label(new Rect(20f, 14f, 310f, 130f), text, style);
        }

        static string Format(ProfilerRecorder recorder)
        {
            // En algunos Builds (sin "Development Build") estos contadores no están disponibles.
            if (!recorder.Valid || recorder.LastValue <= 0) return "n/d";
            return recorder.LastValue.ToString();
        }
    }
}
