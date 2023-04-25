using System.Linq;
using FastScriptReload.Runtime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FastScriptReload.Editor.Compilation.CodeRewriting
{
    class ReturnRewriter : FastScriptReloadCodeRewriterBase
    {
        private string _currentReturnType;
        private string _currentClassName;

        public ReturnRewriter(bool writeRewriteReasonAsComment) : base(writeRewriteReasonAsComment)
        {
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            _currentClassName = node.Identifier.Text;
            return base.VisitClassDeclaration(node);
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            _currentReturnType = node.ReturnType.ToString();
            return base.VisitMethodDeclaration(node);
        }

        // public override SyntaxNode VisitReturnStatement(ReturnStatementSyntax node)
        // {
        //     if (node.Expression != null && _currentReturnType == _currentClassName)
        //     {
        //         var newReturnExpression = SyntaxFactory.ParseExpression($"({_currentReturnType + AssemblyChangesLoader.ClassnamePatchedPostfix})(object){node.Expression}");
        //         var newReturnStatement = node.WithExpression(newReturnExpression);
        //         return newReturnStatement;
        //     }
        //     return base.VisitReturnStatement(node);
        // }
    }
}
