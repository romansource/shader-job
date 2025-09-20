using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace RomanSource.ShaderJob.Editor {
  public static class BinderGenerator {
    public static string GenerateBinder(int id, (string Name, ITypeSymbol Type)[] parameters, HashSet<string> writtenBuffers, DispatchDims dispatchDims) {
      var realParameters = parameters
        .Where(p => p.Type != null)
        .ToArray();

      var typeArgs = string.Join(", ", realParameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
      var arrayBufferCount = realParameters.Count(p => p.Type is IArrayTypeSymbol);

      var binderBody = new StringBuilder();
      var bufferIndex = 0;
      var arrayNameToIndex = new Dictionary<string, int>();

      foreach (var p in realParameters) {
        if (p.Type is IArrayTypeSymbol arrType) {
          var elemType = arrType.ElementType.ToDisplayString();
          binderBody.AppendLine(
            $@"    buffers[{bufferIndex}] = new ComputeBuffer({p.Name}.Length, System.Runtime.InteropServices.Marshal.SizeOf<{elemType}>());
    buffers[{bufferIndex}].SetData({p.Name});
    shader.SetBuffer(kernel, ""{p.Name}"", buffers[{bufferIndex}]);
");
          arrayNameToIndex[p.Name] = bufferIndex;
          bufferIndex++;
        }
        else {
          // handle scalars/constants
          binderBody.AppendLine($@"    shader.SetInt(""{p.Name}"", {p.Name});");
        }
      }

      binderBody.AppendLine(DimensionsSetCall(dispatchDims));

      var updaterBody = new StringBuilder();
      bufferIndex = 0;
      foreach (var p in realParameters) {
        if (p.Type is IArrayTypeSymbol && writtenBuffers.Contains(p.Name)) {
          updaterBody.AppendLine($"    buffers[{arrayNameToIndex[p.Name]}].GetData({p.Name});");
          bufferIndex++;
        }
      }

      updaterBody.AppendLine("    foreach (var b in buffers) b.Dispose();");
      var groupCount = dispatchDims.GetThreadGroupCount();

      return $@"using RomanSource.ShaderJob;
using UnityEngine;

public static class ComputeBinding_{id}
{{
  static ComputeBuffer[] buffers = new ComputeBuffer[{arrayBufferCount}];

  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
  static void Init()
  {{
    ShaderRegistry.Register<{typeArgs}>(
      key: {id},
      resourcesPath: ""Generated/Computes/{id}"",
      binder: Binder,
      updater: Updater,
      kernelIndex: 0,
      dispatchGroups: () => ({groupCount.X},{groupCount.Y},{groupCount.Z}));
  }}

  private static void Binder(ComputeShader shader, int kernel, {string.Join(", ", realParameters.Select(p => p.Type.ToDisplayString() + " " + p.Name))})
  {{
{binderBody}
  }}

  private static void Updater({string.Join(", ", realParameters.Select(p => p.Type.ToDisplayString() + " " + p.Name))})
  {{
{updaterBody}  }}
}}
";
    }

    private static string DimensionsSetCall(DispatchDims dims) {
      if (dims.Z > 1)
        return $"    shader.SetInts(\"_DispatchSize\", {dims.X}, {dims.Y}, {dims.Z});";
      if (dims.Y > 1)
        return $"    shader.SetInts(\"_DispatchSize\", {dims.X}, {dims.Y});";
      return $"    shader.SetInts(\"_DispatchSize\", {dims.X});";
    }
  }
}
