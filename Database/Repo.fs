namespace ArrangementService

open Microsoft.AspNetCore.Http
open System.Collections.Generic

module Repo =

    type Models<'dbModel, 'DomainModel, 'ViewModel, 'WriteModel, 'key, 'table when 'table :> IEnumerable<'dbModel>> =
        { table: HttpContext -> 'table
          create: 'table -> 'dbModel
          delete: 'dbModel -> Unit
          key: 'dbModel -> 'key
          dbToDomain: 'dbModel -> 'DomainModel
          updateDbWithDomain: 'dbModel -> 'DomainModel -> 'dbModel
          domainToView: 'DomainModel -> 'ViewModel
          writeToDomain: 'key -> 'WriteModel -> 'DomainModel }

    let from models =
        {| create =
               fun createRow ctx ->
                   let row = models.table ctx |> models.create
                   let newEvent = models.key row |> createRow
                   models.updateDbWithDomain row newEvent |> ignore
                   newEvent
           read = models.table >> Seq.map models.dbToDomain
           update =
               fun newEvent event ->
                   models.updateDbWithDomain event newEvent |> ignore
                   event |> models.dbToDomain
           del = fun row -> models.delete row
           query = models.table |}
