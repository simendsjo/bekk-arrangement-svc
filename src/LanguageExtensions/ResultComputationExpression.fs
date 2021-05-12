namespace ArrangementService

module ResultComputationExpression =

    let withError error result =
        match result with
        | Some x -> Ok x
        | None -> Error error

    // This is the same as `constant`
    // This is used with `let!` when an expression
    // can fail (results in the Result type), but does not
    // need the ctx
    let ignoreContext r _ctx = r

    (*

    This (reader)-result computation expression has two primary goals

     - 1. To handle failures in an elegant way using the Result type
     - 2. To give access to the context provided by Giraffe, which gives access to the external world
          such as HTTP headers, the DB context, and the apps's config.

    The following is the available syntax this computation expression implements.

     - `let! x = ...` binds operations which can fail and needs the context. `x` is then the value of `...` iff `...` evaluates to `Ok x`.
        If it should fail, the whole expression evaluates to this `Error`. The context is passed in as the last parameter.

     - `yield ...` is for side effects, which need access to the context, but cannot fail.

     - `yield! ...` is for side effects which need access to the context and can fail.
        If the side effect does fail, the whole computation expression evaluates to this `Error`.

     - `return ...` takes a regular value and wraps it up.

     - `return! ...` takes a Result value and wraps it up.

    *)
    type ResultBuilder() =
        member this.Return(x) = fun _ -> Ok x
        member this.ReturnFrom(x) = fun _ -> x
        member this.Yield(f) = f >> Ok
        member this.YieldFrom(f) = f
        member this.Delay(f) = f
        member this.Run(f) = f ()

        member this.Combine(lhs, rhs) =
            fun ctx ->
                match lhs ctx with
                | Ok _ -> this.Run rhs ctx
                | Error e -> Error e

        member this.Bind(rx, f) =
            fun ctx -> Result.bind (fun x -> f x ctx) (rx ctx)

    let result = ResultBuilder()
