using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimVO.Core
{
    /// <summary>Scene-level game state: mode settings, score, timer, pause and training metrics.</summary>
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        public GameSettings settings = new GameSettings();

        public SessionState State { get; private set; } = SessionState.Idle;
        public bool Paused { get; private set; }
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
        /// <summary>Position the current stage asks for in the advanced mode.</summary>
        public BodyPosition RequiredPosition { get; set; } = BodyPosition.Standing;
        public bool PositionSatisfied => settings.mode != ShootingMode.Advanced || CurrentPosition == RequiredPosition;

        // Cover mode metrics.
        public float CoverTotalTime { get; private set; }
        public float ExposedTime { get; private set; }
        public float ExposedWhileHiddenTime { get; private set; }
        public int MissedExposures { get; private set; }
        public float CoverRatio => CoverTotalTime <= 0.01f ? 1f : 1f - ExposedWhileHiddenTime / CoverTotalTime;

        public float Accuracy => Shots == 0 ? 0f : (float)Hits / Shots;
        /// <summary>Mean distance of hits from the target centre, in metres.</summary>
        public float MeanSpread => hitOffsets.Count == 0 ? 0f : spreadSum / hitOffsets.Count;
        public IReadOnlyList<Vector2> HitOffsets => hitOffsets;

        public event Action StateChanged;
        public event Action StatsChanged;
        public event Action SettingsChanged;
        public event Action PauseChanged;
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

        public void SetPaused(bool paused)
        {
            if (Paused == paused) return;
            Paused = paused;
            PauseChanged?.Invoke();
        }

        public void StartGame()
        {
            Score = 0; Shots = 0; Hits = 0; Combo = 0; BestCombo = 0;
            Elapsed = 0f; IsNewRecord = false; LastShotInterval = 0f;
            lastShotTime = -1f; lastTickSecond = -1;
            hitOffsets.Clear(); spreadSum = 0f;
            CoverTotalTime = 0f; ExposedTime = 0f; ExposedWhileHiddenTime = 0f; MissedExposures = 0;
            RequiredPosition = BodyPosition.Standing;
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

        /// <summary>Back to the idle range without a result (main menu).</summary>
        public void ResetToIdle()
        {
            State = SessionState.Idle;
            Score = 0; Shots = 0; Hits = 0; Combo = 0;
            TimeRemaining = 0f; Elapsed = 0f; IsNewRecord = false;
            StateChanged?.Invoke();
            StatsChanged?.Invoke();
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
            if (settings.mode == ShootingMode.Cover) multiplier += 0.5f;
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

        /// <summary>Cover mode: the target showed itself and was not hit in time.</summary>
        public void RegisterMissedExposure()
        {
            if (State != SessionState.Running) return;
            MissedExposures++;
            Combo = 0;
            StatsChanged?.Invoke();
        }

        /// <summary>Cover mode, per frame: was the head visible to the target, and was the target showing itself.</summary>
        public void RegisterExposure(float deltaTime, bool headExposed, bool targetExposed)
        {
            if (State != SessionState.Running || Paused) return;
            CoverTotalTime += deltaTime;
            if (headExposed)
            {
                ExposedTime += deltaTime;
                if (!targetExposed) ExposedWhileHiddenTime += deltaTime;
            }
        }

        void Update()
        {
            if (State != SessionState.Running || Paused) return;
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
