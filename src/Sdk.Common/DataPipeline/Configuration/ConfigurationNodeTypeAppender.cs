using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectGraphVisitors;

namespace Meshmakers.Octo.Sdk.Common.DataPipeline.Configuration;

internal class ConfigurationNodeTypeAppender(IObjectGraphVisitor<IEmitter> nextVisitor) : ChainedObjectGraphVisitor(nextVisitor)
{
    public override void VisitMappingStart(IObjectDescriptor mapping, Type keyType, Type valueType, IEmitter context)
    {
        base.VisitMappingStart(mapping, keyType, valueType, context);
        if (typeof(ConfigurationNode).IsAssignableFrom(mapping.Type))
        {
            context.Emit(new Scalar(null, "type"));
            context.Emit(new Scalar(null, mapping.Type.GetConfigurationQualifiedName()));
        }
    }
}