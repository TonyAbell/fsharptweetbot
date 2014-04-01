namespace FsharpTweetBot

type ITwitterStatusCache =
    abstract member Add : LinqToTwitter.Status -> unit
    abstract member Exists : LinqToTwitter.Status -> bool

type Message =
        | Exists of LinqToTwitter.Status * AsyncReplyChannel<bool>
        | Add of LinqToTwitter.Status 