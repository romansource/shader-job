using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using UnityEditor;
using UnityEngine;

namespace ShaderJob.Editor {
  [InitializeOnLoad]
  public static class LambdaUpdater {
    private static bool s_pendingUpdate;
    private static bool s_isProcessing;
    private const string PendingSessionKey = "LambdaUpdater_PendingAcrossReload";
    private const string UpdatedPathsKey = "LambdaUpdater_UpdatedPaths";
    private const string RemovedPathsKey = "LambdaUpdater_RemovedPaths";

    static LambdaUpdater() {
      // No longer rely on compilationFinished (can fire before domain reload).
      // Use afterAssemblyReload and a persisted intent instead.
      AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
      AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;

      // If we queued work before the reload, ensure it runs once after assemblies reload.
      if (SessionState.GetBool(PendingSessionKey, false)) {
        // In rare cases when this ctor runs after the reload event, also queue a delay call now.
        EditorApplication.delayCall += RunUpdateOnce;
      }
    }

    private static void OnAfterAssemblyReload() {
      if (SessionState.GetBool(PendingSessionKey, false)) {
        EditorApplication.delayCall += RunUpdateOnce;
      }
    }

    private static void QueueUpdate() {
      if (s_pendingUpdate) return;
      s_pendingUpdate = true;
      SessionState.SetBool(PendingSessionKey, true); // survive domain reload
      EditorApplication.delayCall += RunUpdateOnce;
    }

    private static void RunUpdateOnce() {
      if (s_isProcessing) return;
      s_isProcessing = true;
      s_pendingUpdate = false;
      SessionState.SetBool(PendingSessionKey, false);

      // Collect persisted paths across reload; fall back to in-domain tracker if empty.
      var persistedUpdated = (SessionState.GetString(UpdatedPathsKey, string.Empty) ?? string.Empty)
        .Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries)
        .ToList();
      var persistedRemoved = (SessionState.GetString(RemovedPathsKey, string.Empty) ?? string.Empty)
        .Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries)
        .ToList();

      var updatedFiles = persistedUpdated.Count > 0 ? persistedUpdated : AssetChangeTracker.Updated;
      var removedFiles = persistedRemoved.Count > 0 ? persistedRemoved : AssetChangeTracker.Removed;

      ShaderJobSetup.SetupShaderJobSystem();
      var map = ShaderJobSetup.ShaderMap;

      // Disable auto-refresh during generation to avoid re-entrant imports
      AssetDatabase.DisallowAutoRefresh();
      AssetDatabase.StartAssetEditing();
      try {
        foreach (var file in updatedFiles.ToList()) {
          if (!file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) continue;
          if (!File.Exists(file)) continue;

          try {
            var asset = File.ReadAllText(file);
            var syntaxTree = CSharpSyntaxTree.ParseText(asset);
            var rawCalls = LambdaParser.GetLambdaInvocations(syntaxTree).ToList();
            var calls = rawCalls
              .GroupBy(x => x.line)
              .Select(g => {
                if (g.Count() > 1) {
                  Debug.LogWarning($"LambdaUpdater: Multiple lambda invocations detected on the same line {g.Key} in {file}. Only the first will be processed.");
                }
                return g.First();
              })
              .ToList();

            foreach (var lambda in calls) {
              var lambdaLocation = new LambdaLocation { filePath = ShaderJobBuilder.NormalizePath(file), line = lambda.line };

              if (map.LambdaLocationToText.TryGetValue(lambdaLocation, out var presentedTextAtThisLocation)) {
                // Case: existing entry with identical text — no work needed
                if (presentedTextAtThisLocation == lambda.lambda) continue;

                // Case: existing entry but lambda text changed — regenerate shader and binder
                var id = map.LambdaLocationToShaderId[lambdaLocation];
                var generated = Generator.GenerateTexts(syntaxTree, lambda.invocation, id, lambda.dimensions);
                FileCreator.CreateComputeShaderFile(id, generated.hlsl);
                FileCreator.CreateBinderFile(id, generated.binderText);
              }
              else {
                // Case: no entry yet at this location — create initial shader and binder
                var freeId = map.LambdaLocationToShaderId.FreeId();
                var generated = Generator.GenerateTexts(syntaxTree, lambda.invocation, freeId, lambda.dimensions);
                map.LambdaLocationToShaderId[lambdaLocation] = freeId;
                map.LambdaLocationToText[lambdaLocation] = lambda.lambda;
                FileCreator.CreateComputeShaderFile(freeId, generated.hlsl);
                FileCreator.CreateBinderFile(freeId, generated.binderText);
              }
            }

            var normalizedFile = ShaderJobBuilder.NormalizePath(file);
            var activeLines = new HashSet<int>(calls.Select(c => c.line));
            var obsoleteLocToIds = map.LambdaLocationToShaderId
              .Where(x => x.Key.filePath == normalizedFile && !activeLines.Contains(x.Key.line))
              .ToList();

            foreach (var obsoleteLocToId in obsoleteLocToIds) {
              map.LambdaLocationToShaderId.Remove(obsoleteLocToId.Key);
              map.LambdaLocationToText.Remove(obsoleteLocToId.Key);
              FileUtil.DeleteFileOrDirectory(Path.Combine(FileCreator.FolderPath, $"ComputeBinding_{obsoleteLocToId.Value}.cs"));
              FileUtil.DeleteFileOrDirectory(Path.Combine(FileCreator.FolderPath, obsoleteLocToId.Value + ".compute"));
            }
          } catch (Exception ex) {
            Debug.LogError($"LambdaUpdater: Failed to process file '{file}': {ex}");
          }
        }

        ClearRemoved(map, removedFiles);
        AssetChangeTracker.Updated.Clear();
        AssetChangeTracker.Removed.Clear();

        SessionState.SetString(UpdatedPathsKey, string.Empty);
        SessionState.SetString(RemovedPathsKey, string.Empty);
      }
      finally {
        AssetDatabase.StopAssetEditing();
        AssetDatabase.AllowAutoRefresh();
      }

      s_isProcessing = false;

      EditorUtility.SetDirty(map);

      if (AssetChangeTracker.Updated.Count > 0 || AssetChangeTracker.Removed.Count > 0
                                               || !string.IsNullOrEmpty(SessionState.GetString(UpdatedPathsKey, string.Empty))
                                               || !string.IsNullOrEmpty(SessionState.GetString(RemovedPathsKey, string.Empty))) {
        QueueUpdate();
      }
    }

    private static void ClearRemoved(ShaderMap map, List<string> removedFiles) {
      var locationToIds = removedFiles
        .Select(ShaderJobBuilder.NormalizePath)
        .SelectMany(normalized => map.LambdaLocationToShaderId.Where(x => x.Key.filePath == normalized).ToList());

      foreach (var locationToId in locationToIds) {
        map.LambdaLocationToText.Remove(locationToId.Key);
        map.LambdaLocationToShaderId.Remove(locationToId.Key);

        FileUtil.DeleteFileOrDirectory(Path.Combine(FileCreator.FolderPath, locationToId.Value + ".compute"));
        FileUtil.DeleteFileOrDirectory(Path.Combine(FileCreator.FolderPath, $"ComputeBinding_{locationToId.Value}.cs"));
      }
    }
  }

  public static class Ext {
    public static int FreeId(this Dictionary<LambdaLocation, int> dict) {
      if (dict.Count == 0) return 0;

      var takenIds = new HashSet<int>(dict.Values);
      var id = 0;
      while (takenIds.Contains(id)) id++;
      return id;
    }
  }
}
