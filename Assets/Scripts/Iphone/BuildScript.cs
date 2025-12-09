using UnityEditor;
using System.IO;

public class BuildScript
{
    public static void BuildIOS()
    {
        string path = "build/ios";
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Menu.unity" },
            locationPathName = path,
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };

        BuildPipeline.BuildPlayer(options);
    }

    public static void BuildAndroid()
    {
        string path = "build/android/game.aab";
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Menu.unity" },
            locationPathName = path,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildPipeline.BuildPlayer(options);
    }
}
