using System.Net;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Chinmaya.Functions;

public class EventsFunction
{
    private readonly ILogger<EventsFunction> _logger;

    public EventsFunction(ILogger<EventsFunction> logger)
    {
        _logger = logger;
    }

    private static bool TryParseMonth(string month, out DateTimeOffset start, out DateTimeOffset end)
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

        start = new DateTimeOffset(year, monthNum, 1, 0, 0, 0, TimeSpan.Zero);
        end = monthNum == 12
            ? new DateTimeOffset(year + 1, 1, 1, 0, 0, 0, TimeSpan.Zero)
            : new DateTimeOffset(year, monthNum + 1, 1, 0, 0, 0, TimeSpan.Zero);

        return true;
    }

    private static bool ShouldHideLocation(string? title)
    {
        return !string.IsNullOrWhiteSpace(title) &&
               title.Contains("Satyanarayana Pooja", StringComparison.OrdinalIgnoreCase);
    }

    [Function("events")]
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
            var request = service.Events.List(calendarId);
            request.TimeMinDateTimeOffset = startDate;
            request.TimeMaxDateTimeOffset = endDate;
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            var eventsResult = await request.ExecuteAsync();
            var events = new List<object>();

            foreach (var item in eventsResult.Items ?? new List<Event>())
            {
                var start = item.Start?.DateTimeDateTimeOffset?.UtcDateTime.ToString("o") ?? item.Start?.Date;
                var end = item.End?.DateTimeDateTimeOffset?.UtcDateTime.ToString("o") ?? item.End?.Date;

                var location = ShouldHideLocation(item.Summary) ? null : item.Location;

                events.Add(new
                {
                    id = item.Id,
                    summary = item.Summary,
                    start,
                    end,
                    location
                });
            }

            return await CreateJsonResponse(req, HttpStatusCode.OK, new { month, events });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching events");
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
            .CreateScoped(CalendarService.Scope.CalendarReadonly);

        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Chinmaya Sharadalaya"
        });
    }

    private static async Task<HttpResponseData> CreateJsonResponse(HttpRequestData req, HttpStatusCode status, object body)
    {
        var response = req.CreateResponse(status);
        await response.WriteAsJsonAsync(body);
        return response;
    }
}


