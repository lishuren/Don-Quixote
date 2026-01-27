using Microsoft.EntityFrameworkCore;
using MapPlannerApi.Entities;
using TaskStatus = MapPlannerApi.Entities.TaskStatus;

namespace MapPlannerApi.Data;

/// <summary>
/// Repository for RobotTask CRUD operations.
/// </summary>
public class TaskRepository
{
    private readonly RestaurantDbContext _db;

    public TaskRepository(RestaurantDbContext db)
    {
        _db = db;
    }

    public async Task<List<RobotTask>> GetAllAsync(int? limit = null)
    {
        var query = _db.Tasks
            .Include(t => t.Robot)
            .Include(t => t.TargetTable)
            .OrderByDescending(t => t.CreatedAt)
            .AsQueryable();

        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync();
    }

    public async Task<RobotTask?> GetByIdAsync(int id)
    {
        return await _db.Tasks
            .Include(t => t.Robot)
            .Include(t => t.TargetTable)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<RobotTask>> GetByStatusAsync(TaskStatus status)
    {
        return await _db.Tasks
            .Include(t => t.Robot)
            .Include(t => t.TargetTable)
            .Where(t => t.Status == status)
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<RobotTask>> GetPendingTasksAsync()
    {
        return await _db.Tasks
            .Include(t => t.TargetTable)
            .Where(t => t.Status == TaskStatus.Pending)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<RobotTask>> GetByRobotAsync(int robotId)
    {
        return await _db.Tasks
            .Include(t => t.TargetTable)
            .Where(t => t.RobotId == robotId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<RobotTask?> GetCurrentTaskForRobotAsync(int robotId)
    {
        return await _db.Tasks
            .Include(t => t.TargetTable)
            .Where(t => t.RobotId == robotId && 
                   (t.Status == TaskStatus.Assigned || t.Status == TaskStatus.InProgress))
            .FirstOrDefaultAsync();
    }

    public async Task<RobotTask> CreateAsync(RobotTask task)
    {
        task.CreatedAt = DateTime.UtcNow;
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        return task;
    }

    public async Task<RobotTask> UpdateAsync(RobotTask task)
    {
        _db.Tasks.Update(task);
        await _db.SaveChangesAsync();
        return task;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task == null) return false;

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task AssignToRobotAsync(int taskId, int robotId)
    {
        var task = await _db.Tasks.FindAsync(taskId);
        if (task != null)
        {
            task.RobotId = robotId;
            task.Status = TaskStatus.Assigned;
            await _db.SaveChangesAsync();
        }
    }

    public async Task StartTaskAsync(int taskId)
    {
        var task = await _db.Tasks.FindAsync(taskId);
        if (task != null)
        {
            task.Status = TaskStatus.InProgress;
            task.StartedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task CompleteTaskAsync(int taskId)
    {
        var task = await _db.Tasks.FindAsync(taskId);
        if (task != null)
        {
            task.Status = TaskStatus.Completed;
            task.CompletedAt = DateTime.UtcNow;
            if (task.StartedAt.HasValue)
            {
                task.ActualDurationSeconds = (int)(DateTime.UtcNow - task.StartedAt.Value).TotalSeconds;
            }
            await _db.SaveChangesAsync();
        }
    }

    public async Task FailTaskAsync(int taskId, string? errorMessage)
    {
        var task = await _db.Tasks.FindAsync(taskId);
        if (task != null)
        {
            task.Status = TaskStatus.Failed;
            task.ErrorMessage = errorMessage;
            task.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<int> GetQueueLengthAsync()
    {
        return await _db.Tasks
            .CountAsync(t => t.Status == TaskStatus.Pending || t.Status == TaskStatus.Assigned);
    }
}
