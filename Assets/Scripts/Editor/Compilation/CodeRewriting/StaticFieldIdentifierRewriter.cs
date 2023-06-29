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
            if (!_staticFieldNamesFlat.Contains(node.Identifier.ToString()) || InLocalScope(node))
            {
                return base.VisitIdentifierName(node);
            }

            var typeDeclarationNode = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();

            if (typeDeclarationNode != null && _staticFieldNamesByNode.TryGetValue(typeDeclarationNode, out var staticFieldNames))
            {
                if (staticFieldNames.Contains(node.Identifier.ToString()))
                {
                    var syntaxNode = SyntaxFactory.ParseExpression($"{typeDeclarationNode.Identifier}.{node.Identifier}");
                    return AddRewriteCommentIfNeeded(syntaxNode, $"{nameof(StaticFieldIdentifierRewriter)}:{nameof(VisitIdentifierName)}").WithTriviaFrom(node);
                }
            }

            return base.VisitIdentifierName(node);
        }

        /// <summary>Check if exists as a parameter or local declaration. Does not check static field declarations</summary>
        private bool InLocalScope(IdentifierNameSyntax node)
        {
            if (node.Parent is MemberAccessExpressionSyntax memberAccess)
            {
                return true;
            }

            // Check if the identifier is a parameter name in a method declaration
            var methodDeclaration = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (methodDeclaration != null)
            {
                if (methodDeclaration.ParameterList.Parameters.Any(parameter => parameter.Identifier.ToString() == node.Identifier.ToString()))
                {
                    return true;
                }
            }

            // Check if the identifier is a local variable within the current method
            var localDeclaration = node.Ancestors().OfType<LocalDeclarationStatementSyntax>().FirstOrDefault();
            if (localDeclaration != null)
            {
                if (localDeclaration.Declaration.Variables.Any(variable => variable.Identifier.ToString() == node.Identifier.ToString()))
                {
                    return true;
                }
            }

            return false;
        }
    }
}