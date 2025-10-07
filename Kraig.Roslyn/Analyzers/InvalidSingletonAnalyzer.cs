using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Locale = Kraig.Roslyn.Locales.Analyzers;

namespace Kraig.Roslyn.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InvalidSingletonAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Locale.InvalidSignletonAnalyzerName), Locale.ResourceManager, typeof(Locale));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Locale.InvalidSignletonAnalyzerMessageFormat), Locale.ResourceManager, typeof(Locale));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Locale.InvalidSignletonAnalyzerDescription), Locale.ResourceManager, typeof(Locale));

    private static readonly DiagnosticDescriptor Rule = new(
        AnalyzerIds.InvalidSingletonAnalyzer, Title, MessageFormat, "",
        DiagnosticSeverity.Error, true, Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static bool HasSingletonAttribute(INamedTypeSymbol nts) =>
        nts.GetAttributes().Any(a => a.AttributeClass.Name.StartsWith("Singleton"));

    private static bool SearchConstructor(ISymbol symbol) =>
        symbol is IMethodSymbol ms && ms.MethodKind == MethodKind.Constructor;

    private static bool HasOnlyOneConstructor(INamedTypeSymbol nts) =>
        nts.GetMembers().Count(SearchConstructor) == 1;

    private static bool ValidConstructor(IMethodSymbol ms) =>
        ms.Parameters.IsEmpty && ms.DeclaredAccessibility == Accessibility.Private;

    private static IMethodSymbol GetConstructorSyntax(INamedTypeSymbol nts) =>
        (IMethodSymbol)nts.GetMembers().First(SearchConstructor);

    private static bool ValidSingleton(INamedTypeSymbol nts) =>
        HasOnlyOneConstructor(nts) && ValidConstructor(GetConstructorSyntax(nts));

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var cds = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var symbol = semanticModel.GetDeclaredSymbol(cds);
        if (symbol is null || !HasSingletonAttribute(symbol) || ValidSingleton(symbol)) return;
        var diagnostic = Diagnostic.Create(Rule, cds.Identifier.GetLocation(), symbol.Name);
        context.ReportDiagnostic(diagnostic);
    }
}