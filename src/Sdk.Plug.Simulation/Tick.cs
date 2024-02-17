using TeaTime;

namespace Sdk.Plug.Simulation;

public struct Tick
{
    public Tick()
    {
        Attributes = new Dictionary<string, object>();
    }
    
    public Time Time;
    
    public Dictionary<string, object> Attributes;
}