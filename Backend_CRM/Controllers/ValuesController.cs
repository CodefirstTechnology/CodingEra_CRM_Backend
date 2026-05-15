using CRM.DATA;
using CRM.DTO;
using CRM.models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly TaskDbcontext _context;

        public ValuesController(TaskDbcontext context)
        {
            _context = context;

        }

        //add task

        [HttpPost("Addtask")]
        public async Task<IActionResult> AddTask([FromBody] TaskUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Task cannot be null");
            }

            var task = CrmWriteMappings.ToTask(dto, 0);
            task.TaskId = 0;

            await _context.Tasks.AddAsync(task);
            await _context.SaveChangesAsync();
            return Ok(task);
        }

        [HttpGet("Gettask")]
        public async Task<IActionResult> Gettask()
        {
            var task = await _context.Tasks.ToListAsync();

            return Ok(task);
        }

        //delete task

        [HttpDelete("Deletetask/{id}")]
        public async Task<IActionResult> Deletetask(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound("Task not found");
            }
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return Ok("Task deleted successfully");


        }

        //update task

        [HttpPut("Updatetask/{id}")]
        public async Task<IActionResult> Updatetask(int id, [FromBody] TaskUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Invalid task data");
            }

            if (dto.TaskId != 0 && dto.TaskId != id)
            {
                return BadRequest("Route id and body taskId must match when the body includes a task id.");
            }
            var existingTask = await _context.Tasks.FindAsync(id);
            if (existingTask == null)
            {
                return NotFound("Task not found");
            }
            CrmWriteMappings.Apply(existingTask, dto);
            _context.Tasks.Update(existingTask);
            await _context.SaveChangesAsync();
            return Ok(existingTask);
        }




    }
}