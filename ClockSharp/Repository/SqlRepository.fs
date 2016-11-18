module private ClockSharp.HoursRepository.Sql

open ClockSharp.HoursRepository.Interface
open ClockSharp.Model
open FSharp.Data.Sql
open System.Data.SQLite
open System.Linq

[<Literal>]
let ConnectionString = "Data Source = " + __SOURCE_DIRECTORY__ + @"\..\empty.db ;Version=3"

[<Literal>]
let SqlLiteDllPath = __SOURCE_DIRECTORY__ + @"\..\..\packages\SQLProvider\docs\sqlite\"

type HoursDatabase = SqlDataProvider< ConnectionString=ConnectionString, ResolutionPath=SqlLiteDllPath, DatabaseVendor=Common.DatabaseProviderTypes.SQLITE >

let dbRecordToTimeRecord = function 
   | (day, start, finish) -> 
      { Date = DateTimeToDate day
        Start = DateTimeToTimePoint start
        Finish = DateTimeToTimePoint finish }
let timeRecordToDbRecord r = (DateToDateTime r.Date, TimePointToDateTime r.Date r.Start, TimePointToDateTime r.Date r.Finish)

let createRecord (ctx : HoursDatabase.dataContext) (r : TimeRecord) : HoursDatabase.dataContext.``[main].[hours]Entity`` = 
   let record = ctx.``[main].[hours]``.Create()
   record.Day <- DateToDateTime r.Date
   record.Start <- TimePointToDateTime r.Date r.Start
   record.Finish <- TimePointToDateTime r.Date r.Finish
   record

type SqlHoursRepository(path : string) = 
   let ctx = HoursDatabase.GetDataContext("Data Source = " + path + " ;Version=3")
   member this.Ctx 
      with internal get () = ctx
   member this.RecordCount = ctx.``[main].[hours]``.Count()
   interface IHoursRepository with
      
      member this.GetTimeRecords() = 
         query { 
            for r in ctx.``[main].[hours]`` do
               select (r.Day, r.Start, r.Finish)
         }
         |> Seq.map dbRecordToTimeRecord
      
      member this.Update (d : Date) (t : TimePoint) = 
         try 
            let recordsToUpdate = 
               let dayToUpdate = DateToDateTime d
               query { 
                  for r in ctx.``[main].[hours]`` do
                     where (r.Day = dayToUpdate)
                     select r
               }
            match recordsToUpdate.Count() with
            | 1 -> 
               let updateTime = TimePointToDateTime d t
               let updatedRecords = recordsToUpdate |> Seq.map (fun r -> r.Finish <- updateTime)
               ctx.SubmitUpdates()
               Some(this :> IHoursRepository)
            | 0 -> 
               let newRecord = 
                  createRecord ctx { Date = d
                                     Start = t
                                     Finish = t }
               ctx.SubmitUpdates()
               Some(this :> IHoursRepository)
            | _ -> None
         with _ -> None
      
      member this.Insert times = 
         try 
            let newRecords = 
               times
               |> Seq.map (createRecord ctx)
               |> Seq.toList
            ctx.SubmitUpdates()
            Some(this :> IHoursRepository)
         with _ -> None

let Load(path : string) : IHoursRepository option = 
   try 
      let repo = SqlHoursRepository(path)
      repo.RecordCount |> ignore // To trigger an exception...
      Some(repo :> IHoursRepository)
   with _ -> None

let Create path = 
   try 
      SQLiteConnection.CreateFile(path)
      use newDbConnection = new SQLiteConnection("Data Source=" + path)
      if isNull newDbConnection then false
      else 
         newDbConnection.Open()
         use createTableCommand = newDbConnection.CreateCommand()
         if isNull createTableCommand then false
         else 
            createTableCommand.CommandText <- """
                  CREATE TABLE [hours] (
                     [Day] DATE NOT NULL,
                     [Start] TIME NOT NULL,
                     [Finish] TIME NOT NULL,
                     CONSTRAINT [] PRIMARY KEY ([Day]));"""
            createTableCommand.ExecuteNonQuery() |> ignore
            true
   with ex -> false
