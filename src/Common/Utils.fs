namespace ArrangementService

open Validation
open DateTime

module Utils =

    let validateMinLength length errorMessage text =
        validate (fun x -> String.length x > length) errorMessage text

    let validateMaxLength length errorMessage text =
        validate (fun x -> String.length x < length) errorMessage text

    let validateBefore errorMessage (before, after) =
        validate (fun (x, y) -> x < y) errorMessage (before, after) 

    let validateDateRange openDate startDate endDate =
        [ fun (openDate, startDate, _) ->
            validateBefore "Registreringsdato må være før åpningsdato" (openDate, startDate)
          fun (openDate, _, endDate) ->
              validateBefore "Registreringsdato må være før sluttdato" (openDate, endDate)
          fun (openDate, _, _) ->
              validateBefore "Åpningsdato må være i fremtiden" (now (), openDate) ]
        |> validateAll id (openDate, startDate, endDate)