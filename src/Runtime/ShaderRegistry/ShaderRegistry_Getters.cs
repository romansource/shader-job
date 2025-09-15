public static partial class ShaderRegistry {
  public static Entry1<T1> Get1<T1>(int key) => (Entry1<T1>)Map[key];
  public static Entry2<T1, T2> Get2<T1, T2>(int key) => (Entry2<T1, T2>)Map[key];
  public static Entry3<T1, T2, T3> Get3<T1, T2, T3>(int key) => (Entry3<T1, T2, T3>)Map[key];
  public static Entry4<T1, T2, T3, T4> Get4<T1, T2, T3, T4>(int key) => (Entry4<T1, T2, T3, T4>)Map[key];
}