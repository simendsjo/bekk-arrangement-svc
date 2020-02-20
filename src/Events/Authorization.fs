namespace ArrangementService.Event

open Giraffe

open ArrangementService
open Auth
open ResultComputationExpression
open UserMessage
open Http

module Authorization =

    let userCreatedEvent eventId =
        result {
            for editToken in queryParam "editToken" do

                for event in Service.getEvent (Id eventId) do

                    let hasCorrectEditToken =
                        editToken = event.EditToken.ToString()

                    if hasCorrectEditToken then
                        return ()
                    else
                        return! [ AccessDenied
                                      (sprintf
                                          "You are trying to edit an event (id %O) which you did not create"
                                           eventId) ] |> Error
        }

    let userCanEditEvent eventId =
        anyOf
            [ userCreatedEvent eventId
              isAdmin ]
