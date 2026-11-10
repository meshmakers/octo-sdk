using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts.Services;

namespace Meshmakers.Octo.Sdk.SourceGeneration;

/// <summary>
///     Emits the generated sources of one compiled CK model (records, types, enums and, since AB#8423, the method
///     clients of the concrete types with methods). Separated from the Roslyn entry point
///     (<see cref="CkDtoSourceGenerator" />) so the tests can drive exactly what the generator adds to a compilation.
/// </summary>
internal static class CkModelSourceEmitter
{
    /// <summary>
    ///     Emits all sources of <paramref name="model" />; <paramref name="addSource" /> receives the hint name and
    ///     the code of every source, in a stable order.
    /// </summary>
    /// <param name="localNamespace">Root namespace of the consuming project</param>
    /// <param name="model">The compiled model, bound to the restored cache (<see cref="CkDtoModelBinder" />)</param>
    /// <param name="tenantId">The cache tenant the model's cache was restored into</param>
    /// <param name="ckCacheService">The cache service</param>
    /// <param name="addSource">Receives hint name and code</param>
    /// <param name="emitMethodClients">
    ///     <c>false</c> when the consuming compilation has no method client runtime (no reference to
    ///     <c>Meshmakers.Octo.Sdk.ServiceClient</c> with <c>CkMethodClientBase</c>): only the DTOs are emitted.
    /// </param>
    /// <param name="emitAsyncMethodClients">
    ///     AB#8446: <c>false</c> when the runtime lacks the operation types (<c>CkMethodAsyncCallResponse</c>): the
    ///     clients get a comment instead of each asynchronous method; synchronous methods are generated as before.
    /// </param>
    public static void Emit(string localNamespace, CkCompiledModelRoot model, string tenantId,
        ICkCacheService ckCacheService, Action<string, string> addSource, bool emitMethodClients = true,
        bool emitAsyncMethodClients = true)
    {
        var ns = DtoNamespace(localNamespace, model.ModelId.Name, model.ModelId.Version.Major);

        if (model.Records != null)
        {
            foreach (var ckRecordDto in model.Records)
            {
                var queryCode = QueryDtoCodeGenerator.Instance.GenerateRecord(ns, ckRecordDto, tenantId, ckCacheService);
                if (!string.IsNullOrWhiteSpace(queryCode))
                {
                    addSource($"{ns}.Record.{ckRecordDto.RecordId.Name}QueryDto.g.cs", queryCode);
                }

                var mutationCode = MutationDtoCodeGenerator.Instance.GenerateRecord(ns, ckRecordDto, tenantId, ckCacheService);
                if (!string.IsNullOrWhiteSpace(mutationCode))
                {
                    addSource($"{ns}.Record.{ckRecordDto.RecordId.Name}MutationDto.g.cs", mutationCode);
                }
            }
        }

        if (model.Types != null)
        {
            foreach (var ckTypeDto in model.Types)
            {
                if (model.ModelId.Name == "System" && ckTypeDto.TypeId.Name == "Entity")
                {
                    continue;
                }

                var queryCode = QueryDtoCodeGenerator.Instance.GenerateType(ns, ckTypeDto, tenantId, ckCacheService);
                if (!string.IsNullOrWhiteSpace(queryCode))
                {
                    addSource($"{ns}.Type.{ckTypeDto.TypeId.Name}QueryDto.g.cs", queryCode);
                }

                var mutationCode = MutationDtoCodeGenerator.Instance.GenerateType(ns, ckTypeDto, tenantId, ckCacheService);
                if (!string.IsNullOrWhiteSpace(mutationCode))
                {
                    addSource($"{ns}.Type.{ckTypeDto.TypeId.Name}MutationDto.g.cs", mutationCode);
                }

                // AB#8423: a concrete type with methods (own, inherited or from an interface) additionally gets a
                // method client. A type without methods gets nothing here, so its sources stay byte-identical.
                var methodClient = !emitMethodClients ? null : MethodClientCodeGenerator.Generate(localNamespace, ns, model.ModelId, ckTypeDto,
                    tenantId, ckCacheService, emitAsyncMethodClients);
                if (methodClient != null)
                {
                    addSource($"{ns}.Type.{ckTypeDto.TypeId.Name}MethodClient.g.cs", methodClient);
                }
            }
        }

        if (model.Enums != null)
        {
            foreach (var ckEnumDto in model.Enums)
            {
                var code = CkEnumCodeGenerator.Instance.Generate(ns, model.ModelId, ckEnumDto, tenantId, ckCacheService);
                if (!string.IsNullOrWhiteSpace(code))
                {
                    addSource($"{ns}.Enum.{ckEnumDto.EnumId.Name}.g.cs", code);
                }
            }
        }
    }

    /// <summary>
    ///     Namespace of the generated sources of a model: <c>{root}.DataTransferObjects.{Model}.v{Major}</c>.
    /// </summary>
    public static string DtoNamespace(string localNamespace, string modelName, int major) =>
        $"{localNamespace}.DataTransferObjects.{modelName}.v{major.ToString()}";
}
