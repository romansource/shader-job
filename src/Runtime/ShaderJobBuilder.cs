using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ShaderJob {
  public readonly struct ShaderJobBuilder {
    private static ShaderMap s_shaderMap;
    private static readonly object s_mapLock = new();

    public void Run<T1>(T1 a1, Action<T1, Vector3Int> kernel, [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0) {
      var binderId = RetrieveShaderId(callerFile, callerLine);
      var entry = ShaderRegistry.Get1<T1>(binderId);

      var handle = Addressables.LoadAssetAsync<ComputeShader>($"shaderjob/{binderId}");
      var shader = handle.WaitForCompletion(); // fully async API Soon™

      if (handle.Status != AsyncOperationStatus.Succeeded || shader == null) {
        var ex = handle.OperationException;
        if (handle.IsValid()) Addressables.Release(handle);
        var where = $"{NormalizePath(callerFile)}:{callerLine}";
        throw new InvalidOperationException($"Failed to load shader for lambda {where}" + (ex != null ? $" — {ex.Message}" : string.Empty), ex);
      }

      try {
        var kernelIndex = entry.Kernel;
        var (gx, gy, gz) = entry.Groups();
        entry.Binder(shader, kernelIndex, a1);
        shader.Dispatch(kernelIndex, gx, gy, gz);
        entry.Updater(a1);
      }
      finally {
        if (handle.IsValid()) Addressables.Release(handle);
      }
    }

    public void Run<T1, T2>(T1 a1, T2 a2, Action<T1, T2, Vector3Int> kernel, [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0) {
      var binderId = RetrieveShaderId(callerFile, callerLine);
      var entry = ShaderRegistry.Get2<T1, T2>(binderId);

      var handle = Addressables.LoadAssetAsync<ComputeShader>($"shaderjob/{binderId}");
      var shader = handle.WaitForCompletion(); // fully async API Soon™

      if (handle.Status != AsyncOperationStatus.Succeeded || shader == null) {
        var ex = handle.OperationException;
        if (handle.IsValid()) Addressables.Release(handle);
        var where = $"{NormalizePath(callerFile)}:{callerLine}";
        throw new InvalidOperationException($"Failed to load shader for lambda {where}" + (ex != null ? $" — {ex.Message}" : string.Empty), ex);
      }

      try {
        var kernelIndex = entry.Kernel;
        var (gx, gy, gz) = entry.Groups();
        entry.Binder(shader, kernelIndex, a1, a2);
        shader.Dispatch(kernelIndex, gx, gy, gz);
        entry.Updater(a1, a2);
      }
      finally {
        if (handle.IsValid()) Addressables.Release(handle);
      }
    }

    public void Run<T1, T2, T3>(T1 a1, T2 a2, T3 a3, Action<T1, T2, T3, Vector3Int> kernel, [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0) {
      var binderId = RetrieveShaderId(callerFile, callerLine);
      var entry = ShaderRegistry.Get3<T1, T2, T3>(binderId);

      var handle = Addressables.LoadAssetAsync<ComputeShader>($"shaderjob/{binderId}");
      var shader = handle.WaitForCompletion(); // fully async API Soon™

      if (handle.Status != AsyncOperationStatus.Succeeded || shader == null) {
        var ex = handle.OperationException;
        if (handle.IsValid()) Addressables.Release(handle);
        var where = $"{NormalizePath(callerFile)}:{callerLine}";
        throw new InvalidOperationException($"Failed to load shader for lambda {where}" + (ex != null ? $" — {ex.Message}" : string.Empty), ex);
      }

      try {
        var kernelIndex = entry.Kernel;
        var (gx, gy, gz) = entry.Groups();
        entry.Binder(shader, kernelIndex, a1, a2, a3);
        shader.Dispatch(kernelIndex, gx, gy, gz);
        entry.Updater(a1, a2, a3);
      }
      finally {
        if (handle.IsValid()) Addressables.Release(handle);
      }
    }

    public void Run<T1, T2, T3, T4>(T1 a1, T2 a2, T3 a3, T4 a4, Action<T1, T2, T3, T4, Vector3Int> kernel, [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0) {
      var binderId = RetrieveShaderId(callerFile, callerLine);
      var entry = ShaderRegistry.Get4<T1, T2, T3, T4>(binderId);

      var handle = Addressables.LoadAssetAsync<ComputeShader>($"shaderjob/{binderId}");
      var shader = handle.WaitForCompletion(); // fully async API Soon™

      if (handle.Status != AsyncOperationStatus.Succeeded || shader == null) {
        var ex = handle.OperationException;
        if (handle.IsValid()) Addressables.Release(handle);
        var where = $"{NormalizePath(callerFile)}:{callerLine}";
        throw new InvalidOperationException($"Failed to load shader for lambda {where}" + (ex != null ? $" — {ex.Message}" : string.Empty), ex);
      }

      try {
        var kernelIndex = entry.Kernel;
        var (gx, gy, gz) = entry.Groups();
        entry.Binder(shader, kernelIndex, a1, a2, a3, a4);
        shader.Dispatch(kernelIndex, gx, gy, gz);
        entry.Updater(a1, a2, a3, a4);;
      }
      finally {
        if (handle.IsValid()) Addressables.Release(handle);
      }
    }

    public static string NormalizePath(string fullPath) {
      if (string.IsNullOrEmpty(fullPath))
        return string.Empty;

      fullPath = fullPath.Replace("\\", "/");

      var idx = fullPath.LastIndexOf("Assets/", StringComparison.OrdinalIgnoreCase);
      return idx >= 0 ? fullPath[idx..] : fullPath;
    }

    private static ShaderMap GetRuntimeMap() {
      // Thread-safety mitigation of Unity callbacks reentrancy
      var map = Volatile.Read(ref s_shaderMap);
      if (map != null) return map;
      lock (s_mapLock) {
        map = s_shaderMap;
        if (map != null) return map;

        var handle = Addressables.LoadAssetAsync<ShaderMap>(ShaderJobAddresses.ShaderMap);
        map = handle.WaitForCompletion();
        if (map == null)
          throw new InvalidOperationException($"Failed to load ShaderMap at address '{ShaderJobAddresses.ShaderMap}'. Ensure it's marked Addressable and included in the build.");
        Volatile.Write(ref s_shaderMap, map);
        return map;
      }
    }

    private static int RetrieveShaderId(string callerFile, int callerLine) {
      var map = GetRuntimeMap();
      var loc = new LambdaLocation { filePath = NormalizePath(callerFile), line = callerLine };

      if (!map.LambdaLocationToShaderId.TryGetValue(loc, out var shaderId))
        throw new InvalidOperationException($"No shader for lambda {NormalizePath(callerFile)}:{callerLine}");
      return shaderId;
    }
  }
}
