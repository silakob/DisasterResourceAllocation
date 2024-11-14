namespace DisasterResourceAllocation.Models;

public class Assignment
{
    public Guid AreaID { get; set; }
    public Guid TruckID { get; set; }
    public Dictionary<string, int> ResourcesDelivered { get; set; }
}