namespace ArrangementService.Email

open ArrangementService
open Validation
open UserMessage

type EmailAddress =
    | EmailAddress of string

    member this.Unwrap =
        match this with
        | EmailAddress e -> e

    static member Parse(address: string) =

        let isAtSign =
            function
            | '@' -> true
            | _ -> false

        [ validate (String.exists isAtSign)
              (BadInput "Email address must include an at sign (@)") ]
        |> validateAll EmailAddress address
