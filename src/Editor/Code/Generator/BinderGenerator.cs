using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace RomanSource.ShaderJob.Editor {
  public static class BinderGenerator {
    public static string GenerateBinder(int id, (string Name, ITypeSymbol Type)[] parameters, HashSet<string> writtenBuffers) {
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

      binderBody.AppendLine(
@"    if (dispatchDims.Z > 1)
      shader.SetInts(""_DispatchSize"", dispatchDims.X, dispatchDims.Y, dispatchDims.Z);
    else if (dispatchDims.Y > 1)
      shader.SetInts(""_DispatchSize"", dispatchDims.X, dispatchDims.Y);
    else
      shader.SetInts(""_DispatchSize"", dispatchDims.X);");

      var updaterBody = new StringBuilder();
      bufferIndex = 0;
      foreach (var p in realParameters) {
        if (p.Type is IArrayTypeSymbol && writtenBuffers.Contains(p.Name)) {
          updaterBody.AppendLine($"    buffers[{arrayNameToIndex[p.Name]}].GetData({p.Name});");
          bufferIndex++;
        }
      }

      updaterBody.AppendLine("    foreach (var b in buffers) b.Dispose();");

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
      kernelIndex: 0);
  }}

  private static void Binder(ComputeShader shader, int kernel, in DispatchDims dispatchDims, {string.Join(", ", realParameters.Select(p => p.Type.ToDisplayString() + " " + p.Name))})
  {{
{binderBody}  }}

  private static void Updater({string.Join(", ", realParameters.Select(p => p.Type.ToDisplayString() + " " + p.Name))})
  {{
{updaterBody}  }}
}}
";
    }
  }
}
