namespace FsharpTweetBot
open FsPickler

open Microsoft.WindowsAzure.Storage;
open Microsoft.WindowsAzure.Storage.Auth;
open Microsoft.WindowsAzure.Storage.Blob

open System.IO

      
type AzureTwitterStatusCache(cnnString) =
    let fsp = new FsPickler()

    let storageAccount = CloudStorageAccount.Parse cnnString
    let blobClient = storageAccount.CreateCloudBlobClient();
    let container = blobClient.GetContainerReference("fsharptweetbot");
    let blobReference = container.GetBlockBlobReference("cachetext");
    let saveCache (blob:CloudBlockBlob) cache =
        let stream = new MemoryStream()
        fsp.Serialize<Set<string>>(stream, cache)
        stream.Seek(0L, SeekOrigin.Begin) |> ignore
        blob.UploadFromStream(stream)
        ()
    let agent = 
        MailboxProcessor<Message>.Start(fun inbox -> 
            container.CreateIfNotExists() |> ignore
            let cache = if blobReference.Exists() then
                            let stream = new MemoryStream()
                            blobReference.DownloadToStream(stream)
                            stream.Seek(0L, SeekOrigin.Begin) |> ignore  
                            fsp.Deserialize<Set<string>>(stream)
                        else 
                            Set.empty<string>
            let rec loop (cache:Set<string>) = 
                async { 
                    let! msg = inbox.Receive()                    
                    match msg with
                        | Exists (tweet,reply) -> reply.Reply((cache.Contains tweet.Text))
                                                  do! loop (cache)
                        | Add tweet -> let updatedCache = cache.Add(tweet.Text)
                                       saveCache blobReference updatedCache
                                       do! loop (updatedCache)                                          
                }
            loop (cache))

    
   
    
    interface ITwitterStatusCache with
        member this.Exists(s) =                           
            let exists = agent.PostAndReply(fun r -> Exists(s,r))
            exists
        member this.Add(s) =  
            agent.Post(Add(s))
                       
            


