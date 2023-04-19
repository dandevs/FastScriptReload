using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FastScriptReload.Editor.Compilation.CodeRewriting
{
    class ThisCallRewriter : ThisRewriterBase
    {
        public ThisCallRewriter(bool writeRewriteReasonAsComment, bool visitIntoStructuredTrivia = false) 
            : base(writeRewriteReasonAsComment, visitIntoStructuredTrivia)
        {
        }

        public override SyntaxNode VisitThisExpression(ThisExpressionSyntax node)
        {
            if (node.Parent is ArgumentSyntax)
            {   
                return CreateCastedThisExpression(node);
            }

            return base.VisitThisExpression(node);
        }

        public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            if (node.Right is ThisExpressionSyntax)
            {
                var typeDelcaration = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();

                if (typeDelcaration != null)
                    return node.WithRight(SyntaxFactory.ParseExpression($"({typeDelcaration.Identifier.Text})(object)this"));
            }

            return base.VisitAssignmentExpression(node);
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var typeDeclarationNode = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();

            if (typeDeclarationNode != null && node.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Expression is ThisExpressionSyntax thisExpr)
            {
                var containsMember = typeDeclarationNode.Members.Any(m => m is MethodDeclarationSyntax method && method.Identifier.Text == memberAccess.Name.Identifier.Text);

                if (!containsMember)
                {
                    var newExpression = SyntaxFactory.ParseExpression($"(({typeDeclarationNode.Identifier.Text})(object)this).{memberAccess.Name}");
                    return node.WithExpression(newExpression);
                }
            }

            return base.VisitInvocationExpression(node);
        }

        public override SyntaxNode VisitReturnStatement(ReturnStatementSyntax node) {
            if (node.Expression is ThisExpressionSyntax thisExpr)
            {
                var typeDelcaration = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();

                if (typeDelcaration != null)
                    return node.WithExpression(SyntaxFactory.ParseExpression($"({typeDelcaration.Identifier.Text})(object)this"));
            }

            return base.VisitReturnStatement(node);
        }
    }
}