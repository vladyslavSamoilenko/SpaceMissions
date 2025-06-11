using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpaceMissions.Core.Entities;
using SpaceMissions.Infrastructure.Data;
using SpaceMissions.WebAP.DTOs;

namespace SpaceMissions.WebAP.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RocketsController : ControllerBase
{
    private readonly SpaceMissionsDbContext _context;

    public RocketsController(SpaceMissionsDbContext context)
    {
        _context = context;
    }

    // GET: api/Rockets
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RocketDto>>> GetRockets()
    {
        var rockets = await _context.Rockets
            .Select(r => new RocketDto
            {
                Id = r.Id,
                Name = r.Name,
                IsActive = r.IsActive
            })
            .ToListAsync();

        return Ok(rockets);
    }

    // GET: api/Rockets/5
    [HttpGet("{id}")]
    public async Task<ActionResult<RocketDto>> GetRocket(int id)
    {
        var rocket = await _context.Rockets.FindAsync(id);

        if (rocket == null) return NotFound();

        return new RocketDto
        {
            Id = rocket.Id,
            Name = rocket.Name,
            IsActive = rocket.IsActive
        };
    }

    // POST: api/Rockets
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<RocketDto>> PostRocket(RocketDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var rocket = new Rocket
        {
            Name = dto.Name,
            IsActive = dto.IsActive
        };

        _context.Rockets.Add(rocket);
        await _context.SaveChangesAsync();

        dto.Id = rocket.Id;
        return CreatedAtAction(nameof(GetRocket), new { id = rocket.Id }, dto);
    }

    // PUT: api/Rockets/5
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> PutRocket(int id, RocketDto dto)
    {
        if (id != dto.Id)
            return BadRequest("ID в URL и теле не совпадают.");

        var existingRocket = await _context.Rockets.FindAsync(id);
        if (existingRocket == null)
            return NotFound();

        existingRocket.Name     = dto.Name;
        existingRocket.IsActive = dto.IsActive;

        _context.Entry(existingRocket).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }



    // DELETE: api/Rockets/5
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteRocket(int id)
    {
        var rocket = await _context.Rockets.FindAsync(id);
        if (rocket == null) return NotFound();

        _context.Rockets.Remove(rocket);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    
    // GET: api/Rockets/5/missions
    [HttpGet("{id}/missions")]
    public async Task<ActionResult<IEnumerable<MissionDto>>> GetMissionsByRocketId(int id)
    {
        var rocket = await _context.Rockets
            .Include(r => r.Missions)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rocket == null) return NotFound();

        var missions = rocket.Missions?.Select(m => new MissionDto
        {
            Id = m.Id,
            MissionName = m.MissionName,
            LaunchDateTime = m.LaunchDateTime,
            Company = m.Company,
            Location = m.Location,
            MissionStatus = m.MissionStatus,
            Price = m.Price,
            RocketId = m.RocketId,
            RocketName = rocket.Name
        }).ToList();

        return Ok(missions);
    }

}
