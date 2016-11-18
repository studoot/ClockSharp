module ClockSharp.HoursRepository.IO

open ClockSharp.HoursRepository

let LoadRepository path = 
   match Sql.Load path with
   | None -> Text.Load path
   | oSqlRepo -> oSqlRepo

let CreateRepository path times = 
   if Sql.Create path then Sql.Load path |> Option.map (fun r -> r.Insert times)
   else None
