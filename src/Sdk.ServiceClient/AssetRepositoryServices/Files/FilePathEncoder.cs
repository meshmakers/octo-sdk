using System.Text;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Files;

/// <summary>
///     Builds the route part <c>{root}/{segment}/…</c> of the path based file endpoints: every segment is
///     URL-encoded on its own, empty segments are dropped (as the server does), and '.' / '..' segments are
///     refused before anything is sent.
/// </summary>
internal static class FilePathEncoder
{
    /// <summary>
    ///     Route segments the REST API uses literally; a root with one of these names cannot be addressed by
    ///     path (the server refuses them as well-known names, RESERVED_NAME).
    /// </summary>
    internal static readonly IReadOnlyCollection<string> ReservedRootNames = ["capabilities", "zip", "items", "stats"];

    /// <summary>
    ///     Splits a '/'-separated path into its segments, dropping empty ones and refusing dot segments.
    /// </summary>
    internal static IReadOnlyList<string> Split(string? path, string parameterName = "path")
    {
        if (string.IsNullOrEmpty(path))
        {
            return [];
        }

        var segments = path!.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            if (segment is "." or "..")
            {
                throw new ArgumentException($"The path '{path}' must not contain '.' or '..' segments.",
                    parameterName);
            }
        }

        return segments;
    }

    /// <summary>
    ///     Validates a root well-known name for use as the first route segment.
    /// </summary>
    internal static string ValidateRoot(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new ArgumentException("The root well-known name must not be empty.", nameof(root));
        }

        if (root.IndexOf('/') >= 0 || root is "." or "..")
        {
            throw new ArgumentException($"'{root}' is not a valid root well-known name.", nameof(root));
        }

        if (ReservedRootNames.Contains(root, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"'{root}' is a reserved route segment and cannot be addressed as a root.",
                nameof(root));
        }

        return root;
    }

    /// <summary>
    ///     Returns <c>{encodedRoot}/{encodedSegment}/…</c>.
    /// </summary>
    internal static string EncodeRoute(string root, IReadOnlyList<string> segments)
    {
        var builder = new StringBuilder(Uri.EscapeDataString(ValidateRoot(root)));
        foreach (var segment in segments)
        {
            builder.Append('/').Append(Uri.EscapeDataString(segment));
        }

        return builder.ToString();
    }

    /// <summary>
    ///     Joins segments with '/'.
    /// </summary>
    internal static string Join(IEnumerable<string> segments) => string.Join("/", segments);
}
