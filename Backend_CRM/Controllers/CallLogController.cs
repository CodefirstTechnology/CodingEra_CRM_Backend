using CRM.DATA;
using CRM.DTO;
using CRM.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    [Route("api/callLogs")]
    [ApiController]
    public class CallLogController : ControllerBase
    {
        private readonly TaskDbcontext _context;

        public CallLogController(TaskDbcontext context)
        {
            _context = context;
        }

        // ADD CALL
        [HttpPost("AddCall")]
        public async Task<IActionResult> AddCall([FromBody] CallLogUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var call = CrmWriteMappings.ToCallLog(dto, 0);
            call.CallId = 0;

            await _context.CallLogs.AddAsync(call);
            await _context.SaveChangesAsync();

            return Ok(call);
        }

        // GET ALL CALLS
        [HttpGet("GetCalls")]
        public async Task<IActionResult> GetCalls()
        {
            var data = await _context.CallLogs.ToListAsync();

            return Ok(data);
        }

        // UPDATE CALL
        [HttpPut("UpdateCall/{id}")]
        public async Task<IActionResult> UpdateCall(int id, [FromBody] CallLogUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            if (dto.CallId != 0 && dto.CallId != id)
            {
                return BadRequest("Route id and body callId must match when the body includes a call id.");
            }

            var existingCall = await _context.CallLogs.FindAsync(id);

            if (existingCall == null)
            {
                return NotFound();
            }

            CrmWriteMappings.Apply(existingCall, dto);

            await _context.SaveChangesAsync();

            return Ok(existingCall);
        }

        // DELETE CALL
        [HttpDelete("DeleteCall/{id}")]
        public async Task<IActionResult> DeleteCall(int id)
        {
            var call = await _context.CallLogs.FindAsync(id);

            if (call == null)
            {
                return NotFound();
            }

            _context.CallLogs.Remove(call);

            await _context.SaveChangesAsync();

            return Ok("Deleted Successfully");
        }
    }
}