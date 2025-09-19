using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ShaderJob.Editor {
  internal class HlslEmitter : CSharpSyntaxVisitor<string> {
    public HlslEmitter((string Name, ITypeSymbol Type)[] parameters) {
      parameters.Select(p => p.Name).ToHashSet();
    }

    public override string VisitIdentifierName(IdentifierNameSyntax node)
      => node.Identifier.Text;

    public override string VisitElementAccessExpression(ElementAccessExpressionSyntax node) {
      var expr = Visit(node.Expression);
      var idx = string.Join(", ", node.ArgumentList.Arguments.Select(a => Visit(a.Expression)));
      return $"{expr}[{idx}]";
    }

    public override string VisitAssignmentExpression(AssignmentExpressionSyntax node) {
      var left = Visit(node.Left);
      var right = Visit(node.Right);
      return $"{left} = {right};";
    }

    public override string VisitBlock(BlockSyntax node) {
      var sb = new StringBuilder();
      foreach (var stmt in node.Statements)
        sb.AppendLine(Visit(stmt));
      return sb.ToString();
    }

    public override string VisitExpressionStatement(ExpressionStatementSyntax node)
      => Visit(node.Expression);

    public override string DefaultVisit(SyntaxNode node)
      => node.ToString();
  }
}
