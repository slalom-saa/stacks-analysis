using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Slalom.Stacks.CodeAnalysis
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SlalomStacksCodeAnalysisCodeFixProvider)), Shared]
    public class SlalomStacksCodeAnalysisCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(StacksAnalyzer.CommandsEndWithCommand.Id, StacksAnalyzer.EventsEndWithEvents.Id, StacksAnalyzer.MessagePropertiesAreImmutable.Id, StacksAnalyzer.CommandShouldHaveRules.Id); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            if (diagnostic.Id == StacksAnalyzer.CommandsEndWithCommand.Id)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                // Find the type declaration identified by the diagnostic.
                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();

                // Register a code action that will invoke the fix.
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Append 'Command'",
                        createChangedSolution: c => this.AppendCommandText(context.Document, "Command", declaration, c),
                        equivalenceKey: "Append 'Command'"),
                    diagnostic);
            }
            else if (diagnostic.Id == StacksAnalyzer.EventsEndWithEvents.Id)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                // Find the type declaration identified by the diagnostic.
                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();

                // Register a code action that will invoke the fix.
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Append 'Event'",
                        createChangedSolution: c => this.AppendCommandText(context.Document, "Event", declaration, c),
                        equivalenceKey: "Append 'Event'"),
                    diagnostic);
            }
            else if (diagnostic.Id == StacksAnalyzer.MessagePropertiesAreImmutable.Id)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                var property = root.FindNode(diagnosticSpan) as PropertyDeclarationSyntax;

                // Register a code action that will invoke the fix.
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Remove setter",
                        createChangedDocument: async c =>
                        {
                            var previousWhiteSpacesToken = SF.Token(property.GetLeadingTrivia(), SyntaxKind.StringLiteralToken, SyntaxTriviaList.Empty);

                            var target = property.WithModifiers(SF.TokenList(previousWhiteSpacesToken, SF.Token(SyntaxKind.PublicKeyword)))
                                                 .WithAccessorList(SF.AccessorList(SF.List(new[]
                                                 {
                                                     SF.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                                       .WithSemicolonToken(SF.Token(SyntaxKind.SemicolonToken))
                                                 })));

                            var updated = await context.Document.GetSyntaxRootAsync(c);
                            return context.Document.WithSyntaxRoot(updated.ReplaceNode(property, new[] { target }));
                        },
                        equivalenceKey: "Remove setter"),
                    diagnostic);

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Make setter private",
                        createChangedDocument: async c =>
                        {
                            var previousWhiteSpacesToken = SF.Token(property.GetLeadingTrivia(), SyntaxKind.StringLiteralToken, SyntaxTriviaList.Empty);

                            var target = property.WithModifiers(SF.TokenList(previousWhiteSpacesToken, SF.Token(SyntaxKind.PublicKeyword)))
                                                 .WithAccessorList(SF.AccessorList(SF.List(new[]
                                                 {
                                                     SF.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                                       .WithSemicolonToken(SF.Token(SyntaxKind.SemicolonToken)),
                                                     SF.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                                       .WithModifiers(SF.TokenList(SF.Token(SyntaxKind.PrivateKeyword)))
                                                       .WithSemicolonToken(SF.Token(SyntaxKind.SemicolonToken))
                                                 })));

                            var updated = await context.Document.GetSyntaxRootAsync(c);
                            return context.Document.WithSyntaxRoot(updated.ReplaceNode(property, new[] { target }));
                        },
                        equivalenceKey: "Make setter private"),
                    diagnostic);
            }
            else if (diagnostic.Id == StacksAnalyzer.CommandShouldHaveRules.Id)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                var property = root.FindNode(diagnosticSpan) as ClassDeclarationSyntax;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Create business rule",
                        createChangedDocument: async c =>
                        {
                            //property.Identifier.ValueText;

                            var cu = SF.CompilationUnit().AddUsings(
                                SF.UsingDirective(SF.IdentifierName("System")),
                                SF.UsingDirective(SF.IdentifierName("Slalom.Stacks.Messaging.Validation")));


                            NamespaceDeclarationSyntax namespaceDeclarationSyntax = null;
                            if (!SyntaxNodeHelper.TryGetParentSyntax(property, out namespaceDeclarationSyntax))
                            {
                               
                            }

                            var ns = SF.NamespaceDeclaration(namespaceDeclarationSyntax.Name);

                            var cl = SF.ClassDeclaration("rule")
                                       .WithModifiers(SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)))
                                       .AddBaseListTypes(SF.SimpleBaseType(SF.ParseTypeName("BusinessRule<" + property.Identifier.ValueText + ">")));
                            ns = ns.AddMembers(cl);

                            cu = cu.AddMembers(ns);

                            SyntaxNode formattedNode = Formatter.Format(cu, context.Document.Project.Solution.Workspace);
                            StringBuilder sb = new StringBuilder();
                            using (StringWriter writer = new StringWriter(sb))
                            {
                                formattedNode.WriteTo(writer);
                            }

                            await Task.Delay(50);

                            var x = context.Document.Project.AddDocument("rule.cs", sb.ToString());

                            return x;
                        },
                        equivalenceKey: "Create business rule"),
                    diagnostic);
            }
        }

        static class SyntaxNodeHelper
        {
            public static bool TryGetParentSyntax<T>(SyntaxNode syntaxNode, out T result)
                where T : SyntaxNode
            {
                // set defaults
                result = null;

                if (syntaxNode == null)
                {
                    return false;
                }

                try
                {
                    syntaxNode = syntaxNode.Parent;

                    if (syntaxNode == null)
                    {
                        return false;
                    }

                    if (syntaxNode.GetType() == typeof(T))
                    {
                        result = syntaxNode as T;
                        return true;
                    }

                    return TryGetParentSyntax<T>(syntaxNode, out result);
                }
                catch
                {
                    return false;
                }
            }
        }



        private async Task<Solution> AppendCommandText(Document document, string text, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            // Compute new uppercase name.
            var identifierToken = typeDecl.Identifier;
            var newName = identifierToken.Text + text;

            // Get the symbol representing the type to be renamed.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

            // Produce a new solution that has all references to that type renamed, including the declaration.
            var originalSolution = document.Project.Solution;
            var optionSet = originalSolution.Workspace.Options;
            var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken).ConfigureAwait(false);
            // Return the new solution with the now-uppercase type name.
            return newSolution;
        }
    }
}