namespace ArrangementService.Events

open Models
open System.Linq

module Queries =

    let queryEventBy id events =
        query {
            for event in events do
                where (models.key event = id)
                select (Some event)
                exactlyOne
        }

    let queryEventsForEmployee (employeeId: int) (events: IQueryable<DbModel>) =
        query {
            for event in events do
                where (event.ResponsibleEmployee = employeeId)
                select event
        }
