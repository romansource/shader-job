using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class AssetChangeTracker : AssetPostprocessor {
  public static List<string> Updated = new List<string>();
  public static List<string> Removed = new List<string>();

  static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFromPaths) {
    foreach (string asset in imported) {
      Updated.Add(UnityPathToFullOSPath(asset));
    }

    foreach (string deletedAsset in deleted) {
      Removed.Add(UnityPathToFullOSPath(deletedAsset));
    }
  }

  static string UnityPathToFullOSPath(string path) {
    string osPath = Path.Combine(Application.dataPath, "..", UnityPathToOSPath(path));
    return Path.GetFullPath(osPath);
  }

  static string UnityPathToOSPath(string path) {
    return path.Replace('/', Path.DirectorySeparatorChar);
  }
}