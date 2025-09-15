using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public readonly struct ShaderJobBuilder {
  public void Run<T1>(T1 a1, Action<T1, Vector3Int> kernel, [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0) {
    var binderId = RetrieveBinderId(callerFile, callerLine);
    var entry = ShaderRegistry.Get1<T1>(binderId.value);
    var shader = entry.LoadShader();
    var kernelIndex = entry.Kernel;
    var (gx, gy, gz) = entry.Groups();

    entry.Binder(shader, kernelIndex, a1);
    shader.Dispatch(kernelIndex, gx, gy, gz);
    entry.Updater(a1);
  }
  
  public void Run<T1, T2>(T1 a1, T2 a2, Action<T1, T2, Vector3Int> kernel, [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0) {
    var binderId = RetrieveBinderId(callerFile, callerLine);
    var entry = ShaderRegistry.Get2<T1, T2>(binderId.value);
    var shader = entry.LoadShader();
    var kernelIndex = entry.Kernel;
    var (gx, gy, gz) = entry.Groups();

    entry.Binder(shader, kernelIndex, a1, a2);
    shader.Dispatch(kernelIndex, gx, gy, gz);
    entry.Updater(a1, a2);
  }
  
  public void Run<T1, T2, T3>(T1 a1, T2 a2, T3 a3, Action<T1, T2, T3, Vector3Int> kernel, [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0) {
    var binderId = RetrieveBinderId(callerFile, callerLine);
    var entry = ShaderRegistry.Get3<T1, T2, T3>(binderId.value);
    var shader = entry.LoadShader();
    var kernelIndex = entry.Kernel;
    var (gx, gy, gz) = entry.Groups();

    entry.Binder(shader, kernelIndex, a1, a2, a3);
    shader.Dispatch(kernelIndex, gx, gy, gz);
    entry.Updater(a1, a2, a3);
  }
  
  public void Run<T1, T2, T3, T4>(T1 a1, T2 a2, T3 a3, T4 a4, Action<T1, T2, T3, T4, Vector3Int> kernel, [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0) {
    var binderId = RetrieveBinderId(callerFile, callerLine);
    var entry = ShaderRegistry.Get4<T1, T2, T3, T4>(binderId.value);
    var shader = entry.LoadShader();
    var kernelIndex = entry.Kernel;
    var (gx, gy, gz) = entry.Groups();

    entry.Binder(shader, kernelIndex, a1, a2, a3, a4);
    shader.Dispatch(kernelIndex, gx, gy, gz);
    entry.Updater(a1, a2, a3, a4);
  }
  
  public static string NormalizePath(string fullPath) {
    if (string.IsNullOrEmpty(fullPath))
      return string.Empty;

    fullPath = fullPath.Replace("\\", "/");

    var idx = fullPath.LastIndexOf("Assets/", StringComparison.OrdinalIgnoreCase);
    return idx >= 0 ? fullPath[idx..] : fullPath;
  }
  
  private static LocationToId RetrieveBinderId(string callerFile, int callerLine) {
    var storage = ShaderMapStorage.Load();
    var loc = new LambdaLocation { filePath = NormalizePath(callerFile), line = callerLine };
    var binderId = storage.LambdaLocationToBinderId.Find(x => x.key.Equals(loc));

    if (binderId == null)
      throw new InvalidOperationException($"No shader for lambda {NormalizePath(callerFile)}:{callerLine}");
    return binderId;
  }
}