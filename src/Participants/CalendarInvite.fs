namespace ArrangementService.Participant

open ArrangementService.DomainModels
open ArrangementService.DateTime
open System

// ICS reference: https://tools.ietf.org/html/rfc5545
module CalendarInvite =

    let createMessage redirectUrl (event: Event) (participant: Participant) =
        [ "Hei! 😄"
          sprintf "Du er nå påmeldt %s." event.Title.Unwrap
          sprintf "Vi gleder oss til å se deg på %s den %s 🎉"
              event.Location.Unwrap (toReadableString event.StartDate)
          "Siden det er begrenset med plasser, setter vi pris på om du melder deg av "
          "hvis du ikke lenger kan delta. Da blir det plass til andre på ventelisten 😊"
          sprintf "Klikk her for å melde deg av: %s." redirectUrl
          "Bare spør meg om det er noe du lurer på."
          "Vi sees!"
          sprintf "Hilsen %s i Bekk" event.OrganizerEmail.Unwrap ]
        |> String.concat "<br>" // Sendgrid formats to HTML, \n does not work

    let reminderObject =
        [ "BEGIN:VALARM"
          "TRIGGER;RELATED=START:-PT30M"
          "ACTION:DISPLAY"
          "DESCRIPTION:REMINDER"
          "END:VALARM" ]
        |> String.concat "\n"

    let recurringObject = "RRULE:FREQ=WEEKLY;COUNT=3;INTERVAL=1;WKST=MO\n" // Eksempel

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

    let eventObject (event: Event) (participant: Participant) (message: string) =
        let participantEmail = participant.Email.Unwrap
        let utcNow = toUtcString (toCustomDateTime DateTime.UtcNow (TimeSpan()))
        [ "BEGIN:VEVENT"
          sprintf "ORGANIZER;CN=%s:mailto:%s" event.OrganizerEmail.Unwrap event.OrganizerEmail.Unwrap
          sprintf "ATTENDEE;PARTSTAT=ACCEPTED;RSVP=FALSE;CN=%s:mailto:%s" participantEmail participantEmail
          sprintf "DESCRIPTION;LANGUAGE=nb-NO:%s" (message.Replace("<br>","\\n "))
          sprintf "UID:%O" event.Id.Unwrap
          sprintf "SUMMARY;LANGUAGE=nb-NO:%s" event.Title.Unwrap
          sprintf "DTSTART;TZID=W. Europe Standard Time:%s" (toDateString event.StartDate)
          sprintf "DTEND;TZID=W. Europe Standard Time:%s" (toDateString event.EndDate)
          sprintf "DTSTAMP:%s" utcNow
          sprintf "LOCATION;LANGUAGE=nb-NO:%s" event.Location.Unwrap
          "STATUS:CONFIRMED"
          "SEQUENCE:0"
          reminderObject
          // if recurring, insert recurringObject. TODO: implement frontend.
          "END:VEVENT" ]
        |> String.concat "\n"

    let createCalendarAttachment (event: Event) (participant: Participant) message =
        [ "BEGIN:VCALENDAR"
          "CALSCALE:GREGORIAN"
          "METHOD:REQUEST"
          "PRODID:-//Bekk//arrangement-svc//NO"
          "VERSION:2.0"
          timezoneObject
          eventObject event participant message
          "END:VCALENDAR" ]
        |> String.concat "\n"

