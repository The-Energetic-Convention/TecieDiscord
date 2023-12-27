using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeDLSharp;
using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using NetCord.Gateway.Voice;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Collections;
using YoutubeDLSharp.Options;

namespace TecieDiscordRebuild.Commands
{
    internal static class StaticMusic
    {
        public static VoiceClient? voice = null;
        public static ulong? vc = null;

        public static List<string> queue = [];
        public static List<ulong> queueRequests = [];

        public static TextChannel? channel = null;
    }

    [SlashCommand("music", "Music commands", DMPermission = false)]
    internal class Music : ApplicationCommandModule<SlashCommandContext>
    {
        [SubSlashCommand("join", "Make the bot join you in a vc")]
        public async Task Join()
        {
            try
            {
                await RespondAsync(InteractionCallback.Message(new() { Content = "Connecting..." }));

                if (StaticMusic.voice != null)
                {
                    await ModifyResponseAsync((props) => { props.Content = "Already Connected!"; });
                    return;
                }

                var guild = Context.Guild!;

                // Get the user voice state
                if (!guild.VoiceStates.TryGetValue(Context.User.Id, out var voiceState))
                    throw new("You are not connected to any voice channel!");

                var client = Context.Client;

                StaticMusic.vc = voiceState.ChannelId;
                StaticMusic.voice = await client.JoinVoiceChannelAsync(
                    client.ApplicationId,
                    guild.Id,
                    StaticMusic.vc.GetValueOrDefault());

                // Connect
                await StaticMusic.voice.StartAsync();
                await ModifyResponseAsync((props) => { props.Content = $"Connected to <#{StaticMusic.vc}>!"; });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        [SubSlashCommand("leave", "Make the bot leave the vc")]
        public async Task Leave()
        {
            if (StaticMusic.voice == null) { await RespondAsync(InteractionCallback.Message(new() { Content = $"Not connected to a vc!" })); return; }
            ulong guild = StaticMusic.voice.GuildId;
            await StaticMusic.voice.CloseAsync();
            StaticMusic.voice.Dispose();
            StaticMusic.queue.Clear();
            StaticMusic.queueRequests.Clear();
            await Program.client.UpdateVoiceStateAsync(new NetCord.Gateway.VoiceStateProperties(guild, null));
            await RespondAsync(InteractionCallback.Message(new() { Content = $"Disconnected from <#{StaticMusic.vc}>" }));
            StaticMusic.voice = null;
        }

        [SubSlashCommand("add", "Add a list of songs to the queue")]
        public async Task Add([SlashCommandParameter(Name = "songs", Description = "A space separated list of youtube links to play")] string songs)
        {
            Console.WriteLine(songs);
            string[] urls = songs.Split(' ');
            Console.WriteLine(JsonConvert.SerializeObject(urls));
            await RespondAsync(InteractionCallback.Message(new() { Content = "Adding songs", Flags = MessageFlags.Ephemeral }));

            foreach (string url in urls)
            {
                Console.WriteLine(url);
                StaticMusic.queue.Add(url);
                StaticMusic.queueRequests.Add(Context.User.Id);
                Console.WriteLine(JsonConvert.SerializeObject(StaticMusic.queue));
                await Context.Channel.SendMessageAsync($"<{url}> added to queue! Requested by <@{Context.User.Id}>");
            }
        }

        static bool paused = false;

        void PlayError(string? message = null)
        {
            if (message == null) { Console.WriteLine("Playing fine..."); }
            else { Console.WriteLine($"AH SHIT {message}"); }
        }

        string outputFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\TecieDiscord\\ytdl_downloads\\";
        [SubSlashCommand("play", "Start playing the current queue")]
        public async Task Play()
        {
            if (StaticMusic.voice == null) { await RespondAsync(InteractionCallback.Message(new() { Content = "I need to be in a vc to play!" })); return; }

            await RespondAsync(InteractionCallback.Message(new() { Content = "Playing queue!" }));
            StaticMusic.channel = Context.Channel;

            var ytdl = new YoutubeDL();
            ytdl.YoutubeDLPath = Environment.CurrentDirectory + "\\yt-dlp.exe";
            ytdl.FFmpegPath = Environment.CurrentDirectory + "\\ffmpeg.exe";
            ytdl.OutputFolder = outputFolder;

            try
            {
                if (StaticMusic.queue.Count == 0) { await RespondAsync(InteractionCallback.Message(new() { Content = "Nothing in queue!" })); return; }

                while (StaticMusic.queue.Count > 0)
                {
                    while (paused) { }
                    IProgress<string> progress = new Progress<string>(Console.WriteLine);
                    Console.WriteLine($"Downloading {StaticMusic.queue[0]}");
                    var res = await ytdl.RunAudioDownload(StaticMusic.queue[0], AudioConversionFormat.Mp3, output:progress, overrideOptions: new OptionSet() { AudioQuality = 0 });
                    string path = res.Data;

                    await StaticMusic.voice.ReadyAsync;
                    await StaticMusic.voice.EnterSpeakingStateAsync(SpeakingFlags.Microphone);
                    Regex titleRegex = new(@"(?<=C:\\Users\\awsom\\AppData\\Roaming\\TecieDiscord\\ytdl_downloads\\)(.*?)(?=\s*\[.*\].mp3)", RegexOptions.Compiled); // A regex to extract the video title
                    string title = titleRegex.Match(path, 0).Value;
                    Console.WriteLine(title);
                    //int color = ((int)StaticMusic.queueRequests[0]) & 16777215;
                    int color = new Random().Next(16777215); // 16777215 is the maximum value for color, as it's 3 8bit chunks
                    EmbedProperties embed = new() { Title = "Now Playing", Description = $"[{title}]({StaticMusic.queue[0]}) \nRequested by <@{StaticMusic.queueRequests[0]}>", Color = new(color) };
                    await StaticMusic.channel.SendMessageAsync(new() { Embeds = [embed] });

                    var outStream = StaticMusic.voice.CreateOutputStream();
                    OpusEncodeStream stream = new(outStream, PcmFormat.Short, VoiceChannels.Stereo, OpusApplication.Audio);

                    var ffmpeg = CreateStream(path);

                    await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream);
                    await stream.FlushAsync();

                    File.Delete(path);
                    StaticMusic.queue.RemoveAt(0);
                    StaticMusic.queueRequests.RemoveAt(0);
                }

                await StaticMusic.channel.SendMessageAsync("Finished playing queue!");
                StaticMusic.channel = null;
            }
            catch (Exception e)
            {
                PlayError(e.Message);
            }
        }
        private Process CreateStream(string path)
        {
            
            Console.WriteLine($"Creating stream with path {path}");
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -i \"{path}\" -loglevel panic -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            })!;
        }

        [SubSlashCommand("queue", "Show the current queue")]
        public async Task Queue()
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = "Getting queue...", Flags = MessageFlags.Ephemeral }));

            Console.WriteLine(JsonConvert.SerializeObject(StaticMusic.queue));
            if (StaticMusic.queue.Count < 26)
            {
                Console.WriteLine("Count less than 25");

                List<EmbedFieldProperties> fields = [];
                int i = 0;
                foreach (string url in StaticMusic.queue)
                {
                    fields.Add(new EmbedFieldProperties() { Name = url, Value = $"Requested by: <@{StaticMusic.queueRequests[i]}>" });
                    i++;
                }
                EmbedProperties embed = new() { Title = "Current Queue:", Fields = fields };

                await ModifyResponseAsync((props) => { props.Content = ""; props.Embeds = [embed]; });
            }
            else
            {
                Console.WriteLine("Count more than 25");
                int chunks = (int)Math.Ceiling((decimal)StaticMusic.queue.Count / 25);
                EmbedProperties[] embeds = new EmbedProperties[chunks];
                Console.WriteLine($"Making {chunks} embeds");
                for (int i = 0; i < embeds.Length; i++)
                {
                    EmbedProperties embed = new() { Title = i == 0 ? "Events:" : "Events cont:" };
                    Console.WriteLine($"Chunk {i}");

                    int count = StaticMusic.queue.Count - (25 * i) > 25 ? 25 : StaticMusic.queue.Count - (25 * i);
                    Console.WriteLine($"Count {count}");
                    List<EmbedFieldProperties> fields = [];
                    for (int j = 0; j < count; j++)
                    {
                        int index = j + (25 * i);
                        Console.WriteLine($"Event {index}");
                        fields.Add(new EmbedFieldProperties() { Name = StaticMusic.queue[index], Value = $"Requested by: {StaticMusic.queueRequests[index]}" });
                    }
                    embeds[i] = embed;
                }

                Console.WriteLine("Building embeds");

                await ModifyResponseAsync((props) => { props.Content = ""; props.Embeds = embeds; });
            }
        }
    }
}
