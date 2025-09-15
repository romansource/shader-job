using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class Generator {
  public static (string hlsl, string binderText) GenerateTexts(SyntaxTree tree, InvocationExpressionSyntax call, int shaderId, int binderId, DispatchDims dimensions) {
    var lambda = GetLambda(call);
    var model = GetModel(call, tree);
    (string Name, ITypeSymbol Type)[] parameters = ResolveLambdaParametersFromCall(call, lambda, model);

    var writtenBuffers = AnalyzeUsageByName(lambda, parameters);
    var hlsl = ShaderGenerator.GenerateHlsl(lambda, parameters, writtenBuffers, dimensions);
    var binderText = BinderGenerator.GenerateBinder(shaderId, binderId, parameters, writtenBuffers, parameters.Count(p => p.Type is IArrayTypeSymbol), dimensions);

    return (hlsl, binderText);
  }

  public static string GenerateShader(SyntaxTree tree, InvocationExpressionSyntax call, DispatchDims dimensions) {
    var lambda = GetLambda(call);
    var model = GetModel(call, tree);
    (string Name, ITypeSymbol Type)[] parameters = ResolveLambdaParametersFromCall(call, lambda, model);
    var writtenBuffers = AnalyzeUsageByName(lambda, parameters);
    
    return ShaderGenerator.GenerateHlsl(lambda, parameters, writtenBuffers, dimensions);
  }

  public static string GenerateBinder(SyntaxTree tree, InvocationExpressionSyntax call, int shaderId, int binderId, DispatchDims dimensions) {
    var lambda = GetLambda(call);
    var model = GetModel(call, tree);
    (string Name, ITypeSymbol Type)[] parameters = ResolveLambdaParametersFromCall(call, lambda, model);
    var writtenBuffers = AnalyzeUsageByName(lambda, parameters);
    var binderText = BinderGenerator.GenerateBinder(shaderId, binderId, parameters, writtenBuffers, parameters.Count(p => p.Type is IArrayTypeSymbol), dimensions);

    return binderText;
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

    // FIXED: Only pair lambda parameters with non-lambda arguments
    // The last argument is the lambda itself, so exclude it from pairing
    var nonLambdaArgs = args.Count - 1; // Assuming last arg is always the lambda
    var paired = Math.Min(lambdaParams.Length, nonLambdaArgs);

    for (int i = 0; i < paired; i++) {
      var name = lambdaParams[i].Identifier.Text;
      ITypeSymbol type = null;

      // Get type directly from symbol info first, then fallback to type info
      var argExpression = args[i].Expression;

      // Method 1: Try GetSymbolInfo approach first (more reliable for variables)
      var symbolInfo = model.GetSymbolInfo(argExpression);

      if (symbolInfo.Symbol != null) {
        type = symbolInfo.Symbol switch {
          ILocalSymbol localSym => localSym.Type,
          IFieldSymbol fieldSym => fieldSym.Type,
          IParameterSymbol paramSym => paramSym.Type,
          IPropertySymbol propSym => propSym.Type,
          _ => null
        };
      }

      // Method 2: Fallback to GetTypeInfo if symbol approach didn't work
      if (type == null) {
        var typeInfo = model.GetTypeInfo(argExpression);
        type = typeInfo.Type;
      }

#if DEBUG
      Console.WriteLine($"Arg {i}: '{argExpression}', Final Type: '{type?.ToDisplayString() ?? "null"}'");
#endif

      list.Add((name, type));
    }

    // Handle extra lambda parameters (like 'id')
    // if (lambdaParams.Length > paired) {
    //   for (int i = paired; i < lambdaParams.Length; i++) {
    //     var name = lambdaParams[i].Identifier.Text;
    //     ITypeSymbol type = model.Compilation.GetTypeByMetadataName("UnityEngine.Vector3Int");
    //     list.Add((name, type));
    //   }
    // }

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
}