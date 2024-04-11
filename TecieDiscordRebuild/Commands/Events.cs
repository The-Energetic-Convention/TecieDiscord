using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TECDataClasses;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using NetCord;
using Microsoft.VisualBasic;
using YoutubeDLSharp.Options;
using System.Security;
using NetCord.Services.Interactions;
using NetCord.Gateway;

namespace TecieDiscordRebuild.Commands
{
    [SlashCommand("events", "Event commands", DMPermission = true)]
    internal class Events : ApplicationCommandModule<SlashCommandContext>
    {
        [SubSlashCommand("list", "Get a list of events")]
        public async Task ListEvents()
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = $"Getting events...", Flags = MessageFlags.Ephemeral }));

            // get events
            var ss = Program.ConnectClient();

            // tell the server we are reading
            ss.WriteString("R");
            Program.CheckResponse(ss);

            // tell the server it is an event
            ss.WriteString("E");
            Program.CheckResponse(ss);

            ss.WriteString("ALL");
            EventInfo[] events = JsonConvert.DeserializeObject<EventInfo[]>(ss.ReadString())!;
            if (ss.ReadString() != "SUCCESS") { throw new Exception("Server Error"); }
            Program.pipeClient.Close();

            // create a dict of event number:event name
            Dictionary<int, (string date, string name)> eventDict = new Dictionary<int, (string date, string name)>();
            foreach (EventInfo @event in events)
            {
                eventDict.Add(@event.EventNumber, (@event.EventDate.ToString("MMM dd, hh:mm tt"), @event.EventName));
            }

            if (eventDict.Count < 26)
            {
                Console.WriteLine("Count less than 25");
                // create an embed for the list
                EmbedProperties embed = new() { Title = "Events:" };

                List<EmbedFieldProperties> fields = [];
                foreach (int i in eventDict.Keys)
                {
                    fields.Add(new EmbedFieldProperties() { Name = $"Event Number: {i}", Value = $"{eventDict[i].date}   {eventDict[i].name}" });
                }

                await ModifyResponseAsync((props) => { props.Content = "Here is a list of events"; props.Embeds = [embed]; });
            }
            else
            {
                Console.WriteLine("Count more than 25");
                int chunks = (int)Math.Ceiling((decimal)eventDict.Count/25);
                EmbedProperties[] embeds = new EmbedProperties[chunks];
                Console.WriteLine($"Making {chunks} embeds");
                for (int i = 0; i < embeds.Length; i++)
                {
                    EmbedProperties embed = new() { Title = i == 0 ? "Events:" : "Events cont:" };
                    Console.WriteLine($"Chunk {i}");

                    int count = eventDict.Count - (25 * i) > 25 ? 25 : eventDict.Count - (25 * i);
                    Console.WriteLine($"Count {count}");
                    List<EmbedFieldProperties> fields = [];
                    for (int j = 0; j < count; j++)
                    {
                        int index = j+(25*i);
                        Console.WriteLine($"Event {index}");
                        fields.Add(new EmbedFieldProperties() { Name = $"Event Number: {index}", Value = $"{eventDict[index].date}   {eventDict[index].name}" });
                    }
                    embeds[i] = embed;
                }

                Console.WriteLine("Building embeds");

                await ModifyResponseAsync((props) => { props.Content = "Here is a list of events"; props.Embeds = embeds; });
            }

        }

        [SubSlashCommand("info", "Get info about a specific event")]
        public async Task EventInfo( [SlashCommandParameter(Name = "number", Description = "The event number to get")] int eventID)
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = $"Getting event info...", Flags = MessageFlags.Ephemeral }));

            // get the event
            var ss = Program.ConnectClient();

            // tell the server we are reading
            ss.WriteString("R");
            Program.CheckResponse(ss);

            // tell the server it is an event
            ss.WriteString("E");
            Program.CheckResponse(ss);

            ss.WriteString(eventID.ToString());
            EventInfo @event = JsonConvert.DeserializeObject<EventInfo>(ss.ReadString())!;
            if (ss.ReadString() != "SUCCESS") { throw new Exception("Server Error"); }
            Program.pipeClient.Close();

            @event.EventLink = @event.EventLink == "" ? null : $"[Join Here]({@event.EventLink})";

            // create an embed with all the event info
            EmbedProperties embed = new() { Title = @event.EventName, Description = @event.EventDescription };

            List<EmbedFieldProperties> fields = [];
            fields.Add(new EmbedFieldProperties() { Name = "Event Number"    , Value = @event.EventNumber.ToString() });
            fields.Add(new EmbedFieldProperties() { Name = "Event Type"      , Value = @event.EventType.ToString() });
            fields.Add(new EmbedFieldProperties() { Name = "Quest Compatable", Value = @event.QuestCompatable.ToString() ?? "N/A" });
            fields.Add(new EmbedFieldProperties() { Name = "Event Link"      , Value = @event.EventLink ?? "N/A" });
            fields.Add(new EmbedFieldProperties() { Name = "Event Rating"    , Value = @event.EventRating.ToString() });
            fields.Add(new EmbedFieldProperties() { Name = "Event Date"      , Value = @event.EventDate.ToString("MMM dd, hh:mm tt") });

            await ModifyResponseAsync((props) => { props.Content = $"Here is the info of {@event.EventName}"; props.Embeds = [embed]; });
        }

        [SubSlashCommand("register-for", "Register to be pinged for an event when it starts")]
        public async Task RegisterForEvent([SlashCommandParameter(Name = "number", Description = "The event number to get")] int eventID)
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = $"Registering for event...", Flags = MessageFlags.Ephemeral }));
            bool alreadyRegistered = RegisterForEvent(Context.User.Id, eventID);

            if (alreadyRegistered) { await ModifyResponseAsync((props) => { props.Content = "Already registered for event! You will recieve a ping when the event is starting!"; }); }
            else { await ModifyResponseAsync((props) => { props.Content = "Registered for event! You will recieve a ping when this event is starting!"; }); }
        }

        [SubSlashCommand("register-for-all", "Register to be pinged for all events when they start")]
        public async Task RegisterForAll()
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = $"Registering you for all events...", Flags = MessageFlags.Ephemeral }));
            
            // get events
            var ss = Program.ConnectClient();

            // tell the server we are reading
            ss.WriteString("R");
            Program.CheckResponse(ss);

            // tell the server it is an event
            ss.WriteString("E");
            Program.CheckResponse(ss);

            ss.WriteString("ALL");
            EventInfo[] events = JsonConvert.DeserializeObject<EventInfo[]>(ss.ReadString())!;
            if (ss.ReadString() != "SUCCESS") { throw new Exception("Server Error"); }
            Program.pipeClient.Close();

            for (int i = 0; i < events.Length; i++)
            {
                RegisterForEvent(Context.User.Id, i);
            }

            await ModifyResponseAsync((props) => { props.Content = "Registered for all events! You will recieve a ping when each event is starting!"; });
        }

        bool RegisterForEvent(ulong userID, int eventID)
        {
            // get the event
            var ss = Program.ConnectClient();

            // tell the server we are reading
            ss.WriteString("R");
            Program.CheckResponse(ss);

            // tell the server it is an event
            ss.WriteString("E");
            Program.CheckResponse(ss);

            ss.WriteString(eventID.ToString());
            EventInfo @event = JsonConvert.DeserializeObject<EventInfo>(ss.ReadString())!;
            if (ss.ReadString() != "SUCCESS") { throw new Exception("Server Error"); }
            Program.pipeClient.Close();

            // get the ping list and add them to it by ID
            bool alreadyRegistered;
            List<string> pings = [.. JsonConvert.DeserializeObject<string[]>(@event.UserPings)!];
            if (pings.Contains(JsonConvert.SerializeObject(userID))) { alreadyRegistered = true; }
            else { pings.Add(JsonConvert.SerializeObject(userID)); alreadyRegistered = false; }
            @event.UserPings = JsonConvert.SerializeObject(pings.ToArray());

            // update the event with them in the ping list
            ss = Program.ConnectClient();

            // tell the server we are updating
            ss.WriteString("U");
            Program.CheckResponse(ss);

            // tell the server it is an event
            ss.WriteString("E");
            Program.CheckResponse(ss);

            ss.WriteString(JsonConvert.SerializeObject(@event));
            if (ss.ReadString() != "SUCCESS") { throw new Exception("Server Error"); }

            Program.pipeClient.Close();
            return alreadyRegistered;
        }
    }

    [SlashCommand("admin-events", "Admin events commands", DMPermission = false, DefaultGuildUserPermissions = Permissions.ManageChannels)]
    internal class AdminEvents : ApplicationCommandModule<SlashCommandContext>
    {
        public static Role? pingRole = null;

        [SubSlashCommand("add", "Add an event to the database")]
        public async Task AddEvent(
            [SlashCommandParameter(Name = "number", Description = "The event number to add")] int Eventnumber,
            [SlashCommandParameter(Name = "name", Description = "The event name to add")] string EventName,
            [SlashCommandParameter(Name = "description", Description = "The event description to add")] string EventDescription,
            [SlashCommandParameter(Name = "location", Description = "The event location to add")] string EventLocation,
            [SlashCommandParameter(Name = "type", Description = "The event type to add")] EventType EventType,
            [SlashCommandParameter(Name = "rating", Description = "The event rating to add")] EventRating EventRating,
            [SlashCommandParameter(Name = "date", Description = "The event date to add, in dd/mm/yyyy hh:mm (AM|PM)")] string EventDateString,
            [SlashCommandParameter(Name = "end", Description = "The event end to add, in dd/mm/yyyy hh:mm (AM|PM)")] string? EventEndString = null,
            [SlashCommandParameter(Name = "quest", Description = "If the event is quest compatable")] bool? QuestCompatable = null,
            [SlashCommandParameter(Name = "link", Description = "The event link to add")] string? EventLink = null)
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = $"Adding event...", Flags = MessageFlags.Ephemeral })); // move to handler

            string UserPings = "[\"448846699692032006\"]"; //Json encoded list with my discord ID so I'm always pinged for events
            DateTime EventDate = DateTime.ParseExact(EventDateString, "dd/MM/yyyy hh:mm tt", null);
            DateTime EventEnd = EventEndString != null ? DateTime.ParseExact(EventEndString, "dd/MM/yyyy hh:mm tt", null) : EventDate.AddHours(1);

            // construct an event
            EventInfo @event = new(
                    Eventnumber,
                    EventName,
                    EventDescription,
                    EventType,
                    QuestCompatable ?? false,
                    UserPings,
                    EventLink ?? "",
                    EventRating,
                    EventDate,
                    EventEnd,
                    EventLocation);

            // add it to the database
            var ss = Program.ConnectClient();

            // tell the server we are creating
            ss.WriteString("C");
            Program.CheckResponse(ss);

            // tell the server it is an event
            ss.WriteString("E");
            Program.CheckResponse(ss);

            ss.WriteString(JsonConvert.SerializeObject(@event));
            if (ss.ReadString() != "SUCCESS") { throw new Exception("Server Error"); }

            Program.pipeClient.Close();

            await ModifyResponseAsync((props) => { props.Content = "Added event"; });
        }

        [SubSlashCommand("remove", "Remove an event from the database")]
        public async Task RemoveEvent([SlashCommandParameter(Name = "number", Description = "The event number to remove")] int eventID)
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = $"Removing event...", Flags = MessageFlags.Ephemeral }));

            // get the event to remove
            var ss = Program.ConnectClient();

            // tell the server we are reading
            ss.WriteString("R");
            Program.CheckResponse(ss);

            // tell the server it is an event
            ss.WriteString("E");
            Program.CheckResponse(ss);

            ss.WriteString(eventID.ToString());
            EventInfo @event = JsonConvert.DeserializeObject<EventInfo>(ss.ReadString())!;
            if (ss.ReadString() != "SUCCESS") { throw new Exception("Server Error"); }
            Program.pipeClient.Close();

            // remove it from the database
            ss = Program.ConnectClient();

            // tell the server we are creating
            ss.WriteString("D");
            Program.CheckResponse(ss);

            // tell the server it is an event
            ss.WriteString("E");
            Program.CheckResponse(ss);

            ss.WriteString(JsonConvert.SerializeObject(@event));
            if (ss.ReadString() != "SUCCESS") { throw new Exception("Server Error"); }

            Program.pipeClient.Close();

            await ModifyResponseAsync((props) => { props.Content = "Removed event"; });
        }

        [SubSlashCommand("update", "Update an event in the database")]
        public async Task UpdateEvent(
            [SlashCommandParameter(Name = "number", Description = "The event number to update")] int eventID,
            [SlashCommandParameter(Name = "name", Description = "The event name to update")] string? EventName = null,
            [SlashCommandParameter(Name = "description", Description = "The event description to aupdate")] string? EventDescription = null,
            [SlashCommandParameter(Name = "location", Description = "The event location to add")] string? EventLocation = null,
            [SlashCommandParameter(Name = "type", Description = "The event type to update")] EventType? EventType = null,
            [SlashCommandParameter(Name = "quest", Description = "If the event is quest compatable")] bool? QuestCompatable = null,
            [SlashCommandParameter(Name = "link", Description = "The event link to update")] string? EventLink = null,
            [SlashCommandParameter(Name = "rating", Description = "The event rating to update")] EventRating? EventRating = null,
            [SlashCommandParameter(Name = "date", Description = "The event date to update, in dd/mm/yyyy hh:mm (AM|PM)")] string? EventDateString = null,
            [SlashCommandParameter(Name = "end", Description = "The event end to add, in dd/mm/yyyy hh:mm (AM|PM)")] string? EventEndString = null)
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = $"Updating event...", Flags = MessageFlags.Ephemeral }));

            DateTime? EventDate = EventDateString != null ? DateTime.ParseExact(EventDateString, "dd/MM/yyyy hh:mm tt", null) : null;
            DateTime? EventEnd = EventEndString != null ? DateTime.ParseExact(EventEndString, "dd/MM/yyyy hh:mm tt", null) : null;

            // get the event info from the command
            var ss = Program.ConnectClient();

            // tell the server we are reading
            ss.WriteString("R");
            Program.CheckResponse(ss);

            // tell the server it is an event
            ss.WriteString("E");
            Program.CheckResponse(ss);

            ss.WriteString(eventID.ToString());
            EventInfo @event = JsonConvert.DeserializeObject<EventInfo>(ss.ReadString())!;
            if (ss.ReadString() != "SUCCESS") { throw new Exception("Server Error"); }
            Program.pipeClient.Close();

            //update the event info based off what the user input

            @event.EventNumber = eventID;
            @event.EventName = EventName ?? @event.EventName;
            @event.EventDescription = EventDescription ?? @event.EventDescription;
            @event.EventType = EventType ?? @event.EventType;
            @event.EventRating = EventRating ?? @event.EventRating;
            @event.EventDate = EventDate ?? @event.EventDate;
            @event.QuestCompatable = QuestCompatable ?? @event.QuestCompatable;
            @event.EventLink = EventLink ?? @event.EventLink;
            @event.EventEnd = EventEnd ?? @event.EventEnd;
            @event.EventLocation = EventLocation ?? @event.EventLocation;

            // update the event in the database
            ss = Program.ConnectClient();

            // tell the server we are creating
            ss.WriteString("U");
            Program.CheckResponse(ss);

            // tell the server it is an event
            ss.WriteString("E");
            Program.CheckResponse(ss);

            ss.WriteString(JsonConvert.SerializeObject(@event));
            if (ss.ReadString() != "SUCCESS") { throw new Exception("Server Error"); }

            Program.pipeClient.Close();

            await ModifyResponseAsync((props) => { props.Content = "Updated event"; });
        }

        [SubSlashCommand("ping-for", "Ping for an event when it's starting")]
        public async Task PingForEvent([SlashCommandParameter(Name = "number", Description = "The event number to ping for")] int eventID) // redo with event id arg
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = $"Pinging for event...", Flags = MessageFlags.Ephemeral }));

            // delete the previous temporary role if there is one,
            // to easily remove it from everyone who has it
            if (pingRole != null)
            {
                try
                {
                    await pingRole.DeleteAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            // get the event we are pinging for
            var ss = Program.ConnectClient();

            // tell the server we are reading
            ss.WriteString("R");
            Program.CheckResponse(ss);

            // tell the server it is an event
            ss.WriteString("E");
            Program.CheckResponse(ss);

            ss.WriteString(eventID.ToString());
            EventInfo @event = JsonConvert.DeserializeObject<EventInfo>(ss.ReadString())!;
            if (ss.ReadString() != "SUCCESS") { throw new Exception("Server Error"); }
            Program.pipeClient.Close();

            // make a new temporary role to ping                                                                                                                                              should be orangeish yellow-\/
            pingRole = await Program.mainGuild.CreateRoleAsync(new RoleProperties() { Name = $"{@event.EventName}", Permissions = Permissions.ViewChannel, Color = new Color((int)(pingRole != null ? pingRole.Id : 16760320)), Hoist = false, Mentionable = true });

            // go through the ping list, adding the ping role to all of them
            foreach (string userID in JsonConvert.DeserializeObject<string[]>(@event.UserPings)!)
            {
                await (await Program.mainGuild.GetUserAsync(JsonConvert.DeserializeObject<ulong>(userID))).AddRoleAsync(pingRole.Id);
            }

            // build embed for the event
            string quest = @event.QuestCompatable != null ? $"\nQuest Compatable: {@event.QuestCompatable}" : "";
            string link = @event.EventLink != null ? $"[Join here]({@event.EventLink})" : "No link";
            EmbedProperties embed = new()
            {
                Title = @event.EventName,
                Description = $"{link} \n\n{@event.EventDescription} \n\nType: {@event.EventType} \nRating: {@event.EventRating} {quest}"
            };

            // ping for the event
            foreach (var channel in await Program.mainGuild.GetChannelsAsync())
            {
                if (BotSettings.settings.announceChannels.Contains(JsonConvert.SerializeObject(channel.Key)))
                {
                    
                    switch (channel.Value.GetType().Name)
                    {
                        case nameof(TextGuildChannel):
                            await ((TextGuildChannel)channel.Value).SendMessageAsync(new MessageProperties() { Content = $"<@&{pingRole.Id}>", Embeds = [embed] });
                            break;
                        case nameof(TextChannel):
                            await ((TextChannel)channel.Value).SendMessageAsync(new MessageProperties() { Content = $"<@&{pingRole.Id}>", Embeds = [embed] });
                            break;
                        case nameof(AnnouncementGuildChannel):
                            await ((AnnouncementGuildChannel)channel.Value).SendMessageAsync(new MessageProperties() { Content = $"<@&{pingRole.Id}>", Embeds = [embed] });
                            //publish message if possible
                            break;
                    }
                }
            }

            // Send announcement to bluesky
            ss = Program.ConnectBlueskyClient();

            // tell the server we are sending an announcement
            ss.WriteString("E");
            Program.CheckResponse(ss);

            EventPingInfo eventinfo = new EventPingInfo(@event.EventName, @event.EventDescription, @event.EventLink);
            Console.WriteLine(JsonConvert.SerializeObject(eventinfo, Formatting.Indented));
            string stringinfo = JsonConvert.SerializeObject(eventinfo);
            ss.WriteString(stringinfo);

            string status = ss.ReadString();
            switch (status)
            {
                case "SUCCESS":
                    Console.WriteLine("Bluesky success");
                    break;
                case "FAILURE":
                    Console.WriteLine("Bluesky failure");
                    break;
                default:
                    Console.WriteLine("Bluesky issue?");
                    break;
            }

            Program.pipeClient.Close();

            // Send announcement to Telegram
            ss = Program.ConnectTelegramClient();

            // tell the server we are sending an announcement
            ss.WriteString("E");
            Program.CheckResponse(ss);

            ss.WriteString(stringinfo);

            status = ss.ReadString();
            switch (status)
            {
                case "SUCCESS":
                    Console.WriteLine("Telegram success");
                    break;
                case "FAILURE":
                    Console.WriteLine("Telegram failure");
                    break;
                default:
                    Console.WriteLine("Telegram issue?");
                    break;
            }

            Program.pipeClient.Close();

            await ModifyResponseAsync((props) => { props.Content = "Pinged for event"; });
        }

        class EventPingInfo(string name, string desc, string? link)
        {
            public string  EventName = name;
            public string  EventDescription = desc;
            public string? EventLink = link;
        }
    }
}
