namespace DisasterResourceAllocation.Models;

public class AffectedArea
{
    public Guid AreaID { get; set; }
    public int UrgencyLevel { get; set; }
    public Dictionary<string, int> RequiredResource { get; set; }
    public int TimeConstraint { get; set; }
}