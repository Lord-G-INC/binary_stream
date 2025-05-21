using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace Binary_Stream.SourceGen;

[Generator]
public class SourceGen : IIncrementalGenerator {
    private const string BinaryReadableAttributeName = "Binary_Stream.BinaryReadableAttribute";
    private const string BinaryWritableAttributeName = "Binary_Stream.BinaryWritableAttribute";
    private const string BinaryIgnoreAttributeName = "Binary_Stream.BinaryIgnoreAttribute";

    private const string AttributesSource = @"
using System;
namespace Binary_Stream;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class BinaryReadableAttribute : Attribute {
    public BinaryReadableAttribute() : base() { }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class BinaryWritableAttribute : Attribute {
    public BinaryWritableAttribute() : base() { }
}

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class BinaryIgnoreAttribute : Attribute {
    public BinaryIgnoreAttribute() : base() { }
}
";

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        // Generates the attributes
        context.RegisterPostInitializationOutput(spc =>
            spc.AddSource("Binary_Stream.SourceGen.g.cs", SourceText.From(AttributesSource, Encoding.UTF8))
        );

        // Generates a provider for all classes/structs in the project
        var classProvider = context.SyntaxProvider.CreateSyntaxProvider(
            (node, _) => node is ClassDeclarationSyntax || node is StructDeclarationSyntax,
            (context, _) => context.SemanticModel.GetDeclaredSymbol(context.Node) as INamedTypeSymbol
        );

        // Class/struct generation
        context.RegisterSourceOutput(classProvider, (prodcx, symbol) => {
            if (symbol is null)
                return;

            var attributes = symbol.GetAttributes();

            if (attributes.Length == 0)
                return;

            var readFunc = string.Empty;
            var writeFunc = string.Empty;

            foreach (var attribute in attributes) {
                var name = attribute.AttributeClass?.ToDisplayString();

                if (name == BinaryReadableAttributeName) {
                    readFunc = GenerateRead();
                }
                else if (name == BinaryWritableAttributeName) {
                    writeFunc = GenerateWrite();
                }
            }

            // Todo: Create the partial class/struct and add generated functions
        });
    }

    // These 2 should return functions, not the whole class/struct declaration
    private string GenerateRead() {
        return string.Empty;
    }

    private string GenerateWrite() {
        return string.Empty;
    }
}
