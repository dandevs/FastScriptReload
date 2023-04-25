using System.Linq;
using ImmersiveVrToolsCommon.Runtime.Logging;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FastScriptReload.Editor.Compilation.CodeRewriting
{
    class StaticFieldIdentifierRewriter : FastScriptReloadCodeRewriterBase
    {
        private Dictionary<SyntaxNode, HashSet<string>> _staticFieldNamesByNode = new();
        private HashSet<string> _staticFieldNamesFlat = new();

        public StaticFieldIdentifierRewriter(bool writeRewriteReasonAsComment)
            : base(writeRewriteReasonAsComment)
        {
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            var isStatic = node.Modifiers.Any(m => m.Kind() == SyntaxKind.StaticKeyword);
            var hasPrivateModifier = node.Modifiers.Any(m => m.Kind() == SyntaxKind.PrivateKeyword);
            var hasPublicModifier = node.Modifiers.Any(m => m.Kind() == SyntaxKind.PublicKeyword);
            var isPrivate = hasPrivateModifier || (!hasPublicModifier && !hasPrivateModifier);

            if (isStatic && !isPrivate)
            {
                var typeDeclarationNode = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();

                if (!_staticFieldNamesByNode.TryGetValue(typeDeclarationNode, out var staticFieldNames))
                {
                    staticFieldNames = new HashSet<string>();
                    _staticFieldNamesByNode.Add(typeDeclarationNode, staticFieldNames);
                }

                var fieldIdentifierNames = node.Declaration.Variables.Select(v => v.Identifier.ToString());

                staticFieldNames.UnionWith(fieldIdentifierNames);
                _staticFieldNamesFlat.UnionWith(fieldIdentifierNames);
            }

            return base.VisitFieldDeclaration(node);
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node) 
        {
            // Simple optimization to avoid unnecessary parsing
            if (!_staticFieldNamesFlat.Contains(node.Identifier.ToString()))
                return base.VisitIdentifierName(node);

            var typeDeclarationNode = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();

            if (typeDeclarationNode != null && _staticFieldNamesByNode.TryGetValue(typeDeclarationNode, out var staticFieldNames))
            {
                if (staticFieldNames.Contains(node.Identifier.ToString()))
                {
                    // Check if the parent is a MemberAccessExpressionSyntax and its Expression is the same as the typeDeclarationNode.Identifier
                    if (node.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Expression.ToString() == typeDeclarationNode.Identifier.ToString())
                    {
                        return base.VisitIdentifierName(node);
                    }

                    var syntaxNode = SyntaxFactory.ParseExpression($"{typeDeclarationNode.Identifier}.{node.Identifier}");
                    return AddRewriteCommentIfNeeded(syntaxNode, $"{nameof(StaticFieldIdentifierRewriter)}:{nameof(VisitIdentifierName)}").WithTriviaFrom(node);
                }
            }

            return base.VisitIdentifierName(node);
        }
    }
}