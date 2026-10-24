using SimVO.Audio;
using SimVO.Core;
using TMPro;
using UnityEngine;

namespace SimVO.UI
{
    /// <summary>Wall display: score, record, accuracy, spread, timer, body position.</summary>
    public class Scoreboard : MonoBehaviour
    {
        public TMP_Text title;
        public TMP_Text scoreText;
        public TMP_Text statsText;
        public AudioSource audioSource;

        GameSession session;

        void Start()
        {
            session = GameSession.Instance;
            if (session == null) return;
            session.StatsChanged += Refresh;
            session.StateChanged += OnStateChanged;
            session.SettingsChanged += Refresh;
            session.CountdownTick += OnTick;
            session.NewRecord += OnRecord;
            Refresh();
        }

        void OnDestroy()
        {
            if (session == null) return;
            session.StatsChanged -= Refresh;
            session.StateChanged -= OnStateChanged;
            session.SettingsChanged -= Refresh;
            session.CountdownTick -= OnTick;
            session.NewRecord -= OnRecord;
        }

        void OnStateChanged()
        {
            if (session.State == SessionState.Finished && audioSource != null && !session.IsNewRecord)
            {
                audioSource.PlayOneShot(ProceduralSfx.FinalBeep);
            }
            Refresh();
        }

        void OnTick(int secondsLeft)
        {
            if (audioSource != null) audioSource.PlayOneShot(ProceduralSfx.Beep);
        }

        void OnRecord()
        {
            if (audioSource != null) audioSource.PlayOneShot(ProceduralSfx.Record);
        }

        void Refresh()
        {
            if (session == null) return;
            var s = session.settings;
            if (title != null)
            {
                title.text = session.State switch
                {
                    SessionState.Running => $"{Labels.Mode(s.mode)} · {Labels.Game(s.gameType)}",
                    SessionState.Finished => session.IsNewRecord ? "НОВЫЙ РЕКОРД!" : "ФИНИШ",
                    _ => "SimVO · выберите режим и нажмите СТАРТ",
                };
            }
            if (scoreText != null)
            {
                scoreText.text = $"{session.Score}";
            }
            if (statsText != null)
            {
                var time = s.gameType == GameType.Timed
                    ? $"{Mathf.FloorToInt(session.TimeRemaining / 60f):0}:{Mathf.CeilToInt(session.TimeRemaining % 60f):00}"
                    : $"{Mathf.FloorToInt(session.Elapsed / 60f):0}:{Mathf.FloorToInt(session.Elapsed % 60f):00}";
                var accuracy = Mathf.RoundToInt(session.Accuracy * 100f);
                var spreadCm = session.MeanSpread * 100f;
                statsText.text =
                    $"Рекорд <b>{session.BestScore}</b>    Серия <b>{session.Combo}</b>\n" +
                    $"Выстрелов <b>{session.Shots}</b>    Попаданий <b>{session.Hits}</b>\n" +
                    $"Точность <b>{accuracy}%</b>    Разброс <b>{spreadCm:0.0} см</b>\n" +
                    $"Время <b>{time}</b>    Положение <b>{Labels.Position(session.CurrentPosition)}</b>";
            }
        }
    }
}
