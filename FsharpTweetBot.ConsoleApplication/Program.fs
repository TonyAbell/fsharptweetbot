// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.

open System
open System.Configuration
open LinqToTwitter
open FsharpTweetBot
[<EntryPoint>]
let main argv = 
    let log msg =
        let filename = DateTime.Now.Ticks.ToString() + ".txt"
        System.IO.File.WriteAllText(filename,msg)    

    let creds = new InMemoryCredentialStore()
    creds.ConsumerKey <- ConfigurationManager.AppSettings.["ConsumerKey"]               
    creds.ConsumerSecret <- ConfigurationManager.AppSettings.["ConsumerSecret"]         
    creds.OAuthToken <- ConfigurationManager.AppSettings.["OAuthToken"]                 
    creds.OAuthTokenSecret <- ConfigurationManager.AppSettings.["OAuthTokenSecret"]   
    creds.UserID <- UInt64.Parse(ConfigurationManager.AppSettings.["UserId"])
    creds.ScreenName <- ConfigurationManager.AppSettings.["ScreenName"]
    let cnnString = ConfigurationManager.AppSettings.["StorageCnnString"]               
    let azureCache = new AzureTwitterStatusCache(cnnString) :> ITwitterStatusCache
    let fileCache = new FileTwitterStatusCache("cache.bin") :> ITwitterStatusCache
    let bot = new Bot(creds,fileCache,log)                
    async {
        while true do
            bot.Process()
            System.Console.Clear() 
            printfn "enter to exit"
            printfn "working %s" (DateTime.Now.ToString())
            do! Async.Sleep((10 * 60 * 1000)) // 
    } |> Async.Start
   
    System.Console.ReadLine() |> ignore
    0 

