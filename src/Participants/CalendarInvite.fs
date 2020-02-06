namespace ArrangementService.Participant

open ArrangementService.DomainModels
open ArrangementService.DateTime
open System

// ICS reference: https://tools.ietf.org/html/rfc5545
module CalendarInvite =
    let reminderObject =
        [ "BEGIN:VALARM"
          "TRIGGER:-PT15M"
          "ACTION:DISPLAY"
          "DESCRIPTION:Reminder"
          "END:VALARM" ] 
        |> String.concat "\n"

    let recurringObject =
        [ "RRULE:FREQ=WEEKLY"
          "RECURID:TEST" ] 
        |> String.concat "\n"

    let timezoneObject =
        ["BEGIN:VTIMEZONE"
         "TZID:Greenwich Standard Time"
         "BEGIN:STANDARD"
         "DTSTART:16010101T000000"
         "TZOFFSETFROM:+0000"
         "TZOFFSETTO:+0000"
         "END:STANDARD"
         "BEGIN:DAYLIGHT"
         "DTSTART:16010101T000000"
         "TZOFFSETFROM:+0000"
         "TZOFFSETTO:+0000"
         "END:DAYLIGHT"
         "END:VTIMEZONE" ]
        |> String.concat "\n" 

    let eventObject (event: Event) (participant: Participant) =
        let participantEmail = participant.Email.Unwrap
        [ "BEGIN:VEVENT"
          sprintf "ORGANIZER;CN=%s:mailto:%s" event.OrganizerEmail.Unwrap event.OrganizerEmail.Unwrap
          sprintf "ATTENDEE;CN=%s;RSVP=TRUE:mailto:%s" participantEmail participantEmail
          sprintf "DESCRIPTION;LANGUAGE=nb-NO:%s" "Beskrivelsen her" //event.Description.Unwrap
          sprintf "UID:%O" event.Id.Unwrap
          sprintf "SUMMARY;LANGUAGE=nb-NO:%s" "Oppsummeringen" //event.Title.Unwrap
          sprintf "DTSTART:%s" (toUtcString event.StartDate)
          sprintf "DTEND:%s" (toUtcString event.EndDate)
          sprintf "DTSTAMP:%s" (toUtcString (toCustomDateTime DateTime.UtcNow (TimeSpan())))
          sprintf "LOCATION;LANGUAGE=nb-NO:%s" event.Location.Unwrap
          "COMMENT:Heisann test test - - Ida"
          reminderObject
          // if recurring, insert recurringObject
          "END:VEVENT" ] 
        |> String.concat "\n" 
 
    let createCalendarAttachment (event: Event) (participant: Participant) =
        [ "BEGIN:VCALENDAR"
          "CALSCALE:GREGORIAN"
          "METHOD:REQUEST"
          "PRODID:-//Bekk//arrangement-svc//NO"
          "VERSION:2.0"
          timezoneObject
          eventObject event participant
          "END:VCALENDAR" ]
        |> String.concat "\n"

    let createMessage redirectUrl (event: Event) (participant: Participant) =
        [ "Hei! 😄"
          sprintf "Du er nå påmeldt %s." event.Title.Unwrap
          sprintf "Vi gleder oss til å se deg på %s den %s 🎉"
              event.Location.Unwrap (toReadableString event.StartDate)
          "Siden det er begrenset med plasser, setter vi pris på om du melder deg av hvis du ikke lenger"
          "kan delta. Da blir det plass til andre på ventelisten 😊"
          sprintf "Klikk her for å melde deg av: %s." redirectUrl
          "Bare spør meg om det er noe du lurer på."
          "Vi sees!"
          sprintf "Hilsen %s i Bekk" event.OrganizerEmail.Unwrap ]
        |> String.concat "\n"
