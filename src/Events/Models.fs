namespace ArrangementService.Events

open System
open Giraffe

open ArrangementService

open Http
open Database
open Repo
open Utils.Validation
open Validation
open DateTime
open Email.Models


module Models =

    type Key = Guid

    type Id = Id of Key

    type Title = Title of string

    type Description = Description of string

    type Location = Location of string

    type StartDate = DateTimeOffset

    type EndDate = DateTimeOffset

    type ResponsibleEmployee = ResponsibleEmployee of int

    type TableModel = ArrangementDbContext.dboSchema.``dbo.Events``

    type DbModel = ArrangementDbContext.``dbo.EventsEntity``

    type DomainModel =
        { Id: Id
          Title: Title
          Description: Description
          Location: Location
          StartDate: DateTimeCustom
          EndDate: DateTimeCustom
          OrganizerEmail: EmailAddress
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

    let unwrapId = function
        | Id id -> id
    let unwrapTitle = function
        | Title t -> t
    let unwrapDescription = function
        | Description d -> d
    let unwrapLocation = function
        | Location l -> l

    let createDomainModel id title description location organizerEmail
        (openForRegistrationDate, startDate, endDate): DomainModel =
        { Id = id
          Title = title
          Description = description
          Location = location
          OrganizerEmail = organizerEmail
          StartDate = startDate
          EndDate = endDate
          OpenForRegistrationDate = openForRegistrationDate }

    let validateTitle title: Result<Title, CustomErrorMessage list> =
        [ validateMinLength 3 "Tittel må ha minst 3 tegn"
          validateMaxLength 60 "Tittel må være mindre enn 60 tegn" ]
        |> validateAll Title title

    let validateDescription description =
        [ validateMinLength 3 "Beskrivelse må ha minst 3 tegn"
          validateMaxLength 255 "Beskrivelse må være mindre enn 255 tegn" ]
        |> validateAll Description description

    let validateLocation location =
        [ validateMinLength 3 "Sted må ha minst 3 tegn"
          validateMaxLength 30 "Sted må være mindre enn 30 tegn" ]
        |> validateAll Location location

    let validateEmail email =
        [ validateEmail "Ansvarlig må ha en gyldig epost-addresse" ]
        |> validateAll EmailAddress email

    let validateDateRange openDate startDate endDate =
        [ fun (openDate, startDate, _) ->
            validateBefore "Registreringsdato må være før åpningsdato" (openDate, startDate)
          fun (openDate, _, endDate) ->
              validateBefore "Registreringsdato må være før sluttdato" (openDate, endDate)
          fun (openDate, _, _) ->
              validateBefore "Åpningsdato må være i fremtiden" (now(), openDate) ]
        |> validateAll id (openDate, startDate, endDate)

    let writeToDomain (id: Key) (writeModel: WriteModel): Result<DomainModel, CustomErrorMessage list> =
        Ok createDomainModel <*> (Id id |> Ok) <*> validateTitle writeModel.Title
        <*> validateDescription writeModel.Description <*> validateLocation writeModel.Location
        <*> validateEmail writeModel.OrganizerEmail
        <*> validateDateRange writeModel.OpenForRegistrationDate writeModel.StartDate
                writeModel.EndDate

    let toCustomDateTime (date: DateTime) (time: TimeSpan): DateTimeCustom =
        { Date =
              { Day = date.Day
                Month = date.Month
                Year = date.Year }
          Time =
              { Hour = time.Hours
                Minute = time.Minutes } }

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
        db.Title <- unwrapTitle event.Title
        db.Description <- unwrapDescription event.Description
        db.Location <- unwrapLocation event.Location
        db.OrganizerEmail <- emailAddressToString event.OrganizerEmail
        db.StartDate <- customToDateTime event.StartDate.Date
        db.StartTime <- customToTimeSpan event.StartDate.Time
        db.EndDate <- customToDateTime event.EndDate.Date
        db.EndTime <- customToTimeSpan event.EndDate.Time
        db.OpenForRegistrationDate <- customToDateTime event.OpenForRegistrationDate.Date
        db.OpenForRegistrationTime <- customToTimeSpan event.OpenForRegistrationDate.Time
        db

    let domainToView (domainModel: DomainModel): ViewModel =
        { Id = unwrapId domainModel.Id
          Title = unwrapTitle domainModel.Title
          Description = unwrapDescription domainModel.Description
          Location = unwrapLocation domainModel.Location
          OrganizerEmail = emailAddressToString domainModel.OrganizerEmail
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
