namespace DisasterResourceAllocation.Models;

public class ResourceTruck
{
    public Guid TruckId { get; set; }
    public Dictionary<string, int> AvailableResource { get; set; }
    public Dictionary<Guid, int> TravelTimeToArea { get; set; }
}