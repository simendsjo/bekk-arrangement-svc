namespace ArrangementService.Event



open ArrangementService
open ArrangementService.DomainModels
open ArrangementService.ResultComputationExpression

module Validation =
    let assertNumberOfParticipantsLessThanOrEqualMax (event:Event) =
        result {
            let maxParticipants = event.MaxParticipants.Unwrap 
            if maxParticipants = 0 then
                return ()
            else
                let! numberOfParticipants = Participant.Queries.getNumberOfParticipantsForEvent event.Id

                if numberOfParticipants <= maxParticipants then return () 
                else return! Error [UserMessages.invalidMaxParticipantValue] 
        }