using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class AssetChangeTracker : AssetPostprocessor {
  public static List<string> Updated = new List<string>();
  public static List<string> Removed = new List<string>();

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

    // Minimal work: only mark pending if any .cs changed; also persist the exact lists across reload
    bool anyCsUpdated = false;

    if (imported != null) {
      foreach (var p in imported) {
        if (p.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase)) {
          anyCsUpdated = true;
          AppendToSessionString(UpdatedPathsKey, UnityPathToFullOSPath(p));
        }
      }
    }

    if (deleted != null) {
      foreach (var p in deleted) {
        if (p.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase)) {
          anyCsUpdated = true;
          AppendToSessionString(RemovedPathsKey, UnityPathToFullOSPath(p));
        }
      }
    }

    if (anyCsUpdated) {
      SessionState.SetBool(PendingSessionKey, true);
    }
  }

  private static void AppendToSessionString(string key, string value) {
    var current = SessionState.GetString(key, string.Empty) ?? string.Empty;
    if (current.Length == 0) {
      SessionState.SetString(key, value);
    } else {
      // Avoid duplicates at low cost
      // Fast check: if exact line already present, skip; otherwise append
      // This avoids converting to sets for performance.
      var needle = "\n" + value + "\n";
      var hay = "\n" + current + "\n";
      if (hay.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) < 0) {
        SessionState.SetString(key, current + "\n" + value);
      }
    }
  }

  static string UnityPathToFullOSPath(string path) {
    string osPath = Path.Combine(Application.dataPath, "..", UnityPathToOSPath(path));
    return Path.GetFullPath(osPath);
  }

  public static string UnityPathToOSPath(string path) {
    return path.Replace('/', Path.DirectorySeparatorChar);
  }
}