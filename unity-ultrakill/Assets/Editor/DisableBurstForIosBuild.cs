using UnityEditor;

[InitializeOnLoad]
public static class DisableBurstForIosBuild
{
    static DisableBurstForIosBuild()
    {
        EditorApplication.delayCall += () =>
        {
            var burstEditor = System.Type.GetType("Unity.Burst.Editor.BurstCompiler, Unity.Burst.Editor");
            var options = burstEditor?.GetProperty("Options", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetValue(null);
            options?.GetType().GetProperty("EnableBurstCompilation")?.SetValue(options, false);
        };
    }
}
