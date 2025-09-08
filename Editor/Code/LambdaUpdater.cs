using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using UnityEditor;
using UnityEditor.Compilation;

[InitializeOnLoad]
public static class LambdaUpdater {
  static LambdaUpdater() {
    CompilationPipeline.compilationFinished -= OnCompilationFinished;
    CompilationPipeline.compilationFinished += OnCompilationFinished;
  }

  static void OnCompilationFinished(object obj) {
    var map = ShaderMapStorage.Load();

    foreach (var file in AssetChangeTracker.Updated.ToList()) {
      var asset = File.ReadAllText(file);
      var syntaxTree = CSharpSyntaxTree.ParseText(asset);
      var calls = ShaderCodeParser.GetLambdaInvocations(syntaxTree).GroupBy(x => x.line).Select(g => g.First()).ToList();
      
      foreach (var lambda in calls) {
        var lambdaLocation = new LambdaLocation { filePath = ShaderJob.NormalizePath(file), line = lambda.line };

        var shaderIdFromText = map.LambdaTextToShaderId.FirstOrDefault(x => x.key == lambda.lambda)?.value;
        if (shaderIdFromText != null) {
          var oldShaderIdAtLocation = map.LambdaLocationToShaderId.FirstOrDefault(x => x.key.Equals(lambdaLocation))?.value;
          if (oldShaderIdAtLocation == null) {
            // create new with existing text
            map.LambdaLocationToShaderId.Add(new LocationToIdEntry{key = lambdaLocation, value = shaderIdFromText.Value});
            
            continue;
          }
          
          if (oldShaderIdAtLocation != shaderIdFromText) {
            // change in place
            // check if the old was the last of its text and remove if so
            if (map.LambdaLocationToShaderId.Count(x => x.value == oldShaderIdAtLocation) > 1) {
              var index = map.LambdaLocationToShaderId.FindIndex(x => x.key.Equals(lambdaLocation));
              map.LambdaLocationToShaderId[index] = new LocationToIdEntry { key = lambdaLocation, value = shaderIdFromText.Value };
            }
            else {
              // REMOVE SHADER AS NOT NEEDED
             map.LambdaLocationToShaderId.RemoveAll(x => x.value ==  oldShaderIdAtLocation);
             FileUtil.DeleteFileOrDirectory(Path.Combine("Assets/Resources/Generated/Computes/", oldShaderIdAtLocation + ".compute"));
             FileUtil.DeleteFileOrDirectory(Path.Combine("Assets/Resources/Generated/Computes/", $"ComputeBInding_{oldShaderIdAtLocation}.cs"));
            }
          }
          else {
            // it's already here
            continue;
          }
        }
        else {
          // create new with new text
          var newShaderId = 0;
          if (map.LambdaTextToShaderId.Count > 0) {
            var takenIds = map.LambdaTextToShaderId.Select(p => p.value).ToHashSet();
            newShaderId = Enumerable.Range(0, takenIds.Count + 1).First(n => !takenIds.Contains(n));
          }

          var generated = ShaderCodeParser.GenerateTexts(syntaxTree, lambda.invocation, newShaderId);
          map.LambdaTextToShaderId.Add(new TextToIdEntry { key = lambda.lambda, value = newShaderId });
          map.LambdaLocationToShaderId.Add(new LocationToIdEntry { key = lambdaLocation, value = newShaderId });
          ComputeShaderGenerator.CreateComputeShaderFile(newShaderId.ToString(), generated.hlsl, generated.binderText);
        }
      }

      // clear unused in file parse
      foreach (var entry in map.LambdaLocationToShaderId.Where(x => x.key.filePath == file && calls.All(y => y.line != x.key.line))) {
        if (map.LambdaLocationToShaderId.Count(x => x.value == entry.value) > 1) {
          map.LambdaLocationToShaderId.Remove(entry);
        }
        else {
          map.LambdaLocationToShaderId.RemoveAll(x => x.value == entry.value);
          map.LambdaTextToShaderId.RemoveAll(x => x.value == entry.value);
        }
      }
    }

    ClearRemoved(map);

    ShaderMapStorage.Save(map);
    AssetDatabase.Refresh();
  }

  private static void ClearRemoved(SerializableShaderMap map) {
    foreach (var file in AssetChangeTracker.Removed) {
      var shaderId = map.LambdaLocationToShaderId.Find(x => x.key.filePath == file).value;

      if (map.LambdaLocationToShaderId.Count(x => x.value == shaderId) <= 1) {
        map.LambdaLocationToShaderId.RemoveAll(x => x.value == shaderId);
        map.LambdaTextToShaderId.RemoveAll(x => x.value == shaderId);
      }
    }
  }
}