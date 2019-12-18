namespace ArrangementService

open Utils.Validation

module Validation =

    let validateMinLength length errorMessage text =
        validate (fun x -> String.length x > length) text errorMessage

    let validateMaxLength length errorMessage text =
        validate (fun x -> String.length x < length) text errorMessage

    let validateEmail errorMessage email = Ok()

    let validateBefore errorMessage (before, after) =
        validate (fun (x, y) -> x < y) (before, after) errorMessage
