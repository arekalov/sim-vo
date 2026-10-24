using SimVO.Core;
using UnityEngine;

namespace SimVO.UI
{
    /// <summary>The range console: mode, game type, target kind and start/stop.</summary>
    public class ControlPanel : MonoBehaviour
    {
        public PanelButton modeButton;
        public PanelButton gameButton;
        public PanelButton targetButton;
        public PanelButton startButton;

        GameSession session;

        void Start()
        {
            session = GameSession.Instance;
            if (session == null) return;
            session.StateChanged += Refresh;
            modeButton.onPress.AddListener(CycleMode);
            gameButton.onPress.AddListener(CycleGame);
            targetButton.onPress.AddListener(CycleTarget);
            startButton.onPress.AddListener(ToggleStart);
            Refresh();
        }

        void OnDestroy()
        {
            if (session != null) session.StateChanged -= Refresh;
        }

        void CycleMode()
        {
            if (session.State == SessionState.Running) return;
            session.settings.mode = session.settings.mode == ShootingMode.Standard ? ShootingMode.Advanced : ShootingMode.Standard;
            session.NotifySettingsChanged();
            Refresh();
        }

        void CycleGame()
        {
            if (session.State == SessionState.Running) return;
            session.settings.gameType = session.settings.gameType == GameType.Arcade ? GameType.Timed : GameType.Arcade;
            session.NotifySettingsChanged();
            Refresh();
        }

        void CycleTarget()
        {
            if (session.State == SessionState.Running) return;
            session.settings.targetKind = session.settings.targetKind switch
            {
                TargetKind.Ring => TargetKind.Silhouette,
                TargetKind.Silhouette => TargetKind.Clay,
                _ => TargetKind.Ring,
            };
            session.NotifySettingsChanged();
            Refresh();
        }

        void ToggleStart()
        {
            if (session.State == SessionState.Running) session.StopGame();
            else session.StartGame();
            Refresh();
        }

        void Refresh()
        {
            var running = session.State == SessionState.Running;
            modeButton.SetLabel($"Режим\n<b>{Labels.Mode(session.settings.mode)}</b>");
            gameButton.SetLabel($"Игра\n<b>{Labels.Game(session.settings.gameType)}</b>");
            targetButton.SetLabel($"Мишени\n<b>{Labels.Target(session.settings.targetKind)}</b>");
            startButton.SetLabel(running ? "<b>СТОП</b>" : "<b>СТАРТ</b>");
        }
    }
}
