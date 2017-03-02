using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Slalom.Stacks.CodeAnalysis
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StacksAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor CommandsEndWithCommand = new DiagnosticDescriptor("SS001", "The type name does not end with 'Command'", "The type name '{0}' does not end in 'Command'", "Naming", DiagnosticSeverity.Warning,
            true, "Command names should end in 'Command'");

        internal static readonly DiagnosticDescriptor EventsEndWithEvents = new DiagnosticDescriptor("SS002", "The type name does not end with 'Event'", "The type name '{0}' does not end in 'Event'", "Naming", DiagnosticSeverity.Warning,
          true, "Command names should end in 'Event'");

        internal static readonly DiagnosticDescriptor CommandPropertiesAreImmutable = new DiagnosticDescriptor("SS003", "The property is not immutable",
            "The property '{0}' cannot be mutable", "Messaging", DiagnosticSeverity.Error, true, "Command properties cannot be mutable");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(CommandsEndWithCommand, EventsEndWithEvents, CommandPropertiesAreImmutable);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(AnalyzeClass, SymbolKind.NamedType);
            context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);

            context.RegisterCompilationStartAction((compilation) =>
            {
                var symbols = compilation.Compilation.GetSymbolsWithName(e => true);

                foreach (var symbol in symbols)
                {
                }
            });
        }

        private void AnalyzeProperty(SymbolAnalysisContext context)
        {
            try
            {
                var target = (IPropertySymbol)context.Symbol;
                if (target.SetMethod?.DeclaredAccessibility == Accessibility.Public)
                {
                    var diagnostic = Diagnostic.Create(CommandPropertiesAreImmutable, target.Locations[0], target.Name);

                    context.ReportDiagnostic(diagnostic);
                }
            }
            catch
            {
            }
        }

        private static void AnalyzeClass(SymbolAnalysisContext context)
        {
            try
            {
                var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

                // Find just those named type symbols with names containing lowercase letters.
                if (namedTypeSymbol.BaseType?.Name == "Command" && !namedTypeSymbol.Name.EndsWith("Command"))
                {
                    // For all such symbols, produce a diagnostic.
                    var diagnostic = Diagnostic.Create(CommandsEndWithCommand, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

                    context.ReportDiagnostic(diagnostic);
                }
                else if (namedTypeSymbol.BaseType?.Name == "Event" && !namedTypeSymbol.Name.EndsWith("Event"))
                {
                    // For all such symbols, produce a diagnostic.
                    var diagnostic = Diagnostic.Create(EventsEndWithEvents, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

                    context.ReportDiagnostic(diagnostic);
                }
            }
            catch
            {
            }
        }
    }
}