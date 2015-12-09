module private ClockSharp.HoursRepository.Text

open System
open System.Globalization
open System.IO
open System.Text
open FSharpx
open FSharpx.Option
open ClockSharp.HoursRepository
open ClockSharp.Model

let private recordLength = 30

type System.DateTime with
   static member ParseExact(s : string, format : string) = 
      let mutable value = Unchecked.defaultof<System.DateTime>
      ofBoolAndValue 
         (DateTime.TryParseExact(s, format, CultureInfo.InvariantCulture, DateTimeStyles.None, &value), value)

let private dateOf (record : string) = DateTime.ParseExact(record.Substring(5, 11), "dd-MMM-yyyy")
let private startOf (record : string) = DateTime.ParseExact(record.Substring(17, 5), "HH:mm")
let private finishOf (record : string) = DateTime.ParseExact(record.Substring(23, 5), "HH:mm")

let private toTimeRecord d s f = 
   { Date = AsDate d
     Start = AsTimePoint s
     Finish = AsTimePoint f }

let private TimeRecordFrom record = toTimeRecord <!> dateOf record <*> startOf record <*> finishOf record

let ReadRecord(f : FileStream) : TimeRecord option = 
   try 
      let buffer : byte [] = Array.zeroCreate recordLength
      if recordLength = f.Read(buffer, 0, recordLength) then TimeRecordFrom <| Encoding.ASCII.GetString buffer
      else None
   with _ -> None

let WriteRecord (f : FileStream) (r : TimeRecord) : bool = 
   try 
      let day = (DateToDateTime r.Date).ToString("ddd, dd-MMM-yyyy")
      let start = (TimePointToDateTime r.Start).ToString("HH:mm")
      let finish = (TimePointToDateTime r.Finish).ToString("HH:mm")
      let newRecord = sprintf "%16s %5s %5s\x0d\x0a" day start finish
      let recordBytes = Encoding.ASCII.GetBytes newRecord
      if recordBytes.Length = recordLength then 
         f.Write(recordBytes, 0, recordLength)
         true
      else false
   with _ -> false

type TextHoursRepository(path : string) = 
   let f = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read)
   
   member this.LoadRecords() = 
      try 
         File.ReadLines(path)
         |> Seq.map TimeRecordFrom
         |> Seq.toList
         |> Option.sequence
         |> Option.map Seq.ofList
      with _ -> None
   
   interface IDisposable with
      member this.Dispose() = 
         f.Dispose()
         ()
   
   interface IHoursRepository with
      member this.GetTimeRecords() = this.LoadRecords() |> getOrElse Seq.empty
      
      member this.Update newTimesForDay = 
         try 
            let existingRecords = (this :> IHoursRepository).GetTimeRecords()
            let recordIndex = Seq.findIndex (fun r -> r.Date = newTimesForDay.Date) existingRecords
            f.Position <- int64 recordLength * int64 recordIndex
            WriteRecord f newTimesForDay |> ignore
            Some(this :> IHoursRepository)
         with _ -> Some(this :> IHoursRepository)
      
      member this.Insert newRecords = 
         let existingRecords = (this :> IHoursRepository).GetTimeRecords()
         let allRecords = Seq.append existingRecords newRecords
         let distinctRecordCount = Seq.distinctBy (fun r -> r.Date) allRecords |> Seq.length
         if distinctRecordCount = Seq.length allRecords then 
            let sortedRecords = Seq.sortBy (fun r -> r.Date) allRecords
            f.Position <- 0L
            Seq.map (WriteRecord f) sortedRecords |> ignore
            Some(this :> IHoursRepository)
         else Some(this :> IHoursRepository)

let Load path = 
   let repo = new TextHoursRepository(path)
   repo.LoadRecords() |> Option.map (fun _ -> repo :> IHoursRepository)
