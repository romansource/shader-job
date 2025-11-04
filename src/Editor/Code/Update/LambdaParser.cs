using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RomanSource.ShaderJob.Editor {
  public static class LambdaParser
  {
    public static IEnumerable<(InvocationExpressionSyntax invocation, string lambda, int line)> GetLambdaInvocations(SyntaxTree tree)
    {
      var root = tree.GetRoot();

      var calls = root.DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Where(LooksLikeLambdaInvocation);

      return calls.Select(x => {
        var lambda = ExtractLambdaExpression(x);
        var line = x.Expression is MemberAccessExpressionSyntax ma && IsRunIdentifier(ma.Name)
          ? ma.Name.GetLocation().GetLineSpan().StartLinePosition.Line + 1
          : x.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

        return (x, lambda, line);
      });
    }

    public static int GetForArgumentCount(InvocationExpressionSyntax invocation) {
      // Walk up the expression tree to find ShaderJob.For(...)
      var current = invocation.Expression;

      while (current != null) {
        if (current is MemberAccessExpressionSyntax ma) {
          // Check if this is a .Run() call
          if (IsRunIdentifier(ma.Name) && ma.Expression is InvocationExpressionSyntax forInvocation) {
            // Check if the invocation is ShaderJob.For(...)
            if (forInvocation.Expression is MemberAccessExpressionSyntax forMa && IsForIdentifier(forMa.Name)) {
              return forInvocation.ArgumentList?.Arguments.Count ?? 1;
            }
          }
          current = ma.Expression;
        }
        else {
          break;
        }
      }

      UnityEngine.Debug.LogWarning("Could not determine for argument count");
      return 1;
    }

    private static string ExtractLambdaExpression(InvocationExpressionSyntax invocation)
    {
      try
      {
        if (invocation.ArgumentList?.Arguments.Count == 0)
          return string.Empty;

        var lastArg = invocation.ArgumentList.Arguments.Last();
        if (lastArg?.Expression == null)
          return string.Empty;

        // More robust lambda extraction
        var lambdaText = lastArg.Expression.ToFullString().Trim();
        if (string.IsNullOrWhiteSpace(lambdaText))
          return string.Empty;

        var parsed = SyntaxFactory.ParseExpression(lambdaText);
        return parsed.NormalizeWhitespace().ToFullString();
      }
      catch
      {
        // Fallback to raw text if parsing fails
        return invocation.ArgumentList?.Arguments.LastOrDefault()?.Expression?.ToFullString()?.Trim() ?? string.Empty;
      }
    }

    private static bool LooksLikeLambdaInvocation(InvocationExpressionSyntax inv)
    {
      if (inv?.Expression == null || inv.ArgumentList?.Arguments.Count == 0)
        return false;

      // Must have at least one argument that looks like a lambda
      var lastArg = inv.ArgumentList.Arguments.LastOrDefault()?.Expression;
      if (!IsLikelyLambdaExpression(lastArg))
        return false;

      var callee = inv.Expression;

      // Handle direct calls: Run(...) or Run<T>(...)
      if (IsRunIdentifier(callee))
        return true;

      // Handle member access: ShaderJob.Run(...) or builder.Run(...)
      if (callee is MemberAccessExpressionSyntax ma)
      {
        // Check if the method name is "Run"
        if (!IsRunIdentifier(ma.Name))
          return false;

        // Check if it's called on ShaderJob or a builder (ShaderJob.For(...).Run)
        return IsShaderJobRelated(ma.Expression);
      }

      return false;
    }

    private static bool IsLikelyLambdaExpression(ExpressionSyntax expr)
    {
      if (expr == null) return false;

      // Check for lambda expressions
      return expr is SimpleLambdaExpressionSyntax ||
             expr is ParenthesizedLambdaExpressionSyntax ||
             // Also check for delegate expressions or method references
             expr is AnonymousMethodExpressionSyntax;
    }

    private static bool IsRunIdentifier(ExpressionSyntax expr)
    {
      return expr switch
      {
        IdentifierNameSyntax id => id.Identifier.ValueText == "Run",
        GenericNameSyntax gen => gen.Identifier.ValueText == "Run",
        _ => false
      };
    }

    private static bool IsShaderJobRelated(ExpressionSyntax expr)
    {
      // Walk the expression chain to find ShaderJob
      var current = expr;

      while (current != null)
      {
        switch (current)
        {
          case IdentifierNameSyntax id:
            return id.Identifier.ValueText == "ShaderJob";

          case GenericNameSyntax gen:
            return gen.Identifier.ValueText == "ShaderJob";

          case MemberAccessExpressionSyntax ma:
            // Check if this is ShaderJob.For(...) pattern
            if (GetRightmostName(ma.Expression) == "ShaderJob" &&
                IsForIdentifier(ma.Name))
              return true;
            current = ma.Expression;
            continue;

          case InvocationExpressionSyntax inv:
            // Handle ShaderJob.For(10) where For(10) is an invocation
            return IsShaderJobRelated(inv.Expression);

          default:
            return false;
        }
      }

      return false;
    }

    private static bool IsForIdentifier(ExpressionSyntax expr)
    {
      return expr switch
      {
        IdentifierNameSyntax id => id.Identifier.ValueText == "For",
        GenericNameSyntax gen => gen.Identifier.ValueText == "For",
        _ => false
      };
    }

    private static string GetRightmostName(ExpressionSyntax expr)
    {
      if (expr == null) return string.Empty;

      // Walk down the rightmost identifier through member accesses
      var current = expr;
      while (current != null)
      {
        switch (current)
        {
          case IdentifierNameSyntax id:
            return id.Identifier.ValueText;

          case GenericNameSyntax gen:
            return gen.Identifier.ValueText;

          case MemberAccessExpressionSyntax ma:
            current = ma.Name; // peel right side
            continue;

          case InvocationExpressionSyntax inv:
            current = inv.Expression;
            continue;

          default:
            // Safer fallback
            var lastToken = current.GetLastToken();
            return lastToken.IsKind(SyntaxKind.IdentifierToken) ? lastToken.ValueText : string.Empty;
        }
      }

      return string.Empty;
    }
  }
}
