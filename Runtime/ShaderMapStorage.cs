using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using Newtonsoft.Json;

public static class ShaderMapStorage
{
  private static string storagePath = "Assets/EditorData/shaderMap.json";

  public static SerializableShaderMap Load()
  {
    if (!File.Exists(storagePath))
      return new SerializableShaderMap();

    var json = File.ReadAllText(storagePath);
    return JsonConvert.DeserializeObject<SerializableShaderMap>(json);
  }

  public static void Save(SerializableShaderMap map)
  {
    Directory.CreateDirectory(Path.GetDirectoryName(storagePath)!);
    var json = JsonConvert.SerializeObject(map, Formatting.Indented);
    File.WriteAllText(storagePath, json);
    AssetDatabase.Refresh();
  }
}

[Serializable]
public struct LambdaLocation : IEquatable<LambdaLocation> {
  private sealed class FilePathLineEqualityComparer : IEqualityComparer<LambdaLocation> {
    public bool Equals(LambdaLocation x, LambdaLocation y) {
      return x.filePath == y.filePath && x.line == y.line;
    }

    public int GetHashCode(LambdaLocation obj) {
      return HashCode.Combine(obj.filePath, obj.line);
    }
  }

  public static IEqualityComparer<LambdaLocation> FilePathLineComparer { get; } = new FilePathLineEqualityComparer();

  public string filePath;
  public int line;

  public bool Equals(LambdaLocation other) {
    return filePath == other.filePath && line == other.line;
  }

  public override bool Equals(object obj) {
    return obj is LambdaLocation other && Equals(other);
  }

  public override int GetHashCode() {
    return HashCode.Combine(filePath, line);
  }
}

[Serializable]
public class LocationToIdEntry {
  public LambdaLocation key;
  public int value;
}

[Serializable]
public class TextToIdEntry {
  public string key;
  public int value;
}

[Serializable]
public class SerializableShaderMap
{
  public List<TextToIdEntry> LambdaTextToShaderId = new();
  public List<LocationToIdEntry> LambdaLocationToShaderId = new();
}