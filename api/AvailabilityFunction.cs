using System.Net;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Chinmaya.Functions;

public class AvailabilityFunction
{
    private static readonly string[] DefaultSlots = new[]
    {
        "09:00-10:30",
        "10:30-12:00",
        "13:00-14:30",
        "14:30-16:00",
        "16:00-17:30"
    };

    private readonly ILogger<AvailabilityFunction> _logger;

    public AvailabilityFunction(ILogger<AvailabilityFunction> logger)
    {
        _logger = logger;
    }

    [Function("availability")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        var month = req.Query["month"];
        if (string.IsNullOrWhiteSpace(month))
        {
            return await CreateJsonResponse(req, HttpStatusCode.BadRequest, new { error = "Missing required query parameter: month (YYYY-MM)" });
        }

        if (!TryParseMonth(month, out var startDate, out var endDate))
        {
            return await CreateJsonResponse(req, HttpStatusCode.BadRequest, new { error = "Invalid month format. Use YYYY-MM." });
        }

        var calendarId = Environment.GetEnvironmentVariable("GOOGLE_CALENDAR_ID");
        if (string.IsNullOrWhiteSpace(calendarId))
        {
            return await CreateJsonResponse(req, HttpStatusCode.InternalServerError, new { error = "Google Calendar ID not configured" });
        }

        try
        {
            var service = CreateCalendarService();
            var busy = await GetBusySlotsAsync(service, calendarId, startDate, endDate);

            var available = new List<object>();
            for (var date = startDate; date < endDate; date = date.AddDays(1))
            {
                var dateStr = date.ToString("yyyy-MM-dd");
                foreach (var slot in DefaultSlots)
                {
                    var key = $"{dateStr} {slot}";
                    if (!busy.Contains(key))
                    {
                        available.Add(new { date = dateStr, timeSlot = slot });
                    }
                }
            }

            return await CreateJsonResponse(req, HttpStatusCode.OK, new { month, availableSlots = available });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching availability");
            return await CreateJsonResponse(req, HttpStatusCode.InternalServerError, new { error = ex.Message });
        }
    }

    private static bool TryParseMonth(string month, out DateTime start, out DateTime end)
    {
        start = default;
        end = default;

        var parts = month.Split('-');
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out var year) ||
            !int.TryParse(parts[1], out var monthNum) ||
            monthNum < 1 || monthNum > 12)
        {
            return false;
        }

        start = new DateTime(year, monthNum, 1, 0, 0, 0, DateTimeKind.Utc);
        end = monthNum == 12
            ? new DateTime(year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            : new DateTime(year, monthNum + 1, 1, 0, 0, 0, DateTimeKind.Utc);

        return true;
    }

    private static CalendarService CreateCalendarService()
    {
        var json = Environment.GetEnvironmentVariable("GOOGLE_SERVICE_ACCOUNT_JSON");
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Google Service Account credentials not configured");
        }

        var credential = GoogleCredential.FromJson(json)
            .CreateScoped(CalendarService.Scope.CalendarReadonly);

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
