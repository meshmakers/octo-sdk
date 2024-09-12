using Meshmakers.Octo.Sdk.Common.Adapters;
using Meshmakers.Octo.Sdk.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Plug.Simulation;
using Sdk.Plug.Simulation.Generators;
using Sdk.Plug.Simulation.Nodes;

var plugBuilder = new AdapterBuilder();


var timer = new Timer(_ => plugBuilder.Stop(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));


plugBuilder.Run(args, (_, services) =>
{
    services.AddDataPipeline()
        .RegisterNode<SimulationNode>()
        .RegisterNode<WriteJsonNode>()
        .RegisterEtlContext<IAdapterEtlContext>();
    services.AddTransient<IPollingService, PollingService>();
    services.AddSingleton<IAdapterService, SimulationAdapterService>();
    
    services.AddKeyedTransient<IValueGenerator, CityGenerator>("Address.City");
    services.AddKeyedTransient<IValueGenerator, StreetAddressGenerator>("Address.StreetAddress");
    services.AddKeyedTransient<IValueGenerator, StreetNameGenerator>("Address.StreetName");
    services.AddKeyedTransient<IValueGenerator, BuildingNumberGenerator>("Address.BuildingNumber");
    
    services.AddKeyedTransient<IValueGenerator, FirstNameGenerator>("Person.FirstName");
    services.AddKeyedTransient<IValueGenerator, LastNameGenerator>("Person.LastName");
    
    services.AddKeyedTransient<IValueGenerator, SinusGenerator>("Math.Sinus");
    services.AddKeyedTransient<IValueGenerator, TriangleGenerator>("Math.Triangle");
    services.AddKeyedTransient<IValueGenerator, ConstantGenerator>("Math.Constant");
});