using SimVO.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using static SimVO.EditorTools.SimVOAssetBuilder;

namespace SimVO.EditorTools
{
    /// <summary>Console with knobs, lamps and a display; menu board with vertical buttons.</summary>
    public static class SimVOUiBuilder
    {
        public static GameObject BuildMenuButton(string name, float width)
        {
            var root = new GameObject(name);
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(width + 0.01f, 0.085f, 0.03f);
            collider.center = new Vector3(0f, 0f, -0.025f);
            var cap = Primitive(PrimitiveType.Cube, "Cap", root.transform, new Vector3(0f, 0f, -0.027f), new Vector3(width, 0.075f, 0.02f), Button, default, false);
            var label = Text("Label", root.transform, new Vector3(0f, 0f, -0.039f), "Кнопка", 0.32f, new Vector2(width - 0.02f, 0.075f), default, new Color(0.1f, 0.1f, 0.1f));
            root.AddComponent<XRSimpleInteractable>();
            root.AddComponent<UnityEngine.XR.Interaction.Toolkit.Filtering.XRPokeFilter>();
            var button = root.AddComponent<PanelButton>();
            button.label = label;
            button.cap = cap.transform;
            button.pressDirection = Vector3.forward;
            button.pressDepth = 0.008f;
            button.audioSource = SimVOAssetBuilder.Audio(root, 0.5f);
            return root;
        }

        static PanelButton Place(GameObject prefab, Transform parent, string name, Vector3 position, string text)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.localPosition = position;
            var button = instance.GetComponent<PanelButton>();
            try { button.label.text = text; } catch (MissingReferenceException) { }
            return button;
        }

        public static GameObject BuildMenuPanel(GameObject wideButton, GameObject smallButton)
        {
            var root = new GameObject("MenuPanel");
            var board = Empty("Board", root.transform, Vector3.zero);
            Primitive(PrimitiveType.Cube, "Backing", board.transform, new Vector3(0f, 0f, 0.015f), new Vector3(1.15f, 0.78f, 0.03f), Panel);
            Primitive(PrimitiveType.Cube, "Bezel", board.transform, new Vector3(0f, 0f, 0.03f), new Vector3(1.21f, 0.84f, 0.03f), Rack, default, false);
            var menu = root.AddComponent<GameMenu>();
            menu.board = board;
            menu.title = Text("Title", board.transform, new Vector3(0f, 0.29f, -0.002f), "SimVO", 0.75f, new Vector2(1.1f, 0.14f), default, new Color(1f, 0.85f, 0.2f));
            menu.title.fontStyle = FontStyles.Bold;
            menu.subtitle = Text("Subtitle", board.transform, new Vector3(0f, 0.17f, -0.002f), "", 0.24f, new Vector2(1.05f, 0.12f));

            var start = Empty("StartGroup", board.transform, Vector3.zero);
            menu.playButton = Place(wideButton, start.transform, "Play", new Vector3(0f, 0.03f, 0f), "ИГРАТЬ");
            menu.startSettingsButton = Place(wideButton, start.transform, "Settings", new Vector3(0f, -0.09f, 0f), "НАСТРОЙКИ");
            menu.startQuitButton = Place(wideButton, start.transform, "Quit", new Vector3(0f, -0.21f, 0f), "ВЫХОД");
            menu.startGroup = start;

            var pause = Empty("PauseGroup", board.transform, Vector3.zero);
            menu.resumeButton = Place(wideButton, pause.transform, "Resume", new Vector3(0f, 0.06f, 0f), "ПРОДОЛЖИТЬ");
            menu.pauseSettingsButton = Place(wideButton, pause.transform, "Settings", new Vector3(0f, -0.04f, 0f), "НАСТРОЙКИ");
            menu.mainMenuButton = Place(wideButton, pause.transform, "MainMenu", new Vector3(0f, -0.14f, 0f), "ГЛАВНОЕ МЕНЮ");
            menu.pauseQuitButton = Place(wideButton, pause.transform, "Quit", new Vector3(0f, -0.24f, 0f), "ВЫХОД");
            menu.pauseGroup = pause;

            var settings = Empty("SettingsGroup", board.transform, Vector3.zero);
            menu.musicValue = Text("MusicValue", settings.transform, new Vector3(0f, 0.06f, -0.002f), "Музыка 50%", 0.34f, new Vector2(0.6f, 0.08f));
            menu.musicDownButton = Place(smallButton, settings.transform, "MusicDown", new Vector3(-0.42f, 0.06f, 0f), "−");
            menu.musicUpButton = Place(smallButton, settings.transform, "MusicUp", new Vector3(0.42f, 0.06f, 0f), "+");
            menu.effectsValue = Text("EffectsValue", settings.transform, new Vector3(0f, -0.06f, -0.002f), "Эффекты 80%", 0.34f, new Vector2(0.6f, 0.08f));
            menu.effectsDownButton = Place(smallButton, settings.transform, "EffectsDown", new Vector3(-0.42f, -0.06f, 0f), "−");
            menu.effectsUpButton = Place(smallButton, settings.transform, "EffectsUp", new Vector3(0.42f, -0.06f, 0f), "+");
            menu.laserButton = Place(wideButton, settings.transform, "Assist", new Vector3(0f, -0.17f, 0f), "УПРОЩЁННЫЙ РЕЖИМ: ВКЛ");
            menu.backButton = Place(wideButton, settings.transform, "Back", new Vector3(0f, -0.28f, 0f), "НАЗАД");
            menu.settingsGroup = settings;
            return root;
        }

        public static GameObject BuildControlPanel()
        {
            var root = new GameObject("ControlPanel");
            Primitive(PrimitiveType.Cube, "Body", root.transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.72f, 0.04f, 0.42f), MetalPlate);
            Primitive(PrimitiveType.Cube, "Face", root.transform, new Vector3(0f, 0.045f, 0f), new Vector3(0.68f, 0.012f, 0.38f), ConsoleFace);
            Primitive(PrimitiveType.Cube, "Skirt", root.transform, new Vector3(0f, 0.002f, 0.21f), new Vector3(0.72f, 0.04f, 0.02f), Rack, default, false);
            Primitive(PrimitiveType.Cylinder, "Cable", root.transform, new Vector3(0.3f, -0.08f, 0.2f), new Vector3(0.012f, 0.1f, 0.012f), GunDark, default, false);
            Text("Header", root.transform, new Vector3(0f, 0.052f, 0.165f), "ПУЛЬТ УПРАВЛЕНИЯ · ДОРОЖКА 3", 0.19f, new Vector2(0.64f, 0.04f), new Vector3(90f, 0f, 0f), new Color(0.9f, 0.9f, 0.85f));

            Primitive(PrimitiveType.Cube, "DisplayFrame", root.transform, new Vector3(-0.15f, 0.053f, 0.095f), new Vector3(0.34f, 0.006f, 0.125f), Rack, default, false);
            Primitive(PrimitiveType.Cube, "Display", root.transform, new Vector3(-0.15f, 0.057f, 0.095f), new Vector3(0.32f, 0.004f, 0.11f), DisplayGlass, default, false);
            var panel = root.AddComponent<ControlPanel>();
            panel.display = Text("DisplayText", root.transform, new Vector3(-0.15f, 0.0605f, 0.095f), "", 0.11f, new Vector2(0.3f, 0.1f), new Vector3(90f, 0f, 0f), new Color(0.55f, 1f, 0.6f), TextAlignmentOptions.TopLeft);
            panel.display.fontStyle = FontStyles.Bold;

            panel.modeButton = Knob(root, "ModeKnob", -0.24f, "РЕЖИМ", out panel.modeKnob);
            panel.gameButton = Knob(root, "GameKnob", -0.08f, "ИГРА", out panel.gameKnob);
            panel.targetButton = Knob(root, "TargetKnob", 0.08f, "МИШЕНИ", out panel.targetKnob);
            panel.motionButton = Knob(root, "MotionKnob", 0.24f, "ДВИЖЕНИЕ", out panel.motionKnob);

            var startRoot = Empty("StartButton", root.transform, new Vector3(0.2f, 0.051f, 0.095f));
            var guard = Primitive(PrimitiveType.Cylinder, "Guard", startRoot.transform, new Vector3(0f, 0.004f, 0f), new Vector3(0.13f, 0.006f, 0.13f), Rack, default, false);
            var button = Primitive(PrimitiveType.Cylinder, "Button", startRoot.transform, new Vector3(0f, 0.018f, 0f), new Vector3(0.09f, 0.014f, 0.09f), StartRed, default, false);
            var startCollider = startRoot.AddComponent<BoxCollider>();
            startCollider.size = new Vector3(0.12f, 0.05f, 0.12f);
            startCollider.center = new Vector3(0f, 0.02f, 0f);
            startRoot.AddComponent<XRSimpleInteractable>();
            startRoot.AddComponent<UnityEngine.XR.Interaction.Toolkit.Filtering.XRPokeFilter>();
            var startButton = startRoot.AddComponent<PanelButton>();
            startButton.cap = button.transform;
            startButton.pressDepth = 0.008f;
            startButton.audioSource = SimVOAssetBuilder.Audio(startRoot);
            panel.startButton = startButton;
            panel.startLabel = Text("StartLabel", startRoot.transform, new Vector3(0f, 0.034f, 0f), "СТАРТ", 0.2f, new Vector2(0.1f, 0.04f), new Vector3(90f, 0f, 0f), Color.white);
            panel.startLabel.fontStyle = FontStyles.Bold;

            panel.readyLamp = Lamp(root, "ReadyLamp", new Vector3(0.1f, 0.055f, 0.135f), "ГОТОВ");
            panel.fireLamp = Lamp(root, "FireLamp", new Vector3(0.1f, 0.055f, 0.06f), "ОГОНЬ");
            panel.lampOff = LampOff;
            panel.lampGreen = LampGreen;
            panel.lampRed = LampRed;
            return root;
        }

        static PanelButton Knob(GameObject root, string name, float x, string caption, out Transform knobTransform)
        {
            var knobRoot = Empty(name, root.transform, new Vector3(x, 0.051f, -0.07f));
            Primitive(PrimitiveType.Cylinder, "Ring", knobRoot.transform, new Vector3(0f, 0.002f, 0f), new Vector3(0.085f, 0.003f, 0.085f), Rack, default, false);
            var knob = Primitive(PrimitiveType.Cylinder, "Knob", knobRoot.transform, new Vector3(0f, 0.014f, 0f), new Vector3(0.05f, 0.012f, 0.05f), GunDark, default, false);
            Primitive(PrimitiveType.Cube, "Pointer", knob.transform, new Vector3(0f, 1.05f, 0.35f), new Vector3(0.08f, 0.2f, 0.3f), Button, default, false);
            for (var i = 0; i < 5; i++)
            {
                var angle = -60f + i * 45f;
                var dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Primitive(PrimitiveType.Cube, "Tick", knobRoot.transform, dir * 0.047f + new Vector3(0f, 0.004f, 0f), new Vector3(0.004f, 0.002f, 0.012f), Button, new Vector3(0f, angle, 0f), false);
            }
            var collider = knobRoot.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.08f, 0.04f, 0.08f);
            collider.center = new Vector3(0f, 0.015f, 0f);
            knobRoot.AddComponent<XRSimpleInteractable>();
            knobRoot.AddComponent<UnityEngine.XR.Interaction.Toolkit.Filtering.XRPokeFilter>();
            var button = knobRoot.AddComponent<PanelButton>();
            button.cap = knob.transform;
            button.pressDepth = 0.004f;
            button.audioSource = SimVOAssetBuilder.Audio(knobRoot);
            Text("Caption", knobRoot.transform, new Vector3(0f, 0.005f, 0.065f), caption, 0.16f, new Vector2(0.15f, 0.03f), new Vector3(90f, 0f, 0f), new Color(0.9f, 0.9f, 0.85f));
            button.label = Text("Value", knobRoot.transform, new Vector3(0f, 0.005f, -0.068f), "", 0.15f, new Vector2(0.15f, 0.03f), new Vector3(90f, 0f, 0f), new Color(1f, 0.85f, 0.3f));
            knobTransform = knob.transform;
            return button;
        }

        static Renderer Lamp(GameObject root, string name, Vector3 position, string caption)
        {
            var lamp = Primitive(PrimitiveType.Sphere, name, root.transform, position, new Vector3(0.028f, 0.028f, 0.028f), LampOff, default, false);
            Primitive(PrimitiveType.Cylinder, "Bezel", root.transform, position + new Vector3(0f, -0.004f, 0f), new Vector3(0.04f, 0.003f, 0.04f), Rack, default, false);
            Text(name + "Caption", root.transform, position + new Vector3(0f, -0.003f, -0.032f), caption, 0.12f, new Vector2(0.1f, 0.025f), new Vector3(90f, 0f, 0f), new Color(0.9f, 0.9f, 0.85f));
            return lamp.GetComponent<Renderer>();
        }
    }
}
