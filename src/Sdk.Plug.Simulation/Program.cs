using Meshmakers.Octo.Sdk.Common.Plugs;
using Microsoft.Extensions.DependencyInjection;
using Sdk.Plug.Simulation;
using TeaTime;


using (var tf = TeaFile<Tick>.Create("acme.tea"))
{
    tf.Write(new Tick { Time = DateTime.Now, Attributes = { { "Price", 5 }, { "Volume", 700 }  }  });
    tf.Write(new Tick { Time = DateTime.Now, Attributes = { { "Price", 15 }, { "Volume", 1700 }  } } );
    // ...
}
// sum the prices of all items in the file
using (var tf = TeaFile<Tick>.OpenRead("acme.tea"))
{
    var x = tf.Items.Sum(item => (int)item.Attributes["Price"]);
    Console.WriteLine("Sum of all prices: " + x);
}


// var plugBuilder = new PlugBuilder();
//
// plugBuilder.Run(args, (_, services) => { services.AddSingleton<IPlugService, SimulationPlugService>(); });