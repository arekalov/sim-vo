using SimVO.Audio;
using SimVO.Core;
using SimVO.Targets;
using TMPro;
using UnityEngine;

namespace SimVO.UI
{
    /// <summary>Wall display: score, record, accuracy, spread, timer, body position, cover and stage hints.</summary>
    public class Scoreboard : MonoBehaviour
    {
        public TMP_Text title;
        public TMP_Text scoreText;
        public TMP_Text statsText;
        public AudioSource audioSource;

        GameSession session;
        TargetSpawner spawner;

        void Start()
        {
            session = GameSession.Instance;
            spawner = FindFirstObjectByType<TargetSpawner>();
            if (spawner != null) spawner.HintChanged += Refresh;
            if (session == null) return;
            session.StatsChanged += Refresh;
            session.StateChanged += OnStateChanged;
            session.SettingsChanged += Refresh;
            session.PauseChanged += Refresh;
            session.CountdownTick += OnTick;
            session.NewRecord += OnRecord;
            Refresh();
        }

        void OnDestroy()
        {
            if (spawner != null) spawner.HintChanged -= Refresh;
            if (session == null) return;
            session.StatsChanged -= Refresh;
            session.StateChanged -= OnStateChanged;
            session.SettingsChanged -= Refresh;
            session.PauseChanged -= Refresh;
            session.CountdownTick -= OnTick;
            session.NewRecord -= OnRecord;
        }

        void OnStateChanged()
        {
            if (session.State == SessionState.Finished && !session.IsNewRecord) Sfx.Play(audioSource, ProceduralSfx.FinalBeep);
            Refresh();
        }

        void OnTick(int secondsLeft) => Sfx.Play(audioSource, ProceduralSfx.Beep);
        void OnRecord() => Sfx.Play(audioSource, ProceduralSfx.Record);

        void Refresh()
        {
            if (session == null) return;
            var s = session.settings;
            var hint = spawner != null ? spawner.StageHint : "";
            if (title != null)
            {
                title.text = session.State switch
                {
                    SessionState.Running when session.Paused => "ПАУЗА",
                    SessionState.Running when !string.IsNullOrEmpty(hint) => hint,
                    SessionState.Running => $"{Labels.Mode(s.mode)} · {Labels.Game(s.gameType)} · {Labels.Target(s.targetKind)}",
                    SessionState.Finished => session.IsNewRecord ? "НОВЫЙ РЕКОРД!" : "ФИНИШ",
                    _ => "ТРЕНИРОВКА · очки пойдут после кнопки СТАРТ",
                };
            }
            if (scoreText != null) scoreText.text = $"{session.Score}";
            if (statsText != null)
            {
                var time = s.gameType == GameType.Timed
                    ? $"{Mathf.FloorToInt(session.TimeRemaining / 60f):0}:{Mathf.CeilToInt(session.TimeRemaining % 60f):00}"
                    : $"{Mathf.FloorToInt(session.Elapsed / 60f):0}:{Mathf.FloorToInt(session.Elapsed % 60f):00}";
                var accuracy = Mathf.RoundToInt(session.Accuracy * 100f);
                var spreadCm = session.MeanSpread * 100f;
                var lastLine = s.mode switch
                {
                    ShootingMode.Cover => $"В укрытии <b>{Mathf.RoundToInt(session.CoverRatio * 100f)}%</b>    Пропущено <b>{session.MissedExposures}</b>",
                    ShootingMode.Advanced => $"Положение <b>{Labels.Position(session.CurrentPosition)}</b>    Нужно <b>{Labels.Position(session.RequiredPosition)}</b>",
                    _ when s.targetKind == TargetKind.Clay => $"Положение <b>{Labels.Position(session.CurrentPosition)}</b>    Упущено <b>{session.MissedExposures}</b>",
                    _ when s.targetKind == TargetKind.Ducks => $"Уток сбито <b>{session.Hits}</b>    Серия <b>{session.Combo}</b>",
                    _ => $"Положение <b>{Labels.Position(session.CurrentPosition)}</b>    Серия <b>{session.Combo}</b>",
                };
                statsText.text =
                    $"Рекорд <b>{session.BestScore}</b>    Время <b>{time}</b>\n" +
                    $"Выстрелов <b>{session.Shots}</b>    Попаданий <b>{session.Hits}</b>\n" +
                    $"Точность <b>{accuracy}%</b>    Разброс <b>{spreadCm:0.0} см</b>\n" +
                    lastLine;
            }
        }
    }
}
