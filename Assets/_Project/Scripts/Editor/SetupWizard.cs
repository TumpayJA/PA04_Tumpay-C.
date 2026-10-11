using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace EscapeGame.EditorTools
{
    /// <summary>
    /// ASISTENTE DEL PROYECTO (solo existe en el Editor, no entra al Build).
    /// Con un clic crea los prefabs (Player, Orb, Enemy, ExitDoor, Lamp, GameSystems, UI_Canvas...),
    /// las escenas MainMenu y Level01 (un nivel de prueba "greybox") y las registra en el Build,
    /// todo ya conectado entre sí. Después tú reemplazas/amplías el mapa dentro de "Environment".
    /// Menú: Tools > Escape Game
    /// </summary>
    public static partial class SetupWizard
    {
        const string Title = "Escape Game - Asistente";
        const string Root = "Assets/_Project";
        const string PrefabsDir = Root + "/Prefabs";
        const string ScenesDir = Root + "/Scenes";
        const string MaterialsDir = Root + "/Materials";
        const string MenuScenePath = ScenesDir + "/MainMenu.unity";
        const string LevelScenePath = ScenesDir + "/Level01.unity";

        // ============================ MENÚ ============================

        [MenuItem("Tools/Escape Game/0 - Diagnóstico del proyecto", false, 0)]
        public static void MenuDiagnose()
        {
            string report;
            bool ok = Diagnose(out report);
            Debug.Log("[Escape Game] Diagnóstico:\n" + report);
            EditorUtility.DisplayDialog(Title, (ok ? "Todo en orden.\n\n" : "Hay cosas por arreglar:\n\n") + report, "OK");
        }

        [MenuItem("Tools/Escape Game/1 - Crear TODO (prefabs + escenas)", false, 1)]
        public static void MenuCreateEverything()
        {
            string report;
            if (!Diagnose(out report))
            {
                EditorUtility.DisplayDialog(Title, "Antes de continuar hay que arreglar esto:\n\n" + report, "OK");
                return;
            }

            bool go = EditorUtility.DisplayDialog(Title,
                "Voy a crear (o sobrescribir) los prefabs y las escenas MainMenu y Level01 dentro de Assets/_Project.\n\n" +
                "Si ya modificaste esos prefabs o escenas, cancela y usa las opciones 2, 3 o 4 por separado.\n\n¿Continuar?",
                "Continuar", "Cancelar");
            if (!go) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                EditorUtility.DisplayProgressBar(Title, "Creando carpetas y materiales...", 0.05f);
                EnsureFolders();
                CreateMaterials();

                EditorUtility.DisplayProgressBar(Title, "Creando prefabs...", 0.25f);
                CreateAllPrefabs();

                EditorUtility.DisplayProgressBar(Title, "Creando el menú principal...", 0.6f);
                BuildMainMenuScene();

                EditorUtility.DisplayProgressBar(Title, "Creando el mapa grande (Level01)...", 0.8f);
                BuildLevelScene();

                RegisterScenesInBuild();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogException(e);
                EditorUtility.DisplayDialog(Title,
                    "Ocurrió un error:\n" + e.Message + "\n\nMira la consola (Window > General > Console) y mándale el mensaje rojo a Claude.",
                    "OK");
                return;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.DisplayDialog(Title,
                "¡Listo!\n\nSe abrió la escena Level01 (mapa grande de 2 zonas).\n" +
                "1) Pulsa PLAY y comprueba que todo funciona.\n" +
                "2) Luego diseña tu mapa DENTRO del objeto 'Environment' y vuelve a hacer Bake del NavMesh.\n\n" +
                "Sigue la guía paso a paso para el resto (audio, luces, optimización).",
                "OK");
        }

        [MenuItem("Tools/Escape Game/2 - Solo prefabs", false, 20)]
        public static void MenuPrefabsOnly()
        {
            string report;
            if (!Diagnose(out report))
            {
                EditorUtility.DisplayDialog(Title, "Antes de continuar hay que arreglar esto:\n\n" + report, "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                EnsureFolders();
                CreateMaterials();
                CreateAllPrefabs();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            EditorUtility.DisplayDialog(Title, "Prefabs creados en " + PrefabsDir, "OK");
        }

        [MenuItem("Tools/Escape Game/3 - Solo escena MainMenu", false, 21)]
        public static void MenuMainMenuOnly()
        {
            string report;
            if (!Diagnose(out report))
            {
                EditorUtility.DisplayDialog(Title, "Antes de continuar hay que arreglar esto:\n\n" + report, "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolders();
            BuildMainMenuScene();
            RegisterScenesInBuild();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Escape Game/4 - Solo escena Level01 (reemplaza la actual)", false, 22)]
        public static void MenuLevelOnly()
        {
            string report;
            if (!Diagnose(out report))
            {
                EditorUtility.DisplayDialog(Title, "Antes de continuar hay que arreglar esto:\n\n" + report, "OK");
                return;
            }
            if (!EditorUtility.DisplayDialog(Title,
                    "Esto REEMPLAZA la escena Level01 por el mapa grande original (2 zonas). Perderás el mapa que hayas hecho en ella.\n\n¿Seguro?",
                    "Reemplazar", "Cancelar")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                EnsureFolders();
                CreateMaterials();
                BuildLevelScene();
                RegisterScenesInBuild();
                AssetDatabase.SaveAssets();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog(Title, "Ocurrió un error:\n" + e.Message + "\n\nMira la consola.", "OK");
            }
        }

        [MenuItem("Tools/Escape Game/5 - Añadir escenas al Build", false, 40)]
        public static void MenuRegisterScenes()
        {
            RegisterScenesInBuild();
            EditorUtility.DisplayDialog(Title, "Escenas MainMenu y Level01 añadidas a la lista del Build.", "OK");
        }

        // ============================ DIAGNÓSTICO ============================

        static bool Diagnose(out string report)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            bool ok = true;

#if ENABLE_INPUT_SYSTEM
            sb.AppendLine("✔ Entrada: Input System nuevo");
#elif ENABLE_LEGACY_INPUT_MANAGER
            sb.AppendLine("✔ Entrada: Input Manager antiguo");
#else
            sb.AppendLine("✖ Entrada: no hay ningún sistema activo (Project Settings > Player > Active Input Handling).");
            ok = false;
#endif

            if (GraphicsSettings.currentRenderPipeline != null)
                sb.AppendLine("✔ Render pipeline: " + GraphicsSettings.currentRenderPipeline.GetType().Name);
            else
                sb.AppendLine("⚠ Render pipeline: Built-in. Funciona, pero se recomienda la plantilla Universal 3D (URP).");

            if (Resources.Load<TMP_Settings>("TMP Settings") != null)
            {
                sb.AppendLine("✔ TextMeshPro Essentials importado");
            }
            else
            {
                sb.AppendLine("✖ Falta importar TextMeshPro Essentials: menú Window > TextMeshPro > Import TMP Essential Resources, " +
                              "pulsa 'Import TMP Essentials' y vuelve a ejecutar el asistente.");
                ok = false;
            }

            if (Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation") != null)
            {
                sb.AppendLine("✔ Paquete AI Navigation instalado");
            }
            else
            {
                sb.AppendLine("✖ Falta el paquete AI Navigation: Window > Package Manager > pestaña 'Unity Registry' > " +
                              "busca 'AI Navigation' > Install. Espera a que compile y vuelve a ejecutar el asistente.");
                ok = false;
            }

            report = sb.ToString();
            return ok;
        }

        // ============================ CARPETAS Y MATERIALES ============================

        static void EnsureFolders()
        {
            string[] folders =
            {
                Root, PrefabsDir, ScenesDir, MaterialsDir,
                Root + "/Audio", Root + "/Audio/Music", Root + "/Audio/Ambience", Root + "/Audio/SFX",
                Root + "/Art", Root + "/Art/Models", Root + "/Art/Textures"
            };
            for (int i = 0; i < folders.Length; i++) EnsureFolder(folders[i]);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static Material matGround, matWall, matCover, matOrb, matEnemy, matEnemyEyes;
        static Material matDoorWood, matDoorFrame, matDoorLocked, matDoorUnlocked, matLamp;

        static void CreateMaterials()
        {
            matGround = GetMaterial("M_Ground", new Color(0.16f, 0.17f, 0.15f), null);
            matWall = GetMaterial("M_Wall", new Color(0.30f, 0.30f, 0.32f), null);
            matCover = GetMaterial("M_Cover", new Color(0.24f, 0.20f, 0.17f), null);
            matOrb = GetMaterial("M_Orb", new Color(0.3f, 0.9f, 1f), new Color(0.2f, 1.2f, 1.6f));
            matEnemy = GetMaterial("M_Enemy", new Color(0.12f, 0.05f, 0.05f), null);
            matEnemyEyes = GetMaterial("M_EnemyEyes", new Color(1f, 0.1f, 0.1f), new Color(3f, 0.1f, 0.1f));
            matDoorWood = GetMaterial("M_DoorWood", new Color(0.30f, 0.18f, 0.10f), null);
            matDoorFrame = GetMaterial("M_DoorFrame", new Color(0.22f, 0.22f, 0.24f), null);
            matDoorLocked = GetMaterial("M_DoorLocked", new Color(0.5f, 0.05f, 0.05f), new Color(2f, 0.15f, 0.1f));
            matDoorUnlocked = GetMaterial("M_DoorUnlocked", new Color(0.05f, 0.5f, 0.15f), new Color(0.2f, 2f, 0.5f));
            matLamp = GetMaterial("M_Lamp", new Color(1f, 0.85f, 0.55f), new Color(2f, 1.5f, 0.8f));
        }

        static Material GetMaterial(string materialName, Color color, Color? emission)
        {
            string path = MaterialsDir + "/" + materialName + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader);
            mat.name = materialName;
            mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f); // URP
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.15f); // Built-in

            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // ============================ UTILIDADES ============================

        static void NewTempScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        static GameObject SavePrefab(GameObject temporary, string prefabName)
        {
            string path = PrefabsDir + "/" + prefabName + ".prefab";
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(temporary, path);
            Object.DestroyImmediate(temporary);
            return asset;
        }

        static GameObject LoadPrefab(string prefabName)
        {
            string path = PrefabsDir + "/" + prefabName + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                throw new InvalidOperationException("No existe el prefab '" + path + "'. Ejecuta primero 'Crear TODO' o 'Solo prefabs'.");
            return prefab;
        }

        static GameObject InstantiatePrefab(string prefabName, Vector3 position)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefab(prefabName));
            instance.transform.position = position;
            return instance;
        }

        /// <summary>Crea una primitiva (cubo, esfera...) como hija de "parent" con posición, escala y material.</summary>
        static GameObject Prim(PrimitiveType type, string objectName, Transform parent, Vector3 localPosition,
                               Vector3 localScale, Material material, bool keepCollider)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = objectName;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;

            if (!keepCollider)
            {
                Collider col = go.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
            }
            return go;
        }

        static GameObject NewLightObject(string objectName, Transform parent, Vector3 localPosition, LightType type,
                                         Color color, float intensity, float range)
        {
            GameObject go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;

            Light light = go.AddComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return go;
        }

        // ---- Asignar campos [SerializeField] privados desde código de Editor ----

        static void SetRef(Object target, string field, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning("[Asistente] " + target.GetType().Name + " no tiene el campo '" + field + "'.");
                return;
            }
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        static void SetRefs(Object target, string field, Object[] values)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning("[Asistente] " + target.GetType().Name + " no tiene el campo '" + field + "'.");
                return;
            }
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        static void SetInt(Object target, string field, int value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning("[Asistente] " + target.GetType().Name + " no tiene el campo '" + field + "'.");
                return;
            }
            property.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        static void SetFloat(Object target, string field, float value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning("[Asistente] " + target.GetType().Name + " no tiene el campo '" + field + "'.");
                return;
            }
            property.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        static void RegisterScenesInBuild()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath) != null)
                scenes.Add(new EditorBuildSettingsScene(MenuScenePath, true));
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LevelScenePath) != null)
                scenes.Add(new EditorBuildSettingsScene(LevelScenePath, true));

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
