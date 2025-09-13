using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class ShaderGenerator {
  public static string GenerateHlsl(LambdaExpressionSyntax lambda, (string Name, ITypeSymbol Type)[] parameters, HashSet<string> writtenBuffers) {
    var sb = new StringBuilder();
    sb.AppendLine("// Auto-generated HLSL");
    sb.AppendLine("#pragma kernel CSMain");
    sb.AppendLine();

    var scalarParams = new List<string>();
    foreach (var (name, type) in parameters) {
      if (type == null) {
        UnityEngine.Debug.LogWarning($"Type for parameter '{name}' is null, skipping declaration.");
        continue;
      }

      if (type is IArrayTypeSymbol arr) {
        var elemType = arr.ElementType.SpecialType switch {
          SpecialType.System_Int32 => "int",
          SpecialType.System_Single => "float",
          _ => arr.ElementType.Name
        };

        bool written = writtenBuffers.Contains(name);
        string decl = written
          ? $"RWStructuredBuffer<{elemType}> {name};"
          : $"StructuredBuffer<{elemType}> {name};";
        sb.AppendLine(decl);
      }
      else if (type.SpecialType == SpecialType.System_Int32 || type.SpecialType == SpecialType.System_Single) {
        scalarParams.Add(name);
      }
      else {
        sb.AppendLine($"// {name} : {type.Name} (unsupported)");
      }
    }

    foreach (var name in scalarParams) {
      var type = parameters.First(p => p.Name == name).Type;
      string hlslType = type.SpecialType switch {
        SpecialType.System_Int32 => "int",
        SpecialType.System_Single => "float",
        _ => "float" // fallback
      };
      sb.AppendLine($"{hlslType} {name};");
    }

    sb.AppendLine();
    sb.AppendLine("[numthreads(64,1,1)]");
    sb.AppendLine("void CSMain(uint3 id : SV_DispatchThreadID)");
    sb.AppendLine("{");

    var visitor = new HlslEmitter(parameters);
    string body = visitor.Visit(lambda.Body);

    foreach (var line in body.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
      sb.AppendLine("    " + line);

    sb.AppendLine("}");
    return sb.ToString();
  }
}