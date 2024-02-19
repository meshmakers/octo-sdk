using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Meshmakers.Octo.Sdk.Common.DataPipeline.Configuration;

internal class ConfigurationNodeResolver : INodeTypeResolver
{
    public bool Resolve(NodeEvent? nodeEvent, ref Type currentType)
    {
        if (nodeEvent?.Tag.Value.StartsWith("!clr:") ?? false)
        {
            var netTypeName = nodeEvent.Tag.Value.Substring(5);
            var type = Type.GetType(netTypeName);
            if (type != null)
            {
                currentType = type;
                return true;
            }
        }

        return true;
    }
}