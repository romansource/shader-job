using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShaderJob {
  [Serializable]
  public struct LambdaLocation {
    public string filePath;
    public int line;

    public override bool Equals(object obj) =>
      obj is LambdaLocation other && string.Equals(filePath, other.filePath, StringComparison.OrdinalIgnoreCase) && line == other.line;

    public override int GetHashCode() =>
      ((filePath?.ToLowerInvariant().GetHashCode() ?? 0) * 397) ^ line;
  }

  [CreateAssetMenu(menuName = "ShaderJob/ShaderMap")]
  public class ShaderMap : ScriptableObject, ISerializationCallbackReceiver {
    // Runtime dictionaries
    public Dictionary<LambdaLocation, int> LambdaLocationToShaderId = new();
    public Dictionary<LambdaLocation, string> LambdaLocationToText = new();

    // Serializable mirrors
    [SerializeField] private List<LambdaLocation> shaderIdKeys = new();
    [SerializeField] private List<int> shaderIdValues = new();

    [SerializeField] private List<LambdaLocation> textKeys = new();
    [SerializeField] private List<string> textValues = new();

    public void OnBeforeSerialize() {
      shaderIdKeys.Clear(); shaderIdValues.Clear();
      foreach (var kv in LambdaLocationToShaderId) { shaderIdKeys.Add(kv.Key); shaderIdValues.Add(kv.Value); }

      textKeys.Clear(); textValues.Clear();
      foreach (var kv in LambdaLocationToText) { textKeys.Add(kv.Key); textValues.Add(kv.Value); }
    }

    public void OnAfterDeserialize() {
      LambdaLocationToShaderId = new Dictionary<LambdaLocation, int>(shaderIdKeys.Count);
      for (int i = 0; i < Math.Min(shaderIdKeys.Count, shaderIdValues.Count); i++)
        LambdaLocationToShaderId[shaderIdKeys[i]] = shaderIdValues[i];

      LambdaLocationToText = new Dictionary<LambdaLocation, string>(textKeys.Count);
      for (int i = 0; i < Math.Min(textKeys.Count, textValues.Count); i++)
        LambdaLocationToText[textKeys[i]] = textValues[i];
    }
  }
}
