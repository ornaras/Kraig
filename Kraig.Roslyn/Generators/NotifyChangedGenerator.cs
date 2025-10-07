using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Kraig.Roslyn.Generators
{
    [Generator(LanguageNames.CSharp)]
    internal class NotifyChangedGenerator : IIncrementalGenerator
    {
        const string ATTRIBUTE_FULLNAME = "Kraig.Attributes.NotifyChangedAttribute";
        const string ATTRIBUTE_CLASS = """
            using System;

            namespace Kraig.Attributes
            {
                [AttributeUsage(AttributeTargets.Field)]
                public class NotifyChangedAttribute : Attribute 
                {
                    public bool GenerateEvent = false;
                }
            }
            """;
        private const string CLASS_TEMPLATE = """
            namespace {0}
            {{
                partial class {1}: INotifyPropertyChanged
                {{            
                    public event PropertyChangedEventHandler PropertyChanged;
                    public void OnPropertyChanged(string propertyName) =>
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            {2}
                }}
            }}
            """;
        private const string PROPERTY_TEMPLATE = """
                    public {0} {1}
                    {{
                        get => {2};
                        set
                        {{
                            if (Equals({2}, value)) 
                                return;
                            {2} = value;
                            OnPropertyChanged("{1}");{3}
                        }}
                    }}
            """;

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(GenerateAttribute);
            var classes = context.SyntaxProvider.CreateSyntaxProvider(
                (node, _) => node is FieldDeclarationSyntax fds && fds.AttributeLists.Count > 0,
                (ctx, _) => GetFieldWithAttribute(ctx));
            context.RegisterSourceOutput(classes.Collect(), GenerateCode);
        }

        private static void GenerateAttribute(IncrementalGeneratorPostInitializationContext context) =>
            context.AddSource("NotifyChangedAttribute.g.cs", ATTRIBUTE_CLASS);

        private static IFieldSymbol GetFieldWithAttribute(GeneratorSyntaxContext context)
        {
            var field = (FieldDeclarationSyntax)context.Node;

            foreach (var variable in field.Declaration.Variables)
            {
                if (context.SemanticModel.GetDeclaredSymbol(variable) is not IFieldSymbol symbol) continue;
                if (symbol.GetAttributes().FirstOrDefault(ad => ad.AttributeClass?.ToDisplayString() == ATTRIBUTE_FULLNAME) is null) continue;
                return symbol;
            }

            return null;
        }

        private static void GenerateCode(SourceProductionContext context, ImmutableArray<IFieldSymbol> fields)
        {
            var builder = new StringBuilder();
            builder.AppendLine("""
                using System.ComponentModel;
                using System;
                """);
            builder.AppendLine();
            foreach (var group in fields.Where(f => f is not null).GroupBy(f => f.ContainingType, SymbolEqualityComparer.Default))
            {
                builder.AppendLine(GenerateClass([.. group]));
            }
            context.AddSource("NotifyUpdated.g.cs", builder.ToString());
        }

        private static string GenerateClass(List<IFieldSymbol> fields)
        {
            var propertiesBuilder = new StringBuilder();
            foreach (var field in fields)
            {
                var attr = field.GetAttributes().First(a => a.AttributeClass.ToDisplayString() == ATTRIBUTE_FULLNAME)!;

                var argGenerateEvent = attr.NamedArguments.FirstOrDefault(i => i.Key == "GenerateEvent").Value;
                var generateEvent = !argGenerateEvent.IsNull && (bool)argGenerateEvent.Value;

                var type = field.Type.ToDisplayString();
                var name = field.Name.ToPascalCase();
                var invokeEvent = "";
                if(generateEvent)
                    invokeEvent = $"\n\t\t\t\t{name}Changed?.Invoke();";
                propertiesBuilder.AppendFormat(PROPERTY_TEMPLATE, type, name, field.Name, invokeEvent);
                propertiesBuilder.AppendLine();
                if(generateEvent)
                    propertiesBuilder.AppendLine($"\t\tpublic event Action {name}Changed;");
            }
            var classBuilder = new StringBuilder();
            var className = fields[0].ContainingType.Name;
            var ns = fields[0].ContainingType.ContainingNamespace.ToDisplayString();
            classBuilder.AppendFormat(CLASS_TEMPLATE, ns, className, propertiesBuilder.ToString());
            return classBuilder.ToString();
        }
    }
}
