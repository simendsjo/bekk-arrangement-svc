namespace ArrangementService.Participant

open System
open System.Linq
open Giraffe

open ArrangementService

open TimeStamp
open Validation
open Database
open Repo
open UserMessage
open ArrangementService.Email
open ArrangementService.DomainModels
open Microsoft.AspNetCore.Http

type ViewModel =
    { Name: string
      Email: string
      Comment: string
      EventId: string
      RegistrationTime: int64 }

type NewlyCreatedParticipationViewModel =
    { Participant: ViewModel
      CancellationToken: string }

type WriteModel =
    { name: string
      comment: string
      cancelUrlTemplate: string }

type Key = Guid * string

type DbModel = ArrangementDbContext.``dbo.ParticipantsEntity``

type TableModel = ArrangementDbContext.dboSchema.``dbo.Participants``

module Models =

    let getParticipants (ctx: HttpContext): TableModel =
        ctx.GetService<ArrangementDbContext>().Dbo.Participants

    let dbToDomain (dbRecord: DbModel): Participant =
        { Name = Name dbRecord.Name
          Email = EmailAddress dbRecord.Email
          Comment = Comment dbRecord.Comment
          EventId = Event.Id dbRecord.EventId
          RegistrationTime = TimeStamp dbRecord.RegistrationTime
          CancellationToken = dbRecord.CancellationToken }

    let writeToDomain ((id, email): Key) (writeModel: WriteModel): Result<Participant, UserMessage list> =
        Ok Participant.Create <*> Name.Parse writeModel.name
        <*> EmailAddress.Parse email <*> Comment.Parse writeModel.comment
        <*> (Event.Id id |> Ok) <*> (now() |> Ok) <*> (Guid.NewGuid() |> Ok)

    let updateDbWithDomain (db: DbModel) (participant: Participant) =
        db.Name <- participant.Name.Unwrap
        db.Email <- participant.Email.Unwrap
        db.Comment <- participant.Comment.Unwrap
        db.EventId <- participant.EventId.Unwrap
        db.RegistrationTime <- participant.RegistrationTime.Unwrap
        db

    let domainToView (participant: Participant): ViewModel =
        { Name = participant.Name.Unwrap
          Email = participant.Email.Unwrap
          Comment = participant.Comment.Unwrap
          EventId = participant.EventId.Unwrap.ToString()
          RegistrationTime = participant.RegistrationTime.Unwrap }

    let domainToViewWithCancelInfo (participant: Participant): NewlyCreatedParticipationViewModel =
        { Participant = domainToView participant
          CancellationToken = participant.CancellationToken.ToString() }

    let models: Models<DbModel, Participant, ViewModel, WriteModel, Key, IQueryable<DbModel>> =
        { key = fun record -> (record.EventId, record.Email)

          table = fun ctx -> getParticipants ctx :> IQueryable<DbModel>
          create = fun ctx -> (getParticipants ctx).Create()
          delete = fun record -> record.Delete()

          dbToDomain = dbToDomain
          updateDbWithDomain = updateDbWithDomain }
