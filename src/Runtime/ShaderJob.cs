using JetBrains.Annotations;

public static class ShaderJob {
  /// Used by parser LambdaParser.ExtractForDimensions in Editor
  public static ShaderJobBuilder For([UsedImplicitly] int x) => new();
  /// Used by parser LambdaParser.ExtractForDimensions in Editor
  public static ShaderJobBuilder For([UsedImplicitly]int x, [UsedImplicitly]int y) => new();
  /// Used by parser LambdaParser.ExtractForDimensions in Editor
  public static ShaderJobBuilder For([UsedImplicitly]int x, [UsedImplicitly]int y, [UsedImplicitly]int z) => new();
}