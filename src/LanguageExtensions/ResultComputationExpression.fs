namespace ArrangementService

module ResultComputationExpression =

    let withError error result =
        match result with
        | Some x -> Ok x
        | None -> Error error

    (*

    Å handtere feil på ein elegant måte med Result typen.
    Å gi tilgang til contexten vi får av giraffe, som er slik vi kan aksessere den eksterne verda, som databasen.

    Syntaxen man må kjenne til:

    let = ... går ikkje an å overskrive i språket og betyr derfor det vanlige.
    let! = ... er for å binde operasjoner som kan feile, slik at man kan jobbe som om man alltid får Ok ut av Result typen. Dersom den skulle feile, blir returverdien til heile computation expressionen den Erroren.
    for ... in ... do er tilsvarende, men for å aksesere contexten; som inneholder http-headere, writemodelen, database connectionen, etc.
    yield ... er ein side effekt som trenger tilgang til contexten
    yield! ... er side effekt som trenger tilgang til contexten og kan feile. Dersom den gjer det, vil returverdien til heile computation expressionen vere den Erroren.
    do! ... er side effekt som ikkje trenger tilgang til contexten, men kan faile. Usikker på kva dette skal vere (det er ikkje i bruk nokon plass), men trur det burde virke.
    return ... vanlig retur.
    return! ... returner noko som allereie er av typen Result.

    *)
    type ResultBuilder() =
        member this.Return(x) = fun _ -> Ok x
        member this.ReturnFrom(x) = x
        member this.Yield(f) = f >> Ok
        member this.Delay(f) = f
        member this.Run(f) = f()

        member this.Combine(lhs, rhs) =
            fun ctx ->
                match lhs ctx with
                | Ok _ -> rhs () ctx
                | Error e -> Error e

        member this.Bind(rx, f) = fun ctx -> Result.bind (fun x -> f x ctx) rx

        member this.For(rx, f) = fun ctx -> this.Bind (rx ctx, f) ctx

    let result = ResultBuilder()
