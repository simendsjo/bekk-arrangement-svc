namespace ArrangementService.Events

open System
open Giraffe

open ArrangementService

open Database
open Repo
open Validation
open DateTime

module Models =
    type Id = Id of Guid
    type Title = Title of string
    type Description = Description of string
    type Location = Location of string
    type StartDate = DateTimeOffset
    type EndDate = DateTimeOffset
    type ResponsibleEmployee = ResponsibleEmployee of int
    type TableModel = ArrangementDbContext.dboSchema.``dbo.Events``
    type DbModel = ArrangementDbContext.``dbo.EventsEntity``
    type Email = Email of string
    
    type DomainModel =
        { Id: Guid
          Title: Title
          Description: Description
          Location: Location
          StartDate: DateTimeCustom
          EndDate: DateTimeCustom
          OrganizerEmail: Email
          OpenForRegistrationDate: DateTimeCustom }

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

    let unwrapId = function | Id id -> id
    let unwrapTitle = function | Title t -> t
    let unwrapDescription = function | Description d -> d
    let unwrapLocation = function | Location l -> l
    let unwrapEmail = function | Email e -> e

    let titleValidator title : Result<Title, CustomErrorMessage list> =
        [ validateMinLength 3 "Tittel må ha minst 3 tegn"
          validateMaxLength 60 "Tittel må være mindre enn 60 tegn" ]
        |> validateAll Title title

    let descriptionValidator description =
        [ validateMinLength 3 "Beskrivelse må ha minst 3 tegn"
          validateMaxLength 255 "Beskrivelse må være mindre enn 255 tegn" ]
        |> validateAll Description description

    let locationValidator location =
        [ validateMinLength 3 "Sted må ha minst 3 tegn"
          validateMaxLength 30 "Sted må være mindre enn 30 tegn" ]
        |> validateAll Location location

    let organizerEmailValidator email =
        [ validateEmail "Ansvarlig må ha en gyldig epost-addresse" ]
        |> validateAll Email email
    
    let dateRangeValidator openDate startDate endDate =
      [ fun (openDate, startDate, _) -> validateBefore "Registreringsdato må være før åpningsdato" (openDate, startDate)
        fun (openDate, _, endDate) -> validateBefore "Registreringsdato må være før sluttdato" (openDate, endDate)
        fun (openDate, _, _) -> validateBefore "Åpningsdato må være i fremtiden" (now (), openDate) ]
      |> validateAll id (openDate, startDate, endDate)
   
    let dbToDomain (dbRecord: DbModel): DomainModel =
        { Id = dbRecord.Id
          Title = Title dbRecord.Title
          Description = Description dbRecord.Description
          Location = Location dbRecord.Location
          OrganizerEmail = Email dbRecord.OrganizerEmail
          StartDate = toCustomDateTime dbRecord.StartDate dbRecord.StartTime
          EndDate = toCustomDateTime dbRecord.EndDate dbRecord.EndTime
          OpenForRegistrationDate = toCustomDateTime dbRecord.OpenForRegistrationDate dbRecord.OpenForRegistrationTime
          }

    let updateDbWithDomain (db: DbModel) (event: DomainModel) =
        db.Title <- unwrapTitle event.Title
        db.Description <- unwrapDescription event.Description
        db.Location <- unwrapLocation event.Location
        db.OrganizerEmail <- unwrapEmail event.OrganizerEmail
        db.StartDate <- customToDateTime event.StartDate
        db.StartTime <- customToTimeSpan event.StartDate.Time
        db.EndDate <- customToDateTime event.EndDate
        db.EndTime <- customToTimeSpan event.EndDate.Time
        db.OpenForRegistrationDate <-customToDateTime event.OpenForRegistrationDate
        db.OpenForRegistrationTime <- customToTimeSpan event.OpenForRegistrationDate.Time
        db

    let domainToView (domainModel: DomainModel): ViewModel =
        { Id = domainModel.Id
          Title = unwrapTitle domainModel.Title
          Description = unwrapDescription domainModel.Description
          Location = unwrapLocation domainModel.Location
          OrganizerEmail = unwrapEmail domainModel.OrganizerEmail
          StartDate = domainModel.StartDate
          EndDate = domainModel.EndDate
          OpenForRegistrationDate = domainModel.OpenForRegistrationDate }

    let writeToDomain (id: Id) (writeModel: WriteModel): Result<DomainModel, CustomErrorMessage list> =
      validator {
          yield! titleValidator writeModel.Title 
          yield! descriptionValidator writeModel.Description 
          yield! locationValidator writeModel.Location
          yield! organizerEmailValidator writeModel.OrganizerEmail
          yield! dateRangeValidator writeModel.OpenForRegistrationDate writeModel.StartDate writeModel.EndDate

          return fun (openForRegistrationDate, startDate, endDate) organizerEmail location description title -> { 
            Id = unwrapId id
            Title = title
            Description = description 
            Location = location
            OrganizerEmail = organizerEmail
            StartDate = startDate
            EndDate = endDate
            OpenForRegistrationDate = openForRegistrationDate
          }
        }

    let models: Models<DbModel, DomainModel, ViewModel, WriteModel, Id, TableModel> =
        { key = fun record -> Id record.Id 
          table = fun ctx -> ctx.GetService<ArrangementDbContext>().Dbo.Events
          create = fun table -> table.Create()
          delete = fun record -> record.Delete()
          dbToDomain = dbToDomain
          updateDbWithDomain = updateDbWithDomain
          domainToView = domainToView
          writeToDomain = writeToDomain }
