namespace ArrangementService.Participant

open System
open System.Linq
open Giraffe

open ArrangementService

open TimeStamp
open Validation
open Repo
open UserMessage
open ArrangementService.Email
open ArrangementService.DomainModels
open Microsoft.AspNetCore.Http
open System.Data
open System.Collections.Generic

type DbModel =
  { Name: string
    Email: string
    Comment: string
    RegistrationTime: int64
    EventId: Guid
    CancellationToken: Guid
  }

type ViewModel =
    { Name: string
      Email: string
      Comment: string
      EventId: string
      RegistrationTime: int64
    }

type NewlyCreatedParticipationViewModel =
    { Participant: ViewModel
      CancellationToken: string }

type WriteModel =
    { name: string
      comment: string
      cancelUrlTemplate: string }

type ParticipantsWithWaitingList =
    { attendees: Participant seq
      waitingList: Participant seq }

type ParticipantViewModelsWithWaitingList =
    { attendees: ViewModel list
      waitingList: ViewModel list option }

type Key = Guid * string

module Models =

    let dbToDomain (dbRecord: DbModel): Participant =
        { Name = Name dbRecord.Name
          Email = EmailAddress dbRecord.Email
          Comment = Comment dbRecord.Comment
          EventId = Event.Id dbRecord.EventId
          RegistrationTime = TimeStamp dbRecord.RegistrationTime
          CancellationToken = dbRecord.CancellationToken }

    let writeToDomain
        ((id, email): Key)
        (writeModel: WriteModel)
        : Result<Participant, UserMessage list>
        =
          Ok Participant.Create 
          <*> Name.Parse writeModel.name
          <*> EmailAddress.Parse email 
          <*> Comment.Parse writeModel.comment
          <*> (Event.Id id |> Ok) 
          <*> (now() |> Ok) 
          <*> (Guid.NewGuid() |> Ok)

    let domainToView (participant: Participant): ViewModel =
        { Name = participant.Name.Unwrap
          Email = participant.Email.Unwrap
          Comment = participant.Comment.Unwrap
          EventId = participant.EventId.Unwrap.ToString()
          RegistrationTime = participant.RegistrationTime.Unwrap }

    let domainToViewWithCancelInfo (participant: Participant): NewlyCreatedParticipationViewModel
        =
        { Participant = domainToView participant
          CancellationToken = participant.CancellationToken.ToString() }
