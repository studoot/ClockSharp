module private ClockSharp.HoursRepository.Sql

open FSharp.Data.Sql
open System.Data.SQLite
open System.Linq
open ClockSharp.HoursRepository.Interface
open ClockSharp.Model

[<Literal>]
let ConnectionString = "Data Source = " + __SOURCE_DIRECTORY__ + @"\..\empty.db ;Version=3"

[<Literal>]
let SqlLiteDllPath = __SOURCE_DIRECTORY__ + @"\..\..\packages\SQLProvider\docs\sqlite\"

type HoursDatabase = SqlDataProvider< ConnectionString=ConnectionString, ResolutionPath=SqlLiteDllPath, DatabaseVendor=Common.DatabaseProviderTypes.SQLITE >

let private dbRecordToTimeRecord = function 
   | (day, start, finish) -> 
      { Date = DateTimeToDate day
        Start = DateTimeToTimePoint start
        Finish = DateTimeToTimePoint finish }
let private timeRecordToDbRecord r = (DateToDateTime r.Date, TimePointToDateTime r.Start, TimePointToDateTime r.Finish)

let private createRecord (ctx : HoursDatabase.dataContext) (r : TimeRecord) : HoursDatabase.dataContext.``[main].[hours]Entity`` = 
   let record = ctx.``[main].[hours]``.Create()
   record.Day <- DateToDateTime r.Date
   record.Start <- TimePointToDateTime r.Start
   record.Finish <- TimePointToDateTime r.Finish
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
      
      member this.Update newTimesForDay = 
         try 
            let (dayToUpdate, newStart, newFinish) = timeRecordToDbRecord newTimesForDay
            
            let recordsToUpdate = 
               query { 
                  for r in ctx.``[main].[hours]`` do
                     where (r.Day = dayToUpdate)
                     select r
               }
            match recordsToUpdate.Count() with
            | 1 -> 
               let updatedRecords = 
                  recordsToUpdate |> Seq.map (fun r -> 
                                        r.Start <- newStart
                                        r.Finish <- newFinish)
               ctx.SubmitUpdates()
               Some(this :> IHoursRepository)
            | 0 -> 
               let newRecord = createRecord ctx newTimesForDay
               ctx.SubmitUpdates()
               Some(this :> IHoursRepository)
            | _ -> None
         with _ -> None
      
      member this.Insert(times : TimeRecords) = 
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
