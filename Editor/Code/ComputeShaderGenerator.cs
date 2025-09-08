using UnityEditor;
using System.IO;

public static class ComputeShaderGenerator {
  public const string folderPath = "Assets/Resources/Generated/Computes/";

  public static void CreateComputeShaderFile(string shaderName, string shaderCode, string binderCode) {
    Directory.CreateDirectory(folderPath);

    var shaderPath = Path.Combine(folderPath, shaderName + ".compute");
    var binderPath = Path.Combine(folderPath, $"ComputeBinding_{shaderName}.cs");
    File.WriteAllText(shaderPath, shaderCode);
    File.WriteAllText(binderPath, binderCode);
    AssetDatabase.ImportAsset(shaderPath);
    AssetDatabase.ImportAsset(binderPath);
  }
}