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
        public static readonly DiagnosticDescriptor CommandsEndWithCommand = new DiagnosticDescriptor("SS001", "The type name does not end with 'Command'",
            "The type name '{0}' should end in 'Command'", "Naming", DiagnosticSeverity.Warning, true, "Command names should end in 'Command'");

        public static readonly DiagnosticDescriptor EventsEndWithEvent = new DiagnosticDescriptor("SS002", "The type name does not end with 'Event'",
            "The type name '{0}' should end in 'Event'", "Naming", DiagnosticSeverity.Warning, true, "Command names should end in 'Event'");

        public static readonly DiagnosticDescriptor MessagePropertiesMustBeImmutable = new DiagnosticDescriptor("SS101", "The message property is mutable",
            "The message property '{0}' cannot be mutable.", "Messaging", DiagnosticSeverity.Error, true, "Messages properties cannot be mutable");

        public static readonly DiagnosticDescriptor MessagesCannotHaveFields = new DiagnosticDescriptor("SS102", "The message contains fields",
            "The message type '{0}' cannot have fields.", "Messaging", DiagnosticSeverity.Error, true, "Messages cannot have fields");

        public static readonly DiagnosticDescriptor UseCaseShouldHaveRules = new DiagnosticDescriptor("SS301", "The use case does not have any rules",
           "The use case '{0}' should have rules.", "Rules", DiagnosticSeverity.Warning, true, "Use cases should have rules", "http://slalom.com");

        public static readonly DiagnosticDescriptor UseCasesMustHaveImplementation = new DiagnosticDescriptor("SS302", "The use case does not have implementation",
           "The use case '{0}' should have implementation.", "Rules", DiagnosticSeverity.Error, true, "Use cases should have implementation", "http://slalom.com");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(CommandsEndWithCommand, EventsEndWithEvent, MessagePropertiesMustBeImmutable, MessagesCannotHaveFields, UseCaseShouldHaveRules, UseCasesMustHaveImplementation);

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
                if (target.ContainingType.HasBase("Event") && target.SetMethod?.DeclaredAccessibility == Accessibility.Public)
                {
                    var diagnostic = Diagnostic.Create(MessagePropertiesMustBeImmutable, target.Locations[0], target.Name);
                    context.ReportDiagnostic(diagnostic);
                }
                else
                {
                    var types = context.Compilation.GetSymbolsWithName(e => true).OfType<INamedTypeSymbol>().Where(e => e.BaseType?.TypeArguments.FirstOrDefault()?.Name == target.ContainingType.Name && e.HasBase("UseCase"));
                    if (types.Any() && target.SetMethod?.DeclaredAccessibility == Accessibility.Public)
                    {
                        var diagnostic = Diagnostic.Create(MessagePropertiesMustBeImmutable, target.Locations[0], target.Name);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
            else if (context.Symbol is IFieldSymbol)
            {
                var target = (IFieldSymbol)context.Symbol;
                if (target.ContainingType.HasBase("Event"))
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
            if (target.BaseType.Name == "Object")
            {
                //var types = context.Compilation.GetSymbolsWithName(e => true).OfType<INamedTypeSymbol>().Where(e => e.BaseType?.TypeArguments.FirstOrDefault()?.Name == target.Name && e.HasBase("UseCase"));
                //if (types.Any())
                //{
                //    if (target.IsMutable())
                //    {
                //        var diagnostic = Diagnostic.Create(MessagePropertiesMustBeImmutable, target.Locations[0], target.Name);
                //        context.ReportDiagnostic(diagnostic);
                //    }
                //    if (target.HasFields())
                //    {
                //        var diagnostic = Diagnostic.Create(MessagesCannotHaveFields, target.Locations[0], target.Name);
                //        context.ReportDiagnostic(diagnostic);
                //    }
                //}
            }
            else if (target.BaseType?.Name == "Event")
            {
                if (!target.Name.EndsWith("Event"))
                {
                    var diagnostic = Diagnostic.Create(EventsEndWithEvent, target.Locations[0], target.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
            else if (target.BaseType?.Name == "UseCase")
            {
                var command = target.BaseType.TypeArguments[0];
                var rules = context.Compilation.GetSymbolsWithName(e => true).OfType<INamedTypeSymbol>()
                    .Where(e => e.BaseType.Name == "BusinessRule");

                var business = rules.Where(e => e.BaseType?.TypeArguments.FirstOrDefault()?.Name == command.Name).ToList();
                if (!business.Any())
                {
                    var diagnostic = Diagnostic.Create(UseCaseShouldHaveRules, target.Locations[0], target.Name);
                    context.ReportDiagnostic(diagnostic);
                }
                if (!target.GetMembers().Any(e => e.Name == "Execute" || e.Name == "ExecuteAsync"))
                {
                    var diagnostic = Diagnostic.Create(UseCasesMustHaveImplementation, target.Locations[0], target.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}