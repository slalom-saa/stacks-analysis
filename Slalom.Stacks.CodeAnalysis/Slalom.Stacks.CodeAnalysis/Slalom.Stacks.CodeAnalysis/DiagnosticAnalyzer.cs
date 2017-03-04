using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Slalom.Stacks.CodeAnalysis
{
    public static class Extensions
    {
        public static IEnumerable<INamedTypeSymbol> AllBases(this INamedTypeSymbol instance)
        {
            yield return instance.BaseType;
            if (instance.BaseType.Name != "Object")
            {
                foreach (var parent in instance.AllBases())
                {
                    yield return parent;
                }
            }
        }

        public static bool HasBase(this INamedTypeSymbol instance, string name)
        {
            return instance.AllBases().Any(e => e.Name == name);
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StacksAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor CommandsEndWithCommand = new DiagnosticDescriptor("SS001", "The type name does not end with 'Command'",
            "The type name '{0}' should end in 'Command'", "Naming", DiagnosticSeverity.Warning, true, "Command names should end in 'Command'");

        public static readonly DiagnosticDescriptor EventsEndWithEvents = new DiagnosticDescriptor("SS002", "The type name does not end with 'Event'",
            "The type name '{0}' should end in 'Event'", "Naming", DiagnosticSeverity.Warning, true, "Command names should end in 'Event'");

        public static readonly DiagnosticDescriptor MessagePropertiesAreImmutable = new DiagnosticDescriptor("SS101", "The property is not immutable",
            "The property '{0}' cannot be mutable.", "Messaging", DiagnosticSeverity.Error, true, "Message properties cannot be mutable");

        public static readonly DiagnosticDescriptor MessagesCannotHaveFields = new DiagnosticDescriptor("SS102", "The message contains fields",
            "The message type '{0}' cannot have fields.", "Messaging", DiagnosticSeverity.Error, true, "Messages cannot have fields");

        public static readonly DiagnosticDescriptor UseCaseShouldHaveRules = new DiagnosticDescriptor("SS301", "The use case does not have any rules",
           "The use case '{0}' should have rules.", "Rules", DiagnosticSeverity.Warning, true, "Use cases should have rules", "http://slalom.com");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(CommandsEndWithCommand, EventsEndWithEvents, MessagePropertiesAreImmutable, MessagesCannotHaveFields, UseCaseShouldHaveRules);

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
            if (target.BaseType.Name == "Object")
            {
                var types = context.Compilation.GetSymbolsWithName(e => true).OfType<INamedTypeSymbol>().Where(e => e.BaseType?.TypeArguments.FirstOrDefault()?.Name == target.Name);
                foreach (var item in types)
                {
                    if (item.BaseType.Name == "UseCase")
                    {
                        var diagnostic = Diagnostic.Create(CommandsEndWithCommand, target.Locations[0], target.Name);
                        context.ReportDiagnostic(diagnostic);
                    }
                }



                //if (!target.Name.EndsWith("Command"))
                //{
                //    // For all such symbols, produce a diagnostic.
                //    var diagnostic = Diagnostic.Create(CommandsEndWithCommand, target.Locations[0], target.Name);

                //    context.ReportDiagnostic(diagnostic);
                //}

                //var types = context.Compilation.GetSymbolsWithName(e => true).OfType<INamedTypeSymbol>().Where(e => e.AllInterfaces.Any(x => x.Name == "IValidate"));
                //bool found = false;
                //foreach (var item in types)
                //{
                //    if (item.BaseType?.TypeArguments.Any(x => x.Name == target.Name) ?? false)
                //    {
                //        found = true;
                //        break;
                //    }
                //}
                //if (!found)
                //{
                //    var diagnostic = Diagnostic.Create(CommandShouldHaveRules, target.Locations[0], target.Name);
                //    context.ReportDiagnostic(diagnostic);
                //}
            }
            else if (target.BaseType?.Name == "Event" && !target.Name.EndsWith("Event"))
            {
                // For all such symbols, produce a diagnostic.
                var diagnostic = Diagnostic.Create(EventsEndWithEvents, target.Locations[0], target.Name);

                context.ReportDiagnostic(diagnostic);
            }
            else if (target.BaseType?.Name == "UseCase")
            {
                var command = target.BaseType.TypeArguments[0];
                var rules = context.Compilation.GetSymbolsWithName(e => true).OfType<INamedTypeSymbol>()
                    .Where(e => e.BaseType.Name == "BusinessRule");

                var business = rules.Where(e => e.BaseType?.TypeArguments.FirstOrDefault()?.Name == command.Name);
                if (!business.Any())
                {
                    var diagnostic = Diagnostic.Create(UseCaseShouldHaveRules, target.Locations[0], target.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}