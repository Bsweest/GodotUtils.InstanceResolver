using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using static GodotUtils.InstanceResolver.Generators.Constants.ClassNameConst;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace GodotUtils.InstanceResolver.Generators.Components;

partial record HierarchyInfo
{
    public CompilationUnitSyntax GetCompilationUnit(
        ImmutableArray<MemberDeclarationSyntax> memberDeclarations,
        ImmutableArray<StatementSyntax> expressionStatementSyntaxes,
        BaseListSyntax? baseList = null
    )
    {
        // Create the partial type declaration with the given member declarations.
        // This code produces a class declaration as follows:
        //
        // partial <TYPE_KIND> TYPE_NAME>
        // {
        //      public class BuildParameters
        //      {
        //          <MEMBERS>
        //      }
        //
        //      public void Map(BuildParameters parameters)...
        // }
        TypeDeclarationSyntax typeDeclarationSyntax = (
            (ClassDeclarationSyntax)
                Hierarchy[0].GetSyntax().AddModifiers(Token(SyntaxKind.PartialKeyword))
        )
            .AddBaseListTypes(
                SimpleBaseType(
                    IdentifierName(RequiredResolveInterface(Hierarchy[0].QualifiedName))
                )
            )
            .AddMembers(
                ClassDeclaration(BuildParametersClassName)
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .AddBaseListTypes(
                        SimpleBaseType(
                            IdentifierName(RequiredParamsInterface(Hierarchy[0].QualifiedName))
                        )
                    )
                    .AddMembers([.. memberDeclarations]),
                MethodDeclaration(IdentifierName(BuildParametersClassName), Identifier("Map"))
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(
                        Parameter(Identifier(PassingObj))
                            .WithType(IdentifierName(BuildParametersClassName))
                    )
                    .AddBodyStatements([.. expressionStatementSyntaxes])
                    .AddBodyStatements(ReturnStatement(IdentifierName(PassingObj)))
            );

        // Add the base list, if present
        if (baseList is not null)
        {
            typeDeclarationSyntax = typeDeclarationSyntax.WithBaseList(baseList);
        }

        // Add all parent types in ascending order, if any
        foreach (TypeInfor parentType in Hierarchy.AsSpan()[1..])
        {
            typeDeclarationSyntax = parentType
                .GetSyntax()
                .AddModifiers(Token(SyntaxKind.PartialKeyword))
                .AddMembers(typeDeclarationSyntax);
        }

        // Prepare the leading trivia for the generated compilation unit.
        // This will produce code as follows:
        //
        // <auto-generated/>
        // #pragma warning disable
        // #nullable enable
        SyntaxTriviaList syntaxTriviaList = TriviaList(
            Comment("// <auto-generated/>"),
            Trivia(PragmaWarningDirectiveTrivia(Token(SyntaxKind.DisableKeyword), true)),
            Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true))
        );

        if (Namespace is "")
        {
            // If there is no namespace, attach the pragma directly to the declared type,
            // and skip the namespace declaration. This will produce code as follows:
            //
            // <SYNTAX_TRIVIA>
            // <TYPE_HIERARCHY>
            return CompilationUnit()
                .AddMembers(typeDeclarationSyntax.WithLeadingTrivia(syntaxTriviaList))
                .NormalizeWhitespace();
        }

        // Create the compilation unit with disabled warnings, target namespace and generated type.
        // This will produce code as follows:
        //
        // <SYNTAX_TRIVIA>
        // namespace <NAMESPACE>
        // {
        //     <TYPE_HIERARCHY>
        // }
        return CompilationUnit()
            .AddMembers(
                NamespaceDeclaration(IdentifierName(Namespace))
                    .WithLeadingTrivia(syntaxTriviaList)
                    .AddMembers(typeDeclarationSyntax)
            )
            .NormalizeWhitespace();
    }
}
