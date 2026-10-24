namespace SimVO.Core
{
    public enum ShootingMode { Standard, Advanced }
    public enum GameType { Arcade, Timed }
    public enum TargetKind { Ring, Silhouette, Clay }
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
        public bool dynamicTargets;
        public float timedDurationSeconds = 60f;
    }

    /// <summary>Russian UI labels for enum values.</summary>
    public static class Labels
    {
        public static string Mode(ShootingMode m) => m == ShootingMode.Standard ? "Обычный" : "Сложный";
        public static string Game(GameType g) => g == GameType.Arcade ? "Аркада" : "На время";
        public static string Target(TargetKind k) => k switch
        {
            TargetKind.Ring => "Круглая",
            TargetKind.Silhouette => "Силуэт",
            _ => "Тарелочка",
        };
        public static string Position(BodyPosition p) => p switch
        {
            BodyPosition.Standing => "Стоя",
            BodyPosition.Kneeling => "С колена",
            _ => "Лёжа",
        };
    }
}
