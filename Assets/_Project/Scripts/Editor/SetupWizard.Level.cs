using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EscapeGame.EditorTools
{
    public static partial class SetupWizard
    {
        // "Todo estático" (equivale a marcar la casilla Static): permite baking de luces, batching y occlusion culling.
        static readonly StaticEditorFlags AllStatic = (StaticEditorFlags)(-1);

        /// <summary>
        /// MAPA GRANDE de 2 zonas (greybox, listo para decorar): cementerio (zona 1) + cripta (zona 2), unidos por una
        /// puerta. 13 orbes, 5 enemigos, obstáculos, 2 puertas y 7 lámparas. Los datos están en LevelLayout.cs
        /// (verificados: todo es alcanzable y las zonas solo se comunican por sus puertas).
        /// </summary>
        static void BuildLevelScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ConfigureNightLighting();

            // ---------- Environment: TODO lo sólido va aquí dentro (el NavMesh solo lee lo que cuelga de él) ----------
            GameObject environment = new GameObject("Environment");
            Prim(PrimitiveType.Plane, "Ground", environment.transform, LevelLayout.GroundCenter, LevelLayout.GroundScale, matGround, true);

            Dictionary<string, Transform> boxGroups = new Dictionary<string, Transform>();
            for (int i = 0; i < LevelLayout.Boxes.Length; i++)
            {
                LevelLayout.Box b = LevelLayout.Boxes[i];
                Transform parent;
                if (!boxGroups.TryGetValue(b.group, out parent))
                {
                    GameObject groupGo = new GameObject(b.group);
                    groupGo.transform.SetParent(environment.transform, false);
                    parent = groupGo.transform;
                    boxGroups[b.group] = parent;
                }
                Prim(PrimitiveType.Cube, b.name, parent, new Vector3(b.cx, b.cy, b.cz), new Vector3(b.sx, b.sy, b.sz),
                     b.cover ? matCover : matWall, true);
            }
            SetStaticRecursive(environment);
            SetupNavMesh(environment);

            // ---------- Sistemas e interfaz ----------
            GameObject systems = InstantiatePrefab("GameSystems", Vector3.zero);
            GameManager manager = systems.GetComponent<GameManager>();
            SetFloat(manager, "timeLimit", 900f);        // 15 min en total
            SetFloat(manager, "stageTimeBonus", 120f);   // +2 min al pasar de zona
            PrefabUtility.InstantiatePrefab(LoadPrefab("UI_Canvas"));

            // ---------- Jugador (mira hacia +Z) ----------
            GameObject player = InstantiatePrefab("Player", LevelLayout.PlayerStart);

            // ---------- Puertas: la de la zona 1 lleva a la 2; la última es la salida final ----------
            GameObject doorsRoot = new GameObject("Doors");
            for (int i = 0; i < LevelLayout.Doors.Length; i++)
            {
                LevelLayout.Spot spot = LevelLayout.Doors[i];
                GameObject door = InstantiatePrefab("ExitDoor", new Vector3(spot.x, 0f, spot.z));
                door.name = i == LevelLayout.Doors.Length - 1 ? "ExitDoor_Final" : "ExitDoor_Zona" + spot.stage;
                SetInt(door.GetComponent<ExitDoor>(), "stage", spot.stage);
                door.transform.SetParent(doorsRoot.transform, true);
            }

            // ---------- Orbes (agrupados por zona; NO van dentro de un StageGroup) ----------
            Dictionary<int, Transform> orbGroups = new Dictionary<int, Transform>();
            int[] orbCounter = new int[16];
            for (int i = 0; i < LevelLayout.Orbs.Length; i++)
            {
                LevelLayout.Spot spot = LevelLayout.Orbs[i];
                Transform parent;
                if (!orbGroups.TryGetValue(spot.stage, out parent))
                {
                    GameObject groupGo = new GameObject("Orbs_Zona" + spot.stage);
                    parent = groupGo.transform;
                    orbGroups[spot.stage] = parent;
                }
                GameObject orb = InstantiatePrefab("Orb", new Vector3(spot.x, 0f, spot.z));
                orb.name = "Orb_Z" + spot.stage + "_" + (++orbCounter[spot.stage]);
                SetInt(orb.GetComponent<Collectible>(), "stage", spot.stage);
                orb.transform.SetParent(parent, true);
            }

            // ---------- Lámparas (luces Realtime: son el "antes" de la optimización con luces Baked) ----------
            GameObject lampsRoot = new GameObject("Lamps");
            for (int i = 0; i < LevelLayout.Lamps.Length; i++)
            {
                GameObject lamp = InstantiatePrefab("Lamp", LevelLayout.Lamps[i]);
                lamp.name = "Lamp_" + (i + 1);
                lamp.transform.SetParent(lampsRoot.transform, true);
            }

            // ---------- Rutas de patrulla ----------
            GameObject routesRoot = new GameObject("PatrolRoutes");
            PatrolRoute[] routes = new PatrolRoute[LevelLayout.Routes.Length];
            for (int r = 0; r < LevelLayout.Routes.Length; r++)
            {
                GameObject routeGo = InstantiatePrefab("PatrolRoute", Vector3.zero);
                routeGo.name = "Ruta_" + (r + 1);
                Vector2[] points = LevelLayout.Routes[r];
                for (int p = 0; p < points.Length && p < routeGo.transform.childCount; p++)
                    routeGo.transform.GetChild(p).position = new Vector3(points[p].x, 0f, points[p].y);
                routeGo.transform.SetParent(routesRoot.transform, true);
                routes[r] = routeGo.GetComponent<PatrolRoute>();
            }

            // ---------- Enemigos: cada zona tiene su StageGroup (solo existen mientras estás en su zona) ----------
            Dictionary<int, Transform> enemyGroups = new Dictionary<int, Transform>();
            int[] enemyCounter = new int[16];
            for (int i = 0; i < LevelLayout.Enemies.Length; i++)
            {
                LevelLayout.Spot spot = LevelLayout.Enemies[i];
                Transform parent;
                if (!enemyGroups.TryGetValue(spot.stage, out parent))
                {
                    GameObject groupGo = new GameObject("Enemigos_Zona" + spot.stage);
                    StageGroup stageGroup = groupGo.AddComponent<StageGroup>();
                    SetInt(stageGroup, "stage", spot.stage);
                    parent = groupGo.transform;
                    enemyGroups[spot.stage] = parent;
                }

                GameObject enemy = InstantiatePrefab("Enemy", new Vector3(spot.x, 0.05f, spot.z));
                enemy.name = "Enemy_Z" + spot.stage + "_" + (++enemyCounter[spot.stage]);
                EnemyAI ai = enemy.GetComponent<EnemyAI>();
                SetRef(ai, "route", routes[spot.route]);
                if (spot.stage >= 2)
                {
                    // Los enemigos de la zona 2 son más rápidos y ven más lejos
                    SetFloat(ai, "chaseSpeed", 5f);
                    SetFloat(ai, "sightRange", 17f);
                }
                enemy.transform.SetParent(parent, true);
            }

            Selection.activeGameObject = player;
            EditorSceneManager.SaveScene(scene, LevelScenePath);
        }

        static void ConfigureNightLighting()
        {
            // Luna: luz direccional tenue y azulada
            GameObject moonGo = new GameObject("Moon (Directional Light)");
            Light moon = moonGo.AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(0.55f, 0.65f, 1f);
            moon.intensity = 0.35f;
            moon.shadows = LightShadows.Soft;
            moon.lightmapBakeType = LightmapBakeType.Mixed;
            moonGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.sun = moon;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.05f, 0.06f, 0.09f);

            // Niebla: ambienta y además reduce lo que hay que dibujar a lo lejos
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.03f, 0.04f, 0.06f);
            RenderSettings.fogDensity = 0.03f;
        }

        static void SetStaticRecursive(GameObject go)
        {
            GameObjectUtility.SetStaticEditorFlags(go, AllStatic);
            foreach (Transform child in go.transform) SetStaticRecursive(child.gameObject);
        }

        /// <summary>
        /// Añade un NavMeshSurface a "Environment" (solo hornea lo que cuelga de él, así el enemigo y el jugador no
        /// quedan "tallados" en el NavMesh) e intenta hornearlo. Usa los COLLIDERS (no los modelos visibles): así
        /// puedes decorar con modelos sin Collider encima de las paredes sin alterar por dónde camina el enemigo.
        /// Se usa reflexión para no depender de la versión exacta del paquete AI Navigation.
        /// </summary>
        static void SetupNavMesh(GameObject environment)
        {
            Type surfaceType = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
            if (surfaceType == null)
            {
                Debug.LogWarning("[Asistente] Falta el paquete AI Navigation: Window > Package Manager > Unity Registry > AI Navigation > Install. " +
                                 "Luego añade un NavMeshSurface a 'Environment' y pulsa Bake.");
                return;
            }

            Component surface = environment.AddComponent(surfaceType);

            SerializedObject so = new SerializedObject(surface);
            SerializedProperty collect = so.FindProperty("m_CollectObjects");
            if (collect != null) collect.enumValueIndex = 2;   // 2 = "Current Object Hierarchy" (solo lo que cuelga de Environment)
            SerializedProperty geometry = so.FindProperty("m_UseGeometry");
            if (geometry != null) geometry.enumValueIndex = 1; // 1 = "Physics Colliders" (usa los colliders, no los modelos)
            so.ApplyModifiedPropertiesWithoutUndo();

            try
            {
                MethodInfo build = surfaceType.GetMethod("BuildNavMesh", Type.EmptyTypes);
                if (build == null) throw new MissingMethodException("BuildNavMesh");

                build.Invoke(surface, null);

                // Guardamos los datos del NavMesh como asset para que sobrevivan al cerrar la escena.
                PropertyInfo dataProperty = surfaceType.GetProperty("navMeshData");
                Object data = dataProperty != null ? dataProperty.GetValue(surface, null) as Object : null;
                if (data != null && !AssetDatabase.Contains(data))
                {
                    AssetDatabase.CreateAsset(data, ScenesDir + "/Level01_NavMesh.asset");
                    EditorUtility.SetDirty(surface);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Asistente] No pude hornear el NavMesh automáticamente (" + e.Message + "). " +
                                 "Selecciona 'Environment' en la Hierarchy y pulsa el botón Bake del NavMeshSurface.");
            }
        }
    }
}
