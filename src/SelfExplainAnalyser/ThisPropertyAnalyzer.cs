using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SelfExplainAnalyser
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ThisPropertyAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SE0001";
        internal static readonly LocalizableString Title = "All access of properties must begin with the keyword this";
        internal static readonly LocalizableString MessageFormat = "The keyword this is missing for a call on the property '{0}'";
        internal const string Category = "Missing this keyword";

        public ThisPropertyAnalyzer()
        {
            this.PropertyNames = new HashSet<string>();
        }

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        private HashSet<string> PropertyNames { get; }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(c => this.PropertyNames.Add(c.Symbol.Name), SymbolKind.Property);
            context.RegisterCodeBlockAction(this.FindPropetyUsage);
        }

        private void FindPropetyUsage(CodeBlockAnalysisContext context)
        {
            var model = context.SemanticModel;
            var syntaxNode = context.CodeBlock;

            var identifierNames = syntaxNode.DescendantNodes()
                                            .OfType<IdentifierNameSyntax>();

            foreach (var identifier in identifierNames)
            {
                if (this.PropertyNames.Contains(identifier.Identifier.Value))
                {
                    if (!(identifier.Parent is MemberAccessExpressionSyntax))
                    {
                        var diagnostic = Diagnostic.Create(Rule, identifier.GetLocation(), identifier.Identifier.Value);

                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}