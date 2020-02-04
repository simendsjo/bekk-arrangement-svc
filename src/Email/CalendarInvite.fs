namespace ArrangementService.Email

open ArrangementService.DomainModels
open ArrangementService.DateTime

module CalendarInvite =

    let createCalendarAttachment (event: Event) (participant: EmailAddress) =
        let participant = participant.Unwrap
        [ "BEGIN:VCALENDAR"
          "PRODID:-//Schedule a Meeting"
          "VERSION:2.0"
          "METHOD:REQUEST"
          "BEGIN:VEVENT"
          sprintf "DTSTART:%s" (toUtcString event.StartDate)
          sprintf "DTSTAMP:%s" (System.DateTimeOffset.UtcNow.ToString())
          sprintf "DTEND:%s" (toUtcString event.EndDate)
          sprintf "LOCATION:%s" event.Location.Unwrap
          sprintf "UID:%O" event.Id
          sprintf "DESCRIPTION:%s" event.Description.Unwrap
          sprintf "X-ALT-DESC;FMTTYPE=text/html:%s"
              event.Description.Unwrap
          sprintf "SUMMARY:%s" event.Title.Unwrap
          sprintf "ORGANIZER:MAILTO:%s" event.OrganizerEmail.Unwrap
          sprintf "ATTENDEE;CN=\"%s\";RSVP=TRUE:mailto:%s" participant
              participant
          "BEGIN:VALARM"
          "TRIGGER:-PT15M"
          "ACTION:DISPLAY"
          "DESCRIPTION:Reminder"
          "END:VALARM"
          "END:VEVENT"
          "END:VCALENDAR" ]
        |> String.concat "\n"
