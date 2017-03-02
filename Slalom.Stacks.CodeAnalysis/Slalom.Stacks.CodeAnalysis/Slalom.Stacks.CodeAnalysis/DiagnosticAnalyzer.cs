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
        internal static readonly DiagnosticDescriptor CommandsEndWithCommand = new DiagnosticDescriptor("SS001", "The type name does not end with 'Command'",
            "The type name '{0}' should end in 'Command'", "Naming", DiagnosticSeverity.Warning, true, "Command names should end in 'Command'");

        internal static readonly DiagnosticDescriptor EventsEndWithEvents = new DiagnosticDescriptor("SS002", "The type name does not end with 'Event'",
            "The type name '{0}' should end in 'Event'", "Naming", DiagnosticSeverity.Warning, true, "Command names should end in 'Event'");

        internal static readonly DiagnosticDescriptor MessagePropertiesAreImmutable = new DiagnosticDescriptor("SS101", "The property is not immutable",
            "The property '{0}' cannot be mutable.", "Messaging", DiagnosticSeverity.Error, true, "Message properties cannot be mutable");

        internal static readonly DiagnosticDescriptor MessagesCannotHaveFields = new DiagnosticDescriptor("SS102", "The message contains fields",
            "The message type '{0}' cannot have fields.", "Messaging", DiagnosticSeverity.Error, true, "Messages cannot have fields");

        internal static readonly DiagnosticDescriptor CommandShouldHaveRules = new DiagnosticDescriptor("SS301", "The command does not have any rules",
           "The command '{0}' should have rules.", "Rules", DiagnosticSeverity.Warning, true, "Commands should have rules", "http://slalom.com");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(CommandsEndWithCommand, EventsEndWithEvents, MessagePropertiesAreImmutable, MessagesCannotHaveFields, CommandShouldHaveRules);

        public override void Initialize(AnalysisContext context)
        {

            context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
            context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Field);
            context.RegisterSymbolAction(AnalyzeClass, SymbolKind.NamedType);
        }

        private void AnalyzeProperty(SymbolAnalysisContext context)
        {
            if (context.Symbol is IPropertySymbol)
            {
                var target = (IPropertySymbol)context.Symbol;
                if (target.ContainingType.AllInterfaces.Any(e => e.Name == "IMessage") && target.SetMethod?.DeclaredAccessibility == Accessibility.Public)
                {
                    var diagnostic = Diagnostic.Create(MessagePropertiesAreImmutable, target.Locations[0], target.Name);

                    context.ReportDiagnostic(diagnostic);
                }
            }
            else if (context.Symbol is IFieldSymbol)
            {
                var target = (IFieldSymbol)context.Symbol;
                if (target.ContainingType.AllInterfaces.Any(e => e.Name == "IMessage"))
                {
                    var diagnostic = Diagnostic.Create(MessagesCannotHaveFields, target.Locations[0], target.ContainingType.Name);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private void AnalyzeClass(SymbolAnalysisContext context)
        {
            //var xxx = compilation.Compilation.GetSymbolsWithName(e => true);

            var target = (INamedTypeSymbol)context.Symbol;

            // Find just those named type symbols with names containing lowercase letters.
            if (target.BaseType?.Name == "Command")
            {
                if (!target.Name.EndsWith("Command"))
                {
                    // For all such symbols, produce a diagnostic.
                    var diagnostic = Diagnostic.Create(CommandsEndWithCommand, target.Locations[0], target.Name);

                    context.ReportDiagnostic(diagnostic);
                }

                var types = context.Compilation.GetSymbolsWithName(e => true).OfType<INamedTypeSymbol>().Where(e => e.AllInterfaces.Any(x => x.Name == "IValidate"));
                bool found = false;
                foreach (var item in types)
                {
                    if (item.BaseType?.TypeArguments.Any(x => x.Name == target.Name) ?? false)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    var diagnostic = Diagnostic.Create(CommandShouldHaveRules, target.Locations[0], target.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
            else if (target.BaseType?.Name == "Event" && !target.Name.EndsWith("Event"))
            {
                // For all such symbols, produce a diagnostic.
                var diagnostic = Diagnostic.Create(EventsEndWithEvents, target.Locations[0], target.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}