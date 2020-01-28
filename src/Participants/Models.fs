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
    { Email: string
      EventId: string
      RegistrationTime: int64 }

type NewlyCreatedParticipationViewModel =
    { Participant: ViewModel
      CancellationToken: string
      RedirectUrl: string }

type WriteModel =
    { redirectUrlTemplate: string }

type Key = Guid * string

type DbModel = ArrangementDbContext.``dbo.ParticipantsEntity``

type TableModel = ArrangementDbContext.dboSchema.``dbo.Participants``

module Models =

    let createRedirectUrl (redirectUrlTemplate: string)
        (participant: Participant) =
        redirectUrlTemplate.Replace("{eventId}",
                                    participant.EventId.Unwrap.ToString())
                           .Replace("{email}", participant.Email.Unwrap)
                           .Replace("{cancellationToken}",
                                    participant.CancellationToken.ToString())

    let getParticipants (ctx: HttpContext): TableModel =
        ctx.GetService<ArrangementDbContext>().Dbo.Participants

    let dbToDomain (dbRecord: DbModel): Participant =
        { Email = EmailAddress dbRecord.Email
          EventId = Event.Id dbRecord.EventId
          RegistrationTime = TimeStamp dbRecord.RegistrationTime
          CancellationToken = dbRecord.CancellationToken }

    let writeToDomain ((id, email): Key) (_: WriteModel): Result<Participant, UserMessage list> =
        Ok Participant.Create <*> EmailAddress.Parse email
        <*> (Event.Id id |> Ok) <*> (now() |> Ok) <*> (Guid.NewGuid() |> Ok)

    let updateDbWithDomain (db: DbModel) (participant: Participant) =
        db.Email <- participant.Email.Unwrap
        db.EventId <- participant.EventId.Unwrap
        db.RegistrationTime <- participant.RegistrationTime.Unwrap
        db

    let domainToView (participant: Participant): ViewModel =

        { Email = participant.Email.Unwrap
          EventId = participant.EventId.Unwrap.ToString()
          RegistrationTime = participant.RegistrationTime.Unwrap }

    let domainToViewWithCancelInfo redirectUrlTemplate
        (participant: Participant): NewlyCreatedParticipationViewModel =
        { Participant = domainToView participant
          CancellationToken = participant.CancellationToken.ToString()
          RedirectUrl = createRedirectUrl redirectUrlTemplate participant }

    let models: Models<DbModel, Participant, ViewModel, WriteModel, Key, IQueryable<DbModel>> =
        { key = fun record -> (record.EventId, record.Email)

          table = fun ctx -> getParticipants ctx :> IQueryable<DbModel>
          create = fun ctx -> (getParticipants ctx).Create()
          delete = fun record -> record.Delete()

          dbToDomain = dbToDomain
          updateDbWithDomain = updateDbWithDomain }
