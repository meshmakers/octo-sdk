using Meshmakers.Octo.Sdk.Common.Plugs;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Plugs.Sample;

var plugBuilder = new PlugBuilder();

plugBuilder.Run(args, (_, services) => { services.AddSingleton<IPlugService, DemoPlugService>(); });