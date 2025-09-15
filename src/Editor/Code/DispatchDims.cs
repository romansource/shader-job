public class DispatchDims {
  public readonly int X;
  public readonly int Y;
  public readonly int Z;

  public DispatchDims(int x, int y, int z) {
    X = x;
    Y = y;
    Z = z;
  }

  public (int X, int Y, int Z) GetThreadGroupSize() {
    if (Z > 1) return (4,  4, 4);
    if (Y > 1) return (8,  8, 1);
               return (64, 1, 1);
  }
  
  public (int X, int Y, int Z) GetThreadGroupCount() {
    var groupSize = GetThreadGroupSize();

    return (
      (X + groupSize.X - 1) / groupSize.X,
      (Y + groupSize.Y - 1) / groupSize.Y,
      (Z + groupSize.Z - 1) / groupSize.Z
    );
  } 
}