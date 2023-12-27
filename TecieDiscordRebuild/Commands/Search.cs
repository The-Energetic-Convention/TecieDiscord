using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.API.Search;
using Google.Apis;
using Google.Apis.CustomSearchAPI;
using Google.Apis.CustomSearchAPI.v1;
using Google.Apis.CustomSearchAPI.v1.Data;
using System.Net.Http;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;

namespace TecieDiscordRebuild.Commands
{
    [SlashCommand("search", "Search commands", DMPermission = true)]
    internal class Search : ApplicationCommandModule<SlashCommandContext>
    {
        static string apiKey = Environment.GetEnvironmentVariable("GOOGLE_KEY")??"NULL";
        static string imageAPI = "b9eafac2401363f46";
        static string searchAPI = "60e05025fccdc45a0";

        [SubSlashCommand("image", "Used to get an image from google, based off a search term")]
        public async Task Image([SlashCommandParameter(Name = "search", Description = "The search term to use")] string searchTerms)
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = "Searching..." }));

            HttpClient googleImages = new HttpClient();
            //This regex searches for image urls in the html from google
            Regex googleRegex = new Regex(@"src=""https://[^""]*""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            //get google html for image search
            string html = await googleImages.GetStringAsync($"https://www.google.com/search?tbm=isch&q={searchTerms}");
            MatchCollection googleMatches = googleRegex.Matches(html);

            string image = googleMatches[new Random().Next(googleMatches.Count-1)].Value.Replace("src=\"", "").Replace("\"", "");

            EmbedProperties embed = new() { Title = $"Google Images: {searchTerms}", Image = new(image) };

            await ModifyResponseAsync((props) => { props.Embeds = [embed]; });
        }

        [SubSlashCommand("search", "Used to search using a search term")]
        public async Task Search_([SlashCommandParameter(Name = "search", Description = "The search term to use")] string searchTerms)
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = "Searching..." }));

            var customSearchService = new CustomSearchAPIService(new Google.Apis.Services.BaseClientService.Initializer { ApiKey = apiKey });
            var listRequest = customSearchService.Cse.List();
            listRequest.Cx = searchAPI;
            listRequest.Q = searchTerms;

            List<Result>? results = [];
            int count = 0;
            while (results != null)
            {
                listRequest.Start = count;
                listRequest.Execute().Items?.ToList().ForEach(x => results.Add(x));

                count++;
                if (count > 10)
                {
                    results = null;
                    break;
                }

                List<EmbedFieldProperties> fields = [];
                foreach (Result result in results)
                {
                    fields.Add(new() { Name = $"{result.Title} | {result.Link}", Value = $"{result.Snippet}" });
                }
                EmbedProperties embed = new() { Title = "Results: ", Fields = fields };

                await ModifyResponseAsync((props) => { props.Embeds = [embed]; });
            }
        }

        [SubSlashCommand("dog", "Gets a random dog")]
        public async Task Dog()
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = "Searching..." }));

            try
            {
                HttpClient client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://random.dog/woof.json");
                var r = await client.SendAsync(request);
                if (r.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string content = await r.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<dynamic>(content)!;

                    string url = data.url;
                    EmbedImageProperties image = new(url);
                    EmbedProperties embed = new() { Title = "Doggo", Image = image };

                    await ModifyResponseAsync((props) => { props.Embeds = [embed]; });
                }
            } 
            catch (Exception ex) 
            {
                await ModifyResponseAsync((props) => { props.Content = ex.Message; });
            }
        }
    }
}
