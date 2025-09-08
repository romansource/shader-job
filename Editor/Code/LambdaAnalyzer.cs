using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

class LambdaAnalyzer : CSharpSyntaxWalker
{
  private readonly SemanticModel _model;
  public readonly List<IParameterSymbol> WrittenArrays = new();
  public readonly List<IParameterSymbol> ReadArrays = new();
  public readonly List<IParameterSymbol> UsedConstants = new();

  public LambdaAnalyzer(SemanticModel model) 
    : base(SyntaxWalkerDepth.StructuredTrivia)
  {
    _model = model;
  }

  public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
  {
    // Check if the LHS is something like arr1[index]
    if (node.Left is ElementAccessExpressionSyntax elementAccess)
    {
      var symbol = _model.GetSymbolInfo(elementAccess.Expression).Symbol;
      if (symbol is IParameterSymbol paramSymbol)
      {
        WrittenArrays.Add(paramSymbol);
      }
    }
    base.VisitAssignmentExpression(node);
  }

  public override void VisitElementAccessExpression(ElementAccessExpressionSyntax node)
  {
    // arr1[index] as RHS (read)
    var symbol = _model.GetSymbolInfo(node.Expression).Symbol;
    if (symbol is IParameterSymbol paramSymbol)
    {
      ReadArrays.Add(paramSymbol);
    }
    base.VisitElementAccessExpression(node);
  }

  public override void VisitIdentifierName(IdentifierNameSyntax node)
  {
    // Check if it's a plain variable/constant
    var symbol = _model.GetSymbolInfo(node).Symbol;
    if (symbol is IParameterSymbol paramSymbol 
        && paramSymbol.Type.SpecialType != SpecialType.System_Array)
    {
      UsedConstants.Add(paramSymbol);
    }
    base.VisitIdentifierName(node);
  }
}