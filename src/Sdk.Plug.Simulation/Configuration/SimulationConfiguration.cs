namespace Sdk.Plug.Simulation.Configuration;

public class SimulationConfiguration
{
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(1);
    public SimulationTypes SimulationTypes { get; set; } = SimulationTypes.Sinus;
    
    public int? ScaleInputMin { get; set; }
    public int? ScaleInputMax { get; set; }
    public int? ScaleOutputMin { get; set; }
    public int? ScaleOutputMax { get; set; }
}