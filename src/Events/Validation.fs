namespace ArrangementService.Event

open ArrangementService
open ArrangementService.DomainModels
open ArrangementService.ResultComputationExpression

module Validation =

    (*
        Checks capacity of event still makes sense
        For instance if the event is full it is not
        allowed to decrease number of spots
    *)
    let assertValidCapacityChange (oldEvent: Event) (newEvent: Event) (participants: Participant seq) =
        let oldMax = oldEvent.MaxParticipants.Unwrap
        let newMax = newEvent.MaxParticipants.Unwrap

        // Man kan alltid øke så lenge den forrige
        // ikke var uendelig
        // og man har venteliste
        if newMax >= oldMax && oldMax <> 0 && newEvent.HasWaitingList then
            Ok ()
        else

        // Den nye er uendelig, all good
        if newMax = 0 then
            Ok ()
        else

        // Det er plass til alle
        let numberOfParticipants = Seq.length participants 
        if numberOfParticipants <= newMax then
            Ok ()
        else

        // Dette er ikke lov her nede fordi vi sjekker over at
        // det ikke er plass til alle
        // Derfor vil det være frekt å fjerne ventelista
        let isRemovingWaitingList = newEvent.HasWaitingList = false && oldEvent.HasWaitingList = true
        if isRemovingWaitingList then
            Error [ UserMessages.invalidRemovalOfWaitingList ]
        else
            Error [ UserMessages.invalidMaxParticipantValue ]
