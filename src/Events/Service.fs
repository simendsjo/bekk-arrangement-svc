namespace ArrangementService.Event

open ArrangementService
open ArrangementService.Email
open ResultComputationExpression
open Queries
open UserMessages
open ArrangementService.DomainModels
open ArrangementService.DateTime

module Service =

    let models = ArrangementService.Event.Models.models
    let repo = Repo.from models

    let getEvents =
        result {
            for events in repo.read do
                return Seq.map models.dbToDomain events
        }

    let getEventsOrganizedBy organizerEmail =
        result {
            for events in repo.read do

                let eventsByOrganizer =
                    queryEventsOrganizedBy organizerEmail events
                return Seq.map models.dbToDomain eventsByOrganizer
        }

    let getEvent id =
        result {
            for events in repo.read do

                let! event = events |> queryEventBy id
                return models.dbToDomain event
        }

    let createCalendarAttachment (event: Event) (emailAddress: EmailAddress) =
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
          sprintf "ATTENDEE;CN=\"%s\";RSVP=TRUE:mailto:%s" emailAddress.Unwrap
              emailAddress.Unwrap
          "BEGIN:VALARM"
          "TRIGGER:-PT15M"
          "ACTION:DISPLAY"
          "DESCRIPTION:Reminder"
          "END:VALARM"
          "END:VEVENT"
          "END:VCALENDAR" ]
        |> String.concat "\n"

    let createEmail (event: Event) =
        { Subject = sprintf "Du opprettet %s" event.Title.Unwrap
          Message = "Hei.."
          From = EmailAddress "brjilo@bekk.no"
          To = event.OrganizerEmail
          CalendarInvite = createCalendarAttachment event event.OrganizerEmail }

    let createEvent event =
        result {
            for newEvent in repo.create event do
                let mail = createEmail newEvent
                yield Service.sendMail mail
                return newEvent
        }

    let updateEvent id event =
        result {
            for events in repo.read do

                let! oldEvent = events |> queryEventBy id
                return repo.update event oldEvent
        }

    let deleteEvent id =
        result {
            for events in repo.read do

                let! event = events |> queryEventBy id
                repo.del event
                return eventSuccessfullyDeleted id
        }
