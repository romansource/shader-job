public static class ShaderJob {
  public static ShaderJobBuilder For(int x) {
    return new ShaderJobBuilder(x);
  }

  public static ShaderJobBuilder For(int x, int y) {
    return new ShaderJobBuilder(x, y);
  }

  public static ShaderJobBuilder For(int x, int y, int z) {
    return new ShaderJobBuilder(x, y, z);
  }
}