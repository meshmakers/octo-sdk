using System.Diagnostics;

namespace Sdk.ServiceClient.SystemTests;

public class FireGuardiansOptions
{
    public string AssetServiceUrl { get; set; } = "https://localhost:5001";
    public string AuthorityUrl { get; set; } = "https://localhost:5003";
    public string PublicUrl { get; set; } = Debugger.IsAttached ? "https://localhost:44486" : "https://localhost:7171";
    public string TenantId { get; set; } = "fireguardians";
    public string VapidPublicKey { get; set; } = "BBOj-esXJSfksOSFWd06_dDOletUn3XRvzY4IsOYSb_1ora5Vdi8SrmOAWya1g9vZcPhq-lGu_wWocEoBRID5Pk";
    public string VapidPrivateKey { get; set; } = "P4dBHPm7XH1L5JzkUcDQY6jo5NoJkD4ByJK1P1LwKTo";
    
    public string ClientId { get; set; } = "fire-guardians-app-backend";
    public string ClientSecret { get; set; } = "l8L@w5iEv*Ym";
}
