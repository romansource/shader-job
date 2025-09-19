using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace RomanSource.ShaderJob.Editor {
  public static class FileCreator {
    public const string FolderPath = "Assets/ShaderJob/";
    private const string GroupName = "ShaderJob Assets";

    public static void CreateComputeShaderFile(int shaderId, string shaderCode) {
      Directory.CreateDirectory(FolderPath);
      var shaderPath = Path.Combine(FolderPath, shaderId + ".compute");
      File.WriteAllText(shaderPath, shaderCode);
      AssetDatabase.ImportAsset(shaderPath);
      EnsureAddressable(shaderPath);
    }

    public static void CreateBinderFile(int binderId, string binderCode) {
      Directory.CreateDirectory(FolderPath);
      var binderPath = Path.Combine(FolderPath, $"ComputeBinding_{binderId}.cs");
      File.WriteAllText(binderPath, binderCode);
      AssetDatabase.ImportAsset(binderPath);
    }

#if UNITY_EDITOR
    private static void EnsureAddressable(string assetPath) {
      var guid = AssetDatabase.AssetPathToGUID(assetPath);
      if (string.IsNullOrEmpty(guid)) return;

      var settings = AddressableAssetSettingsDefaultObject.Settings;
      if (settings == null) {
        settings = AddressableAssetSettings.Create(
          AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
          AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName,
          true, true);
        AddressableAssetSettingsDefaultObject.Settings = settings;
      }

      var group = settings.groups.FirstOrDefault(g => g != null && g.Name == GroupName);
      if (group == null)
        group = settings.CreateGroup(GroupName, false, false, true, null,
          typeof(ContentUpdateGroupSchema),
          typeof(BundledAssetGroupSchema));

      // Create or move entry into the group
      var entry = settings.FindAssetEntry(guid);
      if (entry == null || entry.parentGroup != group) {
        entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
      }

      if (entry == null) return;

      var fileName = Path.GetFileNameWithoutExtension(assetPath);
      entry.address = $"shaderjob/{fileName}";
      settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true);
      AssetDatabase.SaveAssets();
    }
#endif
  }
}
