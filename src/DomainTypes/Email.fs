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

        let isDotSign =
            function
            | '.' -> true
            | _ -> false

        [ validate (String.exists isAtSign)
              (BadInput "E-post må inneholde en alfakrøll (@)")
          validate (String.exists isDotSign)
              (BadInput "E-post må inneholde et punktum (.)") ]
        |> validateAll EmailAddress address
