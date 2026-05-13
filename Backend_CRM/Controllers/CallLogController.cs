using CRM.DATA;
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
        public async Task<IActionResult> AddCall([FromBody] CallLog call)
        {
            if (call == null)
            {
                return BadRequest();
            }

            call.CallId = 0;
            call.LastModified = DateTime.UtcNow;

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
        public async Task<IActionResult> UpdateCall(int id, [FromBody] CallLog updatedCall)
        {
            if (updatedCall == null)
            {
                return BadRequest();
            }

            if (updatedCall.CallId != 0 && updatedCall.CallId != id)
            {
                return BadRequest("Route id and body callId must match when the body includes a call id.");
            }

            var existingCall = await _context.CallLogs.FindAsync(id);

            if (existingCall == null)
            {
                return NotFound();
            }

            existingCall.ContactName = updatedCall.ContactName;
            existingCall.Direction = updatedCall.Direction;
            existingCall.PhoneNumber = updatedCall.PhoneNumber;
            existingCall.ContactCompany = updatedCall.ContactCompany;
            existingCall.CallStarted = updatedCall.CallStarted;
            existingCall.DurationMinutes = updatedCall.DurationMinutes;
            existingCall.DurationSeconds = updatedCall.DurationSeconds;
            existingCall.Outcome = updatedCall.Outcome;
            existingCall.CallSummary = updatedCall.CallSummary;
            existingCall.ContactId = updatedCall.ContactId;
            existingCall.RelatedLeadId = updatedCall.RelatedLeadId;
            existingCall.RelatedDealId = updatedCall.RelatedDealId;
            existingCall.LastModified = DateTime.UtcNow;

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