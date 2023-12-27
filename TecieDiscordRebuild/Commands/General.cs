using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using Newtonsoft.Json;

namespace TecieDiscordRebuild.Commands
{
    [SlashCommand("general", "General commands", DMPermission = true)]
    internal class General : ApplicationCommandModule<SlashCommandContext>
    {
        [SubSlashCommand("where-am-i", "Get some info about the current guild")]
        public async Task WhereAmI()
        {
            int color = new Random().Next(16777215);
            if (Context.Guild != null)
            {
                var guild = Context.Guild;
                var guildID = guild.Id;
                var owner = guild.OwnerId;
                var memberCount = guild.UserCount;
                var icon = ImageUrl.GuildIcon(guildID, guild.IconHash, ImageFormat.Png).ToString();

                EmbedProperties embed = new() { Title = guild.Name + " Server Info", Color = new(color), Image = new(icon), Fields = [
                    new EmbedFieldProperties() { Name = "Owner", Value = $"<@{owner}>", Inline = true },
                    new EmbedFieldProperties() { Name = "Server ID", Value = $"{guildID}", Inline = true },
                    new EmbedFieldProperties() { Name = "Member Count", Value = $"{memberCount}", Inline = true }] };

                await RespondAsync(InteractionCallback.Message(new() { Embeds = [embed], Flags = MessageFlags.Ephemeral }));
            }
            else
            {
                if ("DMChannel" == Context.Channel.GetType().Name)
                {
                    EmbedProperties embed = new() { Title = "DM Channel", Color = new(color), Fields = [new EmbedFieldProperties() { Name = "With", Value = $"<@{Context.User.Id}>" }] };

                    await RespondAsync(InteractionCallback.Message(new() { Embeds = [embed], Flags = MessageFlags.Ephemeral }));
                }
            }
        }

        [SubSlashCommand("delete", "Quickly delete your last message")]
        public async Task Delete()
        {
            bool messageFound = false;
            await foreach (var message in Context.Channel.GetMessagesAsync(new() { Limit = 25 }))
            { 
                if (!messageFound)
                {
                    if (message.Author.Id == Context.User.Id)
                    {
                        await message.DeleteAsync();
                        messageFound = true;
                        await RespondAsync(InteractionCallback.Message(new() { Content = "Deleted", Flags = MessageFlags.Ephemeral }));
                    }
                }
            }
        }

        [SubSlashCommand("show-warns", "Shows the number of warnings for a given user")]
        public async Task ShowWarns([SlashCommandParameter(Name = "user", Description = "The user to show the warnings of")] User member)
        {
            string memberID = member.Id.ToString();
            string name = member.GlobalName ?? member.Username;
            try
            {
                await RespondAsync(InteractionCallback.Message(new() { Content = $"{name} has {BotSettings.settings.warnList[memberID]} warns" }));
            }
            catch
            {
                await RespondAsync(InteractionCallback.Message(new() { Content = $"{name} has no warns" }));
            }
        }

        [SubSlashCommand("who-am-i", "Gives some info about yourself, or another user if provided")]
        public async Task WhoAmI([SlashCommandParameter(Name = "user", Description = "The user to show the warnings of")] User? member = null)
        {
            member ??= Context.User; 
            string url = ImageUrl.UserAvatar(member.Id, member.AvatarHash, ImageFormat.Png).ToString();
            //int color = ((int)member.Id) & 16777215;
            int color = new Random().Next(16777215);
            EmbedProperties embed = new() { Title = $"{member.GlobalName} Member Info", Color = new(color), Image = new(url), Fields = [
                new EmbedFieldProperties() { Name = "User avatar: ", Value = $"Avatar: {url}", Inline = true } ] };

            if (member.Id == Program.authorID) { await RespondAsync(InteractionCallback.Message(new() { Content = "Hello my creator!", Embeds = [embed] })); }
            else { await RespondAsync(InteractionCallback.Message(new() { Embeds = [embed] })); }
        }
    }
}
