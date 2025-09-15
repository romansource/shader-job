using UnityEngine;

// Binder creates and binds GPU buffers, sets constants, and stores them in a ticket slot.
// NO allocations besides ComputeBuffers.
public delegate void ShaderBinder<T1>(ComputeShader shader, int kernel, T1 arg1);
public delegate void ShaderBinder<T1,T2>(ComputeShader shader, int kernel, T1 arg1, T2 arg2);
public delegate void ShaderBinder<T1,T2,T3>(ComputeShader shader, int kernel, T1 a1, T2 a2, T3 a3);
public delegate void ShaderBinder<T1,T2,T3,T4>(ComputeShader shader, int kernel, T1 a1, T2 a2, T3 a3, T4 a4);
// … generate up to T16

// Updater reads back ONLY the buffers marked as outputs by the generator, then disposes buffers.
public delegate void ShaderUpdater<T1>(T1 a1);
public delegate void ShaderUpdater<T1,T2>(T1 a1, T2 a2);
public delegate void ShaderUpdater<T1,T2,T3>(T1 a1, T2 a2, T3 a3);
public delegate void ShaderUpdater<T1,T2,T3,T4>(T1 a1, T2 a2, T3 a3, T4 a4);
// … generate up to T16