
#if NETSTANDARD2_0

using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// To support init only setters
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public record IsExternalInit;
    

    /// <summary>
    /// To support required members
    /// </summary>
    public class RequiredMemberAttribute : Attribute { }
    
    /// <summary>
    /// To support required members
    /// </summary>
    public class CompilerFeatureRequiredAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        public CompilerFeatureRequiredAttribute(string name) { }
    }
}
namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// To support required setters
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public record SetsRequiredMembersAttribute;
}

// namespace System.Collections.Concurrent
// {
//     /// <summary>
//     /// Extension to support deconstruction
//     /// </summary>
//     public static class ConcurrentDictionaryExtensions
//     {
//     /// <summary>
//     /// Extension to support deconstruction
//     /// </summary>
//         public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
//         {
//             key = kvp.Key;
//             value = kvp.Value;
//         }
//     }
// }

#endif