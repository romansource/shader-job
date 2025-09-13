using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

public static class BinderGenerator {
  public static string GenerateBinder(int shaderId, (string Name, ITypeSymbol Type)[] parameters, HashSet<string> writtenBuffers, int bufferCount) {
    var realParameters = parameters
      .Where(p => p.Type != null)
      .ToArray();

    var typeArgs = string.Join(", ",
      realParameters.Select(p =>
        p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

    var binderBody = new StringBuilder();
    int bufferIndex = 0;
    foreach (var p in realParameters) {
      if (p.Type is IArrayTypeSymbol arrType) {
        var elemType = arrType.ElementType.ToDisplayString();
        binderBody.AppendLine($@"
        buffers[{bufferIndex}] = new ComputeBuffer({p.Name}.Length, System.Runtime.InteropServices.Marshal.SizeOf<{elemType}>());
        buffers[{bufferIndex}].SetData({p.Name});
        shader.SetBuffer(kernel, ""{p.Name}"", buffers[{bufferIndex}]);");
        bufferIndex++;
      }
      else {
        // TODO: handle scalars/constants
        binderBody.AppendLine($@"        shader.SetInt(""{p.Name}"", {p.Name});");
      }
    }

    var updaterBody = new StringBuilder();
    bufferIndex = 0;
    foreach (var p in realParameters) {
      if (p.Type is IArrayTypeSymbol && writtenBuffers.Contains(p.Name)) {
        updaterBody.AppendLine($@"        buffers[{bufferIndex}].GetData({p.Name});");
        bufferIndex++;
      }
    }

    updaterBody.AppendLine("        foreach (var b in buffers) b.Dispose();");

    return $@"using UnityEngine;

public static class ComputeBinding_{shaderId}
{{
    static ComputeBuffer[] buffers = new ComputeBuffer[{bufferCount}];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    static void Init()
    {{
        ShaderRegistry.Register<{typeArgs}>(
            {shaderId},
            ""Generated/Computes/{shaderId}"",
            Binder,
            Updater,
            /* kernelIndex */ 0,
            () => (1,1,1),
            {bufferCount});
    }}

    private static void Binder(ComputeShader shader, int kernel, {string.Join(", ", realParameters.Select(p => p.Type.ToDisplayString() + " " + p.Name))})
    {{ {binderBody}    }}

    private static void Updater({string.Join(", ", realParameters.Select(p => p.Type.ToDisplayString() + " " + p.Name))})
    {{ {updaterBody}    }}
}}
";
  }
}