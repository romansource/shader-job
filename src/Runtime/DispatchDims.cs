namespace RomanSource.ShaderJob {
  public readonly struct DispatchDims {
    public readonly int X;
    public readonly int Y;
    public readonly int Z;

    public DispatchDims(int x, int y, int z) {
      X = x;
      Y = y;
      Z = z;
    }

    public (int X, int Y, int Z) GetThreadGroupCount() {
      var dims = (Y, Z)switch {
        (_   , > 0) => 3,
        ( > 1,   _) => 2,
        _           => 1
      };

      var groupSize = GetThreadGroupSize(dims);

      return (
        (X + groupSize.X - 1) / groupSize.X,
        (Y + groupSize.Y - 1) / groupSize.Y,
        (Z + groupSize.Z - 1) / groupSize.Z
      );
    }

    public static (int X, int Y, int Z) GetThreadGroupSize(int dims) {
      if (dims == 3) return (4,  4, 4);
      if (dims == 2) return (8,  8, 1);
                     return (64, 1, 1);
    }
  }
}
