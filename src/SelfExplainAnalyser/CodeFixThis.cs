using System;
using System.Composition;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace SelfExplainAnalyser
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CodeFixThis)), Shared]
    public class CodeFixThis : CodeFixProvider
    {
        private const string title = "Add this";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(ThisPropertyAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            Diagnostic diagnostic = context.Diagnostics.First();
            TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

            var tmp1 = root.FindToken(diagnosticSpan.Start);
            var tmp2 = (IdentifierNameSyntax)tmp1.Parent;

            // Find the type declaration identified by the diagnostic.
            //TypeDeclarationSyntax declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => MakeUppercaseAsync(context.Document, tmp2, tmp1.ValueText, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Document> MakeUppercaseAsync(Document document, IdentifierNameSyntax identifierName, string propertyName, CancellationToken cancellationToken)
        {
            var newLiteral = SyntaxFactory.ParseExpression($"this.{propertyName}")
                .WithLeadingTrivia(identifierName.GetLeadingTrivia())
                .WithTrailingTrivia(identifierName.GetTrailingTrivia());

            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(identifierName, newLiteral);

            var newDocument = document.WithSyntaxRoot(newRoot);

            // use this to debug the result
            //var semanticModel2 = await newDocument.GetSemanticModelAsync(cancellationToken);

            return newDocument;
        }
    }
}