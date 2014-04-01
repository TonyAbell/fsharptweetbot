namespace FsharpTweetBot
open FsPickler
open System.IO

type FileTwitterStatusCache(file) =
    let fsp = new FsPickler()
    let saveCache cache =
        let stream = new MemoryStream()
        fsp.Serialize<Set<string>>(stream, cache)               
        File.WriteAllBytes(file,stream.ToArray())        
        ()
    let agent = 
        MailboxProcessor<Message>.Start(fun inbox -> 
            
            let cache = if File.Exists(file) then
                            
                            let fs = File.Open(file, FileMode.Open)                            
                            let set = fsp.Deserialize<Set<string>>(fs)
                            fs.Close()
                            set
                        else 
                            Set.empty<string>
            let rec loop (cache:Set<string>) = 
                async { 
                    let! msg = inbox.Receive()                    
                    match msg with
                        | Exists (tweet,reply) -> let a =  if tweet.RetweetedStatus <> null then
                                                                cache.Contains tweet.RetweetedStatus.Text
                                                           else 
                                                                false
                                                  let b = (cache.Contains tweet.Text)
                                                  reply.Reply(a || b)
                                                  do! loop (cache)
                        | Add tweet -> let updatedCache = cache.Add(tweet.Text)
                                       saveCache updatedCache
                                       do! loop (updatedCache)                                          
                }
            loop (cache))
    interface ITwitterStatusCache with
        member this.Exists(s) =                           
            let exists = agent.PostAndReply(fun r -> Exists(s,r))
            exists
        member this.Add(s) =  
            agent.Post(Add(s))