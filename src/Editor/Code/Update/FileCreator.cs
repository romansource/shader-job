using UnityEditor;
using System.IO;

public static class FileCreator {
  public const string folderPath = "Assets/Resources/Generated/Computes/";

  public static void CreateComputeShaderFile(int shaderId, string shaderCode) {
    Directory.CreateDirectory(folderPath);
    var shaderPath = Path.Combine(folderPath, shaderId + ".compute");
    File.WriteAllText(shaderPath, shaderCode);
    AssetDatabase.ImportAsset(shaderPath);
  }
  
  public static void CreateBinderFile(int binderId, string binderCode) {
    Directory.CreateDirectory(folderPath);
    var binderPath = Path.Combine(folderPath, $"ComputeBinding_{binderId}.cs");
    File.WriteAllText(binderPath, binderCode);
    AssetDatabase.ImportAsset(binderPath);
  }
}