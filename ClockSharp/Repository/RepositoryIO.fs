module ClockSharp.HoursRepository.IO

open System

open FSharpx
open FSharpx.Option

open ClockSharp.HoursRepository
open ClockSharp.HoursRepository.Text
open ClockSharp.HoursRepository.Sql

let LoadRepository path =
   match Sql.Load path with
      | None -> Text.Load path
      | oSqlRepo -> oSqlRepo

let CreateRepository path times = 
   if Sql.Create path then
      Sql.Load path |> Option.map (fun r -> r.Insert times)
   else
      None
