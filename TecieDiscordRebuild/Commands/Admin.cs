using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using NetCord;
using NetCord.Gateway;

namespace TecieDiscordRebuild.Commands
{
    [SlashCommand("admin", "Admin commands", DMPermission = false, DefaultGuildUserPermissions = Permissions.ManageChannels)]
    internal class Admin : ApplicationCommandModule<SlashCommandContext>
    {
        //kick
        [SubSlashCommand("kick", "Kick a user")]
        public async Task Kick([SlashCommandParameter(Name = "user", Description = "The user to kick")] User guildUser,
                               [SlashCommandParameter(Name = "reason", Description = "The reason for the kick")] string reason)
        {
            try
            {
                await KickUser(Context.Guild, guildUser.Id, reason);
                await RespondAsync(InteractionCallback.Message(new() { Content = $"Kicked <@{guildUser.Id}> for {reason}" }));
            }
            catch { await RespondAsync(InteractionCallback.Message(new() { Content = "Bot does not have kick user permission", Flags = MessageFlags.Ephemeral })); }
        }

        private async Task KickUser(Guild guild, ulong user, string reason)
        {
            await guild.KickUserAsync(user, new() { AuditLogReason = reason});
        }

        //ban
        [SubSlashCommand("ban", "Ban a user")]
        public async Task Ban([SlashCommandParameter(Name = "user", Description = "The user to ban")] User guildUser,
                               [SlashCommandParameter(Name = "reason", Description = "The reason for the ban")] string reason)
        {
            try
            {
                await BanUser(Context.Guild, guildUser.Id, reason);
                await RespondAsync(InteractionCallback.Message(new() { Content = $"Hit <@{guildUser.Id}> with the ban hammer for {reason}" }));
            }
            catch { await RespondAsync(InteractionCallback.Message(new() { Content = "Bot does not have ban users permission", Flags = MessageFlags.Ephemeral })); }
        }

        private async Task BanUser(Guild guild, ulong user, string reason)
        {
            await guild.BanUserAsync(user, 0, new() { AuditLogReason = reason });
        }

        //guildID
        [SubSlashCommand("guild-id", "Get the ID of the current guild")]
        public async Task GuildID()
        {
            if (Context.Guild == null) { await RespondAsync(InteractionCallback.Message(new() { Content = $"Must be used in a guild!", Flags = MessageFlags.Ephemeral })); return; }
            await RespondAsync(InteractionCallback.Message(new() { Content = $"{Context.Guild.Id}", Flags = MessageFlags.Ephemeral }));
        }

        //channelID
        [SubSlashCommand("channel-id", "Get the ID of the current channel")]
        public async Task ChannelID()
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = $"{Context.Channel.Id}", Flags = MessageFlags.Ephemeral }));
        }

        //echo
        [SubSlashCommand("echo", "Echo a message in a given channel")]
        public async Task Echo([SlashCommandParameter(Name = "message", Description = "The message to send", MaxLength = 300)] string message,
                               [SlashCommandParameter(Name = "channel", Description = "The channel to send it in")] TextChannel channel)
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = "Sending message...", Flags = MessageFlags.Ephemeral }));
            await channel.SendMessageAsync(message);
            await ModifyResponseAsync((properties) => { properties.Content = "Message sent"; });
        }

        //announceAll
        [SubSlashCommand("announce-all", "Send an @everyone announcement to all announcement channels")]
        public async Task AnnounceAll([SlashCommandParameter(Name = "message", Description = "The announcement message to send", MaxLength = 300)] string message,
                                      [SlashCommandParameter(Name = "announcement", Description = "Whether it is an announcement (T) or update (F)")] bool announce = true)
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = "Sending announcement...", Flags = MessageFlags.Ephemeral }));
            await foreach (var guild in Program.client.Rest.GetCurrentUserGuildsAsync())
            {
                foreach (var channel in await guild.GetChannelsAsync())
                {
                    if (BotSettings.settings.announceChannels.Contains(JsonConvert.SerializeObject(channel.Key)))
                    {
                        switch (channel.Value.GetType().Name)
                        {
                            case nameof(TextGuildChannel):
                                await ((TextGuildChannel)channel.Value).SendMessageAsync($"@everyone! {message}");
                                break;
                            case nameof(TextChannel):
                                await ((TextChannel)channel.Value).SendMessageAsync($"@everyone! {message}");
                                break;
                            case nameof(AnnouncementGuildChannel):
                                RestMessage sentmessage = await ((AnnouncementGuildChannel)channel.Value).SendMessageAsync($"@everyone! {message}");
                                await sentmessage.CrosspostAsync();
                                break;
                        }
                    }
                }
            }

            // Send announcement to bluesky
            var ss = Program.ConnectBlueskyClient();

            // tell the server we are sending an announcement
            ss.WriteString(announce ? "A" : "U");
            Program.CheckResponse(ss);

            ss.WriteString(message);

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

            // Send announcement to telegram
            ss = Program.ConnectTelegramClient();

            // tell the server we are sending an announcement
            ss.WriteString(announce ? "A" : "U");
            Program.CheckResponse(ss);

            ss.WriteString(message);

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

            // Send announcement to twitter
            ss = Program.ConnectTwitterClient();

            // tell the server we are sending an announcement
            ss.WriteString(announce ? "A" : "U");
            Program.CheckResponse(ss);

            ss.WriteString(message);

            status = ss.ReadString();
            switch (status)
            {
                case "SUCCESS":
                    Console.WriteLine("Twitter success");
                    break;
                case "FAILURE":
                    Console.WriteLine("Twitter failure");
                    break;
                default:
                    Console.WriteLine("Twitter issue?");
                    break;
            }

            Program.pipeClient.Close();

            await ModifyResponseAsync((properties) => { properties.Content = "Announcements sent"; });
        }

        //allChan  --not reccomended for use lol--
        [SubSlashCommand("all-chan", "Send a message to every channel of every server")]
        public async Task AllChan([SlashCommandParameter(Name = "message", Description = "The message to send")] string message)
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = "Sending...", Flags = MessageFlags.Ephemeral }));
            await foreach (var guild in Program.client.Rest.GetCurrentUserGuildsAsync())
            {
                foreach (var channel in await guild.GetChannelsAsync())
                {
                    switch (channel.Value.GetType().Name)
                    {
                        case nameof(TextGuildChannel):
                            await ((TextGuildChannel)channel.Value).SendMessageAsync(message);
                            break;
                        case nameof(TextChannel):
                            await ((TextChannel)channel.Value).SendMessageAsync(message);
                            break;
                        case nameof(AnnouncementGuildChannel):
                            await ((AnnouncementGuildChannel)channel.Value).SendMessageAsync(message);
                            break;
                    }
                }
            }
            await ModifyResponseAsync((properties) => { properties.Content = "Messages sent lol"; });
        }

        //purge
        [SubSlashCommand("purge", "Delete a number of messages")]
        public async Task Purge([SlashCommandParameter(Name = "count", Description = "The number of messages to delete, no more than 100!")] long count)
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = "Deleting messages...", Flags = MessageFlags.Ephemeral }));
            if (count > 100) { await ModifyResponseAsync((properties) => { properties.Content = "Cannot delete over 100 messages at once"; }); return; }

            await foreach (var message in Context.Channel.GetMessagesAsync(new() { Limit = (int)count }))
            {
                await message.DeleteAsync();
            }
            await ModifyResponseAsync((properties) => { properties.Content = "Messages deleted"; });
        }

        //ignore
        [SubSlashCommand("ignore", "Ignore the current channel")]
        public async Task Ignore()
        {
            BotSettings.settings.ignoreChannels.Add(JsonConvert.SerializeObject(Context.Channel.Id));
            BotSettings.Save();
            await RespondAsync(InteractionCallback.Message(new() { Content = "Now ignoring this channel", Flags = MessageFlags.Ephemeral }));
        }

        //delIgnore
        [SubSlashCommand("del-ignore", "Remove a channel from the list to be ignored")]
        public async Task DelIgnore([SlashCommandParameter(Name = "channel", Description = "The channel to remove from the announcements list")] long index)
        {
            BotSettings.settings.ignoreChannels.RemoveAt((int)index);
            BotSettings.Save();
            await RespondAsync(InteractionCallback.Message(new() { Content = "Now listening to channel", Flags = MessageFlags.Ephemeral }));
        }

        //listIgnored
        [SubSlashCommand("list-ignore", "See the list of ignored channels")]
        public async Task ListIgnored()
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = JsonConvert.SerializeObject(BotSettings.settings.ignoreChannels), Flags = MessageFlags.Ephemeral }));
        }

        //setWarns
        [SubSlashCommand("set-warns", "Set the warn limit")]
        public async Task SetWarns([SlashCommandParameter(Name = "limit", Description = "The new warn limit")] long limit)
        {
            BotSettings.settings.warnLimit = (int)limit;
            BotSettings.Save();
            await RespondAsync(InteractionCallback.Message(new() { Content = $"Set limit to {limit}", Flags = MessageFlags.Ephemeral }));
        }

        //warn
        [SubSlashCommand("warn", "Warn a user")]
        public async Task Warn([SlashCommandParameter(Name = "user", Description = "The user to warn")] User member, 
                               [SlashCommandParameter(Name = "reason", Description = "The reason for the warning")] string reason)
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = "...", Flags = MessageFlags.Ephemeral }));
            string memberID = member.Id.ToString();

            List<EmbedFieldProperties> fields = [];
            try
            {
                BotSettings.settings.warnList[memberID]++;
                fields.Add(new() { Name = $"{member.GlobalName ?? member.Username}, you have been warned for \"{reason}\"",
                    Value = $"This is your #{BotSettings.settings.warnList[memberID]} warning", Inline = true });
                BotSettings.Save();
                if (BotSettings.settings.warnList[memberID] == BotSettings.settings.warnLimit)
                {
                    fields[0].Value = $"This is your #{BotSettings.settings.warnList[memberID]} warning. You are at the warn limit, once more and you are kicked! Be careful not to break the rules, maybe go familiarize yourself with them.";
                }
                else if (BotSettings.settings.warnList[memberID] == BotSettings.settings.warnLimit +1)
                {
                    fields[0].Value = $"This is their #{BotSettings.settings.warnList[memberID]} warning. They have been kicked.";
                    await KickUser(Context.Guild!, member.Id, $"You have exceeded your warn limit. You have been kicked for \"{reason}\". If you return and are warned again, you will be banned");
                }
                else if (BotSettings.settings.warnList[memberID] >= BotSettings.settings.warnLimit +2)
                {
                    fields[0].Value = $"This is their #{BotSettings.settings.warnList[memberID]} warning. They have been struck down by the ban hammer.";
                    await BanUser(Context.Guild!, member.Id, $"Your warnings were not reset, and you had been kicked but returned. You have now been banned for \"{reason}\". You may contact a member of staff to discuss unbanning you");
                }
            }
            catch (KeyNotFoundException)
            {
                fields.Add(new() { Name = $"{member.GlobalName ?? member.Username}, you have been warned for \"{reason}\"",
                    Value = $"This is your first warning, maybe be a little more carful next time :DD", Inline = true });
                BotSettings.settings.warnList[memberID] = 1;
                BotSettings.Save();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            EmbedProperties embed = new() { Title = "Warning!", Color = new(0xFF, 0x00, 0x00), Thumbnail = new(member.GetAvatarUrl().ToString()), Fields = fields };

            await Context.Channel.SendMessageAsync(new() { Content = $"<@{member.Id}>", Embeds = [embed] });
            await ModifyResponseAsync((props) => { props.Content = "Warned user"; } );
        }

        //delWarn
        [SubSlashCommand("del-warn", "Clear someone's warnings")]
        public async Task DelWarn([SlashCommandParameter(Name = "user", Description = "The user to remove warnings from")] User user)
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = "...", Flags = MessageFlags.Ephemeral }));
            string userID = user.Id.ToString();

            try
            {
                BotSettings.settings.warnList[userID] -= BotSettings.settings.warnList[userID];
                BotSettings.Save();
                await ModifyResponseAsync((props) => { props.Content = $"Removed warnings from {user.GlobalName ?? user.Username}"; });
            }
            catch 
            {
                await ModifyResponseAsync((props) => { props.Content = $"{user.GlobalName ?? user.Username} has no warnings on record"; });
            }
        }

        //addBadWord
        [SubSlashCommand("add-bad-word", "Add a list of words to the bad words list")]
        public async Task AddBadWord([SlashCommandParameter(Name = "words", Description = "A space separated list of words to add")] string words)
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = "Adding words...", Flags = MessageFlags.Ephemeral }));

            foreach (string word in words.Split(new char[] { ' ' }))
            {
                if (BotSettings.settings.badWords.Contains(word))
                {
                    continue;
                }
                else
                {
                    BotSettings.settings.badWords.Add(word);
                }
            }

            await ModifyResponseAsync((props) => { props.Content = "Added words"; });
        }

        //savesett
        [SubSlashCommand("save-sett", "Save the current bot settings")]
        public async Task SaveSett()
        {
            BotSettings.Save();
            await RespondAsync(InteractionCallback.Message(new() { Content = "Saved", Flags = MessageFlags.Ephemeral }));
        }

        //addAnnounce
        [SubSlashCommand("add-announce", "Add the current channel to the announcement list")]
        public async Task AddAnnounce()
        {
            BotSettings.settings.announceChannels.Add(JsonConvert.SerializeObject(Context.Channel.Id));
            BotSettings.Save();
            await RespondAsync(InteractionCallback.Message(new() { Content = "Channel added to announcement list!", Flags = MessageFlags.Ephemeral }));
        }

        //delAnnounce
        [SubSlashCommand("del-announce", "Remove a channel from the announcement list")]
        public async Task DelAnnounce([SlashCommandParameter(Name = "channel", Description = "The channel to remove from the announcements list")] long index)
        {
            BotSettings.settings.announceChannels.RemoveAt((int)index);
            BotSettings.Save();
            await RespondAsync(InteractionCallback.Message(new() { Content = "No longer announcing in channel", Flags = MessageFlags.Ephemeral }));
        }

        //listAnnounce
        [SubSlashCommand("list-announce", "See the announcement channel list")]
        public async Task ListAnnounce()
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = JsonConvert.SerializeObject(BotSettings.settings.announceChannels), Flags = MessageFlags.Ephemeral }));
        }

        //logBadWords
        [SubSlashCommand("log-bad-words", "See the list of bad words")]
        public async Task LogbadWords()
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = JsonConvert.SerializeObject(BotSettings.settings.badWords), Flags = MessageFlags.Ephemeral }));
        }
    }
}
