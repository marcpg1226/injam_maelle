using UnityEngine;
using UnityEngine.SceneManagement;

public static class CheckpointData
{
    private static string sceneName;
    private static Vector2 position;

    public static bool HasCheckpoint => sceneName == SceneManager.GetActiveScene().name;
    public static Vector2 Position => position;

    public static void Set(Vector2 pos)
    {
        sceneName = SceneManager.GetActiveScene().name;
        position = pos;
    }

    public static void Clear() => sceneName = null;

    // Stops old checkpoints from carrying over between Play sessions in the editor
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => sceneName = null;
}