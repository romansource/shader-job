using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class LambdaParser {
  public static IEnumerable<(InvocationExpressionSyntax invocation, string lambda, int line)> GetLambdaInvocations(SyntaxTree tree) {
    var root = tree.GetRoot();

    var calls = root.DescendantNodes()
      .OfType<InvocationExpressionSyntax>()
      .Where(LooksLikeLambdaInvocation);

    return calls.Select(x => new ValueTuple<InvocationExpressionSyntax, string, int>(x,
      SyntaxFactory.ParseExpression(x.ArgumentList.Arguments.Last().Expression.ToFullString().Trim())
        .NormalizeWhitespace().ToFullString(),
      x.GetLocation().GetLineSpan().StartLinePosition.Line + 1));
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