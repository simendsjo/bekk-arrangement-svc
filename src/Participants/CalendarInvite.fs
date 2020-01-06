namespace ArrangementService.Participant

open ArrangementService
open ArrangementService.DomainModels

module CalendarInvite =

    let toUtcString (dt: DateTimeCustom) =
        sprintf "%s%s%sT%s%s%sZ" (dt.Date.Year.ToString()) (dt.Date.Month.ToString().PadLeft(2, '0'))
            (dt.Date.Day.ToString().PadLeft(2, '0'))
            (dt.Time.Hour.ToString().PadLeft(2, '0'))  (dt.Time.Minute.ToString().PadLeft(2, '0'))  "00" // Format: "20200101T192209Z"

    let createCalendarAttachment startTime endTime location guid description subject fromAddress toName toAddress =
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
END:VCALENDAR" (toUtcString startTime) (System.DateTimeOffset.UtcNow.ToString()) (toUtcString endTime)
            (location.ToString()) guid (description.ToString()) (description.ToString()) (subject.ToString())
            (fromAddress.ToString()) (toName.ToString()) (toAddress.ToString())

    let createMessage (event: Event) (participant: Participant) =
        let url = sprintf "https://api.dev.bekk.no/arrangement-svc/%O/cancel/%s" event.Id.Unwrap participant.Email.Unwrap
        sprintf "Hei %s.
Du er nå påmeldt %s.
Vi gleder oss til å se deg på %s den %i/%i/%i kl %i:%i.
Siden det er begrenset med plasser, setter vi pris på om du melder deg av hvis du ikke lenger
kan delta. Da blir det plass til andre på ventelisten 😊
Klikk her for å melde deg av: %s.
Bare spør meg om det er noe du lurer på.
Vi sees!
Hilsen %s i Bekk." participant.Email.Unwrap event.Title.Unwrap event.Location.Unwrap event.StartDate.Date.Day
            event.StartDate.Date.Month event.StartDate.Date.Year event.StartDate.Time.Hour event.StartDate.Time.Minute
            url event.OrganizerEmail.Unwrap
