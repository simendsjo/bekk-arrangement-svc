namespace ArrangementService.Email

open ArrangementService.DomainModels
open ArrangementService.DateTime
open ArrangementService.Config
open System

// ICS reference: https://tools.ietf.org/html/rfc5545
module CalendarInvite =

    let reminderObject =
        [ "BEGIN:VALARM"
          "TRIGGER;RELATED=START:-PT30M"
          "ACTION:DISPLAY"
          "DESCRIPTION:REMINDER"
          "END:VALARM" ]
        |> String.concat "\n"

    let recurringObject = "RRULE:FREQ=WEEKLY;COUNT=3;INTERVAL=1;WKST=MO\n" // Eksempel, må implementeres

    let timezoneObject =
        [ "BEGIN:VTIMEZONE"
          "TZID:W. Europe Standard Time"
          "BEGIN:STANDARD"
          "DTSTART:16010101T030000"
          "TZOFFSETFROM:+0200"
          "TZOFFSETTO:+0100"
          "RRULE:FREQ=YEARLY;INTERVAL=1;BYDAY=-1SU;BYMONTH=10"
          "END:STANDARD"
          "BEGIN:DAYLIGHT"
          "DTSTART:16010101T020000"
          "TZOFFSETFROM:+0100"
          "TZOFFSETTO:+0200"
          "RRULE:FREQ=YEARLY;INTERVAL=1;BYDAY=-1SU;BYMONTH=3"
          "END:DAYLIGHT"
          "END:VTIMEZONE" ]
        |> String.concat "\n"

    type CalendarInviteStatus =
        | Create
        | Cancel

    let eventObject ((event: Event), (participant: Participant),
                     (noReplyMail: EmailAddress), (message: string),
                     (status: CalendarInviteStatus))
        =
        let utcNow =
            toUtcString (toCustomDateTime DateTime.UtcNow (TimeSpan()))
        [ "BEGIN:VEVENT"
          sprintf "UID:%O" event.Id.Unwrap
          sprintf "DTSTART;TZID=W. Europe Standard Time:%s"
              (toDateString event.StartDate)
          sprintf "DTEND;TZID=W. Europe Standard Time:%s"
              (toDateString event.EndDate)
          sprintf "DTSTAMP:%s" utcNow
          sprintf "ORGANIZER:mailto:%s" noReplyMail.Unwrap
          sprintf "ATTENDEE;PARTSTAT=ACCEPTED;RSVP=FALSE;CN=%s:mailto:%s"
              event.OrganizerName.Unwrap event.OrganizerEmail.Unwrap
          sprintf "ATTENDEE;PARTSTAT=ACCEPTED;RSVP=FALSE;CN=%s:mailto:%s"
              participant.Name.Unwrap participant.Email.Unwrap
          sprintf "SUMMARY;LANGUAGE=nb-NO:%s" event.Title.Unwrap
          sprintf "DESCRIPTION;LANGUAGE=nb-NO:%s"
              (message.Replace("<br>", "\n"))
          sprintf "X-ALT-DESC;FMTTYPE=text/html:%s" event.Description.Unwrap
          sprintf "LOCATION;LANGUAGE=nb-NO:%s" event.Location.Unwrap

          (if status = Create then "STATUS:CONFIRMED" else "STATUS:CANCELLED")
          (if status = Create then "SEQUENCE:0" else "SEQUENCE:1")

          reminderObject
          // if recurring, insert recurringObject. TODO: implement frontend.
          "END:VEVENT" ]
        |> String.concat "\n"

    let createCalendarAttachment eventDetails =
        [ "BEGIN:VCALENDAR"
          "CALSCALE:GREGORIAN"
          "METHOD:REQUEST"
          "PRODID:-//Bekk//arrangement-svc//NO"
          "VERSION:2.0"
          timezoneObject
          eventObject eventDetails
          "END:VCALENDAR" ]
        |> String.concat "\n"
