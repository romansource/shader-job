using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public readonly struct ShaderJobBuilder {
  private readonly int _xCount;
  private readonly int _yCount;
  private readonly int _zCount;

  public ShaderJobBuilder(int xCount, int yCount = 1, int zCount = 1) {
    _xCount = xCount;
    _yCount = yCount;
    _zCount = zCount;
  }
  
  public void Run<T1, T2>(T1 a1, T2 a2, Action<T1, T2, Vector3Int> kernel,
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0) {
    var storage = ShaderMapStorage.Load();
    var loc = new LambdaLocation { filePath = NormalizePath(callerFile), line = callerLine };
    var shaderEntry = storage.LambdaLocationToShaderId.Find(x => x.key.Equals(loc));

    if (shaderEntry == null)
      throw new InvalidOperationException($"No shader for lambda {NormalizePath(callerFile)}:{callerLine}");

    var entry = ShaderRegistry.Get2<T1, T2>(shaderEntry.value);

    var shader = entry.LoadShader();
    var kernelIndex = entry.Kernel;

    entry.Binder(shader, kernelIndex, a1, a2);

    var (gx, gy, gz) = entry.Groups(); // to get group sizes from a generated Binder
    shader.Dispatch(kernelIndex, gx, gy, gz);

    entry.Updater(a1, a2);
  }
  
  public void Run<T1, T2, T3>(T1 a1, T2 a2, T3 a3, Action<T1, T2, T3, Vector3Int> kernel,
    [CallerFilePath] string callerFile = "",
    [CallerLineNumber] int callerLine = 0) {
    var storage = ShaderMapStorage.Load();
    var loc = new LambdaLocation { filePath = NormalizePath(callerFile), line = callerLine };
    var shaderEntry = storage.LambdaLocationToShaderId.Find(x => x.key.Equals(loc));

    if (shaderEntry == null)
      throw new InvalidOperationException($"No shader for lambda {NormalizePath(callerFile)}:{callerLine}");

    var entry = ShaderRegistry.Get3<T1, T2, T3>(shaderEntry.value);

    var shader = entry.LoadShader();
    var kernelIndex = entry.Kernel;

    entry.Binder(shader, kernelIndex, a1, a2, a3);

    var (gx, gy, gz) = entry.Groups(); // to get group sizes from a generated Binder
    shader.Dispatch(kernelIndex, gx, gy, gz);

    entry.Updater(a1, a2, a3);
  }
  
  public static string NormalizePath(string fullPath) {
    if (string.IsNullOrEmpty(fullPath))
      return string.Empty;

    fullPath = fullPath.Replace("\\", "/");

    var idx = fullPath.LastIndexOf("Assets/", StringComparison.OrdinalIgnoreCase);
    return idx >= 0 ? fullPath[idx..] : fullPath;
  }
}