namespace ArrangementService

open Giraffe
open Microsoft.AspNetCore.Http

module Handler =
    let (>->) f g x = g (f x) x

    let handle f (next: HttpFunc) (ctx: HttpContext) = json (f ctx) next ctx

    let handleWithError errorMessage f (next: HttpFunc) (ctx: HttpContext) =
        f ctx
        |> function
        | Some result -> json result next ctx
        | None -> errorMessage next ctx

    let getBody<'WriteModel> (ctx: HttpContext) = ctx.BindJsonAsync<'WriteModel>().Result
