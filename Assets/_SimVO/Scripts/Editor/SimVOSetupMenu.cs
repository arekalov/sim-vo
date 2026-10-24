using UnityEditor;

namespace SimVO.EditorTools
{
    public static class SimVOSetupMenu
    {
        [MenuItem("SimVO/Setup/Run All")]
        public static void RunAll()
        {
            SimVOProjectConfig.Apply();
            SimVOAssetBuilder.BuildAll();
            SimVOSceneBuilder.BuildAll();
        }

        [MenuItem("SimVO/Setup/1. Configure Project")]
        public static void Configure() => SimVOProjectConfig.Apply();

        [MenuItem("SimVO/Setup/2. Build Assets")]
        public static void BuildAssets() => SimVOAssetBuilder.BuildAll();

        [MenuItem("SimVO/Setup/3. Build Scenes")]
        public static void BuildScenes() => SimVOSceneBuilder.BuildAll();
    }
}
