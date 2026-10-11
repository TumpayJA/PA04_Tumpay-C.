using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EscapeGame.EditorTools
{
    public static partial class SetupWizard
    {
        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        static readonly Vector2 ButtonSize = new Vector2(560f, 90f);
        static readonly Color ButtonColor = new Color(0.16f, 0.19f, 0.26f, 0.95f);

        // ============================ HELPERS DE UI ============================

        static GameObject CreateCanvasObject(string objectName, int sortingOrder)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform));

            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        static RectTransform NewRect(string objectName, Transform parent)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        static Image AddImage(RectTransform rt, Color color, bool raycastTarget)
        {
            Image image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        static TextMeshProUGUI AddText(Transform parent, string objectName, string text, float fontSize,
                                       TextAlignmentOptions alignment, Color color)
        {
            RectTransform rt = NewRect(objectName, parent);
            TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        static Button AddButton(Transform parent, string label, Vector2 position)
        {
            RectTransform rt = NewRect(label + "Button", parent);
            Anchor(rt, Center, Center, position, ButtonSize);

            Image image = AddImage(rt, ButtonColor, true);
            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            TextMeshProUGUI text = AddText(rt, "Label", label, 40, TextAlignmentOptions.Center, Color.white);
            Stretch(text.rectTransform);
            return button;
        }

        static Slider AddBar(Transform parent, string objectName, Vector2 anchor, Vector2 pivot, Vector2 position,
                             Vector2 size, Color back, Color fill)
        {
            RectTransform root = NewRect(objectName, parent);
            Anchor(root, anchor, pivot, position, size);
            Slider slider = root.gameObject.AddComponent<Slider>();

            RectTransform background = NewRect("Background", root);
            Stretch(background);
            AddImage(background, back, false);

            RectTransform fillArea = NewRect("Fill Area", root);
            Stretch(fillArea);
            RectTransform fillRect = NewRect("Fill", fillArea);
            Stretch(fillRect);
            AddImage(fillRect, fill, false);

            slider.fillRect = fillRect;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            return slider;
        }

        /// <summary>Panel a pantalla completa con fondo oscuro y un título grande.</summary>
        static RectTransform AddPanel(Transform parent, string panelName, Color background, string title, Color titleColor)
        {
            RectTransform panel = NewRect(panelName, parent);
            Stretch(panel);
            AddImage(panel, background, true); // bloquea los clics hacia lo que haya detrás

            TextMeshProUGUI titleText = AddText(panel, "Title", title, 100, TextAlignmentOptions.Center, titleColor);
            titleText.fontStyle = FontStyles.Bold;
            Anchor(titleText.rectTransform, Center, Center, new Vector2(0f, 270f), new Vector2(1500f, 150f));
            return panel;
        }

        // ============================ CANVAS DEL NIVEL (HUD + PAUSA + VICTORIA + DERROTA) ============================

        static GameObject CreateUiCanvas()
        {
            GameObject canvasGo = CreateCanvasObject("UI_Canvas", 10);
            UIManager ui = canvasGo.AddComponent<UIManager>();
            Transform canvasTf = canvasGo.transform;

            // ---------- HUD ----------
            RectTransform hud = NewRect("HUD", canvasTf);
            Stretch(hud);

            Image vignette = AddImage(NewRect("ChaseVignette", hud), new Color(0.8f, 0f, 0f, 0f), false);
            Stretch(vignette.rectTransform);

            RectTransform crosshair = NewRect("Crosshair", hud);
            Anchor(crosshair, Center, Center, Vector2.zero, new Vector2(6f, 6f));
            AddImage(crosshair, new Color(1f, 1f, 1f, 0.55f), false);

            TextMeshProUGUI orbs = AddText(hud, "OrbsText", "Orbes: 0/0", 46, TextAlignmentOptions.TopLeft, Color.white);
            Anchor(orbs.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(600f, 70f));

            TextMeshProUGUI timer = AddText(hud, "TimerText", "05:00", 46, TextAlignmentOptions.TopRight, Color.white);
            Anchor(timer.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(400f, 70f));

            TextMeshProUGUI objective = AddText(hud, "ObjectiveText", "Encuentra todos los orbes", 34, TextAlignmentOptions.Top, new Color(0.8f, 0.9f, 1f));
            Anchor(objective.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(900f, 60f));

            TextMeshProUGUI chaseWarning = AddText(hud, "ChaseWarningText", "¡TE ESTÁN PERSIGUIENDO!", 42, TextAlignmentOptions.Top, new Color(1f, 0.25f, 0.25f));
            chaseWarning.fontStyle = FontStyles.Bold;
            Anchor(chaseWarning.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -95f), new Vector2(900f, 60f));
            chaseWarning.gameObject.SetActive(false);

            TextMeshProUGUI prompt = AddText(hud, "PromptText", "", 38, TextAlignmentOptions.Center, Color.white);
            Anchor(prompt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 230f), new Vector2(1300f, 60f));
            prompt.gameObject.SetActive(false);

            TextMeshProUGUI message = AddText(hud, "MessageText", "", 42, TextAlignmentOptions.Center, new Color(1f, 0.92f, 0.5f));
            Anchor(message.rectTransform, Center, Center, new Vector2(0f, 260f), new Vector2(1500f, 90f));
            message.gameObject.SetActive(false);

            Slider stamina = AddBar(hud, "StaminaBar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f),
                                    new Vector2(520f, 16f), new Color(0f, 0f, 0f, 0.5f), new Color(0.9f, 0.9f, 0.9f, 0.85f));

            // ---------- PAUSA ----------
            RectTransform pausePanel = AddPanel(canvasTf, "PausePanel", new Color(0f, 0f, 0f, 0.72f), "PAUSA", Color.white);
            Button resume = AddButton(pausePanel, "Reanudar", new Vector2(0f, 70f));
            Button pauseRestart = AddButton(pausePanel, "Reiniciar", new Vector2(0f, -40f));
            Button pauseMenu = AddButton(pausePanel, "Menú principal", new Vector2(0f, -150f));
            pausePanel.gameObject.SetActive(false);

            // ---------- VICTORIA ----------
            RectTransform winPanel = AddPanel(canvasTf, "WinPanel", new Color(0.02f, 0.12f, 0.05f, 0.9f), "¡ESCAPASTE!", new Color(0.4f, 1f, 0.55f));
            TextMeshProUGUI winDetails = AddText(winPanel, "DetailsText", "", 46, TextAlignmentOptions.Center, Color.white);
            Anchor(winDetails.rectTransform, Center, Center, new Vector2(0f, 90f), new Vector2(1200f, 170f));
            Button winRestart = AddButton(winPanel, "Jugar de nuevo", new Vector2(0f, -90f));
            Button winMenu = AddButton(winPanel, "Menú principal", new Vector2(0f, -200f));
            winPanel.gameObject.SetActive(false);

            // ---------- DERROTA ----------
            RectTransform losePanel = AddPanel(canvasTf, "LosePanel", new Color(0.16f, 0.02f, 0.02f, 0.92f), "GAME OVER", new Color(1f, 0.3f, 0.3f));
            TextMeshProUGUI loseReason = AddText(losePanel, "ReasonText", "", 46, TextAlignmentOptions.Center, Color.white);
            Anchor(loseReason.rectTransform, Center, Center, new Vector2(0f, 90f), new Vector2(1200f, 170f));
            Button loseRestart = AddButton(losePanel, "Reintentar", new Vector2(0f, -90f));
            Button loseMenu = AddButton(losePanel, "Menú principal", new Vector2(0f, -200f));
            losePanel.gameObject.SetActive(false);

            // ---------- Conectar todo al UIManager ----------
            SetRef(ui, "hudRoot", hud.gameObject);
            SetRef(ui, "orbsText", orbs);
            SetRef(ui, "timerText", timer);
            SetRef(ui, "objectiveText", objective);
            SetRef(ui, "promptText", prompt);
            SetRef(ui, "messageText", message);
            SetRef(ui, "chaseWarningText", chaseWarning);
            SetRef(ui, "staminaBar", stamina);
            SetRef(ui, "chaseVignette", vignette);
            SetRef(ui, "pausePanel", pausePanel.gameObject);
            SetRef(ui, "winPanel", winPanel.gameObject);
            SetRef(ui, "losePanel", losePanel.gameObject);
            SetRef(ui, "winDetailsText", winDetails);
            SetRef(ui, "loseReasonText", loseReason);
            SetRef(ui, "resumeButton", resume);
            SetRefs(ui, "restartButtons", new Object[] { pauseRestart, winRestart, loseRestart });
            SetRefs(ui, "menuButtons", new Object[] { pauseMenu, winMenu, loseMenu });

            return SavePrefab(canvasGo, "UI_Canvas");
        }

        // ============================ ESCENA MAIN MENU ============================

        static void BuildMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.025f, 0.04f);
            camGo.AddComponent<AudioListener>();

            GameObject canvasGo = CreateCanvasObject("MenuCanvas", 0);
            MainMenuUI menu = canvasGo.AddComponent<MainMenuUI>(); // añade un AudioSource solo (RequireComponent)

            RectTransform background = NewRect("Background", canvasGo.transform);
            Stretch(background);
            AddImage(background, new Color(0.02f, 0.025f, 0.04f, 1f), false);

            // ---------- Panel principal ----------
            RectTransform mainPanel = NewRect("MainPanel", canvasGo.transform);
            Stretch(mainPanel);

            TextMeshProUGUI title = AddText(mainPanel, "Title", "ESCAPE", 170, TextAlignmentOptions.Center, new Color(0.75f, 0.95f, 1f));
            title.fontStyle = FontStyles.Bold;
            Anchor(title.rectTransform, Center, Center, new Vector2(0f, 290f), new Vector2(1400f, 230f));

            TextMeshProUGUI subtitle = AddText(mainPanel, "Subtitle", "Encuentra los orbes y escapa antes de que te atrapen.", 40,
                                               TextAlignmentOptions.Center, new Color(0.7f, 0.75f, 0.82f));
            Anchor(subtitle.rectTransform, Center, Center, new Vector2(0f, 150f), new Vector2(1500f, 70f));

            Button play = AddButton(mainPanel, "Jugar", new Vector2(0f, 10f));
            Button controls = AddButton(mainPanel, "Controles", new Vector2(0f, -100f));
            Button quit = AddButton(mainPanel, "Salir", new Vector2(0f, -210f));

            // ---------- Panel de controles ----------
            RectTransform controlsPanel = NewRect("ControlsPanel", canvasGo.transform);
            Stretch(controlsPanel);

            TextMeshProUGUI controlsTitle = AddText(controlsPanel, "Title", "CONTROLES", 100, TextAlignmentOptions.Center, new Color(0.75f, 0.95f, 1f));
            controlsTitle.fontStyle = FontStyles.Bold;
            Anchor(controlsTitle.rectTransform, Center, Center, new Vector2(0f, 340f), new Vector2(1400f, 140f));

            TextMeshProUGUI controlsText = AddText(controlsPanel, "Text",
                "W A S D   -   Moverse\n" +
                "Ratón   -   Mirar\n" +
                "Shift   -   Correr (gasta stamina)\n" +
                "Espacio   -   Saltar\n" +
                "F   -   Linterna\n" +
                "E   -   Abrir la salida\n" +
                "Esc / P   -   Pausa\n" +
                "F3   -   Ver rendimiento (FPS)",
                44, TextAlignmentOptions.Center, Color.white);
            Anchor(controlsText.rectTransform, Center, Center, new Vector2(0f, 60f), new Vector2(1400f, 560f));

            Button back = AddButton(controlsPanel, "Volver", new Vector2(0f, -330f));
            controlsPanel.gameObject.SetActive(false);

            // ---------- Conectar al MainMenuUI ----------
            SetRef(menu, "mainPanel", mainPanel.gameObject);
            SetRef(menu, "controlsPanel", controlsPanel.gameObject);
            SetRef(menu, "playButton", play);
            SetRef(menu, "controlsButton", controls);
            SetRef(menu, "backButton", back);
            SetRef(menu, "quitButton", quit);

            EditorSceneManager.SaveScene(scene, MenuScenePath);
        }
    }
}
