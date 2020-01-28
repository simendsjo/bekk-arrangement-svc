namespace ArrangementService.Participant

open ArrangementService.DomainModels
open ArrangementService.DateTime

module CalendarInvite =

    let createCalendarAttachment (event: Event) (participant: Participant) =
        let participantEmail = participant.Email.Unwrap
        [ "BEGIN:VCALENDAR"
          "PRODID:-//Schedule a Meeting"
          "VERSION:2.0"
          "METHOD:REQUEST"
          "BEGIN:VEVENT"
          sprintf "DTSTART:%s" (toUtcString event.StartDate)
          sprintf "DTSTAMP:%s" (System.DateTimeOffset.UtcNow.ToString())
          sprintf "DTEND:%s" (toUtcString event.EndDate)
          sprintf "LOCATION:%s" (event.Location.ToString())
          sprintf "UID:%O" event.Id
          sprintf "DESCRIPTION:%s" (event.Description.ToString())
          sprintf "X-ALT-DESC;FMTTYPE=text/html:%s"
              (event.Description.ToString())
          sprintf "SUMMARY:%s" (event.Title.ToString())
          sprintf "ORGANIZER:MAILTO:%s" (event.OrganizerEmail.ToString())
          sprintf "ATTENDEE;CN=\"%s\";RSVP=TRUE:mailto:%s" participantEmail
              participantEmail
          "BEGIN:VALARM"
          "TRIGGER:-PT15M"
          "ACTION:DISPLAY"
          "DESCRIPTION:Reminder"
          "END:VALARM"
          "END:VEVENT"
          "END:VCALENDAR" ]
        |> String.concat "\n"

    let createMessage (event: Event) (participant: Participant) =
        let url =
            sprintf
                // URLen må fikses, og gjerne lede til frontenden
                // Eller kanskje må frontenden spesifisere kor urlen skal lede?
                "https://api.dev.bekk.no/arrangement-svc/events/%O/participants/%s?cancellationToken=%s"
                event.Id.Unwrap participant.Email.Unwrap
                (participant.CancellationToken.ToString())
        [ "Hei! 😄"
          sprintf "Du er nå påmeldt %s." event.Title.Unwrap
          sprintf "Vi gleder oss til å se deg på %s den %s 🎉"
              event.Location.Unwrap (toReadableString event.StartDate)
          "Siden det er begrenset med plasser, setter vi pris på om du melder deg av hvis du ikke lenger"
          "kan delta. Da blir det plass til andre på ventelisten 😊"
          sprintf "Klikk her for å melde deg av: %s." url
          "Bare spør meg om det er noe du lurer på."
          "Vi sees!"
          sprintf "Hilsen %s i Bekk" event.OrganizerEmail.Unwrap ]
        |> String.concat "\n"
