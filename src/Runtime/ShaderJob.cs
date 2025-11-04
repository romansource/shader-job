namespace RomanSource.ShaderJob {
  public static class ShaderJob {
    public static ShaderJobBuilder For(int x) => new(x);
    public static ShaderJobBuilder For(int x, int y) => new(x, y);
    public static ShaderJobBuilder For(int x, int y, int z) => new(x, y, z);
  }
}
