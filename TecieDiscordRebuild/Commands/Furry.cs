using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using NetCord;

namespace TecieDiscordRebuild.Commands
{
    internal static class StaticFurry
    {
        public static bool furpileStarted = false;
        public static int furpileCount = 0;
        public static List<ulong> fursInPile = [];
        
        public static bool congaStarted = false;
        public static int congaCount = 0;
        public static List<ulong> fursInConga = [];
    }

    [SlashCommand("furry", "Furry commands :3", DMPermission = false)]
    internal class Furry : ApplicationCommandModule<SlashCommandContext>
    {
        [SubSlashCommand("glomp", "Glomp on someone :3")]
        public async Task Glomp([SlashCommandParameter(Name = "user", Description = "The person to glomp")] User member)
        {
            if (member.Id == Context.User.Id) { await RespondAsync(InteractionCallback.Message(new() { Content = "You can't glomp yourself!" })); }
            else { await RespondAsync(InteractionCallback.Message(new() { Content = $"<@{Context.User.Id}> jumps at <@{member.Id}>, knocking them to the ground in a hug UwU" })); }
        }

        [SubSlashCommand("hug", "Hug someone :3")]
        public async Task Hug([SlashCommandParameter(Name = "user", Description = "The person to hug")] User member)
        {
            if (member.Id == Context.User.Id) { await RespondAsync(InteractionCallback.Message(new() { Content = "You can't hug yourself!" })); }
            else { await RespondAsync(InteractionCallback.Message(new() { Content = $"<@{Context.User.Id}> hugs <@{member.Id}> :3" })); }
        }

        [SubSlashCommand("pet", "pet someone :3")]
        public async Task Pet([SlashCommandParameter(Name = "user", Description = "The person to pet")] User user) => await PetCommand(user); 

        [SubSlashCommand("pat", "pat someone :3")]
        public async Task Pat([SlashCommandParameter(Name = "user", Description = "The person to pat")] User user) => await PetCommand(user); 

        public async Task PetCommand(User member)
        {
            if (member.Id == Context.User.Id) { await RespondAsync(InteractionCallback.Message(new() { Content = "You can't pet yourself!" })); }
            else { await RespondAsync(InteractionCallback.Message(new() { Content = $"<@{Context.User.Id}> gently pats <@{member.Id}> on the head :3" })); }
        }

        [SubSlashCommand("scree", "Just a fox scream lol")]
        public async Task Scree() // lol
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = $"<@{Context.User.Id}>, if not already, turns into a fox. They then let out a loud screeeeeee. Everyone at this channel is now temporarily deaf, and the scream can be heard in all channels through this server. Don't be mad at me, it was <@{Context.User.Id}> who did it." }));
        }

        [SubSlashCommand("furpile", "Join or make a furpile!")]
        public async Task FurPile([SlashCommandParameter(Name = "user", Description = "A person to bring with")] User? member = null)
        {
            ulong authorID = Context.User.Id;

            // if it's not started yet, and they don't bring anyone with
            if (member == null && !StaticFurry.furpileStarted)
            { await RespondAsync(InteractionCallback.Message(new() { Content = "You have to start it with someone!" })); }
            // if it is started, the user is already on the pile, and they aren't the bot creator
            else if (StaticFurry.furpileStarted && StaticFurry.fursInPile.Contains(authorID) && Program.authorID != authorID)
            { await RespondAsync(InteractionCallback.Message(new() { Content = "You are already in the pile!" })); }
            // if it is started, they mention a user, they are already in the pile, they are the bot creator, and the mentioned user isn't in the pile
            else if (StaticFurry.furpileStarted && member != null && StaticFurry.fursInPile.Contains(authorID) && Program.authorID == authorID && !StaticFurry.fursInPile.Contains(member.Id))
            { StaticFurry.furpileCount++; await RespondAsync(InteractionCallback.Message(new() { Content = $"You somehow get <@{member.Id}> on the pile! \nThere are {StaticFurry.furpileCount} furs in the pile." })); StaticFurry.fursInPile.Add(member.Id); }
            // if it is started, they mention a user, they are already in the pile, they are the bot creator, and the mention user is in the pile
            else if (StaticFurry.furpileStarted && member != null && StaticFurry.fursInPile.Contains(authorID) && Program.authorID == authorID && StaticFurry.fursInPile.Contains(member.Id))
            { await RespondAsync(InteractionCallback.Message(new() { Content = $"<@{member.Id}> is on the pile" })); }
            // if it is started, they mention a user, the mentioned user is in the pile, and they are not in the pile
            else if (StaticFurry.furpileStarted && member != null && StaticFurry.fursInPile.Contains(member.Id) && !StaticFurry.fursInPile.Contains(authorID))
            { StaticFurry.furpileCount++; await RespondAsync(InteractionCallback.Message(new() { Content = $"<@{member.Id}> is already in the pile, but <@{authorID}> joins the pile! \nThere are {StaticFurry.furpileCount} furs in the pile." })); StaticFurry.fursInPile.Add(authorID); }
            // if they didn't bring anyone with
            else if (member == null)
            { StaticFurry.furpileCount++; await RespondAsync(InteractionCallback.Message(new() { Content = $"<@{authorID}> has joined the furpile! \nThere are {StaticFurry.furpileCount} furs in the pile." })); StaticFurry.fursInPile.Add(authorID); }
            // if they brought someone with, and it isn't started
            else if (member != null && !StaticFurry.furpileStarted)
            {
                StaticFurry.furpileCount = 2;
                await RespondAsync(InteractionCallback.Message(new() { Content = $"<@{authorID}> started a furpile with <@{member.Id}>! \nThere are {StaticFurry.furpileCount} furs in the pile." }));
                StaticFurry.furpileStarted = true;
                StaticFurry.fursInPile.Add(authorID);
                StaticFurry.fursInPile.Add(member.Id);
            }
            // if they brought someone with, and it is started
            else if (member != null && StaticFurry.furpileStarted)
            {
                StaticFurry.furpileCount += 2;
                await RespondAsync(InteractionCallback.Message(new() { Content = $"<@{authorID}> joined, bringing <@{member.Id}> with them! \nThere are {StaticFurry.furpileCount} furs in the pile." }));
                StaticFurry.fursInPile.Add(authorID);
                StaticFurry.fursInPile.Add(member.Id);
            }
        }

        [SubSlashCommand("leave-furpile", "Leave the furpile :(")]
        public async Task LeavePile()
        {
            if (!StaticFurry.fursInPile.Contains(Context.User.Id)) { await RespondAsync(InteractionCallback.Message(new() { Content = "You are not in the pile" })); return; }
            if (StaticFurry.furpileCount > 1)
            {
                if (StaticFurry.fursInPile.Last() == Context.User.Id)
                {
                    await RespondAsync(InteractionCallback.Message(new() { Content = "You hop off the pile" }));
                    StaticFurry.fursInPile.Remove(StaticFurry.fursInPile.Last());
                }
                else
                {
                    await RespondAsync(InteractionCallback.Message(new() { Content = "You manage to wiggle out of the pile" }));
                    StaticFurry.fursInPile.Remove(Context.User.Id);
                }
            }
            else
            {
                await RespondAsync(InteractionCallback.Message(new() { Content = "You are the last person, you get up and leave" }));
                StaticFurry.fursInPile.RemoveAt(0);
                StaticFurry.furpileStarted = false;
            }
            StaticFurry.furpileCount -= 1;
            await Context.Channel.SendMessageAsync($"There are {StaticFurry.furpileCount} furs in the pile.");
        }

        string emote = "<a:furdancing:947611436551667813> ";
        [SubSlashCommand("conga", "Join or make a conga line!")]
        public async Task Conga([SlashCommandParameter(Name = "user", Description = "A person to bring with")] User? member = null)
        {
            ulong authorID = Context.User.Id;

            // if it's not started yet, and they don't bring anyone with
            if (member == null && !StaticFurry.congaStarted)
            { await RespondAsync(InteractionCallback.Message(new() { Content = "You have to start it with someone!" })); }
            // if it is started, the user is already on the conga, and they aren't the bot creator
            else if (StaticFurry.congaStarted && StaticFurry.fursInConga.Contains(authorID) && Program.authorID != authorID)
            { await RespondAsync(InteractionCallback.Message(new() { Content = "You are already in the conga!" })); }
            // if it is started, they mention a user, they are already in the conga, they are the bot creator, and the mentioned user isn't in the conga
            else if (StaticFurry.congaStarted && member != null && StaticFurry.fursInConga.Contains(authorID) && Program.authorID == authorID && !StaticFurry.fursInConga.Contains(member.Id))
            { StaticFurry.congaCount++; await RespondAsync(InteractionCallback.Message(new() { Content = $"You leave and bring <@{member.Id}> to the conga! \nThere are {StaticFurry.congaCount} furs in the conga. " + string.Concat(Enumerable.Repeat(emote, StaticFurry.congaCount)) })); StaticFurry.fursInConga.Add(member.Id); }
            // if it is started, they mention a user, they are already in the conga, they are the bot creator, and the mention user is in the conga
            else if (StaticFurry.congaStarted && member != null && StaticFurry.fursInConga.Contains(authorID) && Program.authorID == authorID && StaticFurry.fursInConga.Contains(member.Id))
            { await RespondAsync(InteractionCallback.Message(new() { Content = $"<@{member.Id}> is in the conga" })); }
            // if it is started, they mention a user, the mentioned user is in the conga, and they are not in the conga
            else if (StaticFurry.congaStarted && member != null && StaticFurry.fursInConga.Contains(member.Id) && !StaticFurry.fursInConga.Contains(authorID))
            { StaticFurry.congaCount++; await RespondAsync(InteractionCallback.Message(new() { Content = $"<@{member.Id}> is already in the conga, but {authorID} joins the conga! \nThere are {StaticFurry.congaCount} furs in the conga. " + string.Concat(Enumerable.Repeat(emote, StaticFurry.congaCount)) })); StaticFurry.fursInConga.Add(authorID); }
            // if they didn't bring anyone with
            else if (member == null)
            { StaticFurry.congaCount++; await RespondAsync(InteractionCallback.Message(new() { Content = $"{authorID} has joined the conga! \nThere are {StaticFurry.congaCount} furs in the conga. " + string.Concat(Enumerable.Repeat(emote, StaticFurry.congaCount)) })); StaticFurry.fursInConga.Add(authorID); }
            // if they brought someone with, and it isn't started
            else if (member != null && !StaticFurry.congaStarted)
            {
                StaticFurry.congaCount = 2;
                await RespondAsync(InteractionCallback.Message(new() { Content = $"{authorID} started a conga with <@{member.Id}>! \nThere are {StaticFurry.congaCount} furs in the conga. " + string.Concat(Enumerable.Repeat(emote, StaticFurry.congaCount)) }));
                StaticFurry.congaStarted = true;
                StaticFurry.fursInConga.Add(authorID);
                StaticFurry.fursInConga.Add(member.Id);
            }
            // if they brought someone with, and it is started
            else if (member != null && StaticFurry.congaStarted)
            {
                StaticFurry.congaCount += 2;
                await RespondAsync(InteractionCallback.Message(new() { Content = $"{authorID} joined, bringing <@{member.Id}> with them! \nThere are {StaticFurry.congaCount} furs in the conga. " + string.Concat(Enumerable.Repeat(emote, StaticFurry.congaCount)) }));
                StaticFurry.fursInConga.Add(authorID);
                StaticFurry.fursInConga.Add(member.Id);
            }
        }

        [SubSlashCommand("leave-conga", "Leave the conga line :(")]
        public async Task LeaveConga()
        {
            if (!StaticFurry.fursInConga.Contains(Context.User.Id)) { await RespondAsync(InteractionCallback.Message(new() { Content = "You are not in the conga" })); return; }
            if (StaticFurry.congaCount > 1)
            {
                if (StaticFurry.fursInConga.Last() == Context.User.Id)
                {
                    await RespondAsync(InteractionCallback.Message(new() { Content = "You leave the end of the conga" }));
                    StaticFurry.fursInConga.Remove(StaticFurry.fursInConga.Last());
                }
                else
                {
                    await RespondAsync(InteractionCallback.Message(new() { Content = "You leave the conga, the person behind you fills the gap" }));
                    StaticFurry.fursInConga.Remove(Context.User.Id);
                }
            }
            else
            {
                await RespondAsync(InteractionCallback.Message(new() { Content = "You are the last person, you walk away" }));
                StaticFurry.fursInConga.RemoveAt(0);
                StaticFurry.congaStarted = false;
            }
            StaticFurry.congaCount -= 1;
            await Context.Channel.SendMessageAsync($"There are {StaticFurry.congaCount} furs in the conga. " + string.Concat(Enumerable.Repeat(emote, StaticFurry.congaCount)));
        }
    }

    [SlashCommand("furry-nsfw", "Horny furry commands ;3", DMPermission = true, Nsfw = true)]
    public class FurryNSFW : ApplicationCommandModule<SlashCommandContext>
    {
        [SubSlashCommand("yiff", "Searches e621.net based off a search term")]
        public async Task Yiff([SlashCommandParameter(Name = "search", Description = "A space separated list of search terms, use -term to exlude posts")] string search = "gay")
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = "Searching..." }));

            string keywords = string.Join("+", search.Split(' '));

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.77 Safari/537.36");
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://e621.net/posts.json?tags={keywords}+-inflation+-tentacles+-feral+-bdsm+-bondage+-vore+-human+-watersports+-scat+-gore+-young+-loli+-shota+-urine+-peeing+-rating:s+order:score+-type:webm&limit=50");
            var r = await client.SendAsync(request);
            if (r.StatusCode == System.Net.HttpStatusCode.OK) 
            {
                string content = await r.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<dynamic>(content)!;
                if (data.posts.Count == 0)
                {
                    await ModifyResponseAsync((props) => { props.Content = "No results!"; });
                    return;
                }
                var post = data.posts[new Random().Next(0, data.posts.Count -1)];
                var file = post.file;

                string url = file.url ?? post.sources[post.sources.Count - 1];
                EmbedImageProperties image = new(url);
                EmbedProperties embed = new() { Title = $"e621: {search}, id: {post.id}", Image = image };
                embed.Title = $"e621: {search}, id: {post.id}";
                await ModifyResponseAsync((props) => { props.Embeds = [embed]; props.Content = ""; });
            }
            else
            {
                await ModifyResponseAsync((props) => { props.Content = $"Problem status code: {r.StatusCode}"; });
            }
            client.Dispose();
        }

        [SubSlashCommand("post", "Get a post with the given ID")]
        public async Task Post([SlashCommandParameter(Name = "id", Description = "The post ID to get")] ulong postid)
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = "Searching..." }));

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.77 Safari/537.36");
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://e621.net/posts.json?tags=id:{postid}");
            var r = await client.SendAsync(request);
            if (r.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string content = await r.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<dynamic>(content)!;
                if (data.posts.Count == 0)
                {
                    await ModifyResponseAsync((props) => { props.Content = "No results!"; });
                    return;
                }
                var post = data.posts[0];
                var file = post.file; 
                EmbedProperties embed = new() { Title = $"e621: post {postid}" };
                if (file.ext == "webm")
                {
                    string mp4 = post.sample.alternates.original.urls[1];
                    await ModifyResponseAsync((props) => { props.Embeds = [embed]; });
                    await Context.Channel.SendMessageAsync(mp4);
                }
                else
                {

                    string url = file.url ?? post.sources[post.sources.Count - 1];
                    EmbedImageProperties image = new(url);
                    embed.Image = image;
                    await ModifyResponseAsync((props) => { props.Embeds = [embed]; });
                }
            }
            else
            {
                await ModifyResponseAsync((props) => { props.Content = $"Problem status code: {r.StatusCode}"; });
            }
            client.Dispose();
        }

        [SubSlashCommand("e6anim", "Searches e621.net for videos based off a search term")]
        public async Task E6Anim([SlashCommandParameter(Name = "search", Description = "A space separated list of search terms, use -term to exlude posts")] string search = "gay")
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = "Searching..." }));

            string keywords = string.Join("+", search.Split(' '));

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.77 Safari/537.36");
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://e621.net/posts.json?tags={keywords}+-inflation+-tentacles+-feral+-bdsm+-bondage+-vore+-human+-watersports+-scat+-gore+-young+-loli+-shota+-urine+-peeing+-rating:s+order:score+type:webm&limit=50");
            var r = await client.SendAsync(request);
            if (r.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string content = await r.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<dynamic>(content)!;
                if (data.posts.Count == 0)
                {
                    await ModifyResponseAsync((props) => { props.Content = "No results!"; });
                    return;
                }
                var post = data.posts[new Random().Next(0, data.posts.Count - 1)];
                string file = post.sample.alternates.original.urls[1];

                EmbedProperties embed = new() { Title = $"e621: {search}, id: {post.id}" };
                await ModifyResponseAsync((props) => { props.Embeds = [embed]; });
                await Context.Channel.SendMessageAsync(file);
            }
            else
            {
                await ModifyResponseAsync((props) => { props.Content = $"Problem status code: {r.StatusCode}"; });
            }
            client.Dispose();
        }

        [SubSlashCommand("rand-yiff", "Searches e621.net based off a search term, sorts randomly")]
        public async Task RandYiff([SlashCommandParameter(Name = "search", Description = "A space separated list of search terms, use -term to exlude posts")] string search = "gay")
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = "Searching..." }));

            string keywords = string.Join("+", search.Split(' '));

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.77 Safari/537.36");
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://e621.net/posts.json?tags={keywords}+-inflation+-tentacles+-feral+-bdsm+-bondage+-vore+-human+-watersports+-scat+-gore+-young+-loli+-shota+-urine+-peeing+-rating:s+order:random+-type:webm&limit=50");
            var r = await client.SendAsync(request);
            if (r.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string content = await r.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<dynamic>(content)!;
                if (data.posts.Count == 0)
                {
                    await ModifyResponseAsync((props) => { props.Content = "No results!"; });
                    return;
                }
                var post = data.posts[new Random().Next(0, data.posts.Count - 1)];
                var file = post.file;

                string url = file.url ?? post.sources[post.sources.Count - 1];
                EmbedImageProperties image = new(url);
                EmbedProperties embed = new() { Title = $"e621: {search}, id: {post.id}", Image = image };
                await ModifyResponseAsync((props) => { props.Embeds = [embed]; });
            }
            else
            {
                await ModifyResponseAsync((props) => { props.Content = $"Problem status code: {r.StatusCode}"; });
            }
            client.Dispose();
        }

        [SubSlashCommand("rand-e6anim", "Searches e621.net for videos based off a search term, sorts randomly")]
        public async Task RandE6Anim([SlashCommandParameter(Name = "search", Description = "A space separated list of search terms, use -term to exlude posts")] string search = "gay")
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = "Searching..." }));

            string keywords = string.Join("+", search.Split(' '));

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.77 Safari/537.36");
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://e621.net/posts.json?tags={keywords}+-inflation+-tentacles+-feral+-bdsm+-bondage+-vore+-human+-watersports+-scat+-gore+-young+-loli+-shota+-urine+-peeing+-rating:s+order:random+type:webm&limit=50");
            var r = await client.SendAsync(request);
            if (r.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string content = await r.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<dynamic>(content)!;
                if (data.posts.Count == 0)
                {
                    await ModifyResponseAsync((props) => { props.Content = "No results!"; });
                    return;
                }
                var post = data.posts[new Random().Next(0, data.posts.Count - 1)];
                string file = post.sample.alternates.original.urls[1];

                EmbedProperties embed = new() { Title = $"e621: {search}, id: {post.id}" };
                await ModifyResponseAsync((props) => { props.Embeds = [embed]; });
                await Context.Channel.SendMessageAsync(file);
            }
            else
            {
                await ModifyResponseAsync((props) => { props.Content = $"Problem status code: {r.StatusCode}"; });
            }
            client.Dispose();
        }

        [SubSlashCommand("uwu", "UwU copypasta lol")]
        public async Task UwU() // lol, it's just this entire thing
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = "Rawr x3 nuzzles how are you pounces on you you're so warm o3o notices you have a bulge o: someone's happy (; nuzzles your necky wecky~ murr~ hehehe rubbies your bulgy wolgy you're so big :oooo rubbies more on your bulgy wolgy it doesn't stop growing ·///· kisses you and lickies your necky daddy likies (; nuzzles wuzzles I hope daddy really likes $: wiggles butt and squirms I want to see your big daddy meat~ wiggles butt I have a little itch o3o wags tail can you please get my itch~ puts paws on your chest nyea~ its a seven inch itch rubs your chest can you help me pwease squirms pwetty pwease sad face I need to be punished runs paws down your chest and bites lip like I need to be punished really good~ paws on your bulge as I lick my lips I'm getting thirsty. I can go for some milk unbuttons your pants as my eyes glow you smell so musky :v licks shaft mmmm~ so musky drools all over your cock your daddy meat I like fondles Mr. Fuzzy Balls hehe puts snout on balls and inhales deeply oh god im so hard~ licks balls punish me daddy~ nyea~ squirms more and wiggles butt I love your musky goodness bites lip please punish me licks lips nyea~ suckles on your tip so good licks pre of your cock salty goodness~ eyes role back and goes balls deep mmmm~ moans and suckles" }));
        }
    }

    [SlashCommand("furry-nsfw-ex", "Extra horny furry commands~", DMPermission = true, Nsfw = true)]
    public class FurryNSFWEX : ApplicationCommandModule<SlashCommandContext>
    {
        [SubSlashCommand("yiff", "Searches e621.net based off a search term, with less restrictions")]
        public async Task Yiff([SlashCommandParameter(Name = "search", Description = "A space separated list of search terms, use -term to exlude posts")] string search = "gay")
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = "Searching..." }));

            string keywords = string.Join("+", (search??"gay").Split(' '));

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.77 Safari/537.36");
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://e621.net/posts.json?tags={keywords}+-human+-watersports+-scat+-gore+-young+-loli+-shota+-urine+-peeing+-rating:s+order:score+-type:webm&limit=50");
            var r = await client.SendAsync(request);
            if (r.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string content = await r.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<dynamic>(content)!;
                if (data.posts.Count == 0)
                {
                    await ModifyResponseAsync((props) => { props.Content = "No results!"; });
                    return;
                }
                var post = data.posts[new Random().Next(0, data.posts.Count - 1)];
                var file = post.file;

                string url = file.url ?? post.sources[post.sources.Count - 1];
                EmbedImageProperties image = new(url);
                EmbedProperties embed = new() { Title = $"e621: {search}, id: {post.id}", Image = image };
                await ModifyResponseAsync((props) => { props.Embeds = [embed]; });
            }
            else
            {
                await ModifyResponseAsync((props) => { props.Content = $"Problem status code: {r.StatusCode}"; });
            }
            client.Dispose();
        }

        [SubSlashCommand("e6anim", "Searches e621.net for videos based off a search term, with less restrictions")]
        public async Task E6Anim([SlashCommandParameter(Name = "search", Description = "A space separated list of search terms, use -term to exlude posts")] string search = "gay")
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = "Searching..." }));

            string keywords = string.Join("+", search.Split(' '));

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.77 Safari/537.36");
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://e621.net/posts.json?tags={keywords}+-human+-watersports+-scat+-gore+-young+-loli+-shota+-urine+-peeing+-rating:s+order:score+type:webm&limit=50");
            var r = await client.SendAsync(request);
            if (r.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string content = await r.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<dynamic>(content)!;
                if (data.posts.Count == 0)
                {
                    await ModifyResponseAsync((props) => { props.Content = "No results!"; });
                    return;
                }
                var post = data.posts[new Random().Next(0, data.posts.Count - 1)];
                string file = post.sample.alternates.original.urls[1];

                EmbedProperties embed = new() { Title = $"e621: {search}, id: {post.id}" };
                await ModifyResponseAsync((props) => { props.Embeds = [embed]; });
                await Context.Channel.SendMessageAsync(file);
            }
            else
            {
                await ModifyResponseAsync((props) => { props.Content = $"Problem status code: {r.StatusCode}"; });
            }
            client.Dispose();
        }

        [SubSlashCommand("rand-yiff", "Searches e621.net based off a search term, with less restrictions, sorts randomly")]
        public async Task RandYiff([SlashCommandParameter(Name = "search", Description = "A space separated list of search terms, use -term to exlude posts")] string search = "gay")
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = "Searching..." }));

            string keywords = string.Join("+", search.Split(' '));

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.77 Safari/537.36");
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://e621.net/posts.json?tags={keywords}+-human+-watersports+-scat+-gore+-young+-loli+-shota+-urine+-peeing+-rating:s+order:random+-type:webm&limit=50");
            var r = await client.SendAsync(request);
            if (r.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string content = await r.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<dynamic>(content)!;
                if (data.posts.Count == 0)
                {
                    await ModifyResponseAsync((props) => { props.Content = "No results!"; });
                    return;
                }
                var post = data.posts[new Random().Next(0, data.posts.Count - 1)];
                var file = post.file;

                string url = file.url ?? post.sources[post.sources.Count - 1];
                EmbedImageProperties image = new(url);
                EmbedProperties embed = new() { Title = $"e621: {search}, id: {post.id}", Image = image };
                await ModifyResponseAsync((props) => { props.Embeds = [embed]; });
            }
            else
            {
                await ModifyResponseAsync((props) => { props.Content = $"Problem status code: {r.StatusCode}"; });
            }
            client.Dispose();
        }

        [SubSlashCommand("rand-e6anim", "Searches e621.net for videos based off a search term, with less restrictions, sorts randomly")]
        public async Task RandE6Anim([SlashCommandParameter(Name = "search", Description = "A space separated list of search terms, use -term to exlude posts")] string search = "gay")
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = "Searching..." }));

            string keywords = string.Join("+", search.Split(' '));

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.77 Safari/537.36");
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://e621.net/posts.json?tags={keywords}+-human+-watersports+-scat+-gore+-young+-loli+-shota+-urine+-peeing+-rating:s+order:random+type:webm&limit=50");
            var r = await client.SendAsync(request);
            if (r.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string content = await r.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<dynamic>(content);
                if (data.posts.Count == 0)
                {
                    await ModifyResponseAsync((props) => { props.Content = "No results!"; });
                    return;
                }
                var post = data.posts[new Random().Next(0, data.posts.Count - 1)];
                string file = post.sample.alternates.original.urls[1];

                EmbedProperties embed = new() { Title = $"e621: {search}, id: {post.id}" };
                await ModifyResponseAsync((props) => { props.Embeds = [embed]; });
                await Context.Channel.SendMessageAsync(file);
            }
            else
            {
                await ModifyResponseAsync((props) => { props.Content = $"Problem status code: {r.StatusCode}"; });
            }
            client.Dispose();
        }
    }
    
}
