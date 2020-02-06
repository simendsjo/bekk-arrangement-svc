namespace ArrangementService

open Validation
open DateTime
open UserMessage

module Utils =

    let validateMinLength length errorMessage text =
        validate (fun x -> String.length x > length) errorMessage text

    let validateMaxLength length errorMessage text =
        validate (fun x -> String.length x < length) errorMessage text

    let validateBefore errorMessage (before, after) =
        validate (fun (x, y) -> x < y) errorMessage (before, after)

    let validateNotNegative errorMessage number =
        validate (fun x -> x >= 0) errorMessage number

    let validateDateRange openDate startDate endDate =
        [ fun (openDate, startDate, _) ->
            validateBefore
                (BadInput "Registreringsdato må være før åpningsdato")
                (openDate, startDate)
          fun (openDate, _, endDate) ->
              validateBefore
                  (BadInput "Registreringsdato må være før sluttdato")
                  (openDate, endDate) ]
        |> validateAll id (openDate, startDate, endDate)
