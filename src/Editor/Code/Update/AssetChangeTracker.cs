using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RomanSource.ShaderJob.Editor {
  public class AssetChangeTracker : AssetPostprocessor {
    public static List<string> Updated = new();
    public static List<string> Removed = new();

    private const string PendingSessionKey = "LambdaUpdater_PendingAcrossReload";
    private const string UpdatedPathsKey = "LambdaUpdater_UpdatedPaths";
    private const string RemovedPathsKey = "LambdaUpdater_RemovedPaths";

    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFromPaths) {
      foreach (string asset in imported) {
        Updated.Add(UnityPathToFullOSPath(asset));
      }

      foreach (string deletedAsset in deleted) {
        Removed.Add(UnityPathToFullOSPath(deletedAsset));
      }

      var anyCsUpdated = false;

      foreach (var p in imported) {
        if (!p.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase)) continue;

        anyCsUpdated = true;
        AppendToSessionString(UpdatedPathsKey, UnityPathToFullOSPath(p));
      }

      foreach (var p in deleted) {
        if (!p.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase)) continue;

        anyCsUpdated = true;
        AppendToSessionString(RemovedPathsKey, UnityPathToFullOSPath(p));
      }

      if (anyCsUpdated) {
        SessionState.SetBool(PendingSessionKey, true);
      }
    }

    private static void AppendToSessionString(string key, string value) {
      var current = SessionState.GetString(key, string.Empty) ?? string.Empty;
      if (current.Length == 0) {
        SessionState.SetString(key, value);
      }
      else {
        // Avoid duplicates. if the exact line is already present, skip; otherwise append
        var needle = "\n" + value + "\n";
        var hay = "\n" + current + "\n";
        if (hay.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) < 0) {
          SessionState.SetString(key, current + "\n" + value);
        }
      }
    }

    private static string UnityPathToOSPath(string path) =>
      path.Replace('/', Path.DirectorySeparatorChar);

    private static string UnityPathToFullOSPath(string path) =>
      Path.GetFullPath(Path.Combine(Application.dataPath, "..", UnityPathToOSPath(path)));
  }
}
