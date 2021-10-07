using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

#if DEBUG_RESX_FILE_EXTENDER
    using System.Diagnostics;
#endif

namespace En3Tho.ResXFileExtenderSourceGenerator
{
    internal static class StringBuilderExtensions
    {
        public static StringBuilder AppendIndented(this StringBuilder sb, int indent, string value)
        {
            for (int i = 0; i < indent; i++)
                sb.Append("    ");
            return sb.Append(value);
        }

        public static StringBuilder AppendLineIndented(this StringBuilder sb, int indent, string value) =>
            sb.AppendIndented(indent, value).AppendLine();
    }

    [Generator]
    public class ResXFileExtenderSourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new ResXFileExtenderSyntaxReceiver());
            context.RegisterForPostInitialization(_ => { });
        }

        public void Execute(GeneratorExecutionContext context)
        {
#if DEBUG_RESX_FILE_EXTENDER
            if (!Debugger.IsAttached)
                Debugger.Launch();
#endif

            if (context.SyntaxContextReceiver is not ResXFileExtenderSyntaxReceiver { ResXGeneratedClassNames: { Count: > 0 } } receiver)
                return;

            foreach (var classSymbol in receiver.ResXGeneratedClassNames)
            {
                var source = new StringBuilder();
                source.AppendLine("using System;");
                source.AppendLine("using System.Globalization;");
                source.AppendLine("using System.Resources;");
                source.AppendLine("#nullable enable");
                source.Append($"namespace {classSymbol.ContainingNamespace.ToDisplayString()}.Extensions").AppendLine(" {");
                source.AppendIndented(1, $"public static class {classSymbol.Name}").AppendLine(" {");
                var className = $"{classSymbol.ContainingNamespace}.{classSymbol.Name}";
                foreach (var generatedProperty in classSymbol.GetMembers().OfType<IPropertySymbol>())
                {
                    var propName = generatedProperty.Name;

                    switch (propName)
                    {
                        case "ResourceManager":
                            source.AppendLineIndented(2, $"public static ResourceManager {propName} => {className}.{propName};");
                            break;
                        case "Culture":
                            source.AppendLineIndented(2, $"public static CultureInfo {propName} {{ get => {className}.{propName}; set => {className}.{propName} = value; }} ");
                            break;
                        default:
                            source.AppendLineIndented(2, $"public static string {propName}(CultureInfo? cultureInfo = null) => ")
                                  .AppendLineIndented(3, $"{className}.ResourceManager.GetString(\"{propName}\", cultureInfo ?? {className}.Culture)!;");
                            break;
                    }
                }
                source.AppendLineIndented(1, "}");
                source.Append("}");

                context.AddSource($"{classSymbol.Name}.Designer.Extensions.cs", SourceText.From(source.ToString(), Encoding.UTF8));
            }
        }
    }

    internal class ResXFileExtenderSyntaxReceiver : ISyntaxContextReceiver
    {
        private const string GeneratedAttributeName = "System.CodeDom.Compiler.GeneratedCodeAttribute";
        private const string DebuggerNonUserCodeAttributeAttributeName = "System.Diagnostics.DebuggerNonUserCodeAttribute";
        private const string CompilerGeneratedAttributeAttributeName = "System.Runtime.CompilerServices.CompilerGeneratedAttribute";

        public List<INamedTypeSymbol> ResXGeneratedClassNames { get; } = new();

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
#if DEBUG_RESX_FILE_EXTENDER
            if (!Debugger.IsAttached)
                Debugger.Launch();
#endif
            if (context.Node is ClassDeclarationSyntax { AttributeLists: { Count: > 0 } } possibleResXGeneratedClass)
            {
                if (context.SemanticModel.GetDeclaredSymbol(possibleResXGeneratedClass) is INamedTypeSymbol classSymbol)
                {
                    var classAttributes = classSymbol.GetAttributes();
                    if (classAttributes.Length == 3 // Strictly 3 attributes are generated
                     && classAttributes.Any(ad => ad.AttributeClass?.ToDisplayString() == GeneratedAttributeName)
                     && classAttributes.Any(ad => ad.AttributeClass?.ToDisplayString() == DebuggerNonUserCodeAttributeAttributeName)
                     && classAttributes.Any(ad => ad.AttributeClass?.ToDisplayString() == CompilerGeneratedAttributeAttributeName))
                    {
                        ResXGeneratedClassNames.Add(classSymbol);
                    }
                }
            }
        }
    }
}