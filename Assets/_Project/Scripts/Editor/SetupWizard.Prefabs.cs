using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace EscapeGame.EditorTools
{
    public static partial class SetupWizard
    {
        static void CreateAllPrefabs()
        {
            NewTempScene(); // trabajamos en una escena vacía para no ensuciar la tuya

            GameObject fxBurst = CreateFxPickupBurst();
            CreatePlayer();
            CreateOrb(fxBurst);
            CreateEnemy();
            CreatePatrolRoute();
            CreateExitDoor();
            CreateLamp();
            CreateGameSystems(fxBurst);
            CreateUiCanvas();
        }

        // ============================ PARTÍCULAS ============================

        static Material GetParticleMaterial()
        {
            Material material = null;

            // Material de partículas del pipeline activo (URP, HDRP...). Por reflexión para no depender de la versión.
            RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline != null)
            {
                PropertyInfo property = pipeline.GetType().GetProperty("defaultParticleMaterial");
                if (property != null) material = property.GetValue(pipeline, null) as Material;
            }

            if (material == null) material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
            if (material == null) Debug.LogWarning("[Asistente] No encontré un material de partículas; se verán rosados. Asígnalo a mano.");
            return material;
        }

        static Gradient FadeOutGradient()
        {
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        static Gradient FadeInOutGradient()
        {
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        /// <summary>Explosión de chispas celestes al recoger un orbe.</summary>
        static GameObject CreateFxPickupBurst()
        {
            GameObject go = new GameObject("FX_PickupBurst");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetParticleMaterial();

            ParticleSystem.MainModule main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.4f, 0.95f, 1f, 1f), Color.white);
            main.gravityModifier = -0.15f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)30) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(FadeOutGradient());

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            return SavePrefab(go, "FX_PickupBurst");
        }

        /// <summary>Motas de polvo flotando alrededor del jugador (ambientación barata: 150 partículas).</summary>
        static void CreateAmbientDust(Transform parent)
        {
            GameObject go = new GameObject("AmbientDust");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = GetParticleMaterial();

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.duration = 5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.04f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.8f, 0.85f, 1f, 0.3f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 150;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 20f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(16f, 4f, 16f);

            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.2f;
            noise.frequency = 0.3f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(FadeInOutGradient());
        }

        // ============================ PLAYER ============================

        static GameObject CreatePlayer()
        {
            GameObject root = new GameObject("Player");
            root.tag = "Player";

            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 45f;

            GameObject pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            GameObject camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(pivot.transform, false);
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 120f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.012f, 0.02f);
            camGo.AddComponent<AudioListener>();

            // Linterna: Spot Light hija de la cámara (la única luz en tiempo real "cara" del juego)
            GameObject flashlightGo = NewLightObject("Flashlight", camGo.transform, new Vector3(0.2f, -0.15f, 0.1f),
                                                     LightType.Spot, new Color(1f, 0.96f, 0.82f), 35f, 24f);
            Light flashlightLight = flashlightGo.GetComponent<Light>();
            flashlightLight.spotAngle = 62f;
            flashlightLight.innerSpotAngle = 28f;
            flashlightLight.shadows = LightShadows.Hard;
            Flashlight flashlight = flashlightGo.AddComponent<Flashlight>();
            SetRef(flashlight, "lightSource", flashlightLight);

            root.AddComponent<PlayerMovement>();
            PlayerLook look = root.AddComponent<PlayerLook>();
            SetRef(look, "cameraPivot", pivot.transform);
            root.AddComponent<PlayerFootsteps>(); // añade un AudioSource solo (RequireComponent)

            CreateAmbientDust(root.transform);

            return SavePrefab(root, "Player");
        }

        // ============================ ORBE ============================

        static GameObject CreateOrb(GameObject fxBurst)
        {
            GameObject root = new GameObject("Orb");

            // Zona de recogida (trigger). Colócalo en el suelo: el orbe flota a 0.8 m.
            SphereCollider trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.8f;
            trigger.center = new Vector3(0f, 0.8f, 0f);

            GameObject visual = Prim(PrimitiveType.Sphere, "Visual", root.transform, new Vector3(0f, 0.8f, 0f),
                                     Vector3.one * 0.35f, matOrb, false);

            NewLightObject("OrbLight", root.transform, new Vector3(0f, 0.8f, 0f), LightType.Point,
                           new Color(0.35f, 0.9f, 1f), 5f, 6f);

            Collectible collectible = root.AddComponent<Collectible>();
            SetRef(collectible, "visual", visual.transform);
            SetRef(collectible, "pickupEffectPrefab", fxBurst);

            return SavePrefab(root, "Orb");
        }

        // ============================ ENEMIGO ============================

        static GameObject CreateEnemy()
        {
            GameObject root = new GameObject("Enemy");

            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.speed = 1.8f;
            agent.angularSpeed = 240f;
            agent.acceleration = 10f;
            agent.stoppingDistance = 0.5f;
            agent.radius = 0.4f;
            agent.height = 1.9f;

            CapsuleCollider body = root.AddComponent<CapsuleCollider>();
            body.height = 1.9f;
            body.radius = 0.4f;
            body.center = new Vector3(0f, 0.95f, 0f);

            AudioSource audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 2f;
            audioSource.maxDistance = 30f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;

            // Modelo provisional (cápsula con ojos). Para usar tu modelo: ver la guía ("Cambiar el modelo del enemigo").
            GameObject model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);
            Prim(PrimitiveType.Capsule, "PlaceholderBody", model.transform, new Vector3(0f, 0.95f, 0f),
                 new Vector3(0.8f, 0.95f, 0.8f), matEnemy, false);
            Prim(PrimitiveType.Sphere, "EyeLeft", model.transform, new Vector3(-0.14f, 1.62f, 0.37f), Vector3.one * 0.11f, matEnemyEyes, false);
            Prim(PrimitiveType.Sphere, "EyeRight", model.transform, new Vector3(0.14f, 1.62f, 0.37f), Vector3.one * 0.11f, matEnemyEyes, false);

            // Brillo rojo tenue: te deja ver al monstruo en la oscuridad (y ayuda a orientarte)
            NewLightObject("RedGlow", root.transform, new Vector3(0f, 1.2f, 0f), LightType.Point,
                           new Color(1f, 0.1f, 0.05f), 2.5f, 4f);

            root.AddComponent<EnemyAI>();

            return SavePrefab(root, "Enemy");
        }

        static GameObject CreatePatrolRoute()
        {
            GameObject root = new GameObject("PatrolRoute");
            root.AddComponent<PatrolRoute>();

            Vector3[] points =
            {
                new Vector3(-6f, 0f, -6f), new Vector3(6f, 0f, -6f), new Vector3(6f, 0f, 6f), new Vector3(-6f, 0f, 6f)
            };
            for (int i = 0; i < points.Length; i++)
            {
                GameObject point = new GameObject("Point_" + (i + 1));
                point.transform.SetParent(root.transform, false);
                point.transform.localPosition = points[i];
            }

            return SavePrefab(root, "PatrolRoute");
        }

        // ============================ PUERTA DE SALIDA ============================

        static GameObject CreateExitDoor()
        {
            GameObject root = new GameObject("ExitDoor");

            // Zona de interacción (trigger) alrededor de la puerta
            BoxCollider interactZone = root.AddComponent<BoxCollider>();
            interactZone.isTrigger = true;
            interactZone.center = new Vector3(0f, 1.5f, 0f);
            interactZone.size = new Vector3(5f, 3f, 5f);

            // Marco (hueco libre de 2 m de ancho x 2.7 m de alto)
            GameObject frame = new GameObject("Frame");
            frame.transform.SetParent(root.transform, false);
            Prim(PrimitiveType.Cube, "PostLeft", frame.transform, new Vector3(-1.15f, 1.5f, 0f), new Vector3(0.3f, 3f, 0.4f), matDoorFrame, true);
            Prim(PrimitiveType.Cube, "PostRight", frame.transform, new Vector3(1.15f, 1.5f, 0f), new Vector3(0.3f, 3f, 0.4f), matDoorFrame, true);
            Prim(PrimitiveType.Cube, "Lintel", frame.transform, new Vector3(0f, 2.85f, 0f), new Vector3(2.6f, 0.3f, 0.4f), matDoorFrame, true);

            // Bisagra + hoja de la puerta (la hoja es hija de la bisagra: al girar la bisagra, gira la puerta)
            GameObject hinge = new GameObject("Hinge");
            hinge.transform.SetParent(root.transform, false);
            hinge.transform.localPosition = new Vector3(-1f, 0f, 0f);
            GameObject leaf = Prim(PrimitiveType.Cube, "Leaf", hinge.transform, new Vector3(1f, 1.35f, 0f), new Vector3(2f, 2.7f, 0.12f), matDoorWood, true);

            // Lámpara indicadora: roja = bloqueada, verde = desbloqueada
            GameObject status = Prim(PrimitiveType.Sphere, "StatusLamp", root.transform, new Vector3(0f, 3.3f, -0.1f), Vector3.one * 0.3f, matDoorLocked, false);
            GameObject statusLightGo = NewLightObject("StatusLight", status.transform, Vector3.zero, LightType.Point, new Color(1f, 0.15f, 0.1f), 6f, 8f);

            // Zona de victoria: al otro lado de la puerta (hacia donde apunta la flecha azul)
            GameObject exitZoneGo = new GameObject("ExitZone");
            exitZoneGo.transform.SetParent(root.transform, false);
            exitZoneGo.transform.localPosition = new Vector3(0f, 1.5f, 3f);
            BoxCollider exitCollider = exitZoneGo.AddComponent<BoxCollider>();
            exitCollider.isTrigger = true;
            exitCollider.size = new Vector3(3.5f, 3f, 2.5f);
            ExitZone exitZone = exitZoneGo.AddComponent<ExitZone>();

            ExitDoor door = root.AddComponent<ExitDoor>();
            SetRef(door, "hinge", hinge.transform);
            SetRefs(door, "blockingColliders", new Object[] { leaf.GetComponent<Collider>() });
            SetRef(door, "statusLight", statusLightGo.GetComponent<Light>());
            SetRef(door, "statusRenderer", status.GetComponent<Renderer>());
            SetRef(door, "lockedMaterial", matDoorLocked);
            SetRef(door, "unlockedMaterial", matDoorUnlocked);
            SetRef(exitZone, "door", door);

            return SavePrefab(root, "ExitDoor");
        }

        // ============================ LÁMPARA ============================

        static GameObject CreateLamp()
        {
            GameObject root = new GameObject("Lamp");
            Prim(PrimitiveType.Sphere, "Bulb", root.transform, Vector3.zero, Vector3.one * 0.25f, matLamp, false);
            NewLightObject("LampLight", root.transform, Vector3.zero, LightType.Point, new Color(1f, 0.82f, 0.55f), 8f, 12f);
            return SavePrefab(root, "Lamp");
        }

        // ============================ SISTEMAS DEL JUEGO ============================

        static GameObject CreateGameSystems(GameObject fxBurst)
        {
            GameObject root = new GameObject("GameSystems");
            root.AddComponent<GameManager>();
            root.AddComponent<AudioManager>();
            ObjectPool pool = root.AddComponent<ObjectPool>();
            root.AddComponent<PerformanceOverlay>();
            root.AddComponent<DebugTools>();

            // Pool: 8 copias del efecto de recogida listas desde el inicio
            SerializedObject so = new SerializedObject(pool);
            SerializedProperty entries = so.FindProperty("entries");
            entries.arraySize = 1;
            SerializedProperty first = entries.GetArrayElementAtIndex(0);
            first.FindPropertyRelative("prefab").objectReferenceValue = fxBurst;
            first.FindPropertyRelative("prewarm").intValue = 8;
            first.FindPropertyRelative("lifetime").floatValue = 1.5f;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, "GameSystems");
        }
    }
}
