namespace arrangementSvc

open Giraffe

module Health = 
    let healthCheck : HttpHandler = route "/health" >=> Successful.OK "Det gikk fint"