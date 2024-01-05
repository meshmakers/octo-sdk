namespace Meshmakers.Octo.Sdk.ServiceClient;

/// <summary>
///     Makes some extensions to the <see cref="Uri" /> class.
/// </summary>
public static class UriExtensions
{
    /// <summary>
    ///     Appends the specified paths to the uri.
    /// </summary>
    /// <param name="uri">The URI</param>
    /// <param name="paths">Paths to be appended</param>
    /// <returns></returns>
    public static Uri Append(this Uri uri, params string[] paths)
    {
        return new Uri(paths.Aggregate(uri.AbsoluteUri, (current, path) =>
            $"{current.TrimEnd('/')}/{path.TrimStart('/')}"));
    }
}