"""
Azure Functions (Python) + Google Calendar backend for Chinmaya Sharadalaya
Provides two endpoints:
- GET /api/availability?month=YYYY-MM
- POST /api/bookings
"""

import json
import os
from datetime import datetime, timedelta, timezone
from typing import Any

import azure.functions as func
from google.oauth2 import service_account
from googleapiclient.discovery import build
from googleapiclient.errors import HttpError

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------

SCOPES = ["https://www.googleapis.com/auth/calendar"]

# Load Google Service Account credentials from environment
# Preferred: entire JSON as GOOGLE_SERVICE_ACCOUNT_JSON
# Fallback: GOOGLE_CLIENT_EMAIL + GOOGLE_PRIVATE_KEY (with newlines escaped as \n)
SERVICE_ACCOUNT_INFO = None

if os.environ.get("GOOGLE_SERVICE_ACCOUNT_JSON"):
    SERVICE_ACCOUNT_INFO = json.loads(os.environ["GOOGLE_SERVICE_ACCOUNT_JSON"])
elif os.environ.get("GOOGLE_CLIENT_EMAIL") and os.environ.get("GOOGLE_PRIVATE_KEY"):
    SERVICE_ACCOUNT_INFO = {
        "type": "service_account",
        "client_email": os.environ["GOOGLE_CLIENT_EMAIL"],
        "private_key": os.environ["GOOGLE_PRIVATE_KEY"].replace("\\n", "\n"),
        "token_uri": "https://oauth2.googleapis.com/token",
    }

CALENDAR_ID = os.environ.get("GOOGLE_CALENDAR_ID", "")

# Fixed 90-minute time slots for CSP / events (can be made configurable later)
DEFAULT_SLOTS = [
    "09:00-10:30",
    "10:30-12:00",
    "13:00-14:30",
    "14:30-16:00",
    "16:00-17:30",
]

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _get_calendar_service():
    """Return an authorized Google Calendar service instance."""
    if not SERVICE_ACCOUNT_INFO:
        raise RuntimeError("Google Service Account credentials not configured")
    credentials = service_account.Credentials.from_service_account_info(
        SERVICE_ACCOUNT_INFO, scopes=SCOPES
    )
    return build("calendar", "v3", credentials=credentials, cache_discovery=False)


def _parse_month_to_date_range(month: str) -> tuple[str, str]:
    """Convert YYYY-MM to (start_iso, end_iso) covering the whole month (UTC)."""
    year, month_num = map(int, month.split("-"))
    start = datetime(year, month_num, 1, tzinfo=timezone.utc)
    if month_num == 12:
        end = datetime(year + 1, 1, 1, tzinfo=timezone.utc)
    else:
        end = datetime(year, month_num + 1, 1, tzinfo=timezone.utc)
    return start.isoformat(), end.isoformat()


def _get_busy_slots(service, time_min: str, time_max: str) -> set[str]:
    """Return a set of 'YYYY-MM-DD HH:MM-HH:MM' strings for busy slots."""
    body = {
        "timeMin": time_min,
        "timeMax": time_max,
        "items": [{"id": CALENDAR_ID}],
    }
    freebusy = service.freebusy().query(body=body).execute()
    busy_periods = freebusy.get("calendars", {}).get(CALENDAR_ID, {}).get("busy", [])

    busy_set = set()
    for period in busy_periods:
        start = datetime.fromisoformat(period["start"].replace("Z", "+00:00"))
        end = datetime.fromisoformat(period["end"].replace("Z", "+00:00"))
        date_str = start.date().isoformat()
        slot = f"{start.strftime('%H:%M')}-{end.strftime('%H:%M')}"
        busy_set.add(f"{date_str} {slot}")
    return busy_set


def _build_response(status: int, body: dict[str, Any]) -> func.HttpResponse:
    return func.HttpResponse(
        body=json.dumps(body, ensure_ascii=False),
        status_code=status,
        headers={"Content-Type": "application/json"},
    )


# ---------------------------------------------------------------------------
# Functions
# ---------------------------------------------------------------------------

app = func.FunctionApp()


@app.route(route="availability", methods=["GET"], auth_level=func.AuthLevel.ANONYMOUS)
def get_availability(req: func.HttpRequest) -> func.HttpResponse:
    """
    GET /api/availability?month=2026-07
    Returns available 90-minute slots for the given month.
    """
    month = req.params.get("month")
    if not month:
        return _build_response(400, {"error": "Missing required query parameter: month (YYYY-MM)"})

    try:
        time_min, time_max = _parse_month_to_date_range(month)
    except Exception:
        return _build_response(400, {"error": "Invalid month format. Use YYYY-MM."})

    if not CALENDAR_ID:
        return _build_response(500, {"error": "Google Calendar ID not configured"})

    try:
        service = _get_calendar_service()
        busy = _get_busy_slots(service, time_min, time_max)

        # Generate all possible slots for the month
        year, month_num = map(int, month.split("-"))
        start_date = datetime(year, month_num, 1).date()
        if month_num == 12:
            end_date = datetime(year + 1, 1, 1).date()
        else:
            end_date = datetime(year, month_num + 1, 1).date()

        available = []
        current = start_date
        while current < end_date:
            date_str = current.isoformat()
            for slot in DEFAULT_SLOTS:
                key = f"{date_str} {slot}"
                if key not in busy:
                    available.append({"date": date_str, "timeSlot": slot})
            current += timedelta(days=1)

        return _build_response(200, {"month": month, "availableSlots": available})
    except HttpError as e:
        return _build_response(502, {"error": f"Google Calendar API error: {e.reason}"})
    except Exception as e:
        return _build_response(500, {"error": str(e)})


@app.route(route="bookings", methods=["POST"], auth_level=func.AuthLevel.ANONYMOUS)
def create_booking(req: func.HttpRequest) -> func.HttpResponse:
    """
    POST /api/bookings
    Body: { date, timeSlot, eventType, host: {name, email, phone}, notes? }
    """
    try:
        payload = req.get_json()
    except ValueError:
        return _build_response(400, {"error": "Invalid JSON body"})

    required = ["date", "timeSlot", "eventType", "host"]
    for field in required:
        if field not in payload:
            return _build_response(400, {"error": f"Missing required field: {field}"})

    host = payload.get("host", {})
    if not all(k in host for k in ("name", "email", "phone")):
        return _build_response(400, {"error": "host must contain name, email, phone"})

    date_str = payload["date"]
    time_slot = payload["timeSlot"]
    event_type = payload["eventType"]
    notes = payload.get("notes", "")

    # Validate date format
    try:
        datetime.strptime(date_str, "%Y-%m-%d")
    except ValueError:
        return _build_response(400, {"error": "Invalid date format. Use YYYY-MM-DD."})

    if time_slot not in DEFAULT_SLOTS:
        return _build_response(400, {"error": f"Invalid timeSlot. Allowed: {DEFAULT_SLOTS}"})

    if not CALENDAR_ID:
        return _build_response(500, {"error": "Google Calendar ID not configured"})

    try:
        service = _get_calendar_service()

        # Check conflict using FreeBusy for the specific day
        day_start = f"{date_str}T00:00:00Z"
        day_end = f"{date_str}T23:59:59Z"
        busy = _get_busy_slots(service, day_start, day_end)
        slot_key = f"{date_str} {time_slot}"
        if slot_key in busy:
            return _build_response(409, {"error": "Time slot already booked"})

        # Parse slot into start/end datetimes (assume UTC for simplicity; adjust timezone if needed)
        start_time, end_time = time_slot.split("-")
        start_dt = datetime.fromisoformat(f"{date_str}T{start_time}:00").replace(tzinfo=timezone.utc)
        end_dt = datetime.fromisoformat(f"{date_str}T{end_time}:00").replace(tzinfo=timezone.utc)

        event_body = {
            "summary": f"{event_type} – {host['name']}",
            "description": (
                f"Event Type: {event_type}\n"
                f"Host: {host['name']} ({host['email']}, {host['phone']})\n"
                f"Notes: {notes}"
            ).strip(),
            "start": {"dateTime": start_dt.isoformat(), "timeZone": "UTC"},
            "end": {"dateTime": end_dt.isoformat(), "timeZone": "UTC"},
            "attendees": [{"email": host["email"]}],
        }

        created_event = (
            service.events()
            .insert(calendarId=CALENDAR_ID, body=event_body, sendUpdates="none")
            .execute()
        )

        return _build_response(
            201,
            {
                "message": "Booking created successfully",
                "eventId": created_event.get("id"),
                "htmlLink": created_event.get("htmlLink"),
            },
        )
    except HttpError as e:
        return _build_response(502, {"error": f"Google Calendar API error: {e.reason}"})
    except Exception as e:
        return _build_response(500, {"error": str(e)})