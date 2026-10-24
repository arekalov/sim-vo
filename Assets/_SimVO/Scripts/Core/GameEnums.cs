namespace SimVO.Core
{
    public enum ShootingMode { Standard, Advanced, Cover }
    public enum GameType { Arcade, Timed }
    public enum TargetKind { Ring, Silhouette, Gong, Clay, Hologram, Ducks }
    public enum TargetMotionMode { Static, Lateral, Circle }
    public enum TargetSurface { Paper, Metal, Hologram }
    public enum BodyPosition { Standing, Kneeling, Prone }
    public enum SessionState { Idle, Running, Finished }

    [System.Serializable]
    public class GameSettings
    {
        public ShootingMode mode = ShootingMode.Standard;
        public GameType gameType = GameType.Arcade;
        public TargetKind targetKind = TargetKind.Ring;
        public TargetSurface targetSurface = TargetSurface.Paper;
        public TargetMotionMode motion = TargetMotionMode.Static;
        public bool dynamicTargets => motion != TargetMotionMode.Static;
        public float timedDurationSeconds = 60f;
    }

    /// <summary>Russian UI labels for enum values.</summary>
    public static class Labels
    {
        public static string Mode(ShootingMode m) => m switch
        {
            ShootingMode.Standard => "Обычный",
            ShootingMode.Advanced => "Сложный",
            _ => "Укрытие",
        };
        public static string Game(GameType g) => g == GameType.Arcade ? "Аркада" : "На время";
        public static string Target(TargetKind k) => k switch
        {
            TargetKind.Ring => "Круглая",
            TargetKind.Silhouette => "Силуэт",
            TargetKind.Gong => "Гонг",
            TargetKind.Clay => "Тарелочки",
            TargetKind.Hologram => "Голограмма",
            _ => "Утки",
        };
        public static string Motion(TargetMotionMode m) => m switch
        {
            TargetMotionMode.Static => "Статика",
            TargetMotionMode.Lateral => "Вбок",
            _ => "По кругу",
        };
        public static string Position(BodyPosition p) => p switch
        {
            BodyPosition.Standing => "Стоя",
            BodyPosition.Kneeling => "С колена",
            _ => "Лёжа",
        };
    }
}
