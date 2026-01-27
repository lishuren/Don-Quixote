using System.Collections.Concurrent;
using MapPlannerApi.Entities;

namespace MapPlannerApi.Services.Simulation;

/// <summary>
/// Represents a scheduled event in the simulation.
/// </summary>
public record ScheduledEvent(
    DateTime ScheduledTime,
    TriggerEventType EventType,
    int? TableId = null,
    int? GuestId = null,
    TaskPriority Priority = TaskPriority.Normal,
    string? Notes = null
)
{
    public Guid Id { get; init; } = Guid.NewGuid();
}

/// <summary>
/// Configuration for restaurant event generation patterns.
/// </summary>
public record EventPatternConfig
{
    /// <summary>Average guests per hour by hour of day (0-23). Index = hour.</summary>
    public double[] HourlyArrivalRates { get; init; } = DefaultHourlyRates;
    
    /// <summary>Multiplier by day of week. Sunday = 0, Saturday = 6.</summary>
    public double[] DayOfWeekMultipliers { get; init; } = DefaultDayMultipliers;
    
    /// <summary>Average meal duration in minutes.</summary>
    public int AverageMealDurationMinutes { get; init; } = 45;
    
    /// <summary>Standard deviation for meal duration in minutes.</summary>
    public int MealDurationStdDev { get; init; } = 15;
    
    /// <summary>Probability that a guest will need help during their visit (0-1).</summary>
    public double GuestNeedsHelpProbability { get; init; } = 0.15;
    
    /// <summary>Average party size.</summary>
    public double AveragePartySize { get; init; } = 2.5;
    
    /// <summary>Maximum party size.</summary>
    public int MaxPartySize { get; init; } = 8;
    
    /// <summary>Probability of food order per guest.</summary>
    public double FoodOrderProbability { get; init; } = 0.9;
    
    /// <summary>Probability of drink order per guest.</summary>
    public double DrinkOrderProbability { get; init; } = 0.7;
    
    /// <summary>Average food prep time in minutes.</summary>
    public int AverageFoodPrepMinutes { get; init; } = 15;
    
    /// <summary>Average drink prep time in minutes.</summary>
    public int AverageDrinkPrepMinutes { get; init; } = 5;
    
    // Default hourly rates (typical restaurant pattern)
    private static readonly double[] DefaultHourlyRates = new double[]
    {
        0,    // 00:00 - Closed
        0,    // 01:00 - Closed
        0,    // 02:00 - Closed
        0,    // 03:00 - Closed
        0,    // 04:00 - Closed
        0,    // 05:00 - Closed
        0,    // 06:00 - Closed
        2,    // 07:00 - Breakfast early
        8,    // 08:00 - Breakfast peak
        6,    // 09:00 - Breakfast late
        3,    // 10:00 - Mid-morning
        10,   // 11:00 - Lunch early
        20,   // 12:00 - Lunch peak
        18,   // 13:00 - Lunch
        8,    // 14:00 - Lunch late
        3,    // 15:00 - Afternoon
        4,    // 16:00 - Early dinner
        12,   // 17:00 - Dinner early
        25,   // 18:00 - Dinner peak
        22,   // 19:00 - Dinner
        15,   // 20:00 - Dinner late
        8,    // 21:00 - Late dining
        3,    // 22:00 - Closing
        0     // 23:00 - Closed
    };
    
    // Default day multipliers
    private static readonly double[] DefaultDayMultipliers = new double[]
    {
        1.3,  // Sunday - Brunch traffic
        0.7,  // Monday - Slowest day
        0.8,  // Tuesday
        0.9,  // Wednesday
        1.0,  // Thursday
        1.4,  // Friday - Weekend starts
        1.5   // Saturday - Busiest
    };
}

/// <summary>
/// Interface for generating realistic restaurant events for simulation.
/// </summary>
public interface IRestaurantEventGenerator
{
    /// <summary>
    /// Generates all events for a simulation period.
    /// </summary>
    List<ScheduledEvent> GenerateEventsForPeriod(DateTime start, DateTime end, EventPatternConfig? config = null);
    
    /// <summary>
    /// Gets the expected number of guests for a specific hour.
    /// </summary>
    double GetExpectedGuestsForHour(DateTime dateTime, EventPatternConfig? config = null);
    
    /// <summary>
    /// Generates a random meal duration based on config.
    /// </summary>
    TimeSpan GenerateMealDuration(EventPatternConfig? config = null);
}

/// <summary>
/// Generates realistic restaurant events based on time-of-day and day-of-week patterns.
/// </summary>
public class RestaurantEventGenerator : IRestaurantEventGenerator
{
    private readonly Random _random;
    private readonly ILogger<RestaurantEventGenerator>? _logger;
    
    public RestaurantEventGenerator(ILogger<RestaurantEventGenerator>? logger = null)
    {
        _random = new Random();
        _logger = logger;
    }
    
    public RestaurantEventGenerator(int seed, ILogger<RestaurantEventGenerator>? logger = null)
    {
        _random = new Random(seed);
        _logger = logger;
    }
    
    public List<ScheduledEvent> GenerateEventsForPeriod(DateTime start, DateTime end, EventPatternConfig? config = null)
    {
        config ??= new EventPatternConfig();
        var events = new List<ScheduledEvent>();
        
        // Track active "guests" (virtual) for generating departure and service events
        var activeGuests = new List<(int GuestId, int TableId, DateTime SeatedTime, DateTime ExpectedDeparture, bool OrderedFood, bool OrderedDrinks)>();
        int guestIdCounter = 1;
        int tableIdCounter = 1;
        
        // Iterate through each hour of the simulation period
        var currentHour = new DateTime(start.Year, start.Month, start.Day, start.Hour, 0, 0);
        
        while (currentHour < end)
        {
            var expectedGuests = GetExpectedGuestsForHour(currentHour, config);
            
            // Use Poisson-like distribution for number of arrivals this hour
            var numArrivals = GeneratePoissonCount(expectedGuests);
            
            // Generate arrival events spread throughout the hour
            for (int i = 0; i < numArrivals; i++)
            {
                var arrivalMinute = _random.Next(0, 60);
                var arrivalTime = currentHour.AddMinutes(arrivalMinute);
                
                if (arrivalTime < start || arrivalTime >= end)
                    continue;
                
                var guestId = guestIdCounter++;
                var tableId = (tableIdCounter++ % 20) + 1; // Cycle through 20 tables
                
                // Guest arrives
                events.Add(new ScheduledEvent(
                    ScheduledTime: arrivalTime,
                    EventType: TriggerEventType.GuestArrived,
                    GuestId: guestId,
                    Priority: TaskPriority.Normal,
                    Notes: $"Party of {GeneratePartySize(config)}"
                ));
                
                // Guest gets seated (1-5 min after arrival)
                var seatedTime = arrivalTime.AddMinutes(_random.Next(1, 6));
                events.Add(new ScheduledEvent(
                    ScheduledTime: seatedTime,
                    EventType: TriggerEventType.GuestSeated,
                    GuestId: guestId,
                    TableId: tableId,
                    Priority: TaskPriority.Normal
                ));
                
                // Determine meal duration and departure
                var mealDuration = GenerateMealDuration(config);
                var departureTime = seatedTime + mealDuration;
                
                // Generate food/drink orders
                var orderedFood = _random.NextDouble() < config.FoodOrderProbability;
                var orderedDrinks = _random.NextDouble() < config.DrinkOrderProbability;
                
                if (orderedDrinks)
                {
                    var drinkReadyTime = seatedTime.AddMinutes(_random.Next(3, config.AverageDrinkPrepMinutes + 5));
                    events.Add(new ScheduledEvent(
                        ScheduledTime: drinkReadyTime,
                        EventType: TriggerEventType.DrinkReady,
                        TableId: tableId,
                        Priority: TaskPriority.Normal,
                        Notes: $"Drinks for table {tableId}"
                    ));
                }
                
                if (orderedFood)
                {
                    var foodReadyTime = seatedTime.AddMinutes(_random.Next(10, config.AverageFoodPrepMinutes + 10));
                    events.Add(new ScheduledEvent(
                        ScheduledTime: foodReadyTime,
                        EventType: TriggerEventType.FoodReady,
                        TableId: tableId,
                        Priority: TaskPriority.High,
                        Notes: $"Food for table {tableId}"
                    ));
                }
                
                // Maybe guest needs help
                if (_random.NextDouble() < config.GuestNeedsHelpProbability)
                {
                    var helpTime = seatedTime.AddMinutes(_random.Next(5, (int)mealDuration.TotalMinutes - 5));
                    if (helpTime < departureTime)
                    {
                        events.Add(new ScheduledEvent(
                            ScheduledTime: helpTime,
                            EventType: TriggerEventType.GuestNeedsHelp,
                            GuestId: guestId,
                            TableId: tableId,
                            Priority: TaskPriority.High
                        ));
                    }
                }
                
                // Guest requests check (5-10 min before leaving)
                var checkTime = departureTime.AddMinutes(-_random.Next(5, 11));
                events.Add(new ScheduledEvent(
                    ScheduledTime: checkTime,
                    EventType: TriggerEventType.GuestRequestedCheck,
                    GuestId: guestId,
                    TableId: tableId,
                    Priority: TaskPriority.Normal
                ));
                
                // Guest leaves
                if (departureTime < end)
                {
                    events.Add(new ScheduledEvent(
                        ScheduledTime: departureTime,
                        EventType: TriggerEventType.GuestLeft,
                        GuestId: guestId,
                        TableId: tableId,
                        Priority: TaskPriority.Normal
                    ));
                    
                    // Table needs cleaning/bussing
                    events.Add(new ScheduledEvent(
                        ScheduledTime: departureTime.AddMinutes(1),
                        EventType: TriggerEventType.TableNeedsCleaning,
                        TableId: tableId,
                        Priority: TaskPriority.Normal
                    ));
                }
            }
            
            currentHour = currentHour.AddHours(1);
        }
        
        // Sort events by scheduled time
        events.Sort((a, b) => a.ScheduledTime.CompareTo(b.ScheduledTime));
        
        _logger?.LogInformation("Generated {Count} events for period {Start} to {End}", 
            events.Count, start, end);
        
        return events;
    }
    
    public double GetExpectedGuestsForHour(DateTime dateTime, EventPatternConfig? config = null)
    {
        config ??= new EventPatternConfig();
        
        var hourRate = config.HourlyArrivalRates[dateTime.Hour];
        var dayMultiplier = config.DayOfWeekMultipliers[(int)dateTime.DayOfWeek];
        
        return hourRate * dayMultiplier;
    }
    
    public TimeSpan GenerateMealDuration(EventPatternConfig? config = null)
    {
        config ??= new EventPatternConfig();
        
        // Box-Muller transform for normal distribution
        var u1 = 1.0 - _random.NextDouble();
        var u2 = 1.0 - _random.NextDouble();
        var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        
        var minutes = config.AverageMealDurationMinutes + (randStdNormal * config.MealDurationStdDev);
        minutes = Math.Max(15, Math.Min(120, minutes)); // Clamp between 15 min and 2 hours
        
        return TimeSpan.FromMinutes(minutes);
    }
    
    private int GeneratePartySize(EventPatternConfig config)
    {
        // Exponential-ish distribution favoring smaller parties
        var size = (int)Math.Ceiling(-Math.Log(1 - _random.NextDouble()) * config.AveragePartySize / 1.5);
        return Math.Max(1, Math.Min(config.MaxPartySize, size));
    }
    
    private int GeneratePoissonCount(double lambda)
    {
        if (lambda <= 0) return 0;
        
        // Knuth algorithm for Poisson distribution
        var L = Math.Exp(-lambda);
        var k = 0;
        var p = 1.0;
        
        do
        {
            k++;
            p *= _random.NextDouble();
        } while (p > L);
        
        return k - 1;
    }
}
