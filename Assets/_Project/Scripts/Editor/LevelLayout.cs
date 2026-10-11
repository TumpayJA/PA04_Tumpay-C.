using UnityEngine;

namespace EscapeGame.EditorTools
{
    /// <summary>
    /// DATOS DEL MAPA GRANDE (2 zonas, 13 orbes, 5 enemigos). Los usa el asistente (SetupWizard.Level.cs).
    /// Este mapa fue verificado con una simulación: todo es alcanzable, las zonas solo se comunican por sus puertas
    /// y los puntos clave no caen dentro de paredes. Coordenadas en metros (X = lados, Z = adelante).
    /// </summary>
    static class LevelLayout
    {
        public struct Box
        {
            public string group, name;
            public bool cover;                       // true = obstáculo/cobertura, false = muro
            public float cx, cy, cz, sx, sy, sz;     // centro y tamaño
            public Box(string group, string name, bool cover, float cx, float cy, float cz, float sx, float sy, float sz)
            { this.group = group; this.name = name; this.cover = cover; this.cx = cx; this.cy = cy; this.cz = cz; this.sx = sx; this.sy = sy; this.sz = sz; }
        }

        public struct Spot
        {
            public float x, z;
            public int stage;   // zona a la que pertenece
            public int route;   // solo enemigos: índice en Routes
            public Spot(float x, float z, int stage, int route) { this.x = x; this.z = z; this.stage = stage; this.route = route; }
        }

        public static readonly Vector3 PlayerStart = new Vector3(0f, 0.1f, -36f);

        // Suelo: x de -40 a 40, z de -40 a 126
        public static readonly Vector3 GroundCenter = new Vector3(0f, 0f, 43f);
        public static readonly Vector3 GroundScale = new Vector3(8f, 1f, 16.6f);

        public static readonly Box[] Boxes =
        {
            new Box("Perimeter", "Wall_South", false, 0f, 2f, -40.5f, 82f, 4f, 1f),
            new Box("Perimeter", "Wall_North", false, 0f, 2f, 126.5f, 82f, 4f, 1f),
            new Box("Perimeter", "Wall_East", false, 40.5f, 2f, 43f, 1f, 4f, 168f),
            new Box("Perimeter", "Wall_West", false, -40.5f, 2f, 43f, 1f, 4f, 168f),
            new Box("Dividers", "Divider_Zona1_1", false, -20.65f, 2f, 40f, 38.7f, 4f, 0.6f),
            new Box("Dividers", "Divider_Zona1_2", false, 20.65f, 2f, 40f, 38.7f, 4f, 0.6f),
            new Box("Dividers", "Divider_Zona1_Header", false, 0f, 3.5f, 40f, 2.6f, 1f, 0.6f),
            new Box("Dividers", "Divider_Final_1", false, -20.65f, 2f, 120f, 38.7f, 4f, 0.6f),
            new Box("Dividers", "Divider_Final_2", false, 20.65f, 2f, 120f, 38.7f, 4f, 0.6f),
            new Box("Dividers", "Divider_Final_Header", false, 0f, 3.5f, 120f, 2.6f, 1f, 0.6f),
            new Box("Zona1", "Capilla_N_1", false, -30f, 2f, -8f, 8f, 4f, 0.6f),
            new Box("Zona1", "Capilla_N_2", false, -18f, 2f, -8f, 8f, 4f, 0.6f),
            new Box("Zona1", "Capilla_S_1", false, -24f, 2f, -26f, 20f, 4f, 0.6f),
            new Box("Zona1", "Capilla_E_1", false, -14f, 2f, -22.5f, 0.6f, 4f, 7f),
            new Box("Zona1", "Capilla_E_2", false, -14f, 2f, -11.5f, 0.6f, 4f, 7f),
            new Box("Zona1", "Capilla_W_1", false, -34f, 2f, -17f, 0.6f, 4f, 18f),
            new Box("Zona1", "Mausoleo_N_1", false, 24f, 2f, -4f, 20f, 4f, 0.6f),
            new Box("Zona1", "Mausoleo_S_1", false, 18f, 2f, -22f, 8f, 4f, 0.6f),
            new Box("Zona1", "Mausoleo_S_2", false, 30f, 2f, -22f, 8f, 4f, 0.6f),
            new Box("Zona1", "Mausoleo_E_1", false, 34f, 2f, -13f, 0.6f, 4f, 18f),
            new Box("Zona1", "Mausoleo_W_1", false, 14f, 2f, -18.5f, 0.6f, 4f, 7f),
            new Box("Zona1", "Mausoleo_W_2", false, 14f, 2f, -7.5f, 0.6f, 4f, 7f),
            new Box("Zona1", "CriptaMenor_N_1", false, -23f, 2f, 30f, 22f, 4f, 0.6f),
            new Box("Zona1", "CriptaMenor_S_1", false, -29.5f, 2f, 10f, 9f, 4f, 0.6f),
            new Box("Zona1", "CriptaMenor_S_2", false, -16.5f, 2f, 10f, 9f, 4f, 0.6f),
            new Box("Zona1", "CriptaMenor_E_1", false, -12f, 2f, 14f, 0.6f, 4f, 8f),
            new Box("Zona1", "CriptaMenor_E_2", false, -12f, 2f, 26f, 0.6f, 4f, 8f),
            new Box("Zona1", "CriptaMenor_W_1", false, -34f, 2f, 20f, 0.6f, 4f, 20f),
            new Box("Zona1", "Osario_N_1", false, 23f, 2f, 32f, 22f, 4f, 0.6f),
            new Box("Zona1", "Osario_S_1", false, 16f, 2f, 12f, 8f, 4f, 0.6f),
            new Box("Zona1", "Osario_S_2", false, 29f, 2f, 12f, 10f, 4f, 0.6f),
            new Box("Zona1", "Osario_E_1", false, 34f, 2f, 22f, 0.6f, 4f, 20f),
            new Box("Zona1", "Osario_W_1", false, 12f, 2f, 16f, 0.6f, 4f, 8f),
            new Box("Zona1", "Osario_W_2", false, 12f, 2f, 28f, 0.6f, 4f, 8f),
            new Box("Zona1", "Muro_A_1", true, -8f, 1.1f, -2f, 8f, 2.2f, 0.6f),
            new Box("Zona1", "Muro_B_1", true, 8f, 1.1f, 2f, 8f, 2.2f, 0.6f),
            new Box("Zona1", "Muro_C_1", true, -4f, 1.1f, 16f, 0.6f, 2.2f, 8f),
            new Box("Zona1", "Muro_D_1", true, 6f, 1.1f, 14f, 0.6f, 2.2f, 8f),
            new Box("Zona1", "Muro_E_1", true, -6f, 1.1f, 26f, 8f, 2.2f, 0.6f),
            new Box("Zona1", "Muro_F_1", true, 6f, 1.1f, 28f, 8f, 2.2f, 0.6f),
            new Box("Zona1", "Tumba_1", true, -10f, 0.6f, -30f, 2f, 1.2f, 2f),
            new Box("Zona1", "Tumba_2", true, 10f, 0.6f, -30f, 2f, 1.2f, 2f),
            new Box("Zona1", "Tumba_3", true, -20f, 0.75f, -34f, 3f, 1.5f, 1.5f),
            new Box("Zona1", "Tumba_4", true, 20f, 0.75f, -32f, 1.5f, 1.5f, 3f),
            new Box("Zona1", "Tumba_5", true, 0f, 0.75f, -22f, 2f, 1.5f, 2f),
            new Box("Zona1", "Tumba_6", true, -8f, 0.75f, -14f, 2f, 1.5f, 2f),
            new Box("Zona1", "Tumba_7", true, -2f, 0.6f, -9f, 1.5f, 1.2f, 1.5f),
            new Box("Zona1", "Tumba_8", true, 4f, 0.75f, -12f, 2f, 1.5f, 2f),
            new Box("Zona1", "Tumba_9", true, 9f, 0.6f, -8f, 1.5f, 1.2f, 2.5f),
            new Box("Zona1", "Tumba_10", true, -16f, 0.75f, 0f, 2f, 1.5f, 2f),
            new Box("Zona1", "Tumba_11", true, 16f, 0.75f, 2f, 2f, 1.5f, 2f),
            new Box("Zona1", "Tumba_12", true, -9f, 0.75f, 8f, 2f, 1.5f, 2f),
            new Box("Zona1", "Tumba_13", true, 0f, 0.6f, 6f, 3f, 1.2f, 1.5f),
            new Box("Zona1", "Tumba_14", true, 10f, 0.75f, 6f, 2f, 1.5f, 2f),
            new Box("Zona1", "Tumba_15", true, -16f, 0.75f, 6f, 2f, 1.5f, 2f),
            new Box("Zona1", "Tumba_16", true, 16f, 0.75f, 8f, 2f, 1.5f, 2f),
            new Box("Zona1", "Pilar_1", true, -6f, 2f, 31f, 1f, 4f, 1f),
            new Box("Zona1", "Pilar_2", true, 6f, 2f, 31f, 1f, 4f, 1f),
            new Box("Zona2", "Muro_W1_1", false, -33f, 2f, 56f, 14f, 4f, 0.6f),
            new Box("Zona2", "Muro_W1_2", false, 9f, 2f, 56f, 62f, 4f, 0.6f),
            new Box("Zona2", "Muro_V1_1", false, -14f, 2f, 59f, 0.6f, 4f, 6f),
            new Box("Zona2", "Muro_V1_2", false, -14f, 2f, 72f, 0.6f, 4f, 12f),
            new Box("Zona2", "Muro_V1_3", false, -14f, 2f, 85f, 0.6f, 4f, 6f),
            new Box("Zona2", "Muro_V2_1", false, 14f, 2f, 60f, 0.6f, 4f, 8f),
            new Box("Zona2", "Muro_V2_2", false, 14f, 2f, 74f, 0.6f, 4f, 12f),
            new Box("Zona2", "Muro_V2_3", false, 14f, 2f, 86f, 0.6f, 4f, 4f),
            new Box("Zona2", "Muro_W2_1", false, -21f, 2f, 88f, 38f, 4f, 0.6f),
            new Box("Zona2", "Muro_W2_2", false, 21f, 2f, 88f, 38f, 4f, 0.6f),
            new Box("Zona2", "Sarcofago_1", true, 0f, 0.6f, 52f, 3f, 1.2f, 1.5f),
            new Box("Zona2", "Sarcofago_2", true, -20f, 0.75f, 45f, 2f, 1.5f, 2f),
            new Box("Zona2", "Sarcofago_3", true, 20f, 0.75f, 45f, 2f, 1.5f, 2f),
            new Box("Zona2", "Sarcofago_4", true, -30f, 0.75f, 70f, 3f, 1.5f, 1.5f),
            new Box("Zona2", "Sarcofago_5", true, -22f, 0.75f, 76f, 2f, 1.5f, 2f),
            new Box("Zona2", "Sarcofago_6", true, -36f, 0.75f, 78f, 2f, 1.5f, 2f),
            new Box("Zona2", "Sarcofago_7", true, 30f, 0.75f, 70f, 3f, 1.5f, 1.5f),
            new Box("Zona2", "Sarcofago_8", true, 22f, 0.75f, 76f, 2f, 1.5f, 2f),
            new Box("Zona2", "Sarcofago_9", true, 36f, 0.75f, 78f, 2f, 1.5f, 2f),
            new Box("Zona2", "Sarcofago_10", true, -6f, 0.75f, 66f, 2f, 1.5f, 2f),
            new Box("Zona2", "Sarcofago_11", true, 6f, 0.75f, 80f, 2f, 1.5f, 2f),
            new Box("Zona2", "Sarcofago_12", true, 0f, 0.6f, 100f, 4f, 1.2f, 1.5f),
            new Box("Zona2", "Sarcofago_13", true, -26f, 0.75f, 100f, 3f, 1.5f, 1.5f),
            new Box("Zona2", "Sarcofago_14", true, 26f, 0.75f, 100f, 3f, 1.5f, 1.5f),
            new Box("Zona2", "Sarcofago_15", true, 0f, 0.6f, 112f, 3f, 1.2f, 1.5f),
            new Box("Zona2", "Columna_1", true, -12f, 2f, 48f, 1.2f, 4f, 1.2f),
            new Box("Zona2", "Columna_2", true, 12f, 2f, 48f, 1.2f, 4f, 1.2f),
            new Box("Zona2", "Columna_3", true, -12f, 2f, 96f, 1.2f, 4f, 1.2f),
            new Box("Zona2", "Columna_4", true, 12f, 2f, 96f, 1.2f, 4f, 1.2f),
            new Box("Zona2", "Columna_5", true, -12f, 2f, 108f, 1.2f, 4f, 1.2f),
            new Box("Zona2", "Columna_6", true, 12f, 2f, 108f, 1.2f, 4f, 1.2f),
        };

        public static readonly Spot[] Doors =
        {
            new Spot(0f, 40f, 1, 0),
            new Spot(0f, 120f, 2, 0),
        };

        public static readonly Spot[] Orbs =
        {
            new Spot(-26f, -17f, 1, 0),
            new Spot(26f, -12f, 1, 0),
            new Spot(-26f, 20f, 1, 0),
            new Spot(26f, 22f, 1, 0),
            new Spot(0f, 0f, 1, 0),
            new Spot(12f, 35f, 1, 0),
            new Spot(-30f, 62f, 2, 0),
            new Spot(-30f, 84f, 2, 0),
            new Spot(30f, 62f, 2, 0),
            new Spot(30f, 84f, 2, 0),
            new Spot(0f, 72f, 2, 0),
            new Spot(-30f, 108f, 2, 0),
            new Spot(30f, 108f, 2, 0),
        };

        // route = índice de la ruta de patrulla en Routes
        public static readonly Spot[] Enemies =
        {
            new Spot(-8f, -18f, 1, 0),
            new Spot(-16f, 34f, 1, 1),
            new Spot(-30f, 44f, 2, 2),
            new Spot(-8f, 60f, 2, 3),
            new Spot(-24f, 96f, 2, 4),
        };

        public static readonly Vector2[][] Routes =
        {
            new Vector2[] { new Vector2(-8f, -18f), new Vector2(8f, -18f), new Vector2(10f, 14f), new Vector2(-8f, 14f) },
            new Vector2[] { new Vector2(-16f, 34f), new Vector2(16f, 34f), new Vector2(16f, 38f), new Vector2(-16f, 38f) },
            new Vector2[] { new Vector2(-30f, 44f), new Vector2(30f, 44f), new Vector2(30f, 52f), new Vector2(-30f, 52f) },
            new Vector2[] { new Vector2(-8f, 60f), new Vector2(8f, 60f), new Vector2(8f, 84f), new Vector2(-8f, 84f) },
            new Vector2[] { new Vector2(-24f, 96f), new Vector2(24f, 96f), new Vector2(24f, 112f), new Vector2(-24f, 112f) },
        };

        public static readonly Vector3[] Lamps =
        {
            new Vector3(0f, 3.5f, -28f),
            new Vector3(0f, 3.5f, 4f),
            new Vector3(0f, 3.5f, 36f),
            new Vector3(0f, 3.5f, 48f),
            new Vector3(0f, 3.5f, 72f),
            new Vector3(0f, 3.5f, 100f),
            new Vector3(0f, 3.5f, 116f),
        };
    }
}
