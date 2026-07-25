using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TheEngine.SourceGenerator;

// Generates Run(Archetype) for structs implementing IJob/IParallelJob:
// it wires up every ComponentDataAccess<T>/ComponentArrayDataAccess<T>/ManagedComponentDataAccess<T>
// field from the chunk iterator and dispatches via Execute (IJob) or EntityExtensions.RunThreads (IParallelJob).
[Generator]
public sealed class JobRunMethodGenerator : IIncrementalGenerator
{
    private const string IJobName = "TheEngine.ECS.IJob";
    private const string IParallelJobName = "TheEngine.ECS.IParallelJob";
    private const string IChunkDataIteratorName = "TheEngine.ECS.IChunkDataIterator";
    private const string ComponentDataAccessOpenName = "TheEngine.ECS.ComponentDataAccess<>";
    private const string ComponentArrayDataAccessOpenName = "TheEngine.ECS.ComponentArrayDataAccess<>";
    private const string ManagedComponentDataAccessOpenName = "TheEngine.ECS.ManagedComponentDataAccess<>";

    private static readonly DiagnosticDescriptor MissingIteratorFieldRule = new(
        id: "TEJ001",
        title: "Missing IChunkDataIterator field",
        messageFormat: "Job struct '{0}' implements IJob/IParallelJob but does not declare a field of type IChunkDataIterator",
        category: "TheEngine.JobGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NotPartialRule = new(
        id: "TEJ002",
        title: "Job struct must be partial",
        messageFormat: "Job struct '{0}' implements IJob/IParallelJob and must be declared 'partial' so its Run(Archetype) method can be generated",
        category: "TheEngine.JobGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor EnclosingTypeNotPartialRule = new(
        id: "TEJ003",
        title: "Enclosing type must be partial",
        messageFormat: "Enclosing type '{0}' of job struct '{1}' must be declared 'partial' so the generated Run(Archetype) method can be merged into it",
        category: "TheEngine.JobGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => node is StructDeclarationSyntax { BaseList: not null },
                transform: static (ctx, _) => Analyze((StructDeclarationSyntax)ctx.Node, ctx.SemanticModel))
            .Where(static result => result is not null);

        context.RegisterSourceOutput(candidates.Collect(), static (spc, results) => Emit(spc, results!));
    }

    private static JobModel? Analyze(StructDeclarationSyntax structDecl, SemanticModel semanticModel)
    {
        if (semanticModel.GetDeclaredSymbol(structDecl) is not INamedTypeSymbol symbol)
            return null;

        bool isParallelJob = symbol.AllInterfaces.Any(static i => i.ToDisplayString() == IParallelJobName);
        bool isJob = !isParallelJob && symbol.AllInterfaces.Any(static i => i.ToDisplayString() == IJobName);

        if (!isJob && !isParallelJob)
            return null;

        var key = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if (!structDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
            return JobModel.Error(key, NotPartialRule, symbol.Locations.FirstOrDefault(), symbol.Name);

        var containingTypes = new List<TypeShape>();
        var current = symbol.ContainingType;
        while (current is not null)
        {
            bool currentIsPartial = current.DeclaringSyntaxReferences
                .Select(r => r.GetSyntax())
                .OfType<TypeDeclarationSyntax>()
                .Any(t => t.Modifiers.Any(SyntaxKind.PartialKeyword));

            if (!currentIsPartial)
                return JobModel.Error(key, EnclosingTypeNotPartialRule, current.Locations.FirstOrDefault(), current.Name, symbol.Name);

            containingTypes.Add(new TypeShape(current.Name, current.TypeKind == TypeKind.Interface ? "interface" : current.TypeKind == TypeKind.Struct ? "struct" : "class", GetTypeParameterNames(current)));
            current = current.ContainingType;
        }
        containingTypes.Reverse();

        var iteratorField = symbol.GetMembers().OfType<IFieldSymbol>()
            .FirstOrDefault(f => !f.IsStatic && f.Type.ToDisplayString() == IChunkDataIteratorName);

        if (iteratorField is null)
            return JobModel.Error(key, MissingIteratorFieldRule, symbol.Locations.FirstOrDefault(), symbol.Name);

        var assignments = ImmutableArray.CreateBuilder<FieldAssignment>();
        foreach (var field in symbol.GetMembers().OfType<IFieldSymbol>())
        {
            if (field.IsStatic || SymbolEqualityComparer.Default.Equals(field, iteratorField))
                continue;

            if (field.Type is not INamedTypeSymbol namedType || !namedType.IsGenericType)
                continue;

            bool isOptional = false;
            var dataType = namedType;
            if (namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
            {
                isOptional = true;
                if (namedType.TypeArguments[0] is not INamedTypeSymbol inner || !inner.IsGenericType)
                    continue;
                dataType = inner;
            }

            var openTypeName = dataType.ConstructUnboundGenericType().ToDisplayString();

            string? methodName = (openTypeName, isOptional) switch
            {
                (ComponentDataAccessOpenName, false) => "DataAccess",
                (ComponentDataAccessOpenName, true) => "OptionalDataAccess",
                (ComponentArrayDataAccessOpenName, false) => "ArrayDataAccess",
                (ManagedComponentDataAccessOpenName, false) => "ManagedDataAccess",
                (ManagedComponentDataAccessOpenName, true) => "OptionalManagedDataAccess",
                _ => null
            };

            if (methodName is null)
                continue;

            var typeArg = dataType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            assignments.Add(new FieldAssignment(field.Name, methodName, typeArg));
        }

        var namespaceName = symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString();

        return JobModel.Success(
            key,
            namespaceName,
            containingTypes.ToImmutableArray(),
            new TypeShape(symbol.Name, "struct", GetTypeParameterNames(symbol)),
            isParallelJob,
            iteratorField.Name,
            assignments.ToImmutable());
    }

    private static ImmutableArray<string> GetTypeParameterNames(INamedTypeSymbol symbol)
        => symbol.TypeParameters.Length == 0
            ? ImmutableArray<string>.Empty
            : symbol.TypeParameters.Select(t => t.Name).ToImmutableArray();

    private static void Emit(SourceProductionContext context, ImmutableArray<JobModel> results)
    {
        var seen = new HashSet<string>();

        foreach (var job in results)
        {
            if (!seen.Add(job.Key))
                continue;

            if (job.Diagnostic is { } diagnostic)
            {
                context.ReportDiagnostic(Diagnostic.Create(diagnostic.Descriptor, diagnostic.Location, diagnostic.MessageArgs));
                continue;
            }

            context.AddSource(BuildHintName(job), BuildSource(job));
        }
    }

    private static string BuildHintName(JobModel job)
    {
        var parts = job.ContainingTypes.Select(t => t.Name).Append(job.Struct.Name);
        return string.Join("_", parts) + ".Run.g.cs";
    }

    private static string BuildSource(JobModel job)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        if (job.Namespace is not null)
        {
            sb.AppendLine($"namespace {job.Namespace};");
            sb.AppendLine();
        }

        int indent = 0;
        foreach (var containingType in job.ContainingTypes)
        {
            sb.AppendLine($"{Indent(indent)}partial {containingType.Kind} {containingType.Name}{TypeParams(containingType)}");
            sb.AppendLine($"{Indent(indent)}{{");
            indent++;
        }

        sb.AppendLine($"{Indent(indent)}partial struct {job.Struct.Name}{TypeParams(job.Struct)}");
        sb.AppendLine($"{Indent(indent)}{{");
        indent++;

        sb.AppendLine($"{Indent(indent)}public void Run(global::TheEngine.ECS.Archetype archetype)");
        sb.AppendLine($"{Indent(indent)}{{");
        indent++;

        sb.AppendLine($"{Indent(indent)}var entityManager = archetype.EntityManager;");
        sb.AppendLine($"{Indent(indent)}var iterator = entityManager.ArchetypeIterator(archetype);");
        sb.AppendLine($"{Indent(indent)}while (iterator.MoveNext())");
        sb.AppendLine($"{Indent(indent)}{{");
        indent++;

        sb.AppendLine($"{Indent(indent)}{job.IteratorFieldName} = iterator.Current;");
        foreach (var assignment in job.Assignments)
        {
            sb.AppendLine($"{Indent(indent)}{assignment.FieldName} = {job.IteratorFieldName}.{assignment.MethodName}<{assignment.TypeArgument}>();");
        }

        sb.AppendLine();
        sb.AppendLine(job.IsParallelJob
            ? $"{Indent(indent)}global::TheEngine.ECS.EntityExtensions.RunThreads(0, {job.IteratorFieldName}.Length, ref this);"
            : $"{Indent(indent)}Execute(0, {job.IteratorFieldName}.Length);");

        indent--;
        sb.AppendLine($"{Indent(indent)}}}"); // while

        indent--;
        sb.AppendLine($"{Indent(indent)}}}"); // Run method

        indent--;
        sb.AppendLine($"{Indent(indent)}}}"); // struct

        for (int i = job.ContainingTypes.Length - 1; i >= 0; i--)
        {
            indent--;
            sb.AppendLine($"{Indent(indent)}}}");
        }

        return sb.ToString();
    }

    private static string Indent(int level) => new string(' ', level * 4);

    private static string TypeParams(TypeShape shape)
        => shape.TypeParameters.IsEmpty ? string.Empty : "<" + string.Join(", ", shape.TypeParameters) + ">";

    private readonly record struct TypeShape(string Name, string Kind, ImmutableArray<string> TypeParameters);

    private readonly record struct FieldAssignment(string FieldName, string MethodName, string TypeArgument);

    private readonly record struct DiagnosticInfo(DiagnosticDescriptor Descriptor, Location? Location, object?[] MessageArgs);

    private sealed class JobModel
    {
        public required string Key { get; init; }
        public DiagnosticInfo? Diagnostic { get; private init; }
        public string? Namespace { get; private init; }
        public ImmutableArray<TypeShape> ContainingTypes { get; private init; }
        public TypeShape Struct { get; private init; }
        public bool IsParallelJob { get; private init; }
        public string IteratorFieldName { get; private init; } = string.Empty;
        public ImmutableArray<FieldAssignment> Assignments { get; private init; }

        public static JobModel Error(string key, DiagnosticDescriptor descriptor, Location? location, params object?[] messageArgs)
            => new() { Key = key, Diagnostic = new DiagnosticInfo(descriptor, location, messageArgs) };

        public static JobModel Success(
            string key,
            string? @namespace,
            ImmutableArray<TypeShape> containingTypes,
            TypeShape @struct,
            bool isParallelJob,
            string iteratorFieldName,
            ImmutableArray<FieldAssignment> assignments)
            => new()
            {
                Key = key,
                Namespace = @namespace,
                ContainingTypes = containingTypes,
                Struct = @struct,
                IsParallelJob = isParallelJob,
                IteratorFieldName = iteratorFieldName,
                Assignments = assignments
            };
    }
}
