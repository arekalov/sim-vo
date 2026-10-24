using SimVO.Audio;
using SimVO.Core;
using TMPro;
using UnityEngine;

namespace SimVO.UI
{
    /// <summary>World-space menu board: start screen, pause and settings. Opening it pauses the session.</summary>
    public class GameMenu : MonoBehaviour
    {
        public enum MenuState { Hidden, Start, Pause, Settings }

        public GameObject board;
        public GameObject startGroup;
        public GameObject pauseGroup;
        public GameObject settingsGroup;
        public TMP_Text title;
        public TMP_Text subtitle;
        public PanelButton playButton;
        public PanelButton startSettingsButton;
        public PanelButton startQuitButton;
        public PanelButton resumeButton;
        public PanelButton pauseSettingsButton;
        public PanelButton mainMenuButton;
        public PanelButton pauseQuitButton;
        public PanelButton musicDownButton;
        public PanelButton musicUpButton;
        public PanelButton effectsDownButton;
        public PanelButton effectsUpButton;
        public PanelButton backButton;
        public PanelButton laserButton;
        public TMP_Text musicValue;
        public TMP_Text effectsValue;

        public MenuState State { get; private set; } = MenuState.Hidden;
        public bool IsOpen => State != MenuState.Hidden;

        MenuState returnTo = MenuState.Start;
        GameSession session;

        void Start()
        {
            session = GameSession.Instance;
            Wire(playButton, Hide);
            Wire(startSettingsButton, () => OpenSettings(MenuState.Start));
            Wire(startQuitButton, Quit);
            Wire(resumeButton, Hide);
            Wire(pauseSettingsButton, () => OpenSettings(MenuState.Pause));
            Wire(mainMenuButton, MainMenu);
            Wire(pauseQuitButton, Quit);
            Wire(musicDownButton, () => { Sfx.Music -= 0.1f; RefreshSettings(); });
            Wire(musicUpButton, () => { Sfx.Music += 0.1f; RefreshSettings(); });
            Wire(effectsDownButton, () => { Sfx.Effects -= 0.1f; RefreshSettings(); });
            Wire(effectsUpButton, () => { Sfx.Effects += 0.1f; RefreshSettings(); });
            Wire(backButton, () => Show(returnTo));
            Wire(laserButton, () => { Sfx.Assist = !Sfx.Assist; RefreshSettings(); });
            Show(MenuState.Start);
        }

        static void Wire(PanelButton button, UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onPress.AddListener(action);
        }

        public void Toggle()
        {
            if (State == MenuState.Hidden) Show(MenuState.Pause);
            else if (State == MenuState.Settings) Show(returnTo);
            else Hide();
        }

        public void Show(MenuState state)
        {
            State = state;
            if (board != null) board.SetActive(true);
            if (startGroup != null) startGroup.SetActive(state == MenuState.Start);
            if (pauseGroup != null) pauseGroup.SetActive(state == MenuState.Pause);
            if (settingsGroup != null) settingsGroup.SetActive(state == MenuState.Settings);
            if (title != null)
            {
                title.text = state switch
                {
                    MenuState.Start => "SimVO",
                    MenuState.Pause => "ПАУЗА",
                    _ => "НАСТРОЙКИ",
                };
            }
            if (subtitle != null)
            {
                subtitle.text = state switch
                {
                    MenuState.Start => "Симулятор виртуального огня\nВозьмите оружие со стены, выберите режим на пульте и нажмите СТАРТ",
                    MenuState.Pause => session != null && session.State == SessionState.Running ? "Игра остановлена" : "Тир на паузе",
                    _ => "",
                };
            }
            RefreshSettings();
            session?.SetPaused(true);
        }

        public void Hide()
        {
            State = MenuState.Hidden;
            if (board != null) board.SetActive(false);
            session?.SetPaused(false);
        }

        void OpenSettings(MenuState from)
        {
            returnTo = from;
            Show(MenuState.Settings);
        }

        void MainMenu()
        {
            session?.ResetToIdle();
            Show(MenuState.Start);
        }

        void RefreshSettings()
        {
            if (musicValue != null) musicValue.text = $"Музыка  {Mathf.RoundToInt(Sfx.Music * 100f)}%";
            if (effectsValue != null) effectsValue.text = $"Эффекты  {Mathf.RoundToInt(Sfx.Effects * 100f)}%";
            laserButton?.SetLabel(Sfx.Assist ? "УПРОЩЁННЫЙ РЕЖИМ: ВКЛ" : "УПРОЩЁННЫЙ РЕЖИМ: ВЫКЛ");
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
