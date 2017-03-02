using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
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

namespace Slalom.Stacks.CodeAnalysis
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SlalomStacksCodeAnalysisCodeFixProvider)), Shared]
    public class SlalomStacksCodeAnalysisCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(StacksAnalyzer.CommandsEndWithCommand.Id, StacksAnalyzer.EventsEndWithEvents.Id, StacksAnalyzer.CommandPropertiesAreImmutable.Id); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            try
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
                //else if (diagnostic.Id == StacksAnalyzer.CommandPropertiesAreImmutable.Id)
                //{
                //    var diagnosticSpan = diagnostic.Location.SourceSpan;

                //    var property = root.FindNode(diagnosticSpan) as PropertyDeclarationSyntax;

                //    // Register a code action that will invoke the fix.
                //    context.RegisterCodeFix(
                //        CodeAction.Create(
                //            title: "Remove setter",
                //            createChangedDocument: c => this.ReplacePropertyModifierAsync(context.Document, property, SyntaxKind.PrivateKeyword, c),
                //            equivalenceKey: "Remove setter"),
                //        diagnostic);
                //}
            }
            catch
            {
            }
        }

        //private SyntaxToken _whitespaceToken = SyntaxFactory.Token(SyntaxTriviaList.Create(SyntaxFactory.Space), SyntaxKind.StringLiteralToken, SyntaxTriviaList.Empty);


        //private async Task<Document> ReplacePropertyModifierAsync(Document document, PropertyDeclarationSyntax property, SyntaxKind propertyModifier, CancellationToken cancellationToken)
        //{
        //    var previousWhiteSpacesToken = SyntaxFactory.Token(property.GetLeadingTrivia(), SyntaxKind.StringLiteralToken, SyntaxTriviaList.Empty);

        //    var newProperty = property.WithModifiers(SyntaxTokenList.Create(previousWhiteSpacesToken)
        //        .Add(SyntaxFactory.Token(propertyModifier))
        //        .Add(_whitespaceToken));

        //    if (property.Modifiers.Any(m => m.IsKind(SyntaxKind.VirtualKeyword)))
        //    {
        //        newProperty = newProperty.AddModifiers(SyntaxFactory.Token(SyntaxKind.VirtualKeyword), _whitespaceToken);
        //    }

        //    return await ReplacePropertyInDocumentAsync(document, property, newProperty, cancellationToken);
        //}

        //private static async Task<Document> ReplacePropertyInDocumentAsync(Document document, PropertyDeclarationSyntax property, PropertyDeclarationSyntax newProperty, CancellationToken cancellationToken)
        //{
        //    var root = await document.GetSyntaxRootAsync(cancellationToken);
        //    var newRoot = root.ReplaceNode(property, new[] { newProperty });

        //    return document.WithSyntaxRoot(newRoot);
        //}

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