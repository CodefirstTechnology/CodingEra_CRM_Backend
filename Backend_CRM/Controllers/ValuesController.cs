using CRM.DATA;
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
        public async Task<IActionResult> AddTask([FromBody] TaskTable task)
        {
            if (task == null)
            {
                return BadRequest("Task cannot be null");
            }

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
        public async Task<IActionResult> Updatetask(int id, [FromBody] TaskTable updatedTask)
        {
            if (updatedTask == null)
            {
                return BadRequest("Invalid task data");
            }

            if (updatedTask.TaskId != 0 && updatedTask.TaskId != id)
            {
                return BadRequest("Route id and body taskId must match when the body includes a task id.");
            }
            var existingTask = await _context.Tasks.FindAsync(id);
            if (existingTask == null)
            {
                return NotFound("Task not found");
            }
            existingTask.TaskTitle = updatedTask.TaskTitle;
            existingTask.TaskDescription = updatedTask.TaskDescription;
            existingTask.TaskStatus = updatedTask.TaskStatus;
            existingTask.TaskAssignee = updatedTask.TaskAssignee;
            existingTask.TaskDueDate = updatedTask.TaskDueDate;
            existingTask.TaskPriority = updatedTask.TaskPriority;
            existingTask.AssigneeUserId = updatedTask.AssigneeUserId;
            existingTask.RelatedLeadId = updatedTask.RelatedLeadId;
            existingTask.RelatedDealId = updatedTask.RelatedDealId;
            _context.Tasks.Update(existingTask);
            await _context.SaveChangesAsync();
            return Ok(existingTask);
        }




    }
}