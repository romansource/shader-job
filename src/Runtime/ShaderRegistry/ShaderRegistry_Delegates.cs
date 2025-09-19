using UnityEngine;

namespace RomanSource.ShaderJob {
  public static partial class ShaderRegistry {
    public delegate void ShaderBinder<T1>(ComputeShader shader, int kernel, T1 arg1);
    public delegate void ShaderBinder<T1, T2>(ComputeShader shader, int kernel, T1 arg1, T2 arg2);
    public delegate void ShaderBinder<T1, T2, T3>(ComputeShader shader, int kernel, T1 a1, T2 a2, T3 a3);
    public delegate void ShaderBinder<T1, T2, T3, T4>(ComputeShader shader, int kernel, T1 a1, T2 a2, T3 a3, T4 a4);
    public delegate void ShaderUpdater<T1>(T1 a1);
    public delegate void ShaderUpdater<T1, T2>(T1 a1, T2 a2);
    public delegate void ShaderUpdater<T1, T2, T3>(T1 a1, T2 a2, T3 a3);
    public delegate void ShaderUpdater<T1, T2, T3, T4>(T1 a1, T2 a2, T3 a3, T4 a4);
  }
}
