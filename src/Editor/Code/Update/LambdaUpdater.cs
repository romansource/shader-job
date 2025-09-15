using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class LambdaUpdater {
  private static bool _pendingUpdate;
  private static bool _isProcessing;
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
    if (_pendingUpdate) return;
    _pendingUpdate = true;
    SessionState.SetBool(PendingSessionKey, true); // survive domain reload
    EditorApplication.delayCall += RunUpdateOnce;
  }

  private static void RunUpdateOnce() {
    if (_isProcessing) return;
    _isProcessing = true;
    _pendingUpdate = false;
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

    var map = ShaderMapStorage.Load();
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
                Debug.LogWarning(
                  $"LambdaUpdater: Multiple lambda invocations detected on the same line {g.Key} in {file}. Only the first will be processed.");
              }
              return g.First();
            })
            .ToList();

          foreach (var lambda in calls) {
            var lambdaLocation = new LambdaLocation { filePath = ShaderJobBuilder.NormalizePath(file), line = lambda.line };

            // Check if we already have a shader id for this exact location (file+line)
            var shaderEntry = map.LambdaLocationToShaderId.FirstOrDefault(x => x.key.Equals(lambdaLocation));
            if (shaderEntry == null) {
              // New invocation at this location: allocate new shaderId and binderId
              var newShaderId = map.LambdaLocationToShaderId.FreeId();
              var binderId = map.LambdaLocationToBinderId.FreeId();
              var generated = Generator.GenerateTexts(syntaxTree, lambda.invocation, newShaderId, lambda.dimensions);
              map.LambdaLocationToShaderId.Add(new LocationToId { key = lambdaLocation, value = newShaderId });
              map.LambdaLocationToBinderId.Add(new LocationToId { key = lambdaLocation, value = binderId });
              FileCreator.CreateComputeShaderFile(newShaderId, generated.hlsl);
              FileCreator.CreateBinderFile(binderId, generated.binderText);
            }
            else {
              // Existing location: keep ids, regenerate code to reflect any changes (including For dims)
              var shaderId = shaderEntry.value;
              var binderEntry = map.LambdaLocationToBinderId.FirstOrDefault(x => x.key.Equals(lambdaLocation));
              int binderId;
              if (binderEntry == null) {
                binderId = map.LambdaLocationToBinderId.FreeId();
                map.LambdaLocationToBinderId.Add(new LocationToId { key = lambdaLocation, value = binderId });
              }
              else {
                binderId = binderEntry.value;
              }
              var hlsl = Generator.GenerateShader(syntaxTree, lambda.invocation, lambda.dimensions);
              FileCreator.CreateComputeShaderFile(shaderId, hlsl);
              var binder = Generator.GenerateBinder(syntaxTree, lambda.invocation, shaderId, lambda.dimensions);
              FileCreator.CreateBinderFile(binderId, binder);
            }
          }

          // Remove mappings in this file that are no longer present and delete their generated files
          foreach (var entry in map.LambdaLocationToShaderId
                     .Where(x => x.key.filePath == ShaderJobBuilder.NormalizePath(file)
                                 && calls.All(y => y.line != x.key.line)).ToList()) {
            // Remove binder file and mapping
            var binderEntry = map.LambdaLocationToBinderId.Find(x => x.key.Equals(entry.key));
            if (binderEntry != null) {
              var binderId = binderEntry.value;
              map.LambdaLocationToBinderId.RemoveAll(x => x.value == binderId);
              FileUtil.DeleteFileOrDirectory(Path.Combine(FileCreator.folderPath, $"ComputeBinding_{binderId}.cs"));
            }
            // Remove shader file and mapping
            FileUtil.DeleteFileOrDirectory(Path.Combine(FileCreator.folderPath, entry.value + ".compute"));
            map.LambdaLocationToShaderId.RemoveAll(x => x.key.Equals(entry.key));
          }
        }
        catch (Exception ex) {
          Debug.LogError($"LambdaUpdater: Failed to process file '{file}': {ex}");
        }
      }

      // Clear in-memory trackers
      AssetChangeTracker.Updated.Clear();

      // Process removals
      ClearRemoved(map, removedFiles);
      AssetChangeTracker.Removed.Clear();

      // Clear persisted queues now that we've consumed them
      SessionState.SetString(UpdatedPathsKey, string.Empty);
      SessionState.SetString(RemovedPathsKey, string.Empty);

      ShaderMapStorage.Save(map); // refresh's inside
    }
    finally {
      AssetDatabase.StopAssetEditing();
      _isProcessing = false;
      // If more changes arrived while we were processing, schedule another pass.
      if (AssetChangeTracker.Updated.Count > 0 || AssetChangeTracker.Removed.Count > 0
          || !string.IsNullOrEmpty(SessionState.GetString(UpdatedPathsKey, string.Empty))
          || !string.IsNullOrEmpty(SessionState.GetString(RemovedPathsKey, string.Empty))) {
        QueueUpdate();
      }
    }
  }

  private static void ClearRemoved(SerializableShaderMap map, List<string> removedFiles) {
    foreach (var file in removedFiles) {
      var normalized = ShaderJobBuilder.NormalizePath(file);
      foreach (var locationToId in map.LambdaLocationToShaderId.Where(x => x.key.filePath == normalized).ToList()) {
        map.LambdaLocationToShaderId.RemoveAll(x => x.key.Equals(locationToId.key));
        FileUtil.DeleteFileOrDirectory(Path.Combine(FileCreator.folderPath, locationToId.value + ".compute"));

        var binderEntry = map.LambdaLocationToBinderId.Find(x => x.key.Equals(locationToId.key));
        if (binderEntry != null) {
          var binderId = binderEntry.value;
          map.LambdaLocationToBinderId.RemoveAll(x => x.value == binderId);
          FileUtil.DeleteFileOrDirectory(Path.Combine(FileCreator.folderPath, $"ComputeBinding_{binderId}.cs"));
        }
      }
    }
  }
}

public static class Ext {
  public static int FreeId(this List<LocationToId> list) {
    var id = 0;
    if (list.Count > 0) {
      var takenIds = list.Select(p => p.value).ToHashSet();
      id = Enumerable.Range(0, takenIds.Count + 1).First(n => !takenIds.Contains(n));
    }

    return id;
  }
}