namespace arrangementSvc.Services

open arrangementSvc.Repositories

module EventService =
    let getEvents = EventRepository.getEvents

    let getEventsForEmployee employeeId = getEvents >> Seq.filter (fun event -> event.ResponsibleEmployee = employeeId)

    let getEvent id = getEvents >> Seq.tryFind (fun event -> event.Id = id)

    let deleteEvent = EventRepository.deleteEvent

    let updateEvent = EventRepository.updateEvent

    let createEvent = EventRepository.createEvent
