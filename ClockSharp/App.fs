module MainApp

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Markup
open ClockSharp.HoursRepository
open ClockSharp.HoursRepository.IO

// Create the View and bind it to the View Model
let mainWindowViewModel = 
   Application.LoadComponent(new System.Uri("/App;component/mainwindow.xaml", UriKind.Relative)) :?> Window


// Application Entry point
[<STAThread>]
[<EntryPoint>]
let main (_) = 
   let repo = (LoadRepository (__SOURCE_DIRECTORY__ + @"\u404261.clock-card")).Value
   let records = repo.GetTimeRecords()
   (new Application()).Run(mainWindowViewModel)
