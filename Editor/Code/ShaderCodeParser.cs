using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class ShaderCodeParser {
  public static IEnumerable<(InvocationExpressionSyntax invocation, string lambda, int line)> GetLambdaInvocations(SyntaxTree tree) {
    var root = tree.GetRoot();

    var calls = root.DescendantNodes()
      .OfType<InvocationExpressionSyntax>()
      .Where(LooksLikeLambdaInvocation);

    return calls.Select(x => new ValueTuple<InvocationExpressionSyntax, string, int>(x,
      x.ArgumentList.Arguments.Last().Expression.ToFullString().Trim(),
      x.GetLocation().GetLineSpan().StartLinePosition.Line + 1));
  }

  public static (string hlsl, string binderText) GenerateTexts(SyntaxTree tree, InvocationExpressionSyntax call, int id) {
    var lambda = GetLambda(call);
    var model = GetModel(call, tree);
    (string Name, ITypeSymbol Type)[] parameters = ResolveLambdaParametersFromCall(call, lambda, model);
    
    foreach (var p in parameters)
      UnityEngine.Debug.Log($"{p.Name} : {p.Type?.ToDisplayString() ?? "<null>"}");

    var writtenBuffers = AnalyzeUsageByName(lambda, parameters);
    var hlsl = GenerateHlsl(lambda, model, parameters, writtenBuffers);
    var binderText = GenerateBinder(id, parameters, writtenBuffers, parameters.Count(p => p.Type is IArrayTypeSymbol));

    return (hlsl, binderText);
  }

  private static LambdaExpressionSyntax GetLambda(InvocationExpressionSyntax call) =>
    call.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().FirstOrDefault()
    ?? (LambdaExpressionSyntax)call.DescendantNodes().OfType<SimpleLambdaExpressionSyntax>()
      .FirstOrDefault();

  private static SemanticModel GetModel(InvocationExpressionSyntax call, SyntaxTree tree) {
    var refs = new List<MetadataReference> {
      MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(UnityEngine.Vector3).Assembly.Location),
      MetadataReference.CreateFromFile(typeof(UnityEngine.ComputeBuffer).Assembly.Location),
    };

    var compilation = CSharpCompilation.Create(
      "ComputeAnalysis",
      new[] { tree },
      refs,
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    
    return compilation.GetSemanticModel(tree, ignoreAccessibility: true);
  }

  private static (string Name, ITypeSymbol Type)[] ResolveLambdaParametersFromCall(InvocationExpressionSyntax call, LambdaExpressionSyntax lambda, SemanticModel model) {
    var lambdaParams = lambda switch {
      ParenthesizedLambdaExpressionSyntax paren => paren.ParameterList.Parameters.ToArray(),
      SimpleLambdaExpressionSyntax simple => new[] { simple.Parameter },
      _ => Array.Empty<ParameterSyntax>()
    };

    var args = call.ArgumentList.Arguments;
    var list = new List<(string, ITypeSymbol)>();
    var paired = Math.Min(lambdaParams.Length, args.Count);
    
    for (int i = 0; i < paired; i++) {
      var name = lambdaParams[i].Identifier.Text;
      var type = model.GetTypeInfo(args[i].Expression).Type;
      list.Add((name, type));
    }
    
    for (int i = paired; i < lambdaParams.Length; i++) {
      var name = lambdaParams[i].Identifier.Text;
      ITypeSymbol type = null;

      if (name == "id")
        type = model.Compilation.GetTypeByMetadataName("UnityEngine.Vector3Int");

      list.Add((name, type));
    }

    return list.ToArray();
  }

  private static HashSet<string> AnalyzeUsageByName(LambdaExpressionSyntax lambda, (string Name, ITypeSymbol Type)[] parameters) {
    var arrayNames = new HashSet<string>(parameters.Where(p => p.Type is IArrayTypeSymbol).Select(p => p.Name));
    var written = new HashSet<string>();

    var body = lambda.Body;
    
    foreach (var assign in body.DescendantNodes().OfType<AssignmentExpressionSyntax>()) {
      if (assign.Left is ElementAccessExpressionSyntax ea &&
          ea.Expression is IdentifierNameSyntax ident &&
          arrayNames.Contains(ident.Identifier.Text)) {
        written.Add(ident.Identifier.Text);
      }
    }

    return written;
  }

  private static string GenerateHlsl(LambdaExpressionSyntax lambda, SemanticModel model, (string Name, ITypeSymbol Type)[] parameters, HashSet<string> writtenBuffers) {
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
    
    if (scalarParams.Count > 0) {
      sb.AppendLine();
      sb.AppendLine("cbuffer Params");
      sb.AppendLine("{");
      foreach (var name in scalarParams) {
        var type = parameters.First(p => p.Name == name).Type;
        string hlslType = type.SpecialType switch {
          SpecialType.System_Int32 => "int",
          SpecialType.System_Single => "float",
          _ => "float" // fallback
        };
        sb.AppendLine($"    {hlslType} {name};");
      }

      sb.AppendLine("}");
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

  private static string GenerateBinder(int shaderId, (string Name, ITypeSymbol Type)[] parameters, HashSet<string> writtenBuffers, int bufferCount) {
    var realParameters = parameters
      .Where(p => p.Type != null) // drop id, because no type
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
      }

      bufferIndex++;
    }

    updaterBody.AppendLine("        foreach (var b in buffers) b.Dispose();");

    return $@"
using UnityEngine;
using System;

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
    {{
{binderBody}
    }}

    private static void Updater({string.Join(", ", realParameters.Select(p => p.Type.ToDisplayString() + " " + p.Name))})
    {{
{updaterBody}
    }}
}}
";
  }


  private static bool LooksLikeLambdaInvocation(InvocationExpressionSyntax inv) {
    var callee = inv.Expression;

    // Run(...)  — when `using static ShaderJob;`
    if (callee is IdentifierNameSyntax id)
      return id.Identifier.Text == "Run";

    // Run<T>(...)  — still just an identifier but generic
    if (callee is GenericNameSyntax genId)
      return genId.Identifier.Text == "Run";

    // ShaderJob.Run(...)  or  Namespace.ShaderJob.Run<T>(...)
    if (callee is MemberAccessExpressionSyntax ma) {
      // Right side must be "ShaderJob" (with or without generics)
      var rightName =
        (ma.Name as IdentifierNameSyntax)?.Identifier.Text ??
        (ma.Name as GenericNameSyntax)?.Identifier.Text;
      if (rightName != "Run") return false;

      // Left side should end with "Run" (identifier at the far right of the chain)
      return GetRightmostName(ma.Expression) == "ShaderJob";
    }

    return false;
  }

  private static string GetRightmostName(ExpressionSyntax expr) {
    // Walk down the rightmost identifier through member accesses
    while (true) {
      switch (expr) {
        case IdentifierNameSyntax id:
          return id.Identifier.Text;

        case GenericNameSyntax gen:
          return gen.Identifier.Text;

        case MemberAccessExpressionSyntax ma:
          expr = ma.Name; // peel right side
          continue;

        default:
          // Fallback: last token text (handles rare shapes)
          return expr.GetLastToken().ValueText;
      }
    }
  }
}