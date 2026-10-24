using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimVO.Core
{
    /// <summary>Scene-level game state: mode settings, score, timer and training metrics.</summary>
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        public GameSettings settings = new GameSettings();

        public SessionState State { get; private set; } = SessionState.Idle;
        public int Score { get; private set; }
        public int Shots { get; private set; }
        public int Hits { get; private set; }
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }
        public float TimeRemaining { get; private set; }
        public float Elapsed { get; private set; }
        public int BestScore { get; private set; }
        public bool IsNewRecord { get; private set; }
        public float LastShotInterval { get; private set; }
        public BodyPosition CurrentPosition { get; set; } = BodyPosition.Standing;

        public float Accuracy => Shots == 0 ? 0f : (float)Hits / Shots;
        /// <summary>Mean distance of hits from the target centre, in metres.</summary>
        public float MeanSpread => hitOffsets.Count == 0 ? 0f : spreadSum / hitOffsets.Count;
        public IReadOnlyList<Vector2> HitOffsets => hitOffsets;

        public event Action StateChanged;
        public event Action StatsChanged;
        public event Action SettingsChanged;
        /// <summary>Seconds left, raised once per second during the last ten seconds of a timed game.</summary>
        public event Action<int> CountdownTick;
        public event Action<int, Vector3> HitScored;
        public event Action NewRecord;

        readonly List<Vector2> hitOffsets = new List<Vector2>();
        float spreadSum;
        int lastTickSecond = -1;
        float lastShotTime = -1f;

        void Awake()
        {
            Instance = this;
            BestScore = PlayerPrefs.GetInt(BestKey, 0);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        string BestKey => $"simvo.best.{settings.gameType}.{settings.mode}";

        public void NotifySettingsChanged()
        {
            BestScore = PlayerPrefs.GetInt(BestKey, 0);
            SettingsChanged?.Invoke();
            StatsChanged?.Invoke();
        }

        public void StartGame()
        {
            Score = 0; Shots = 0; Hits = 0; Combo = 0; BestCombo = 0;
            Elapsed = 0f; IsNewRecord = false; LastShotInterval = 0f;
            lastShotTime = -1f; lastTickSecond = -1;
            hitOffsets.Clear(); spreadSum = 0f;
            TimeRemaining = settings.gameType == GameType.Timed ? settings.timedDurationSeconds : 0f;
            BestScore = PlayerPrefs.GetInt(BestKey, 0);
            State = SessionState.Running;
            StateChanged?.Invoke();
            StatsChanged?.Invoke();
        }

        public void StopGame()
        {
            if (State != SessionState.Running) return;
            Finish();
        }

        void Finish()
        {
            State = SessionState.Finished;
            if (Score > BestScore && Score > 0)
            {
                BestScore = Score;
                IsNewRecord = true;
                PlayerPrefs.SetInt(BestKey, Score);
                PlayerPrefs.Save();
                NewRecord?.Invoke();
            }
            StateChanged?.Invoke();
            StatsChanged?.Invoke();
        }

        public void RegisterShot()
        {
            if (State != SessionState.Running) return;
            Shots++;
            if (lastShotTime >= 0f) LastShotInterval = Time.time - lastShotTime;
            lastShotTime = Time.time;
            StatsChanged?.Invoke();
        }

        public void RegisterHit(int points, Vector2 offset, Vector3 worldPoint)
        {
            if (State != SessionState.Running) return;
            if (points <= 0)
            {
                Combo = 0;
                StatsChanged?.Invoke();
                return;
            }
            Hits++;
            Combo++;
            if (Combo > BestCombo) BestCombo = Combo;
            hitOffsets.Add(offset);
            spreadSum += offset.magnitude;
            var multiplier = settings.gameType == GameType.Arcade ? 1f + Mathf.Min(Combo - 1, 10) * 0.1f : 1f;
            var awarded = Mathf.RoundToInt(points * multiplier);
            Score += awarded;
            HitScored?.Invoke(awarded, worldPoint);
            StatsChanged?.Invoke();
        }

        /// <summary>A projectile that missed every target. Breaks the combo.</summary>
        public void RegisterMiss()
        {
            if (State != SessionState.Running) return;
            Combo = 0;
            StatsChanged?.Invoke();
        }

        void Update()
        {
            if (State != SessionState.Running) return;
            Elapsed += Time.deltaTime;
            if (settings.gameType != GameType.Timed) return;

            TimeRemaining -= Time.deltaTime;
            var second = Mathf.CeilToInt(TimeRemaining);
            if (second <= 10 && second > 0 && second != lastTickSecond)
            {
                lastTickSecond = second;
                CountdownTick?.Invoke(second);
            }
            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                Finish();
            }
            else
            {
                StatsChanged?.Invoke();
            }
        }
    }
}
