namespace ArrangementService.Participants

open ArrangementService.DateTime

module CalendarInvite =

    let toUtcString (dt: DateTimeCustom) =
        sprintf "%s%s%sT%s%s%sZ" (dt.Date.Year.ToString())  // Format: "20200101T192209Z"
            (dt.Date.Month.ToString().PadLeft(2, '0')) (dt.Date.Day.ToString().PadLeft(2, '0'))
            (dt.Time.Hour.ToString().PadLeft(2, '0')) (dt.Time.Minute.ToString().PadLeft(2, '0'))
            "00"

    let createCalendarAttachment startTime endTime location guid description subject fromAddress
        toName toAddress =
        sprintf "BEGIN:VCALENDAR
PRODID:-//Schedule a Meeting
VERSION:2.0
METHOD:REQUEST
BEGIN:VEVENT
DTSTART:%s
DTSTAMP:%s
DTEND:%s
LOCATION:%s
UID:%O
DESCRIPTION:%s
X-ALT-DESC;FMTTYPE=text/html:%s
SUMMARY:%s
ORGANIZER:MAILTO:%s
ATTENDEE;CN=\"%s\";RSVP=TRUE:mailto:%s
BEGIN:VALARM
TRIGGER:-PT15M
ACTION:DISPLAY
DESCRIPTION:Reminder
END:VALARM
END:VEVENT
END:VCALENDAR" (toUtcString startTime) (System.DateTimeOffset.UtcNow.ToString())
            (toUtcString endTime) (location.ToString()) guid (description.ToString())
            (description.ToString()) (subject.ToString()) (fromAddress.ToString())
            (toName.ToString()) (toAddress.ToString())
