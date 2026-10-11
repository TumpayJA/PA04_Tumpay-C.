using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EscapeGame.EditorTools
{
    public static partial class SetupWizard
    {
        /// <summary>
        /// CLIMA (opcional): añade a la escena abierta lluvia (partículas que siguen al jugador) y relámpagos con trueno.
        /// No toca nada más. Para quitarlo, borra el objeto "Weather" y el hijo "Rain" del Player.
        /// </summary>
        [MenuItem("Tools/Escape Game/6 - Agregar clima (lluvia + relámpagos) a la escena abierta", false, 50)]
        public static void MenuAddWeather()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                EditorUtility.DisplayDialog(Title, "Abre la escena Level01 (la que tiene el Player) y vuelve a intentarlo.", "OK");
                return;
            }
            if (GameObject.Find("Weather") != null || player.transform.Find("Rain") != null)
            {
                EditorUtility.DisplayDialog(Title, "Esta escena ya tiene clima (objeto Weather o Rain).", "OK");
                return;
            }

            // ---------- Lluvia: partículas hijas del Player, 14 m por encima, emitiendo hacia abajo ----------
            GameObject rain = new GameObject("Rain");
            rain.transform.SetParent(player.transform, false);
            rain.transform.localPosition = new Vector3(0f, 14f, 0f);
            rain.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // la caja emite hacia su +Z, que apunta abajo

            ParticleSystem ps = rain.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psr = rain.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = GetParticleMaterial();
            psr.renderMode = ParticleSystemRenderMode.Stretch;   // gotas alargadas
            psr.lengthScale = 15f;
            psr.velocityScale = 0.01f;

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.duration = 5f;
            main.startLifetime = 0.7f;
            main.startSpeed = 22f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.05f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.8f, 0.9f, 0.35f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 2500;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 2200f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(30f, 30f, 0.1f);

            // ---------- Relámpagos ----------
            GameObject weather = new GameObject("Weather");
            LightningFlash flash = weather.AddComponent<LightningFlash>();

            Light moon = RenderSettings.sun;
            if (moon == null)
            {
                GameObject moonGo = GameObject.Find("Moon (Directional Light)");
                if (moonGo != null) moon = moonGo.GetComponent<Light>();
            }
            if (moon != null) SetRef(flash, "lightningLight", moon);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = weather;

            EditorUtility.DisplayDialog(Title,
                "¡Clima agregado!\n\n" +
                "• Lluvia: objeto 'Rain' dentro del Player (te sigue).\n" +
                "• Relámpagos: objeto 'Weather' (puedes asignarle un sonido de trueno en 'Thunder Clip').\n\n" +
                "Guarda la escena con Ctrl+S y dale Play.",
                "OK");
        }
    }
}
