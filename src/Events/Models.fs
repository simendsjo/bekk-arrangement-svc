namespace ArrangementService.Events

open System
open Giraffe

open ArrangementService.Database
open ArrangementService.Repo
open ArrangementService.Events.ErrorMessages
open ArrangementService.Http
open ArrangementService.Validation

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
    type Date =
        { Day: int
          Month: int
          Year: int }
        
    type Time =
        { Hour: int
          Minute: int }
        
    type DateTimeCustom =
        { Date: Date
          Time: Time }
    
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

    let toCustomDateTime (date: DateTime) (time: TimeSpan): DateTimeCustom =
        {
            Date =
                { Day = date.Day
                  Month = date.Month
                  Year = date.Year }
            Time =
                { Hour = time.Hours
                  Minute = time.Minutes }
        }
    
    let customToDateTime (dateTime : DateTimeCustom) : DateTime =
      let date = dateTime.Date
      let time = dateTime.Time
      DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, 0)
    
    let customToTimeSpan (time: Time) : TimeSpan =
        TimeSpan(time.Hour, time.Minute, 0)

    let unwrapId = function | Id id -> id
    let unwrapTitle = function | Title t -> t
    let unwrapDescription = function | Description d -> d
    let unwrapLocation = function | Location l -> l
    let unwrapEmail = function | Email e -> e

    let titleValidator title =
      validator {
        yield validateMinLength title 3 "Tittel må ha minst 3 tegn" 
        yield validateMaxLength title 60 "Tittel må være mindre enn 60 tegn"
      }

    let descriptionValidator description =
      validator {
        yield validateMinLength description 3 "Beskrivelse må ha minst 3 tegn"
        yield validateMaxLength description 255 "Beskrivelse må være mindre enn 255 tegn"
      }

    let locationValidator location =
      validator {
        yield validateMinLength location 3 "Sted må ha minst 3 tegn"
        yield validateMaxLength location 30 "Sted må være mindre enn 30 tegn"
      }

    let organizerEmailValidator email=
      validator {
        yield validateEmail email "Ansvarlig må ha en gyldig epost-addresse"
      }
    
    let dateValidator startDate endDate =
      let startDate = customToDateTime startDate
      let endDate = customToDateTime endDate
      validator {
        yield validateAfter startDate DateTime.Now "Fra-dato må være i fremtiden"
        yield validateAfter endDate DateTime.Now "Til-dato må være i fremtiden"
        yield validateBefore startDate endDate "Til-dato må være etter fra-dato"
      }

    let openForRegistrationValidator openDate startDate endDate =
      let startDate = customToDateTime startDate
      let endDate = customToDateTime endDate
      let openDate = customToDateTime openDate
      validator {
        yield validateBefore openDate startDate "Registreringsdato må være før åpningsdato"
        yield validateBefore openDate endDate "Registreringsdato må være før sluttdato"
        yield validateBefore DateTime.Now openDate "Åpningsdato må være i fremtiden"
      }

    let validateWriteModel (writeModel : WriteModel) : Result<WriteModel, HttpErr> =
      validator {
        yield titleValidator writeModel.Title
        yield descriptionValidator writeModel.Description
        yield locationValidator writeModel.Location
        yield organizerEmailValidator writeModel.OrganizerEmail
        yield dateValidator writeModel.StartDate writeModel.EndDate
        yield openForRegistrationValidator writeModel.OpenForRegistrationDate writeModel.StartDate writeModel.EndDate
      }
      |> function
      | Ok _ -> Ok writeModel
      | Error e -> badRequest id e |> Error
   
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

    let writeToDomain (id: Id) (writeModel: WriteModel): DomainModel =
        { Id = unwrapId id
          Title = Title writeModel.Title
          Description = Description writeModel.Description
          Location = Location writeModel.Location
          OrganizerEmail = Email writeModel.OrganizerEmail
          StartDate = writeModel.StartDate
          EndDate = writeModel.EndDate
          OpenForRegistrationDate = writeModel.OpenForRegistrationDate 
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
