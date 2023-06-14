using System;
using System.Linq;

namespace Meshmakers.Octo.Sdk.ServiceClient;

public static class Extensions
{
    public static Uri Append(this Uri uri, params string[] paths)
    {
        return new Uri(paths.Aggregate(uri.AbsoluteUri, (current, path) =>
            $"{current.TrimEnd('/')}/{path.TrimStart('/')}"));
    }
}
