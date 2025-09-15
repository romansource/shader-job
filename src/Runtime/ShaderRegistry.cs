using System;
using System.Collections.Generic;
using UnityEngine;

public static class ShaderRegistry
{
    // Each entry is stored as 'object' but retrieved via exact generic arity.
    private static readonly Dictionary<int, object> _map = new();

    // -------- Arity 1..4 shown; generate up to 16 the same way --------

    public static void Register<T1>(
        int key,
        string resourcesPath,
        ShaderBinder<T1> binder,
        ShaderUpdater<T1> updater,
        int kernelIndex,
        Func<(int x,int y,int z)> dispatchGroups,
        int bufferCount)
    {
        _map[key] = new Entry1<T1>(resourcesPath, binder, updater, kernelIndex, dispatchGroups, bufferCount);
    }

    public static void Register<T1,T2>(
        int key, string resourcesPath,
        ShaderBinder<T1,T2> binder,
        ShaderUpdater<T1,T2> updater,
        int kernelIndex, Func<(int,int,int)> dispatchGroups, int bufferCount)
    {
        _map[key] = new Entry2<T1,T2>(resourcesPath, binder, updater, kernelIndex, dispatchGroups, bufferCount);
    }

    public static void Register<T1,T2,T3>(
        int key, string resourcesPath,
        ShaderBinder<T1,T2,T3> binder,
        ShaderUpdater<T1,T2,T3> updater,
        int kernelIndex, Func<(int,int,int)> dispatchGroups, int bufferCount)
    {
        _map[key] = new Entry3<T1,T2,T3>(resourcesPath, binder, updater, kernelIndex, dispatchGroups, bufferCount);
    }

    public static void Register<T1,T2,T3,T4>(
        int key, string resourcesPath,
        ShaderBinder<T1,T2,T3,T4> binder,
        ShaderUpdater<T1,T2,T3,T4> updater,
        int kernelIndex, Func<(int,int,int)> dispatchGroups, int bufferCount)
    {
        _map[key] = new Entry4<T1,T2,T3,T4>(resourcesPath, binder, updater, kernelIndex, dispatchGroups, bufferCount);
    }

    // ----- Getters (typed) -----

    public static Entry1<T1> Get1<T1>(int key) => (Entry1<T1>)_map[key];
    public static Entry2<T1,T2> Get2<T1,T2>(int key) => (Entry2<T1,T2>)_map[key];
    public static Entry3<T1,T2,T3> Get3<T1,T2,T3>(int key) => (Entry3<T1,T2,T3>)_map[key];
    public static Entry4<T1,T2,T3,T4> Get4<T1,T2,T3,T4>(int key) => (Entry4<T1,T2,T3,T4>)_map[key];
    
    // ----- Entries (typed) -----

    public sealed class Entry1<T1>
    {
        public readonly string Path;
        public readonly ShaderBinder<T1> Binder;
        public readonly ShaderUpdater<T1> Updater;
        public readonly int Kernel;
        public readonly Func<(int,int,int)> Groups;
        public readonly int BufferCount;

        public Entry1(string path, ShaderBinder<T1> binder, ShaderUpdater<T1> updater, int kernel, Func<(int,int,int)> groups, int bufferCount)
        { Path = path; Binder = binder; Updater = updater; Kernel = kernel; Groups = groups; BufferCount = bufferCount; }
        public ComputeShader LoadShader() => Resources.Load<ComputeShader>(Path);
    }

    public sealed class Entry2<T1,T2>
    {
        public readonly string Path;
        public readonly ShaderBinder<T1,T2> Binder;
        public readonly ShaderUpdater<T1,T2> Updater;
        public readonly int Kernel;
        public readonly Func<(int,int,int)> Groups;
        public readonly int BufferCount;

        public Entry2(string path, ShaderBinder<T1,T2> binder, ShaderUpdater<T1,T2> updater, int kernel, Func<(int,int,int)> groups, int bufferCount)
        { Path = path; Binder = binder; Updater = updater; Kernel = kernel; Groups = groups; BufferCount = bufferCount; }
        public ComputeShader LoadShader() => Resources.Load<ComputeShader>(Path);
    }

    public sealed class Entry3<T1,T2,T3>
    {
        public readonly string Path;
        public readonly ShaderBinder<T1,T2,T3> Binder;
        public readonly ShaderUpdater<T1,T2,T3> Updater;
        public readonly int Kernel;
        public readonly Func<(int,int,int)> Groups;
        public readonly int BufferCount;

        public Entry3(string path, ShaderBinder<T1,T2,T3> binder, ShaderUpdater<T1,T2,T3> updater, int kernel, Func<(int,int,int)> groups, int bufferCount)
        { Path = path; Binder = binder; Updater = updater; Kernel = kernel; Groups = groups; BufferCount = bufferCount; }
        public ComputeShader LoadShader() => Resources.Load<ComputeShader>(Path);
    }

    public sealed class Entry4<T1,T2,T3,T4>
    {
        public readonly string Path;
        public readonly ShaderBinder<T1,T2,T3,T4> Binder;
        public readonly ShaderUpdater<T1,T2,T3,T4> Updater;
        public readonly int Kernel;
        public readonly Func<(int,int,int)> Groups;
        public readonly int BufferCount;

        public Entry4(string path, ShaderBinder<T1,T2,T3,T4> binder, ShaderUpdater<T1,T2,T3,T4> updater, int kernel, Func<(int,int,int)> groups, int bufferCount)
        { Path = path; Binder = binder; Updater = updater; Kernel = kernel; Groups = groups; BufferCount = bufferCount; }
        public ComputeShader LoadShader() => Resources.Load<ComputeShader>(Path);
    }

    // … generate Entry5..Entry16 the same way
}
