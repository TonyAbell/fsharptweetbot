// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.
namespace FsharpTweetBot

open System
open System.Linq
open LinqToTwitter


type Bot(creds:InMemoryCredentialStore, tweetCache:ITwitterStatusCache, log:String -> unit) =
    
    let auth = new SingleUserAuthorizer()   
    let logEx (ex:Exception) (s:Status) =
        let msg = [ex.ToString(); s.ToString(); s.Text; s.StatusID.ToString() ] |> Seq.fold(fun s i -> s + i + System.Environment.NewLine ) ""
        log (msg)
        ()
    do       
        auth.CredentialStore <- creds    
    member this.Process() =
        try 
            auth.AuthorizeAsync().Wait()
        
            let twitterCtx = new TwitterContext(auth)
            
            let userId = auth.CredentialStore.UserID

            let query = query { 
                for search in twitterCtx.Search do
                    where (search.Type = SearchType.Search && search.Query = "#fsharp")
                    for status in search.Statuses do
                        let cnt = status.RetweetCount + 
                                  (if status.FavoriteCount.HasValue then status.FavoriteCount.Value else 0)
                        where (status.User.FollowersCount >= 50 && cnt >= 5 && status.UserID <> userId)
                        distinct 
                        select status 
                }

            for q in query do
                if (tweetCache.Exists q) <> true then
                    try                     
                        twitterCtx.RetweetAsync(q.StatusID).Wait()  
                        tweetCache.Add q
                    with 
                        | :? AggregateException as ae -> 
                                ae.Handle(fun ex -> match ex with 
                                                      | :? TwitterQueryException as twq -> 
                                                            if twq.StatusCode = Net.HttpStatusCode.Forbidden then
                                                                tweetCache.Add q    
                                                            true
                                                      | _ -> 
                                                             logEx ex q 
                                                             true
                                         )                                                                 
                        | ex -> 
                                logEx ex q
                            
        with ex -> log (ex.ToString())
        ()
