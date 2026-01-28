using MapPlannerApi.Entities;

namespace MapPlannerApi.Dtos;

// ========== Robot DTOs ==========
public record RobotDto(
    int Id,
    string Name,
    string? Model,
    string Status,
    PositionDto Position,
    double Heading,
    double BatteryLevel,
    double Velocity,
    int? CurrentTaskId,
    bool IsEnabled,
    DateTime LastUpdated
);

public record CreateRobotRequest(
    string Name,
    string? Model,
    PositionDto? Position,
    bool IsEnabled = true
);

public record UpdateRobotRequest(
    string? Name,
    string? Model,
    string? Status,
    bool? IsEnabled
);

public record RobotPositionUpdate(
    double ScreenX,
    double ScreenY,
    double? PhysicalX,
    double? PhysicalY,
    double Heading,
    double Velocity
);

// ========== Position DTOs ==========
public record PositionDto(
    double ScreenX,
    double ScreenY,
    double PhysicalX,
    double PhysicalY
)
{
    public static PositionDto FromEntity(Position pos) =>
        new(pos.ScreenX, pos.ScreenY, pos.PhysicalX, pos.PhysicalY);

    public Position ToEntity() =>
        new(ScreenX, ScreenY, PhysicalX, PhysicalY);
}

// ========== Task DTOs ==========
public record TaskDto(
    int Id,
    string Type,
    string Status,
    string Priority,
    int? RobotId,
    string? RobotName,
    int? TargetTableId,
    string? TargetTableLabel,
    PositionDto StartPosition,
    PositionDto TargetPosition,
    int? EstimatedDurationSeconds,
    int? ActualDurationSeconds,
    string? ErrorMessage,
    int RetryCount,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt
);

public record CreateTaskRequest(
    string Type,
    string Priority = "Normal",
    int? RobotId = null,
    int? TargetTableId = null,
    PositionDto? StartPosition = null,
    PositionDto? TargetPosition = null
);

public record TaskQueueSummary(
    int PendingCount,
    int AssignedCount,
    int InProgressCount,
    int CompletedToday,
    int FailedToday
);

// ========== Table DTOs ==========
public record TableDto(
    int Id,
    string? Label,
    string Shape,
    string Status,
    PositionDto Center,
    double Width,
    double Height,
    double Rotation,
    int Capacity,
    int? ZoneId,
    string? ZoneName,
    int CurrentGuestCount
);

public record CreateTableRequest(
    string? Label,
    string Shape = "Rectangle",
    PositionDto? Center = null,
    double Width = 80,
    double Height = 60,
    double Rotation = 0,
    int Capacity = 4,
    int? ZoneId = null
);

public record UpdateTableRequest(
    string? Label,
    string? Shape,
    string? Status,
    PositionDto? Center,
    double? Width,
    double? Height,
    double? Rotation,
    int? Capacity,
    int? ZoneId
);

public record TableStatusSummary(
    int Total,
    int Available,
    int Occupied,
    int Reserved,
    int NeedsService,
    int Cleaning,
    int OccupancyPercent
);

// ========== Guest DTOs ==========
public record GuestDto(
    int Id,
    string? Name,
    int PartySize,
    string Status,
    int? TableId,
    string? TableLabel,
    int? QueuePosition,
    DateTime ArrivalTime,
    DateTime? SeatedTime,
    DateTime? DepartedTime,
    int? EstimatedWaitMinutes,
    string? Notes,
    string? PhoneNumber,
    int? ReservationId
);

public record CreateGuestRequest(
    string? Name,
    int PartySize = 1,
    string? Notes = null,
    string? PhoneNumber = null,
    int? ReservationId = null
);

public record SeatGuestRequest(
    int TableId
);

public record WaitlistSummary(
    int TotalWaiting,
    int AverageWaitMinutes,
    List<GuestDto> Guests
);

// ========== Zone DTOs ==========
public record ZoneDto(
    int Id,
    string Name,
    string Type,
    double X,
    double Y,
    double Width,
    double Height,
    string? Color,
    bool IsNavigable,
    double? SpeedLimit,
    int TableCount,
    int CheckpointCount
);

public record CreateZoneRequest(
    string Name,
    string Type = "Dining",
    double X = 0,
    double Y = 0,
    double Width = 100,
    double Height = 100,
    string? Color = null,
    bool IsNavigable = true,
    double? SpeedLimit = null
);

// Map config related DTOs
public record TableSizesDto(int t2, int t4, int t6, int t8);
public record TableMixDto(int t2, int t4, int t6, int t8);
public record DiningAreaDto(double X, double Y, double Width, double Height);

public record MapConfigDto(
    double MapWidth,
    double MapHeight,
    double MainAisle,
    double MinorAisle,
    double SafetyBuffer,
    double SeatBuffer,
    string TableShape,
    TableSizesDto TableSizes,
    TableMixDto TableMix,
    int GridSize,
    List<CreateZoneRequest>? Zones = null,
    DiningAreaDto? DiningArea = null,
    List<CreateTableRequest>? Tables = null
);

public record MapEntitiesDto(
    List<ZoneDto> Zones,
    List<TableDto> Tables,
    MapConfigDto? MapConfig = null
);

// ========== Checkpoint DTOs ==========
public record CheckpointDto(
    int Id,
    string Name,
    string Type,
    PositionDto Position,
    double? RequiredHeading,
    int? ZoneId,
    string? ZoneName,
    bool IsMandatoryStop,
    int? RouteOrder,
    int Capacity
);

public record CreateCheckpointRequest(
    string Name,
    string Type = "Waypoint",
    PositionDto? Position = null,
    double? RequiredHeading = null,
    int? ZoneId = null,
    bool IsMandatoryStop = false,
    int? RouteOrder = null,
    int Capacity = 1
);

// ========== Alert DTOs ==========
public record AlertDto(
    int Id,
    string Type,
    string Severity,
    string Title,
    string? Message,
    int? RobotId,
    string? RobotName,
    int? TableId,
    int? TaskId,
    bool IsAcknowledged,
    string? AcknowledgedBy,
    DateTime? AcknowledgedAt,
    bool IsResolved,
    DateTime? ResolvedAt,
    DateTime CreatedAt
);

public record CreateAlertRequest(
    string Type,
    string Severity,
    string Title,
    string? Message = null,
    int? RobotId = null,
    int? TableId = null,
    int? TaskId = null
);

public record AlertSummary(
    int TotalActive,
    int Critical,
    int Errors,
    int Warnings,
    int Unacknowledged
);

// ========== Reservation DTOs ==========
public record ReservationDto(
    int Id,
    string GuestName,
    string? PhoneNumber,
    string? Email,
    int PartySize,
    int? TableId,
    string? TableLabel,
    DateTime ReservationTime,
    int DurationMinutes,
    string Status,
    string? SpecialRequests,
    string? ConfirmationCode,
    DateTime CreatedAt
);

public record CreateReservationRequest(
    string GuestName,
    string? PhoneNumber,
    string? Email,
    int PartySize,
    int? TableId,
    DateTime ReservationTime,
    int DurationMinutes = 90,
    string? SpecialRequests = null
);

public record UpdateReservationRequest(
    string? GuestName,
    string? PhoneNumber,
    string? Email,
    int? PartySize,
    int? TableId,
    DateTime? ReservationTime,
    int? DurationMinutes,
    string? SpecialRequests
);

// ========== Config DTOs ==========
public record ConfigDto(
    string Key,
    string? Value,
    string? Description,
    string ValueType,
    DateTime UpdatedAt
);

public record SetConfigRequest(
    string Value,
    string? Description = null
);

// ========== Dashboard DTOs ==========
public record DashboardSummary(
    RobotSummary Robots,
    TableStatusSummary Tables,
    TaskQueueSummary Tasks,
    AlertSummary Alerts,
    int ActiveGuests,
    int WaitlistCount
);

public record RobotSummary(
    int Total,
    int Idle,
    int Navigating,
    int Delivering,
    int Charging,
    int Error,
    int Offline,
    double AverageBattery
);

// ========== Robot Command DTOs ==========
public record RobotCommandRequest(
    string Command,  // go_to, return_home, pause, resume, emergency_stop, clear_error
    PositionDto? TargetPosition = null,
    int? TargetTableId = null,
    double? Speed = null,
    string? Reason = null
);

public record CommandAcknowledgment(
    int RobotId,
    string Command,
    bool Accepted,
    string? Message,
    int? EstimatedCompletionSeconds,
    DateTime Timestamp
);

public record RobotRecoveryRequest(
    string Action,  // retry_task, return_home, manual_intervention
    int? NewTaskId = null,
    string? Notes = null
);

public record RobotHeartbeatRequest(
    double BatteryLevel,
    PositionDto Position,
    double Heading,
    double Velocity,
    string? DiagnosticCode = null,
    string? ErrorMessage = null
);

public record RobotHeartbeatResponse(
    bool Acknowledged,
    string? PendingCommand,
    int? AssignedTaskId,
    DateTime ServerTime
);

public record RobotHistoryEntry(
    int TaskId,
    string TaskType,
    string Status,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int? DurationSeconds,
    string? TargetTableLabel
);

public record RobotHistoryResponse(
    int RobotId,
    string RobotName,
    List<RobotHistoryEntry> History,
    int TotalTasks,
    int CompletedTasks,
    int FailedTasks,
    double AverageTaskDurationSeconds
);

// ========== Task Chain DTOs ==========
public record TaskChainRequest(
    List<CreateTaskRequest> Tasks,
    bool ContinueOnFailure = false
);

public record TaskChainResponse(
    int ChainId,
    List<int> TaskIds,
    int TotalTasks
);

public record TaskReassignRequest(
    int NewRobotId,
    string? Reason = null
);

public record TaskPartialCompleteRequest(
    int ProgressPercent,
    string? Checkpoint = null,
    string? Notes = null
);

public record TaskStatsResponse(
    int TotalTasks,
    int CompletedTasks,
    int FailedTasks,
    int CancelledTasks,
    double CompletionRate,
    double AverageDurationSeconds,
    Dictionary<string, int> TasksByType,
    Dictionary<string, int> TasksByRobot
);

public record BulkTaskRequest(
    List<int> TaskIds
);

public record BulkAssignRequest(
    List<int> TaskIds,
    int RobotId
);

// ========== Dispatch Engine DTOs ==========
public record DispatchConfig(
    string Algorithm,  // nearest, round_robin, load_balanced, priority
    int MaxTasksPerRobot,
    int MinBatteryForAssignment,
    bool AutoAssignEnabled,
    int AssignmentIntervalSeconds
);

public record DispatchQueueItem(
    int TaskId,
    string TaskType,
    string Priority,
    DateTime CreatedAt,
    int? SuggestedRobotId,
    string? SuggestedRobotName,
    double? EstimatedDistance
);

public record AutoAssignResult(
    int TasksAssigned,
    int TasksSkipped,
    List<TaskAssignment> Assignments
);

public record TaskAssignment(
    int TaskId,
    int RobotId,
    string RobotName,
    string Reason
);

// ========== Simulation DTOs ==========
public record SimulationSessionDto(
    string SessionId,
    string Status,  // Running, Paused, Stopped
    double TimeScale,
    DateTime StartedAt,
    int SpawnedGuests,
    int CompletedTasks
);

public record CreateSimulationRequest(
    double TimeScale = 1.0,
    int InitialGuests = 0,
    bool AutoSpawnGuests = false,
    int SpawnIntervalSeconds = 60
);

public record SpawnGuestRequest(
    string? Name = null,
    int PartySize = 2,
    int WaitToleranceMinutes = 30,
    List<string>? Preferences = null
);

public record SimulationRobotUpdate(
    PositionDto? Position = null,
    double? BatteryLevel = null,
    string? Status = null,
    bool? TriggerError = null,
    string? ErrorType = null
);

public record SimulationMetrics(
    string SessionId,
    TimeSpan ElapsedTime,
    int TotalGuestsServed,
    int TotalTasksCompleted,
    double AverageWaitTimeMinutes,
    double AverageTaskDurationSeconds,
    double RobotUtilizationPercent,
    double TableTurnoverRate
);

// ========== Integration DTOs ==========
public record WebhookDto(
    int Id,
    string Url,
    List<string> Events,
    bool IsActive,
    int FailureCount,
    DateTime? LastTriggeredAt,
    DateTime CreatedAt
);

public record CreateWebhookRequest(
    string Url,
    List<string> Events,
    string? Secret = null
);

public record WebhookTestResult(
    bool Success,
    int StatusCode,
    string? ResponseBody,
    int ResponseTimeMs
);

public record PosOrderRequest(
    string OrderId,
    int TableId,
    List<string> Items,
    string? Notes = null
);

public record EmergencyStopResponse(
    int RobotsAffected,
    List<int> StoppedRobotIds,
    DateTime Timestamp
);

// ========== SignalR Event DTOs ==========
public record RobotPositionEvent(
    int RobotId,
    string RobotName,
    PositionDto Position,
    double Heading,
    double Velocity,
    DateTime Timestamp
);

public record RobotStatusEvent(
    int RobotId,
    string RobotName,
    string OldStatus,
    string NewStatus,
    string? Reason,
    DateTime Timestamp
);

public record TaskStatusEvent(
    int TaskId,
    string TaskType,
    string OldStatus,
    string NewStatus,
    int? RobotId,
    string? RobotName,
    DateTime Timestamp
);

public record AlertEvent(
    int AlertId,
    string Type,
    string Severity,
    string Title,
    string? Message,
    int? RobotId,
    DateTime Timestamp
);

public record TableStatusEvent(
    int TableId,
    string? Label,
    string OldStatus,
    string NewStatus,
    int? GuestId,
    DateTime Timestamp
);

public record GuestEvent(
    int GuestId,
    string? GuestName,
    string EventType,  // Arrived, Seated, Left
    int? TableId,
    string? TableLabel,
    DateTime Timestamp
);
// ========== Simulation DTOs ==========

/// <summary>
/// Request to start a long-running simulation.
/// </summary>
public record StartSimulationRequest(
    DateTime? SimulatedStartTime = null,
    DateTime? SimulatedEndTime = null,
    double AccelerationFactor = 720,
    int RobotCount = 5,
    int TableCount = 20,
    int? RandomSeed = null,
    SimulationEventPatternDto? EventPatterns = null
);

/// <summary>
/// Event pattern configuration for simulation.
/// </summary>
public record SimulationEventPatternDto(
    double[]? HourlyArrivalRates = null,
    double[]? DayOfWeekMultipliers = null,
    int AverageMealDurationMinutes = 45,
    double GuestNeedsHelpProbability = 0.15,
    double AveragePartySize = 2.5
);

/// <summary>
/// Response when starting a simulation.
/// </summary>
public record StartSimulationResponse(
    string SimulationId,
    string Status,
    DateTime SimulatedStartTime,
    DateTime SimulatedEndTime,
    double AccelerationFactor,
    int EstimatedEventsCount,
    TimeSpan EstimatedRealDuration
);

/// <summary>
/// Progress of an active simulation.
/// </summary>
public record SimulationProgressDto(
    string SimulationId,
    string State,
    DateTime SimulatedStartTime,
    DateTime SimulatedEndTime,
    DateTime CurrentSimulatedTime,
    double ProgressPercent,
    string RealElapsedTime,
    string EstimatedTimeRemaining,
    double AccelerationFactor,
    int EventsProcessed,
    int TotalEventsScheduled,
    int GuestsProcessed,
    int TasksCreated,
    int TasksCompleted,
    double CurrentSuccessRate
);

/// <summary>
/// Summary metrics from a completed simulation.
/// </summary>
public record SimulationReportSummaryDto(
    string SimulationId,
    string State,
    DateTime SimulatedStartTime,
    DateTime SimulatedEndTime,
    string SimulatedDuration,
    string RealDuration,
    double AccelerationFactor,
    int TotalGuests,
    int TotalTasks,
    int TotalDeliveries,
    int TotalFailures,
    double OverallSuccessRate,
    double AverageTaskDurationSeconds,
    double AverageRobotUtilization,
    int PeakGuestCount,
    DateTime PeakGuestTime,
    int TotalAlerts,
    Dictionary<string, int> AlertsByType
);

/// <summary>
/// Daily breakdown from simulation report.
/// </summary>
public record SimulationDailyMetricsDto(
    DateTime Date,
    string DayOfWeek,
    int TotalGuests,
    int TotalTasks,
    int TotalDeliveries,
    int TotalFailures,
    double SuccessRate,
    int PeakHour,
    double PeakHourGuests,
    double AverageRobotUtilization
);

/// <summary>
/// Robot performance from simulation.
/// </summary>
public record SimulationRobotMetricsDto(
    int RobotId,
    string RobotName,
    int TasksCompleted,
    int TasksFailed,
    double SuccessRate,
    double AverageTaskDuration,
    double UtilizationPercent,
    double AverageBattery
);

/// <summary>
/// Full simulation report response.
/// </summary>
public record SimulationReportDto(
    SimulationReportSummaryDto Summary,
    List<SimulationDailyMetricsDto> DailyBreakdown,
    List<SimulationRobotMetricsDto> RobotMetrics
);

/// <summary>
/// SignalR event for simulation progress updates.
/// </summary>
public record SimulationProgressEvent(
    string SimulationId,
    string State,
    DateTime CurrentSimulatedTime,
    double ProgressPercent,
    string RealElapsedTime,
    string EstimatedTimeRemaining,
    int EventsProcessed,
    int TotalEventsScheduled,
    int GuestsProcessed,
    int TasksCreated,
    int TasksCompleted,
    int TasksFailed,
    double CurrentSuccessRate,
    DateTime Timestamp
);