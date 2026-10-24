using SimVO.Core;
using TMPro;
using UnityEngine;

namespace SimVO.UI
{
    /// <summary>The range console: four selector knobs, a status display, two lamps and the big start button.</summary>
    public class ControlPanel : MonoBehaviour
    {
        public PanelButton modeButton;
        public PanelButton gameButton;
        public PanelButton targetButton;
        public PanelButton motionButton;
        public PanelButton startButton;
        public Transform modeKnob;
        public Transform gameKnob;
        public Transform targetKnob;
        public Transform motionKnob;
        public float knobStepDegrees = 45f;
        public TMP_Text display;
        public TMP_Text startLabel;
        public Renderer readyLamp;
        public Renderer fireLamp;
        public Material lampOff;
        public Material lampGreen;
        public Material lampRed;

        GameSession session;
        float lastDisplayRefresh;

        void Start()
        {
            session = GameSession.Instance;
            if (session == null) return;
            session.StateChanged += Refresh;
            session.StatsChanged += OnStats;
            session.PauseChanged += Refresh;
            Wire(modeButton, CycleMode);
            Wire(gameButton, CycleGame);
            Wire(targetButton, CycleTarget);
            Wire(motionButton, CycleMotion);
            Wire(startButton, ToggleStart);
            Refresh();
        }

        void OnDestroy()
        {
            if (session == null) return;
            session.StateChanged -= Refresh;
            session.StatsChanged -= OnStats;
            session.PauseChanged -= Refresh;
        }

        static void Wire(PanelButton button, UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onPress.AddListener(action);
        }

        bool Locked => session.State == SessionState.Running || session.Paused;

        void CycleMode()
        {
            if (Locked) return;
            session.settings.mode = (ShootingMode)(((int)session.settings.mode + 1) % 3);
            session.NotifySettingsChanged();
            Refresh();
        }

        void CycleGame()
        {
            if (Locked) return;
            session.settings.gameType = session.settings.gameType == GameType.Arcade ? GameType.Timed : GameType.Arcade;
            session.NotifySettingsChanged();
            Refresh();
        }

        void CycleTarget()
        {
            if (Locked) return;
            session.settings.targetKind = (TargetKind)(((int)session.settings.targetKind + 1) % 6);
            session.NotifySettingsChanged();
            Refresh();
        }

        void CycleMotion()
        {
            if (Locked) return;
            session.settings.motion = (TargetMotionMode)(((int)session.settings.motion + 1) % 3);
            session.NotifySettingsChanged();
            Refresh();
        }

        void ToggleStart()
        {
            if (session.Paused) return;
            if (session.State == SessionState.Running) session.StopGame();
            else session.StartGame();
            Refresh();
        }

        void OnStats()
        {
            if (session.State != SessionState.Running || Time.unscaledTime - lastDisplayRefresh < 0.2f) return;
            RefreshDisplay();
        }

        void Refresh()
        {
            var s = session.settings;
            var running = session.State == SessionState.Running;
            modeButton?.SetLabel(Labels.Mode(s.mode));
            gameButton?.SetLabel(Labels.Game(s.gameType));
            targetButton?.SetLabel(Labels.Target(s.targetKind));
            motionButton?.SetLabel(Labels.Motion(s.motion));
            if (startLabel != null) startLabel.text = running ? "СТОП" : "СТАРТ";
            Turn(modeKnob, (int)s.mode);
            Turn(gameKnob, (int)s.gameType);
            Turn(targetKnob, (int)s.targetKind);
            Turn(motionKnob, (int)s.motion);
            if (readyLamp != null) readyLamp.sharedMaterial = running ? lampOff : lampGreen;
            if (fireLamp != null) fireLamp.sharedMaterial = running ? lampRed : lampOff;
            RefreshDisplay();
        }

        void Turn(Transform knob, int index)
        {
            if (knob != null) knob.localRotation = Quaternion.Euler(0f, -60f + index * knobStepDegrees, 0f);
        }

        void RefreshDisplay()
        {
            if (display == null) return;
            lastDisplayRefresh = Time.unscaledTime;
            var s = session.settings;
            var status = session.State switch
            {
                SessionState.Running when session.Paused => "ПАУЗА",
                SessionState.Running when s.gameType == GameType.Timed => $"ОГОНЬ  {Mathf.FloorToInt(session.TimeRemaining / 60f):0}:{Mathf.CeilToInt(session.TimeRemaining % 60f):00}",
                SessionState.Running => $"ОГОНЬ  {Mathf.FloorToInt(session.Elapsed / 60f):0}:{Mathf.FloorToInt(session.Elapsed % 60f):00}",
                SessionState.Finished => $"ФИНИШ  {session.Score} ОЧК.",
                _ => "ГОТОВ К СТРЕЛЬБЕ",
            };
            display.text =
                $"РЕЖИМ     {Labels.Mode(s.mode).ToUpperInvariant()}\n" +
                $"ИГРА      {Labels.Game(s.gameType).ToUpperInvariant()}\n" +
                $"МИШЕНИ    {Labels.Target(s.targetKind).ToUpperInvariant()}\n" +
                $"ДВИЖЕНИЕ  {Labels.Motion(s.motion).ToUpperInvariant()}\n" +
                $"<color=#7CFF7C>{status}</color>";
        }
    }
}
