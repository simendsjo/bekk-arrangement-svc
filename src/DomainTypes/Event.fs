namespace ArrangementService.Event

open System
open ArrangementService

open Validation
open ArrangementService.Utils
open UserMessage

type Key = Guid

type Id =
    | Id of Key
    member this.Unwrap =
        match this with
        | Id id -> id

type EmployeeId =
    | EmployeeId of int
    member this.Unwrap =
        match this with
        | EmployeeId id -> id

type Title =
    | Title of string

    member this.Unwrap =
        match this with
        | Title title -> title

    static member Parse(title: string) =
        [ validateMinLength 3 (BadInput "Tittel må ha minst 3 tegn")
          validateMaxLength 60 (BadInput "Tittel kan ha maks 60 tegn") ]
        |> validateAll Title title

type Description =
    | Description of string

    member this.Unwrap =
        match this with
        | Description description -> description

    static member Parse(description: string) =
        [ validateMinLength 3 (BadInput "Beskrivelse må ha minst 3 tegn") ]
        |> validateAll Description description

type Location =
    | Location of string

    member this.Unwrap =
        match this with
        | Location location -> location

    static member Parse(location: string) =
        [ validateMinLength 3 (BadInput "Sted må ha minst 3 tegn")
          validateMaxLength 60 (BadInput "Sted kan ha maks 60 tegn") ]
        |> validateAll Location location

type OrganizerName =
    | OrganizerName of string

    member this.Unwrap =
        match this with
        | OrganizerName organizerName -> organizerName

    static member Parse(organizerName: string) =
        [ validateMinLength 3 (BadInput "Navn må ha minst 3 tegn")
          validateMaxLength 50 (BadInput "Navn kan ha maks 50 tegn") ]
        |> validateAll OrganizerName organizerName

type MaxParticipants =
    | MaxParticipants of int option

    member this.Unwrap =
        match this with
        | MaxParticipants maxParticipants -> maxParticipants

    static member Parse(maxParticipants: int option) =
        [ validateNotNegative (BadInput "Antall kan ikke være negativt") |> optionally ]
        |> validateAll MaxParticipants maxParticipants

type OpenForRegistrationTime =
    | OpenForRegistrationTime of int64

    member this.Unwrap =
        match this with
        | OpenForRegistrationTime time -> time

    static member Parse(time: string) = int64 time |> OpenForRegistrationTime |> Ok

type ParticipantQuestion =
    | ParticipantQuestion of string option

    member this.Unwrap =
        match this with
        | ParticipantQuestion participantQuestion -> participantQuestion

    static member Parse(participantQuestion: string option) =
        [ validateMaxLength 500
              (BadInput "Spørsmål til deltaker kan ha maks 500 tegn")
          |> optionally ]
        |> validateAll ParticipantQuestion participantQuestion

type NumberOfParticipants = 
    | NumberOfParticipants of int

    member this.Unwrap =
        match this with
        | NumberOfParticipants count -> count

type Shortname =
    | Shortname of string option

    member this.Unwrap =
        match this with
        | Shortname shortname -> shortname

    static member Parse(shortname: string option) =
        [ validateMinLength 1
              (BadInput "URL kortnavn kan ikke vere tom streng!")
          |> optionally

          validateMaxLength 200
              (BadInput "URL kortnavn kan ha maks 200 tegn")
          |> optionally

          validateDoesNotContain "/?#"
              (BadInput "URL kortnavn kan ikke inneholde reserverte tegn")
          |> optionally
        ]
        |> validateAll Shortname shortname