using System.Text;
using System.Text.Json;
using DisasterResourceAllocation.Models;
using DisasterResourceAllocation.Shares;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace DisasterResourceAllocation.Repositories;

public class ResourceAllocationRepository(IDistributedCache cache, IConfiguration configuration)
    : IResourceAllocationRepository
{
    // for connect redis db
    private readonly ConnectionMultiplexer _connectionMultiplexer = ConnectionMultiplexer.Connect(
        new ConfigurationOptions
        {
            EndPoints = { configuration[$"RedisEndPoint"]! },
            AllowAdmin = true,
            AbortOnConnectFail = false
        });

    public async Task<List<ResourceTruck>> GetResourceTrucks()
    {
        try
        {
            var resourceTruckKeys = _connectionMultiplexer.GetServer(configuration[$"RedisEndPoint"]!)
                .Keys()
                .Where(key => key.ToString().Contains(Constant.ResourceTruck))
                .ToList();
            List<ResourceTruck> resourceTrucks = new();
            foreach (var resourceTruckKey in resourceTruckKeys)
            {
                string cacheKey = resourceTruckKey.ToString().Replace($"DisasterResourceAllocationRedis", string.Empty);
                string? resourceTruckValue = await cache.GetStringAsync(cacheKey);
                if (resourceTruckValue != null)
                {
                    ResourceTruck resourceTruck = JsonSerializer.Deserialize<ResourceTruck>(resourceTruckValue)!;
                    resourceTrucks.Add(resourceTruck);
                }
            }

            return await Task.FromResult(resourceTrucks);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<int> AddResourceTrucks(List<ResourceTruck> resourceTrucks)
    {
        try
        {
            int addedNo = 0;
            foreach (var resourceTruck in resourceTrucks)
            {
                string cacheKey = string.Concat(Constant.ResourceTruck, resourceTruck.TruckId.ToString());
                string? result = await cache.GetStringAsync(cacheKey);
                if (string.IsNullOrEmpty(result))
                {
                    addedNo++;
                    var jsonData = JsonSerializer.Serialize(resourceTruck, JsonSerializerOptions.Default);
                    var bytes = Encoding.UTF8.GetBytes(jsonData);
                    await cache.SetAsync(cacheKey, bytes, new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(60)));
                }
            }

            return addedNo;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<List<AffectedArea>> GetAffectedAreas()
    {
        try
        {
            var affectedAreaKeys = _connectionMultiplexer.GetServer(configuration["RedisEndPoint"]!)
                .Keys()
                .Where(key => key.ToString().Contains(Constant.AffectedArea))
                .ToList();
            List<AffectedArea> affectedAreas = new();
            foreach (var affectedAreaKey in affectedAreaKeys)
            {
                string cacheKey = affectedAreaKey.ToString().Replace("DisasterResourceAllocationRedis", string.Empty);
                string? affectedAreaValue = await cache.GetStringAsync(cacheKey);
                if (affectedAreaValue != null)
                {
                    AffectedArea affectedArea = JsonSerializer.Deserialize<AffectedArea>(affectedAreaValue)!;
                    affectedAreas.Add(affectedArea);
                }
            }

            return await Task.FromResult(affectedAreas
                .OrderBy(by => by.UrgencyLevel)
                .ToList());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<int> AddAffectedAreas(List<AffectedArea> affectedAreas)
    {
        try
        {
            int addedNo = 0;
            foreach (var affectedArea in affectedAreas)
            {
                string cacheKey = string.Concat(Constant.AffectedArea, affectedArea.AreaID.ToString());
                string? result = await cache.GetStringAsync(cacheKey);
                if (string.IsNullOrEmpty(result))
                {
                    addedNo++;
                    var jsonData = JsonSerializer.Serialize(affectedArea, JsonSerializerOptions.Default);
                    var bytes = Encoding.UTF8.GetBytes(jsonData);
                    await cache.SetAsync(cacheKey, bytes, new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(60)));
                }
            }

            return addedNo;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task RemoveAssignments()
    {
        try
        {
            var server = _connectionMultiplexer.GetServer(configuration["RedisEndPoint"]!);
            var assignmentKeys = server
                .Keys()
                .Where(key => key.ToString().Contains(Constant.Assignment))
                .ToList();
            foreach (var assignmentKey in assignmentKeys)
            {
                var cacheKey = assignmentKey.ToString().Replace("DisasterResourceAllocationRedis", string.Empty);
                await cache.RemoveAsync(cacheKey);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<List<Assignment>> ProcessAssignments(List<ResourceTruck> resourceTrucks,
        List<AffectedArea> affectedAreas)
    {
        try
        {
            List<Assignment> assignments = new();

            foreach (var affectedArea in affectedAreas) // loop areas
            {
                foreach (var requiredResourceType in affectedArea.RequiredResource.Keys) // loop resources type
                {
                    var matchResourceTrucks = resourceTrucks
                        .Where(condition => condition.AvailableResource.ContainsKey(requiredResourceType) &&
                                            condition.TravelTimeToArea.ContainsKey(affectedArea.AreaID))
                        .OrderBy(by => by.TravelTimeToArea[affectedArea.AreaID])
                        .ToList();

                    foreach (var matchResourceTruck in matchResourceTrucks) // loop resource trucks
                    {
                        if (matchResourceTruck.TravelTimeToArea[affectedArea.AreaID] <= affectedArea.TimeConstraint)
                        {
                            var assignment = new Assignment
                            {
                                AreaID = affectedArea.AreaID,
                                TruckID = matchResourceTruck.TruckId
                            };

                            CalculateResource(requiredResourceType, affectedArea, matchResourceTruck, assignment);
                            AddAssignment(assignments, assignment);
                        }
                    }
                }
            }

            return await Task.FromResult(assignments);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task StoredAssignmentsToRedis(List<Assignment> assignments)
    {
        try
        {
            string cacheKey = string.Concat(Constant.Assignment, DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd-HH-mm-ss"));
            string? result = await cache.GetStringAsync(cacheKey);
            if (string.IsNullOrEmpty(result))
            {
                var jsonData = JsonSerializer.Serialize(assignments, JsonSerializerOptions.Default);
                var bytes = Encoding.UTF8.GetBytes(jsonData);
                /*
                 * Q: Assignment Persistence: Persist the assignment results in Redis with an expiration time of 30 minutes.
                 */
                await cache.SetAsync(cacheKey, bytes, new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(30)));
            }

            await Task.CompletedTask;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<List<Assignment>> GetLastAssignments()
    {
        try
        {
            var server = _connectionMultiplexer.GetServer(configuration["RedisEndPoint"]!);
            var lastAssignmentKey = server
                .Keys()
                .OrderBy(key => key.ToString())
                .LastOrDefault(key => key.ToString().Contains(Constant.Assignment));
            if (string.IsNullOrEmpty(lastAssignmentKey))
            {
                return null!;
            }

            var cacheKey = lastAssignmentKey.ToString().Replace("DisasterResourceAllocationRedis", string.Empty);
            string assignmentValue = (await cache.GetStringAsync(cacheKey))!;
            List<Assignment> assignments = JsonSerializer.Deserialize<List<Assignment>>(assignmentValue)!;
            return await Task.FromResult(assignments);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private void CalculateResource(string requiredResourceType, AffectedArea affectedArea,
        ResourceTruck matchResourceTruck, Assignment assignment)
    {
        try
        {
            if (affectedArea.RequiredResource[requiredResourceType] <=
                matchResourceTruck.AvailableResource[requiredResourceType])
            {
                // affectedArea has enough resource
                assignment.ResourcesDelivered = new()
                {
                    {
                        requiredResourceType,
                        affectedArea.RequiredResource[requiredResourceType]
                    }
                };
                matchResourceTruck.AvailableResource[requiredResourceType] -=
                    affectedArea.RequiredResource[requiredResourceType]; // adjust required resource in a truck
            }
            else
            {
                // affectedArea has not enough resource
                assignment.ResourcesDelivered = new()
                    { { requiredResourceType, matchResourceTruck.AvailableResource[requiredResourceType] } };
                affectedArea.RequiredResource[requiredResourceType] -=
                    matchResourceTruck.AvailableResource[requiredResourceType]; // adjust required resource in an area
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private void AddAssignment(List<Assignment> assignments, Assignment assignment)
    {
        try
        {
            if (assignments.Exists(element =>
                    element.AreaID == assignment.AreaID && element.TruckID == assignment.TruckID))
            {
                // already exists in assignments
                var existingAssignment = assignments
                    .First(element => element.AreaID == assignment.AreaID &&
                                      element.TruckID == assignment.TruckID);
                existingAssignment.ResourcesDelivered.Add(assignment.ResourcesDelivered.Keys.First(),
                    assignment.ResourcesDelivered.Values.First());
            }
            else
            {
                // not exists in assignments
                assignments.Add(assignment);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}