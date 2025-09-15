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
    if (Z > 1) return new() { X = 4, Y = 4, Z = 4 };
    if (Y > 1) return new() { X = 8, Y = 8, Z = 1 };
               return new() { X = 64, Y = 1, Z = 1 };
  }
  
  public (int X, int Y, int Z) GetThreadGroupCount() {
    var groupSize = GetThreadGroupSize();

    return new() {
      X = (X + groupSize.X - 1) / groupSize.X,
      Y = (Y + groupSize.Y - 1) / groupSize.Y,
      Z = (Z + groupSize.Z - 1) / groupSize.Z
    };
  } 
}