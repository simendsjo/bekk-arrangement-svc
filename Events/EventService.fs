namespace ArrangementService.Events

module Service =
    let getEvents = Repo.getEvents

    let getEventsForEmployee employeeId = getEvents >> Seq.filter (fun event -> event.ResponsibleEmployee = employeeId)

    let getEvent id = getEvents >> Seq.tryFind (fun event -> event.Id = id)

    let deleteEvent = Repo.deleteEvent

    let updateEvent = Repo.updateEvent

    let createEvent = Repo.createEvent
