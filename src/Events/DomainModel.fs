namespace ArrangementService.Events

open System

open ArrangementService

open Validation
open UserMessage
open Utils
open DateTime
open Email.Models

module DomainModel =

    type Key = Guid
    type Id =
        | Id of Key
        member this.Unwrap =
            match this with
            | Id id -> id

    type Title =
        | Title of string
        member this.Unwrap =
            match this with
            | Title title -> title

        static member Parse(title: string) =
            [ validateMinLength 3 (BadInput "Tittel må ha minst 3 tegn")
              validateMaxLength 60 (BadInput "Tittel må være mindre enn 60 tegn") ]
            |> validateAll Title title

    type Description =
        | Description of string

        member this.Unwrap =
            match this with
            | Description description -> description

        static member Parse(description: string) =
            [ validateMinLength 3 (BadInput "Beskrivelse må ha minst 3 tegn")
              validateMaxLength 255 (BadInput "Beskrivelse må være mindre enn 255 tegn") ]
            |> validateAll Description description

    type Location =
        | Location of string
        member this.Unwrap =
            match this with
            | Location location -> location
        static member Parse(location: string) =
            [ validateMinLength 3 (BadInput "Sted må ha minst 3 tegn")
              validateMaxLength 30 (BadInput "Sted må være mindre enn 30 tegn") ]
            |> validateAll Location location

    type OrganizerName =
        | OrganizerName of string
        member this.Unwrap =
            match this with
            | OrganizerName organizerName -> organizerName
        static member Parse(organizerName: string) =
            [ validateMinLength 3 (BadInput "Navn må ha minst 3 tegn")
              validateMaxLength 50 (BadInput "Navn må være mindre enn 50 tegn") ]
            |> validateAll OrganizerName organizerName

    type MaxParticipants =
        | MaxParticipants of int
        member this.Unwrap =
            match this with
            | MaxParticipants maxParticipants -> maxParticipants
        static member Parse(maxParticipants: int) = 
          [ validateNotNegative (BadInput "Antall kan ikke være negativt")] 
          |> validateAll MaxParticipants maxParticipants

    type DomainModel =
        { Id: Id
          Title: Title
          Description: Description
          Location: Location
          StartDate: DateTimeCustom
          EndDate: DateTimeCustom
          OrganizerName: OrganizerName
          OrganizerEmail: EmailAddress
          OpenForRegistrationDate: DateTimeCustom
          MaxParticipants: MaxParticipants }
        static member Create =
            fun id title description location organizerName organizerEmail maxParticipants (openForRegistrationDate, startDate, endDate) ->
                { Id = id
                  Title = title
                  Description = description
                  Location = location
                  OrganizerName = organizerName
                  OrganizerEmail = organizerEmail
                  MaxParticipants = maxParticipants
                  StartDate = startDate
                  EndDate = endDate
                  OpenForRegistrationDate = openForRegistrationDate }
