using System.IO;
using System.Net;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Chinmaya.Functions;

public class BookingsFunction
{
    private static readonly string[] DefaultSlots = new[]
    {
        "09:00-10:30",
        "10:30-12:00",
        "13:00-14:30",
        "14:30-16:00",
        "16:00-17:30"
    };

    private readonly ILogger<BookingsFunction> _logger;

    public BookingsFunction(ILogger<BookingsFunction> logger)
    {
        _logger = logger;
    }

    [Function("bookings")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        string requestBody;
        using (var reader = new StreamReader(req.Body))
        {
            requestBody = await reader.ReadToEndAsync();
        }

        BookingRequest? payload;
        try
        {
            payload = JsonSerializer.Deserialize<BookingRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return await CreateJsonResponse(req, HttpStatusCode.BadRequest, new { error = "Invalid JSON body" });
        }

        if (payload is null ||
            string.IsNullOrWhiteSpace(payload.Date) ||
            string.IsNullOrWhiteSpace(payload.TimeSlot) ||
            string.IsNullOrWhiteSpace(payload.EventType) ||
            payload.Host is null ||
            string.IsNullOrWhiteSpace(payload.Host.Name) ||
            string.IsNullOrWhiteSpace(payload.Host.Email) ||
            string.IsNullOrWhiteSpace(payload.Host.Phone))
        {
            return await CreateJsonResponse(req, HttpStatusCode.BadRequest,
                new { error = "Missing required fields: date, timeSlot, eventType, host{name, email, phone}" });
        }

        if (!DateTime.TryParse(payload.Date, out _))
        {
            return await CreateJsonResponse(req, HttpStatusCode.BadRequest, new { error = "Invalid date format. Use YYYY-MM-DD." });
        }

        if (Array.IndexOf(DefaultSlots, payload.TimeSlot) < 0)
        {
            return await CreateJsonResponse(req, HttpStatusCode.BadRequest, new { error = $"Invalid timeSlot. Allowed: {string.Join(", ", DefaultSlots)}" });
        }

        var calendarId = Environment.GetEnvironmentVariable("GOOGLE_CALENDAR_ID");
        if (string.IsNullOrWhiteSpace(calendarId))
        {
            return await CreateJsonResponse(req, HttpStatusCode.InternalServerError, new { error = "Google Calendar ID not configured" });
        }

        try
        {
            var service = CreateCalendarService();

            // Check for conflicts
            var dayStart = DateTime.Parse(payload.Date).Date;
            var dayEnd = dayStart.AddDays(1).AddTicks(-1);

            var busy = await GetBusySlotsAsync(service, calendarId, dayStart, dayEnd);
            var slotKey = $"{payload.Date} {payload.TimeSlot}";
            if (busy.Contains(slotKey))
            {
                return await CreateJsonResponse(req, HttpStatusCode.Conflict, new { error = "Time slot already booked" });
            }

            // Create event
            var (startTime, endTime) = payload.TimeSlot.Split("-") switch
            {
                var arr => (arr[0], arr[1])
            };

            var startDt = DateTime.Parse($"{payload.Date}T{startTime}:00Z").ToUniversalTime();
            var endDt = DateTime.Parse($"{payload.Date}T{endTime}:00Z").ToUniversalTime();

            var newEvent = new Event
            {
                Summary = $"{payload.EventType} – {payload.Host.Name}",
                Description = $"Event Type: {payload.EventType}\nHost: {payload.Host.Name} ({payload.Host.Email}, {payload.Host.Phone})\nNotes: {payload.Notes}".Trim(),
                Start = new EventDateTime { DateTimeDateTimeOffset = startDt, TimeZone = "UTC" },
                End = new EventDateTime { DateTimeDateTimeOffset = endDt, TimeZone = "UTC" },
                Attendees = new List<EventAttendee> { new() { Email = payload.Host.Email } }
            };

            var insertRequest = service.Events.Insert(newEvent, calendarId);
            insertRequest.SendUpdates = EventsResource.InsertRequest.SendUpdatesEnum.None;
            var created = await insertRequest.ExecuteAsync();

            return await CreateJsonResponse(req, HttpStatusCode.Created, new
            {
                message = "Booking created successfully",
                eventId = created.Id,
                htmlLink = created.HtmlLink
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking");
            return await CreateJsonResponse(req, HttpStatusCode.InternalServerError, new { error = ex.Message });
        }
    }

    private static CalendarService CreateCalendarService()
    {
        var json = Environment.GetEnvironmentVariable("GOOGLE_SERVICE_ACCOUNT_JSON");
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Google Service Account credentials not configured");
        }

        var credential = GoogleCredential.FromJson(json)
            .CreateScoped(CalendarService.Scope.Calendar);

        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Chinmaya Sharadalaya"
        });
    }

    private static async Task<HashSet<string>> GetBusySlotsAsync(
        CalendarService service, string calendarId, DateTime start, DateTime end)
    {
        var request = service.Freebusy.Query(new FreeBusyRequest
        {
            TimeMinDateTimeOffset = start,
            TimeMaxDateTimeOffset = end,
            Items = new[] { new FreeBusyRequestItem { Id = calendarId } }
        });

        var response = await request.ExecuteAsync();
        var busyPeriods = response.Calendars?[calendarId]?.Busy ?? new List<TimePeriod>();

        var busySet = new HashSet<string>();
        foreach (var period in busyPeriods)
        {
            if (period.StartDateTimeOffset is null || period.EndDateTimeOffset is null) continue;

            var dateStr = period.StartDateTimeOffset.Value.UtcDateTime.ToString("yyyy-MM-dd");
            var slot = $"{period.StartDateTimeOffset.Value.UtcDateTime:HH:mm}-{period.EndDateTimeOffset.Value.UtcDateTime:HH:mm}";
            busySet.Add($"{dateStr} {slot}");
        }

        return busySet;
    }

    private static async Task<HttpResponseData> CreateJsonResponse(HttpRequestData req, HttpStatusCode status, object body)
    {
        var response = req.CreateResponse(status);
        await response.WriteAsJsonAsync(body);
        return response;
    }
}

public record BookingRequest(string Date, string TimeSlot, string EventType, HostInfo Host, string? Notes = null);
public record HostInfo(string Name, string Email, string Phone);
