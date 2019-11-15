namespace ArrangementService.Events

open Giraffe
open Microsoft.AspNetCore.Http

open ArrangementService.Operators
open ArrangementService.Database

module Service =
    let events (ctx: HttpContext) = ctx.GetService<ArrangementDbContext>().Dbo.Events

    let eventQuery id ctx =
        query {
            for e in events ctx do
                where (Models.key e = id)
                select (Some e)
                exactlyOne
        }

    let eventNotFound id = sprintf "Kan ikke finne event %d" id |> RequestErrors.NOT_FOUND
    let cantUpdateEvent id = sprintf "Kan ikke oppdatere event %d" id |> RequestErrors.BAD_REQUEST
    let eventSuccessfullyDeleted id = sprintf "Event %d blei sletta" id |> Ok

    let getEvents ctx = events ctx |> Seq.map Models.dbToDomain

    let getEventsForEmployee employeeId = getEvents >> Seq.filter (fun event -> event.ResponsibleEmployee = employeeId)

    let getEvent id =
        getEvents
        >> Seq.tryFind (fun event -> event.Id = id)
        >> withError (eventNotFound id)

    let createEvent writemodel = events >> Repo.create (fun id -> Models.writeToDomain id writemodel)

    let updateEvent id event =
        eventQuery id
        >> withError (eventNotFound id)
        >> Result.map (Repo.update event)

    let deleteEvent id =
        eventQuery id
        >> withError (eventNotFound id)
        >> Result.map Repo.del
        >> Result.map (fun _ -> eventSuccessfullyDeleted id)
