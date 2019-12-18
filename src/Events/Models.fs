namespace ArrangementService.Events

open System
open Giraffe

open ArrangementService

open Validation
open Database
open Repo
open CustomErrorMessage
open DateTime
open Utils
open Email.Models

open DomainModel

module Models =

    type TableModel = ArrangementDbContext.dboSchema.``dbo.Events``
    type DbModel = ArrangementDbContext.``dbo.EventsEntity``

    type ViewModel =
        { Id: Guid
          Title: string
          Description: string
          Location: string
          OrganizerEmail: string
          StartDate: DateTimeCustom
          EndDate: DateTimeCustom
          OpenForRegistrationDate: DateTimeCustom }

    type WriteModel =
        { Title: string
          Description: string
          Location: string
          OrganizerEmail: string
          StartDate: DateTimeCustom
          EndDate: DateTimeCustom
          OpenForRegistrationDate: DateTimeCustom }

    let writeToDomain (id: Key) (writeModel: WriteModel): Result<DomainModel, CustomErrorMessage list> =
        Ok DomainModel.Create
          <*> (Id id |> Ok)
          <*> Title.Parse writeModel.Title
          <*> Description.Parse writeModel.Description
          <*> Location.Parse writeModel.Location
          <*> EmailAddress.Parse writeModel.OrganizerEmail
          <*> validateDateRange writeModel.OpenForRegistrationDate writeModel.StartDate writeModel.EndDate

    let dbToDomain (dbRecord: DbModel): DomainModel =
        { Id = Id dbRecord.Id
          Title = Title dbRecord.Title
          Description = Description dbRecord.Description
          Location = Location dbRecord.Location
          OrganizerEmail = EmailAddress dbRecord.OrganizerEmail
          StartDate = toCustomDateTime dbRecord.StartDate dbRecord.StartTime
          EndDate = toCustomDateTime dbRecord.EndDate dbRecord.EndTime
          OpenForRegistrationDate =
              toCustomDateTime dbRecord.OpenForRegistrationDate dbRecord.OpenForRegistrationTime }

    let updateDbWithDomain (db: DbModel) (event: DomainModel) =
        db.Title <- event.Title.Unwrap
        db.Description <- event.Description.Unwrap
        db.Location <- event.Location.Unwrap
        db.OrganizerEmail <- event.OrganizerEmail.Unwrap
        db.StartDate <- customToDateTime event.StartDate.Date
        db.StartTime <- customToTimeSpan event.StartDate.Time
        db.EndDate <- customToDateTime event.EndDate.Date
        db.EndTime <- customToTimeSpan event.EndDate.Time
        db.OpenForRegistrationDate <- customToDateTime event.OpenForRegistrationDate.Date
        db.OpenForRegistrationTime <- customToTimeSpan event.OpenForRegistrationDate.Time
        db

    let domainToView (domainModel: DomainModel): ViewModel =
        { Id = domainModel.Id.Unwrap
          Title = domainModel.Title.Unwrap
          Description = domainModel.Description.Unwrap
          Location = domainModel.Location.Unwrap
          OrganizerEmail = domainModel.OrganizerEmail.Unwrap
          StartDate = domainModel.StartDate
          EndDate = domainModel.EndDate
          OpenForRegistrationDate = domainModel.OpenForRegistrationDate }

    let models: Models<DbModel, DomainModel, ViewModel, WriteModel, Key, TableModel> =
        { key = fun record -> record.Id
          table = fun ctx -> ctx.GetService<ArrangementDbContext>().Dbo.Events
          create = fun table -> table.Create()
          delete = fun record -> record.Delete()
          dbToDomain = dbToDomain
          updateDbWithDomain = updateDbWithDomain
          domainToView = domainToView
          writeToDomain = writeToDomain }
