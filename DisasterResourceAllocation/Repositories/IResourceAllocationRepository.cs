using DisasterResourceAllocation.Models;

namespace DisasterResourceAllocation.Repositories;

public interface IResourceAllocationRepository
{
    Task<List<ResourceTruck>> GetResourceTrucks();
    Task<int> AddResourceTrucks(List<ResourceTruck> resourceTrucks);
    Task<List<AffectedArea>> GetAffectedAreas();
    Task<int> AddAffectedAreas(List<AffectedArea> affectedAreas);
    Task RemoveAssignments();
    Task<List<Assignment>> ProcessAssignments(List<ResourceTruck> resourceTrucks, List<AffectedArea> affectedAreas);
    Task StoredAssignmentsToRedis(List<Assignment> assignments);
    Task<List<Assignment>> GetLastAssignments();
}