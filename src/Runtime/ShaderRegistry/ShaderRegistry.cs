using System;
using System.Collections.Generic;

public static partial class ShaderRegistry
{
    // Each entry is stored as 'object' but retrieved via exact generic arity.
    private static readonly Dictionary<int, object> Map = new();

    public static void Register<T1>(
        int key,
        string resourcesPath,
        ShaderBinder<T1> binder,
        ShaderUpdater<T1> updater,
        int kernelIndex,
        Func<(int x,int y,int z)> dispatchGroups)
    {
        Map[key] = new Entry1<T1>(resourcesPath, binder, updater, kernelIndex, dispatchGroups);
    }

    public static void Register<T1,T2>(
        int key, string resourcesPath,
        ShaderBinder<T1,T2> binder,
        ShaderUpdater<T1,T2> updater,
        int kernelIndex,
        Func<(int,int,int)> dispatchGroups)
    {
        Map[key] = new Entry2<T1,T2>(resourcesPath, binder, updater, kernelIndex, dispatchGroups);
    }

    public static void Register<T1,T2,T3>(
        int key, string resourcesPath,
        ShaderBinder<T1,T2,T3> binder,
        ShaderUpdater<T1,T2,T3> updater,
        int kernelIndex,
        Func<(int,int,int)> dispatchGroups)
    {
        Map[key] = new Entry3<T1,T2,T3>(resourcesPath, binder, updater, kernelIndex, dispatchGroups);
    }

    public static void Register<T1,T2,T3,T4>(
        int key, string resourcesPath,
        ShaderBinder<T1,T2,T3,T4> binder,
        ShaderUpdater<T1,T2,T3,T4> updater,
        int kernelIndex,
        Func<(int,int,int)> dispatchGroups)
    {
        Map[key] = new Entry4<T1,T2,T3,T4>(resourcesPath, binder, updater, kernelIndex, dispatchGroups);
    }
}
