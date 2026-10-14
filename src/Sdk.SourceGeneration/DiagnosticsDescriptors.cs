using Microsoft.CodeAnalysis;

namespace Meshmakers.Octo.Sdk.SourceGeneration;

internal static class DiagnosticsDescriptors
{
    public static readonly DiagnosticDescriptor EmptyFile
        = new("OM1000", // id
            "Empty file", // title
            "File '{0}' is empty", // message
            "Construction Kit", // category
            DiagnosticSeverity.Error,
            true);

    /// <summary>
    ///     AB#5688 (range retention): a major-qualified reference of the compiled model has no model of that major in
    ///     the restored compile cache, so DTOs cannot be generated.
    /// </summary>
    public static readonly DiagnosticDescriptor UnboundMajorQualifiedReference
        = new("OM1010", // id
            "Unbound major-qualified construction kit reference", // title
            "Model '{0}' references {1}, but the compile cache holds no version of that model and major; check the " +
            "dependency range in ckModel.yaml and rebuild the model", // message
            "Construction Kit", // category
            DiagnosticSeverity.Error,
            true);
}
