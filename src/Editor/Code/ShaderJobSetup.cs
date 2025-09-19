using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace ShaderJob.Editor {
  public static class ShaderJobSetup {
    private const string ShaderMapPath = "Assets/ShaderJob/ShaderMap.asset";
    private const string GroupName = "ShaderJob Assets";

    public static ShaderMap ShaderMap => AssetDatabase.LoadAssetAtPath<ShaderMap>(ShaderMapPath); // TODO: Load refactoring??

    [MenuItem("Tools/ShaderJob/Setup System")]
    public static void SetupShaderJobSystem() {
      try {
        EnsureAddressablesInitialized();
        var shaderMap = CreateShaderMapAsset();
        MakeShaderMapAddressable(shaderMap);
        AssetDatabase.Refresh();
      }
      catch (System.Exception e) {
        Debug.LogError($"Failed to setup ShaderJob system: {e.Message}");
      }
    }

    private static void EnsureAddressablesInitialized() {
      if (AddressableAssetSettingsDefaultObject.Settings != null)
        return;

      Debug.Log("Initializing Addressables...");
      AddressableAssetSettings.Create(
        AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
        AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName,
        true, true);
    }

    private static ShaderMap CreateShaderMapAsset() {
      var directory = Path.GetDirectoryName(ShaderMapPath);
      if (!Directory.Exists(directory)) {
        Directory.CreateDirectory(directory);
        AssetDatabase.Refresh();
      }

      var existingAsset = AssetDatabase.LoadAssetAtPath<ShaderMap>(ShaderMapPath);
      if (existingAsset != null) {
        Debug.Log("ShaderMap already exists, using existing asset.");
        return existingAsset;
      }

      var shaderMap = ScriptableObject.CreateInstance<ShaderMap>();
      AssetDatabase.CreateAsset(shaderMap, ShaderMapPath);
      AssetDatabase.SaveAssets();

      Debug.Log($"Created ShaderMap at: {ShaderMapPath}");
      return shaderMap;
    }

    private static void MakeShaderMapAddressable(ShaderMap shaderMap) {
      var settings = AddressableAssetSettingsDefaultObject.Settings;
      if (settings == null) {
        Debug.LogError("Addressable settings not found!");
        return;
      }

      var group = GetOrCreateShaderJobGroup(settings);

      string guid = AssetDatabase.AssetPathToGUID(ShaderMapPath);
      var entry = settings.FindAssetEntry(guid);

      var desiredAddress = ShaderJobAddresses.ShaderMap;

      if (entry == null) {
        entry = settings.CreateOrMoveEntry(guid, group, false, false);
        entry.address = desiredAddress;

        Debug.Log($"Made ShaderMap addressable with address: {desiredAddress}");
      }
      else {
        // Ensure the entry has the desired address (fix mismatched keys)
        if (entry.address != desiredAddress) {
          var old = entry.address;
          entry.address = desiredAddress;
          Debug.Log($"Updated ShaderMap address from '{old}' to '{desiredAddress}'.");
        }
        else {
          Debug.Log("ShaderMap is already addressable with the correct address.");
        }
      }

      EditorUtility.SetDirty(settings);
    }

    private static AddressableAssetGroup GetOrCreateShaderJobGroup(AddressableAssetSettings settings) {
      foreach (var group in settings.groups) {
        if (group.Name == GroupName)
          return group;
      }

      var newGroup = settings.CreateGroup(GroupName, false, false, true, null,
        typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));

      Debug.Log($"Created Addressables group: {GroupName}");
      return newGroup;
    }

    public static void AddShaderToMap(Shader shader) {
      if (!IsSystemSetup())
        SetupShaderJobSystem();

      var shaderMap = AssetDatabase.LoadAssetAtPath<ShaderMap>(ShaderMapPath);
      if (shaderMap != null) {
        //TODO: shaderMap.AddShader(shader);
        EditorUtility.SetDirty(shaderMap);
        AssetDatabase.SaveAssets();
      }
      else {
        Debug.LogWarning("ShaderMap not found. Run Tools/ShaderJob/Setup System first.");
      }
    }

    private static bool IsSystemSetup() => AssetDatabase.LoadAssetAtPath<ShaderMap>(ShaderMapPath) != null;
  }
}
