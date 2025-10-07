using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Kraig.Roslyn.Generators
{
    [Generator(LanguageNames.CSharp)]
    internal class SingletonGenerator : IIncrementalGenerator
    {
        private const string ATTRIBUTE_FULLNAME = "Kraig.Attributes.SingletonAttribute";

        private const string ATTRIBUTE_CLASS = """
            using System;
                                         
            namespace Kraig.Attributes 
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class SingletonAttribute : Attribute { }
            }
            """;

        private const string TEMPLATE = """
            namespace {0}
            {{
                partial class {1}
                {{
                    public static {1} Instance
                    {{
                        get
                        {{
                            if(_instance is null) 
                                _instance = new {1}();
                            return _instance;
                        }}
                    }}
                    private static {1} _instance;{2}
                }}
            }}
            """;

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(GenerateAttribute);
            var classes = context.SyntaxProvider.CreateSyntaxProvider(
                (node, _) => node is ClassDeclarationSyntax,
                (ctx, _) => (ITypeSymbol)ctx.SemanticModel.GetDeclaredSymbol(ctx.Node));
            context.RegisterSourceOutput(classes.Collect(), GenerateCode);
        }

        private static void GenerateAttribute(IncrementalGeneratorPostInitializationContext context) =>
            context.AddSource("SingletonAttribute.g.cs", ATTRIBUTE_CLASS);

        private static void GenerateCode(SourceProductionContext ctx, ImmutableArray<ITypeSymbol> arr)
        {
            var builder = new StringBuilder();

            arr = [.. arr.Where(t => t.GetAttributes().Any(a => a.AttributeClass.ToDisplayString() == ATTRIBUTE_FULLNAME))];

            foreach (var type in arr.Distinct<ITypeSymbol>(SymbolEqualityComparer.Default))
            {
                var ns = type.ContainingNamespace.ToDisplayString();
                var constructor = "";
                if (NeedAutoConstructor(type))
                    constructor = $"\n\t\tprivate {type.Name}() {{ }}";
                builder.AppendFormat(TEMPLATE, ns, type.Name, constructor);
                builder.AppendLine();
            }

            ctx.AddSource("Singletons.g.cs", builder.ToString());
        }

        private static bool NeedAutoConstructor(ITypeSymbol type) => type is INamedTypeSymbol @class &&
            @class.InstanceConstructors.All(c => c.DeclaredAccessibility != Accessibility.Private || c.Parameters.Length > 0);
    }
}
