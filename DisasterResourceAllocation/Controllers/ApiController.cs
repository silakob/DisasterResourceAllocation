using DisasterResourceAllocation.Models;
using DisasterResourceAllocation.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DisasterResourceAllocation.Controllers;

[Route("api")]
public class ApiController(
    IResourceAllocationRepository resourceAllocationRepository) : ControllerBase
{
    [HttpPost("areas")]
    public async Task<IActionResult> AddAffectedAreas([FromBody] List<AffectedArea> affectedAreas)
    {
        try
        {
            if (ModelState.IsValid)
            {
                int addedNo = await resourceAllocationRepository.AddAffectedAreas(affectedAreas);
                return Ok($"{addedNo} Areas affected was added.");
            }
            else
            {
                return BadRequest(ModelState);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return BadRequest(e);
        }
    }

    [HttpPost("trucks")]
    public async Task<IActionResult> AddTrucks([FromBody] List<ResourceTruck> resourceTrucks)
    {
        try
        {
            if (ModelState.IsValid)
            {
                int addedNo = await resourceAllocationRepository.AddResourceTrucks(resourceTrucks);
                return Ok($"{addedNo} Trucks was added.");
            }
            else
            {
                return BadRequest(ModelState);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return BadRequest(e);
        }
    }

    [HttpPost("assignments")]
    public async Task<IActionResult> ProcessAssignments()
    {
        try
        {
            var resourceTrucks = await resourceAllocationRepository.GetResourceTrucks();
            var affectedAreas = await resourceAllocationRepository.GetAffectedAreas();
            List<Assignment> assignments = await resourceAllocationRepository.ProcessAssignments(resourceTrucks, affectedAreas);
            await resourceAllocationRepository.StoredAssignmentsToRedis(assignments);
            return Ok(assignments);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return BadRequest(e);
        }
    }

    [HttpGet("assignments")]
    public async Task<IActionResult> GetLastAssignments()
    {
        try
        {
            List<Assignment> assignments = await resourceAllocationRepository.GetLastAssignments();
            return Ok(assignments);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return BadRequest(e);
        }
    }

    [HttpDelete("assignments")]
    public async Task<IActionResult> RemoveAssignments()
    {
        try
        {
            await resourceAllocationRepository.RemoveAssignments();
            return Ok("All assignments removed.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return BadRequest(e);
        }
    }
}