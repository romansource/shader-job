using System;
using UnityEngine;

namespace ShaderJob {
  public static partial class ShaderRegistry {
    public abstract class EntryBase {
      public readonly string Path;
      public readonly int Kernel;
      public readonly Func<(int, int, int)> Groups;

      protected EntryBase(string path, int kernel, Func<(int, int, int)> groups) {
        Path = path;
        Kernel = kernel;
        Groups = groups;
      }

      public ComputeShader LoadShader() => Resources.Load<ComputeShader>(Path);
    }

    public sealed class Entry1<T1> : EntryBase {
      public readonly ShaderBinder<T1> Binder;
      public readonly ShaderUpdater<T1> Updater;

      public Entry1(string path, ShaderBinder<T1> binder, ShaderUpdater<T1> updater, int kernel, Func<(int, int, int)> groups) : base(path, kernel, groups) {
        Binder = binder;
        Updater = updater;
      }
    }

    public sealed class Entry2<T1, T2> : EntryBase {
      public readonly ShaderBinder<T1, T2> Binder;
      public readonly ShaderUpdater<T1, T2> Updater;

      public Entry2(string path, ShaderBinder<T1, T2> binder, ShaderUpdater<T1, T2> updater, int kernel, Func<(int, int, int)> groups) : base(path, kernel, groups) {
        Binder = binder;
        Updater = updater;
      }
    }

    public sealed class Entry3<T1, T2, T3> : EntryBase {
      public readonly ShaderBinder<T1, T2, T3> Binder;
      public readonly ShaderUpdater<T1, T2, T3> Updater;

      public Entry3(string path, ShaderBinder<T1, T2, T3> binder, ShaderUpdater<T1, T2, T3> updater, int kernel, Func<(int, int, int)> groups) : base(path, kernel, groups) {
        Binder = binder;
        Updater = updater;
      }
    }

    public sealed class Entry4<T1, T2, T3, T4> : EntryBase {
      public readonly ShaderBinder<T1, T2, T3, T4> Binder;
      public readonly ShaderUpdater<T1, T2, T3, T4> Updater;

      public Entry4(string path, ShaderBinder<T1, T2, T3, T4> binder, ShaderUpdater<T1, T2, T3, T4> updater, int kernel, Func<(int, int, int)> groups) : base(path, kernel, groups) {
        Binder = binder;
        Updater = updater;
      }
    }
  }
}
