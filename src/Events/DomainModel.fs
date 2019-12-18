namespace ArrangementService.Events

open System

open ArrangementService

open Utils.Validation
open Validation
open Http
open DateTime
open Email.Models

module DomainModel =

    type Key = Guid
    type Id = Id of Key
    type Title = Title of string
    type Description = Description of string
    type Location = Location of string
    type StartDate = DateTimeOffset
    type EndDate = DateTimeOffset
    type ResponsibleEmployee = ResponsibleEmployee of int

    let unwrapId = function | Id id -> id
    let unwrapTitle = function | Title t -> t
    let unwrapDescription = function | Description d -> d
    let unwrapLocation = function | Location l -> l

    type DomainModel =
        { Id: Id
          Title: Title
          Description: Description
          Location: Location
          StartDate: DateTimeCustom
          EndDate: DateTimeCustom
          OrganizerEmail: EmailAddress
          OpenForRegistrationDate: DateTimeCustom }

    let createDomainModel id title description location organizerEmail (openForRegistrationDate, startDate, endDate): DomainModel =
        { Id = id
          Title = title
          Description = description 
          Location = location
          OrganizerEmail = organizerEmail
          StartDate = startDate
          EndDate = endDate
          OpenForRegistrationDate = openForRegistrationDate }

    let validateTitle title : Result<Title, CustomErrorMessage list> =
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
      [ fun (openDate, startDate, _) -> validateBefore "Registreringsdato må være før åpningsdato" (openDate, startDate)
        fun (openDate, _, endDate) -> validateBefore "Registreringsdato må være før sluttdato" (openDate, endDate)
        fun (openDate, _, _) -> validateBefore "Åpningsdato må være i fremtiden" (now (), openDate) ]
      |> validateAll id (openDate, startDate, endDate)