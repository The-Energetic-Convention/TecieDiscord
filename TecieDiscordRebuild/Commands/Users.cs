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
using TECEncryption;

namespace TecieDiscordRebuild.Commands
{
    [SlashCommand("account", "User account commands", DMPermission = true)]
    internal class Users : ApplicationCommandModule<SlashCommandContext>
    {
        [SubSlashCommand("create", "Create an account for TEC")]
        public async Task AddAccount(
            [SlashCommandParameter(Name = "username", Description = "Your username")]                string username,
            [SlashCommandParameter(Name = "password", Description = "Your password")]                string password,
            [SlashCommandParameter(Name = "confirm-password", Description = "Confirm your password")]string confirmpassword,
            [SlashCommandParameter(Name = "email"   , Description = "Your email")]                   string email,
            [SlashCommandParameter(Name = "pronouns", Description = "Your preferred pronouns")]      string pronouns,
            [SlashCommandParameter(Name = "birthday", Description = "Your birthday, in dd/mm/yyyy")] string birthdayString)
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = $"Creating account...", Flags = MessageFlags.Ephemeral })); // move to handler

            DateTime birthday = DateTime.ParseExact(birthdayString, "dd/MM/yyyy", null);
            string encryptedPassword = Encryption.Encrypt(password, JsonConvert.DeserializeObject<byte[]>(Program.authKey));
            if (password != confirmpassword) { await ModifyResponseAsync((props) => { props.Content = "Passswords do not match!"; }); return; }

            // get accounts
            var ss = Program.ConnectClient();

            // tell the server we are reading
            ss.WriteString("R");
            Program.CheckResponse(ss);

            // tell the server it is a user
            ss.WriteString("U");
            Program.CheckResponse(ss);

            ss.WriteString("ALL");
            UserData[] users = JsonConvert.DeserializeObject<UserData[]>(ss.ReadString())!;
            if (ss.ReadString() != "SUCCESS") { throw new Exception("Server Error"); }
            Program.pipeClient.Close();
            // construct a user
            UserData user = new(
                        username,
                        Context.User.Username,
                        encryptedPassword,
                        email,
                        UserRole.user,
                        false,
                        pronouns,
                        birthday,
                        users.Length);

            // add them to the database
            ss = Program.ConnectClient();

            // tell the server we are creating
            ss.WriteString("C");
            Program.CheckResponse(ss);

            // tell the server it is a user
            ss.WriteString("U");
            Program.CheckResponse(ss);

            ss.WriteString(JsonConvert.SerializeObject(user));
            if (ss.ReadString() != "SUCCESS") { throw new Exception("Server Error"); }

            Program.pipeClient.Close();

            await ModifyResponseAsync((props) => { props.Content = "Account created successfully!"; });
        }

        [SubSlashCommand("delete", "Delete your TEC account")]
        public async Task RemoveAccount(
            [SlashCommandParameter(Name = "username", Description = "Your username")] string username,
            [SlashCommandParameter(Name = "password", Description = "Your password")] string password) 
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = $"Deleting account...", Flags = MessageFlags.Ephemeral }));

            string encryptedPassword = Encryption.Encrypt(password, JsonConvert.DeserializeObject<byte[]>(Program.authKey));

            // get the event to remove
            var ss = Program.ConnectClient();

            // tell the server we are reading
            ss.WriteString("R");
            Program.CheckResponse(ss);

            // tell the server it is an event
            ss.WriteString("U");
            Program.CheckResponse(ss);

            ss.WriteString($"USERNAME={username}");
            UserData user = JsonConvert.DeserializeObject<UserData>(ss.ReadString())!;
            if (ss.ReadString() != "SUCCESS") { throw new Exception("Server Error"); }
            Program.pipeClient.Close();

            if (user.encryptedPassword != encryptedPassword) { await ModifyResponseAsync((props) => { props.Content = "Incorrect password!"; }); return; }

            // remove them from the database
            ss = Program.ConnectClient();

            // tell the server we are deleting
            ss.WriteString("D");
            Program.CheckResponse(ss);

            // tell the server it is a user
            ss.WriteString("U");
            Program.CheckResponse(ss);

            ss.WriteString(JsonConvert.SerializeObject(user));
            if (ss.ReadString() != "SUCCESS") { throw new Exception("Server Error"); }

            Program.pipeClient.Close();

            await ModifyResponseAsync((props) => { props.Content = "Account deleted"; });
        }

        [SubSlashCommand("update", "Update some info about your account")]
        public async Task UpdateAccount(
            [SlashCommandParameter(Name = "username"    , Description = "Your current username")]        string  username,
            [SlashCommandParameter(Name = "password"    , Description = "Your current password")]        string  password,
            [SlashCommandParameter(Name = "new-username", Description = "Your new username")]            string? newusername = null,
            [SlashCommandParameter(Name = "new-password", Description = "Your new password")]            string? newpassword = null,
            [SlashCommandParameter(Name = "email"       , Description = "Your email")]                   string? email = null,
            [SlashCommandParameter(Name = "pronouns"    , Description = "Your preferred pronouns")]      string? pronouns = null,
            [SlashCommandParameter(Name = "birthday"    , Description = "Your birthday, in dd/mm/yyyy")] string? birthdayString = null)
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = $"Updating account...", Flags = MessageFlags.Ephemeral }));

            DateTime? birthday = birthdayString != null ? DateTime.ParseExact(birthdayString, "dd/MM/yyyy", null) : null;
            string encryptedPassword = Encryption.Encrypt(password, JsonConvert.DeserializeObject<byte[]>(Program.authKey));
            string newencryptedPassword = Encryption.Encrypt(newpassword, JsonConvert.DeserializeObject<byte[]>(Program.authKey));

            // get the event info from the command
            var ss = Program.ConnectClient();

            // tell the server we are reading
            ss.WriteString("R");
            Program.CheckResponse(ss);

            // tell the server it is a user
            ss.WriteString("U");
            Program.CheckResponse(ss);

            ss.WriteString($"USERNAME={username}");
            UserData user = JsonConvert.DeserializeObject<UserData>(ss.ReadString())!;
            if (ss.ReadString() != "SUCCESS") { throw new Exception("Server Error"); }
            Program.pipeClient.Close();

            if (user.encryptedPassword != encryptedPassword) { await ModifyResponseAsync((props) => { props.Content = "Incorrect password!"; }); return; }

            //update the user info based off what the user input
            user.username          = newusername          ?? username;
            user.encryptedPassword = newencryptedPassword ?? encryptedPassword;
            user.email             = email                ?? user.email;
            user.pronouns          = pronouns             ?? user.pronouns;
            user.birthday          = birthday             ?? user.birthday;

            // update the user in the database
            ss = Program.ConnectClient();

            // tell the server we are updating
            ss.WriteString("U");
            Program.CheckResponse(ss);

            // tell the server it is a user
            ss.WriteString("U");
            Program.CheckResponse(ss);

            ss.WriteString(JsonConvert.SerializeObject(user));
            if (ss.ReadString() != "SUCCESS") { throw new Exception("Server Error"); }

            Program.pipeClient.Close();

            await ModifyResponseAsync((props) => { props.Content = "Account updated!"; });
        }
    }

    [SlashCommand("admin-users", "Admin user commands", DMPermission = false, DefaultGuildUserPermissions = Permissions.ManageChannels)]
    internal class AdminUsers : ApplicationCommandModule<SlashCommandContext>
    {
        [SubSlashCommand("list", "Get a list of accounts")]
        public async Task ListAccounts()
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = $"Getting users...", Flags = MessageFlags.Ephemeral }));

            // get events
            var ss = Program.ConnectClient();

            // tell the server we are reading
            ss.WriteString("R");
            Program.CheckResponse(ss);

            // tell the server it is a user
            ss.WriteString("U");
            Program.CheckResponse(ss);

            ss.WriteString("ALL");
            UserData[] users = JsonConvert.DeserializeObject<UserData[]>(ss.ReadString())!;
            if (ss.ReadString() != "SUCCESS") { throw new Exception("Server Error"); }
            Program.pipeClient.Close();

            // create a dict of event number:event name
            Dictionary<int, (string username, string discordUsername)> userDict = [];
            foreach (UserData user in users)
            {
                userDict.Add(user.userID, (user.username, user.discordUsername));
            }

            if (userDict.Count < 26)
            {
                Console.WriteLine("Count less than 25");
                // create an embed for the list
                EmbedProperties embed = new() { Title = "Users:" };

                List<EmbedFieldProperties> fields = [];
                foreach (int i in userDict.Keys)
                {
                    fields.Add(new EmbedFieldProperties() { Name = $"User Number: {i}", Value = $"{userDict[i].username}   {userDict[i].discordUsername}" });
                }

                await ModifyResponseAsync((props) => { props.Content = "Here is a list of users"; props.Embeds = [embed]; });
            }
            else
            {
                Console.WriteLine("Count more than 25");
                int chunks = (int)Math.Ceiling((decimal)userDict.Count / 25);
                EmbedProperties[] embeds = new EmbedProperties[chunks];
                Console.WriteLine($"Making {chunks} embeds");
                for (int i = 0; i < embeds.Length; i++)
                {
                    EmbedProperties embed = new() { Title = i == 0 ? "Users:" : "Users cont:" };
                    Console.WriteLine($"Chunk {i}");

                    int count = userDict.Count - (25 * i) > 25 ? 25 : userDict.Count - (25 * i);
                    Console.WriteLine($"Count {count}");
                    List<EmbedFieldProperties> fields = [];
                    for (int j = 0; j < count; j++)
                    {
                        int index = j + (25 * i);
                        Console.WriteLine($"User {index}");
                        fields.Add(new EmbedFieldProperties() { Name = $"Event Number: {index}", Value = $"{userDict[index].username}   {userDict[index].discordUsername}" });
                    }
                    embeds[i] = embed;
                }

                await ModifyResponseAsync((props) => { props.Content = "Here is a list of users"; props.Embeds = embeds; });
            }

        }

        [SubSlashCommand("info", "Get info about a specific account")]
        public async Task AccountInfo([SlashCommandParameter(Name = "number", Description = "The user number to get")] int userID)
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = $"Getting user info...", Flags = MessageFlags.Ephemeral }));

            // get the event
            var ss = Program.ConnectClient();

            // tell the server we are reading
            ss.WriteString("R");
            Program.CheckResponse(ss);

            // tell the server it is a user
            ss.WriteString("U");
            Program.CheckResponse(ss);

            ss.WriteString(userID.ToString());
            UserData user = JsonConvert.DeserializeObject<UserData>(ss.ReadString())!;
            if (ss.ReadString() != "SUCCESS") { throw new Exception("Server Error"); }
            Program.pipeClient.Close();

            // create an embed with all the event info
            EmbedProperties embed = new() { Title = user.username, Description = user.discordUsername };

            List<EmbedFieldProperties> fields = [];
            fields.Add(new EmbedFieldProperties() { Name = "Encryped Password", Value = user.encryptedPassword });
            fields.Add(new EmbedFieldProperties() { Name = "Email", Value = user.email });
            fields.Add(new EmbedFieldProperties() { Name = "User Role", Value = user.role.ToString() });
            fields.Add(new EmbedFieldProperties() { Name = "Email Confirmed", Value = user.emailConfirmed ? "True" : "False" });
            fields.Add(new EmbedFieldProperties() { Name = "Pronouns", Value = user.pronouns });
            fields.Add(new EmbedFieldProperties() { Name = "Birthday", Value = user.birthday.ToString("MMM dd, yyyy") });

            await ModifyResponseAsync((props) => { props.Content = $"Here is the info of {user.username}"; props.Embeds = [embed]; });
        }
    }
}
