using System;
using System.Runtime.InteropServices;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using System.Diagnostics;
using Newtonsoft.Json;
using System.IO.Pipes;
using System.Text;
using TecieDiscordRebuild.Commands;

namespace TecieDiscordRebuild
{
    internal class Program
    {
        public static RestGuild mainGuild;
        public static GatewayClient client = new(new Token(TokenType.Bot, Environment.GetEnvironmentVariable("TEC_DISCORD_TOKEN") ?? ""), new GatewayClientConfiguration() { Intents = GatewayIntents.All });
        public static string authKey = Environment.GetEnvironmentVariable("TECKEY") ?? "NULL";
        public static NamedPipeClientStream pipeClient;
        public static ulong authorID = 448846699692032006;

        ApplicationCommandService<SlashCommandContext> applicationCommandService = new();

        static ConsoleEventDelegate handler = new ConsoleEventDelegate(HandleExitTasks);
        private delegate bool ConsoleEventDelegate(int eventType);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

        public static Task Main(string[] args) => new Program().MainAsync();
        public async Task MainAsync()
        {
            client.Log += Log;
            client.Ready += ClientReady;
            client.GuildUserAdd += UserJoined;

            applicationCommandService.AddModules(System.Reflection.Assembly.GetEntryAssembly()!);

            SetConsoleCtrlHandler(handler, true);
            BotSettings.Load();

            await client.StartAsync();
            await client.ReadyAsync;

            await applicationCommandService.CreateCommandsAsync(client.Rest, client.ApplicationId);

            client.InteractionCreate += async interaction =>
            {
                if (interaction is SlashCommandInteraction slashCommandInteraction)
                {
                    try
                    {
                        await applicationCommandService.ExecuteAsync(new SlashCommandContext(slashCommandInteraction, client));
                    }
                    catch (Exception ex)
                    {
                        try { await interaction.SendResponseAsync(InteractionCallback.Message(new() { Content = $"Error: {ex.Message}", Flags = MessageFlags.Ephemeral })); }
                        catch { }
                    }
                }
            };

            await Task.Delay(-1);
        }

        static bool HandleExitTasks(int eventType)
        {
            if (eventType == 2)
            {
                Task.WaitAll([Cleanup()]);
                Task.WaitAll([client.CloseAsync()]);
            }
            return false;
        }
        static async Task Cleanup() //trying to use this so there isn't a role laying around
        {
            if (AdminEvents.pingRole != null)
            {
                await AdminEvents.pingRole.DeleteAsync();
            }
        }

        private static ValueTask Log(LogMessage message)
        {
            Console.WriteLine(message);
            return ValueTask.CompletedTask;
        }

        public async ValueTask ClientReady(ReadyEventArgs args)
        {
            mainGuild = await client.Rest.GetGuildAsync(765712424946761729);
            Console.WriteLine("Started!");
        }

        private async ValueTask UserJoined(GuildUser user)
        {
            Console.WriteLine("User joined");
            var dm = await user.GetDMChannelAsync();
            Console.WriteLine("DM channel");
            await dm.SendMessageAsync("Welcome to TEC! I'm Tecie, the convention bot! You can use my / commands for a lot of things! Hope you have fun here!");
            await dm.SendMessageAsync("You can head over to <#1078858956027478066> to read and accept them. Then you'll have access to the rest of the server!");
            Console.WriteLine("Messages sent");
        }

        public static StreamString ConnectClient()
        {
            try
            {
                pipeClient = new NamedPipeClientStream(".", "TECDatabasePipe", PipeDirection.InOut, PipeOptions.None);
                pipeClient.Connect();
                var ss = new StreamString(pipeClient);
                Console.WriteLine("Authorizing");
                ss.WriteString(authKey);
                if (ss.ReadString() != authKey) { ss.WriteString("Unauthorized server!"); throw new Exception("Unauthorized server connection attemted!"); }

                return ss;
            }
            catch (Exception e) { Console.WriteLine(e.Message); return null; }
        }
        
        public static StreamString ConnectBlueskyClient()
        {
            try
            {
                pipeClient = new NamedPipeClientStream(".", "TecieBlueskyPipe", PipeDirection.InOut, PipeOptions.None);
                pipeClient.Connect();
                var ss = new StreamString(pipeClient);
                Console.WriteLine("Authorizing");
                ss.WriteString(authKey);
                if (ss.ReadString() != authKey) { ss.WriteString("Unauthorized server!"); throw new Exception("Unauthorized server connection attemted!"); }

                return ss;
            }
            catch (Exception e) { Console.WriteLine(e.Message); return null; }
        }

        public static void CheckResponse(StreamString ss)
        {
            if (ss.ReadString() != "READY") { throw new Exception("Server Error"); }
        }
    }

    public class StreamString
    {
        private Stream ioStream;
        private UnicodeEncoding streamEncoding;

        public StreamString(Stream ioStream)
        {
            this.ioStream = ioStream;
            streamEncoding = new UnicodeEncoding();
        }

        public string ReadString()
        {
            int i = 0;
        tryread:
            int len;
            len = ioStream.ReadByte() * 256;
            len += ioStream.ReadByte();
            Thread.Sleep(100);
            if (len < 0 && i < 10) { i++; goto tryread; }
            var inBuffer = new byte[len];
            ioStream.Read(inBuffer, 0, len);

            return streamEncoding.GetString(inBuffer);
        }

        public int WriteString(string outString)
        {
            byte[] outBuffer = streamEncoding.GetBytes(outString);
            int len = outBuffer.Length;
            if (len > UInt16.MaxValue)
            {
                len = (int)UInt16.MaxValue;
            }
            ioStream.WriteByte((byte)(len / 256));
            ioStream.WriteByte((byte)(len & 255));
            ioStream.Write(outBuffer, 0, len);
            ioStream.Flush();

            return outBuffer.Length + 2;
        }
    }
}
