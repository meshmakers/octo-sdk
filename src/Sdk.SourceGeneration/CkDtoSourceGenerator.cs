using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.Messages;
using Meshmakers.Octo.ConstructionKit.Contracts.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts.Services;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Meshmakers.Octo.Sdk.SourceGeneration;

[Generator]
public class CkDtoSourceGenerator : IIncrementalGenerator
{
    private readonly ServiceProvider _serviceProvider;

    public CkDtoSourceGenerator()
    {
        var services = new ServiceCollection();
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
        });
        services.AddConstructionKit();
        _serviceProvider = services.BuildServiceProvider();
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var globalOptions = context.AnalyzerConfigOptionsProvider.Select(GlobalOptions.Select);

        var ckModelCandidates = context.AdditionalTextsProvider
            .Combine(globalOptions)
            .Where(static x =>
            {
                var f = x.Left;
                var options = x.Right;
                if (!options.IsValid) // if not valid -> the project has not loaded props file with CompilerVisibleProperty
                {
                    return false;
                }

                var fileName = Path.GetFileName(f.Path).ToLower();
                return fileName.StartsWith("ck-") && (fileName.EndsWith(".cache.json") || fileName.EndsWith(".yaml"));
            })
            .Select(static (f, _) =>
            {
                var checksum = f.Left.GetText()?.GetChecksum();
                return new AdditionalTextWithHash(f.Left, checksum == null ? null : BitConverter.ToString(checksum.Value.ToArray()));
            });

        var monitor = ckModelCandidates.Collect().SelectMany(static (x, _) => GroupCkModelFiles.Group(x));

        var inputs = monitor
            .Combine(globalOptions)
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(static (x, _) => FileOptions.Select(
                x.Left.Left,
                x.Right,
                x.Left.Right
            ))
            .Where(static x => x.IsValid);
        
        // AB#8423: method clients derive from CkMethodClientBase (Meshmakers.Octo.Sdk.ServiceClient). A consumer that
        // does not reference it (DTO-only, or an older ServiceClient package) gets the DTOs exactly as before and no
        // clients, instead of generated code that does not compile.
        // AB#8446: clients of asynchronous methods need the operation runtime (CkMethodAsyncCallResponse,
        // CkOperationHandle) as well; with a ServiceClient that has CkMethodClientBase but not that runtime, the
        // asynchronous methods are left out (comment) and the synchronous ones are generated as before.
        var methodClientRuntime = context.CompilationProvider.Select(static (compilation, _) =>
            (compilation.GetTypeByMetadataName(MethodClientCodeGenerator.ClientBaseMetadataName) != null,
                compilation.GetTypeByMetadataName(MethodClientCodeGenerator.AsyncResponseMetadataName) != null));

        context.RegisterSourceOutput(inputs.Combine(methodClientRuntime),
            (ctx, x) => GenerateCode(ctx, x.Left, x.Right.Item1, x.Right.Item2));
    }

    private void GenerateCode(SourceProductionContext context, FileOptions fileOptions, bool emitMethodClients,
        bool emitAsyncMethodClients)
    {
        var ckCacheService = _serviceProvider.GetRequiredService<ICkCacheService>();
        var ckSerializer = _serviceProvider.GetRequiredService<ICkYamlSerializer>();

        var tenantId = fileOptions.GroupedFile.MainFile.File.Path;

        ckCacheService.CreateTenant(tenantId);
        fileOptions.GroupedFile.CacheFile.Deconstruct(out var cacheFile, out _);
        var sourceText = cacheFile.GetText();
        if (sourceText == null)
        {
            var error = Diagnostic.Create(DiagnosticsDescriptors.EmptyFile,
                null,
                fileOptions.GroupedFile.CacheFile.File.Path);
            context.ReportDiagnostic(error);
            return;
        }

        ckCacheService.RestoreCache(tenantId, sourceText.ToString());

        fileOptions.GroupedFile.MainFile.Deconstruct(out var mainFile, out _);
        sourceText = mainFile.GetText();
        if (sourceText == null)
        {
            var error = Diagnostic.Create(DiagnosticsDescriptors.EmptyFile,
                null,
                fileOptions.GroupedFile.MainFile.File.Path);
            context.ReportDiagnostic(error);
            return;
        }

        var operationResult = new OperationResult();
        var ckCompiledModelRoot = ckSerializer.DeserializeCompiledModelRoot(sourceText.ToString(), mainFile.Path, operationResult);
        if (operationResult.Messages.Any())
        {
            ReportOperationResults(context, operationResult);
            return;
        }

        // Range-retaining models (CK v2, AB#6675): bind the major-qualified references to the restored cache first.
        var unbound = CkDtoModelBinder.BindToCache(ckCompiledModelRoot, ckCacheService, tenantId);
        if (unbound.Count > 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticsDescriptors.UnboundMajorQualifiedReference, null,
                ckCompiledModelRoot.ModelId.ToString(), string.Join(", ", unbound)));
            return;
        }

        CkModelSourceEmitter.Emit(fileOptions.LocalNamespace, ckCompiledModelRoot, tenantId, ckCacheService,
            context.AddSource, emitMethodClients, emitAsyncMethodClients);
    }

    private static void ReportOperationResults(SourceProductionContext context, OperationResult operationResult)
    {
        foreach (var message in operationResult.Messages)
        {
            switch (message.MessageLevel)
            {
                case MessageLevel.FatalError:
                    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                            $"O{message.MessageNumber.ToString().PadLeft(3, '0')}",
                            "Construction Kit read fatal error",
                            message.MessageText, "Construction Kit", DiagnosticSeverity.Error, true),
                        null));
                    break;
                case MessageLevel.Error:
                    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                            $"O{message.MessageNumber.ToString().PadLeft(3, '0')}",
                            "Construction Kit read error",
                            message.MessageText, "Construction Kit", DiagnosticSeverity.Error, true),
                        null));
                    break;
                case MessageLevel.Warning:
                    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                            $"O{message.MessageNumber.ToString().PadLeft(3, '0')}",
                            "Construction Kit read warning",
                            message.MessageText, "Construction Kit", DiagnosticSeverity.Warning, true),
                        null));
                    break;
                case MessageLevel.Info:
                    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                            $"O{message.MessageNumber.ToString().PadLeft(3, '0')}",
                            "Construction Kit read",
                            message.MessageText, "Construction Kit", DiagnosticSeverity.Info, true),
                        null));
                    break;
            }
        }
    }
}