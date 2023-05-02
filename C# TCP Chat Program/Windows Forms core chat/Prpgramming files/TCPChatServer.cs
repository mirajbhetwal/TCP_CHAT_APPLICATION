/////////////////////////////////////////////
/////////////////////////////////////////////
/// Student name:   Miraj Bhetwal
/// Student ID:     A00105794
/// Assessment_2:   Networking Project
////////////////////////////////////////////
/////////////////////////////////////////// 

using System;
using System.Collections.Generic; 
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using System.Linq;

//https://github.com/AbleOpus/NetworkingSamples/blob/master/MultiServer/Program.cs
namespace Windows_Forms_Chat
{
    public class TCPChatServer : TCPChatBase
    {
        
        public Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //connected clients
        public List<ClientSocket> clientSockets = new List<ClientSocket>();

        //Custom Data declaration
        private DateTime timestamp;
        private bool can_change_name = false;
        private bool can_connect = true;
        public enum EXIT
        {
            LEFT,
            KICKED,
            RULES
        }


        public static TCPChatServer createInstance(int port, TextBox chatTextBox)
        {
            TCPChatServer tcp = null;
            //setup if port within range and valid chat box given
            if (port > 0 && port < 65535 && chatTextBox != null)
            {
            tcp = new TCPChatServer();
            tcp.port = port;
            tcp.chatTextBox = chatTextBox;

            }

            //return empty if user not enter useful details
            return tcp;
        }

        public void SetupServer()
        {
            chatTextBox.Text += "Setting up server...\n";
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            serverSocket.Listen(0);
            //kick off thread to read connecting clients, when one connects, it'll call out AcceptCallback function
            serverSocket.BeginAccept(AcceptCallback, this);
            chatTextBox.Text += "Server setup complete\n";
        }


        public void CloseAllSockets()
        {
            foreach (ClientSocket clientSocket in clientSockets)
            {
            clientSocket.socket.Shutdown(SocketShutdown.Both);
            clientSocket.socket.Close();
            }
            clientSockets.Clear();
            serverSocket.Close();
        }

        public void AcceptCallback(IAsyncResult AR)
        {
            Socket joiningSocket;

            try
            {
            joiningSocket = serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException) // I cannot seem to avoid this (on exit when properly closing sockets)
            {
            return;
            }

            ClientSocket newClientSocket = new ClientSocket();
            newClientSocket.socket = joiningSocket;

            clientSockets.Add(newClientSocket);
            //start a thread to listen out for this new joining socket. Therefore there is a thread open for each client
            joiningSocket.BeginReceive(newClientSocket.buffer, 0, ClientSocket.BUFFER_SIZE, SocketFlags.None, ReceiveCallback, newClientSocket);
            AddToChat("Client connected, waiting for request...\n  ");

            //we finished this accept thread, better kick off another so more people can join
            serverSocket.BeginAccept(AcceptCallback, null);
        }

        public void ReceiveCallback(IAsyncResult AR)
        {
            ClientSocket currentClientSocket = (ClientSocket)AR.AsyncState;
            byte[] data;
            int received = 0;
            can_change_name = false;
            can_connect = true;

            if (currentClientSocket.socket.Connected)
            {
            try
            {
                received = currentClientSocket.socket.EndReceive(AR);
            }
            catch (SocketException)
            {
                AddToChat("Clent Forcefully disconnected");
                // Don't shutdown because the socket may be disposed and its disconnected anyway. 
                currentClientSocket.socket.Close();
                clientSockets.Remove(currentClientSocket);
                return;
            }
            }

            byte[] recBuf = new byte[received];
            Array.Copy(currentClientSocket.buffer, recBuf, received);
            string text = Encoding.ASCII.GetString(recBuf);
            AddToChat(text);


            // Some of the commands that clients can use - Custom Functions for each Command 
            // <------------------------------------------------------------->

            // --> !commands <--
            if (text.ToLower() == "!commands")
            {
            SendServerCommands(currentClientSocket);
            }
            // --> !username <--
            else if (text.ToLower().Contains("!username"))
            {
            text = text.Remove(0, 10);
            ChangeAddUserName(text, currentClientSocket);
            }

            //--> !about <--
            else if (text.ToLower() == "!about"){
                SendAppInfo (currentClientSocket);
            }

            //--> !who <--
            else if (text.ToLower() == "!who")
            {
                SendClientList (currentClientSocket);
            }

            // -->!whisper<--
            else if (text.Contains("!whisper"))
            {
                WhisperToClient(currentClientSocket, text);
            }

            // --->!exit<---
            else if (text.ToLower() == "!exit") // Client wants to exit gracefully
            {
            data = Encoding.ASCII.GetBytes("!exitcode: _+fAZ]QvA-clientexit]");
            currentClientSocket.socket.Send(data);
            RemoveClient(currentClientSocket, EXIT.LEFT); //Removal type
            }

            // --> !timestamps<--
            else if (text == "!timestamps")
            {
            TimeStamps(currentClientSocket);
            }

            // ---> !kick <---
            else if (text.Contains("!kick"))
            {
            KickClient(currentClientSocket, text);
            }


            //normal message broadcast out to all clients
            else
            {
            if (currentClientSocket.is_mod == true)
            { //add a "Mod" tag to server moderators
                SendToAll(currentClientSocket.username + "(Mod) :", text, currentClientSocket);
            }
            else if (currentClientSocket.username != null)
            {
                SendToAll(currentClientSocket.username + ":", text, currentClientSocket);
            }
            }
            if (currentClientSocket.socket.Connected)
            {
            //we just received a message from this socket, better keep an ear out with another thread for the next one
            currentClientSocket.socket.BeginReceive(currentClientSocket.buffer, 0, ClientSocket.BUFFER_SIZE, SocketFlags.None, ReceiveCallback, currentClientSocket);
            }
        }

        public void SendToAll(string name, string str, ClientSocket from)
        {
            byte[] data;
            // -----> !mod  <----- server command only
            // The server checks if it can add a client moderator among the clients
            bool continueSend = ServerMod(name, str);
            if (continueSend == false) {
            return;
            }
            //publish message to all connected clints
            foreach (ClientSocket c in clientSockets){
            if (from == null || !from.socket.Equals(c.socket)){
                if (c.show_timestamps == true){
                    timestamp = DateTime.Now;  // displays date - time stamps for enabled clients
                    data = Encoding.ASCII.GetBytes((char)13 + "\n" + name + " " + str + (char)13 + "\n\t\t\t\t\t     " + timestamp.ToString() + "\n");
                    c.socket.Send(data); 
                }
                if (c.show_timestamps == false){
                    data = Encoding.ASCII.GetBytes((Char)13 + "\n" + name + "" + "\n" + str);
                    c.socket.Send(data);
                }
                data = Encoding.ASCII.GetBytes
                ((char)13 + "-----------------------------\n");
                c.socket.Send(data);
            }
            }
        }

            //function to send a particular message to a particular client
            public void WhisperToClient(ClientSocket client, string text)
            {
            byte[] data;
            bool user_found = false;
            text = text.Remove(0, 9);
            string recipient_name = "";
            foreach (char c in text ){
                if (char.IsWhiteSpace(c)){ //extract the desired client name from the command string
                    break;
                }
                recipient_name += c;
            }
            text = text.Remove(0, recipient_name.Length); //extract the message from the command string
            foreach (ClientSocket c in clientSockets){
                if (c.username == recipient_name){
                user_found = true;
                if(c.show_timestamps == true){//sends the private message is the user is found 
                    timestamp = DateTime.Now;  // displays date - time stamps for enabled clients
                    data = Encoding.ASCII.GetBytes("[Whisper] Private message from" + client.username + ": " + text +  " (character count: " + text.Length.ToString() + ") at " + timestamp.ToString() + ".");
                    c.socket.Send(data);
                } 
                else{
                    data = Encoding.ASCII.GetBytes("[Whisper] Private message from" + client.username + ": " + text);
                    c.socket.Send(data);
                }
                data = Encoding.ASCII.GetBytes("Whisper sent to " + c.username);
                client.socket.Send(data);
                data = Encoding.ASCII.GetBytes
                ((char)13 + "\n___________________________");
                c.socket.Send(data);
                break;
            }
            }
            if (user_found == false){
            data = Encoding.ASCII.GetBytes("Username Not found. Whisper Not Sent.");
            client.socket.Send(data);
            }
            }

            //Function To handle the Clean Removal Of any Client leaving the server
            public void RemoveClient(ClientSocket client, EXIT exittype)
            {
            if (exittype == EXIT.RULES || exittype == EXIT.LEFT){
                SendToAll(client.username, "Left the chat",  null);
            }
            AddToChat("''" + client.username + "''(client) disconnected");
            client.username = null;
            client.is_connected = false;
            client.socket.Shutdown(SocketShutdown.Both);
            client.socket.Close();
            clientSockets.Remove(client);
            return;
            }

            //function to enable or disable time stamps for individual Clients
            public void TimeStamps (ClientSocket client)
            {
            byte[] data;
            if (client.show_timestamps == false){
                client.show_timestamps = true;
                data =Encoding.ASCII.GetBytes("Time-stamps = Enabled");
                client.socket.Send(data);
            }
            else if (client.show_timestamps == true){
                client.show_timestamps = false;
                data =Encoding.ASCII.GetBytes("Time-stamps = Disabled");
                client.socket.Send(data);
            }
            }

            //function to send a list of server commands to a requesting client
            public void SendServerCommands(ClientSocket currentClientSocket)
            {
            byte[] data;
            data = Encoding.ASCII.GetBytes(
                (char)13 + "\n................................................................................................................"
                + (char)13 + "\n" + "Server Commands lists:"
                + (char)13 + "\n" + "!username -- Change your Username."
                + (char)13 + "\n................................................"
                + (char)13 + "\n" + "!who -- Shows a list of all connected client usernames."
                + (char)13 + "\n................................................"
                + (char)13 + "\n" + "!about -- Displays information about this application"
                + (char)13 + "\n................................................"
                + (char)13 + "\n" + "!whisper <username> <message> -- sends a private message to the specified user"
                + (char)13 + "\n................................................"
                + (char)13 + "\n" + "!timestamps - toggles the display of  timestamps on/off and displays the date and time of all messages"
                + (char)13 + "\n................................................"
                + (char)13 + "\n" + "!exit - disconnects from the server"
                + (char)13 + "\n...............................................................................................................");

                currentClientSocket.socket.Send(data);
            if (currentClientSocket.is_mod == true){ //Commands seen by moderatoronly
            data = Encoding.ASCII.GetBytes(
                (char)13 + "\n................................................................................................................"
                + (char)13 + "\n" + "Moderator Commands lists:"
                + (char)13 + "\n" + "!mod -- Enables or disables Moderator Mode"
                + (char)13 + "\n................................................"
                + (char)13 + "\n" + "!kick -- kick any connected client");
                currentClientSocket.socket.Send(data);
                AddToChat("Commands sent to Client");
            }
            }

            // function to send information about the application to a requesting client
            public void SendAppInfo(ClientSocket currentClientSocket)
            {
            byte[] data;
            data = Encoding.ASCII.GetBytes(
                (char)13 + "\n........................................"
                + (char)13 + "\n" + "Application Information:"
                + (char)13 + "\n" + "Developer name: Matthew Carr"
                + (char)13 + "\n" + "Modifier name: Miraj Bhetwal"
                + (char)13 + "\n" + "Version: 1.0"
                + (char)13 + "\n" + "Purpose: establish TCP Connections between clients for chat Interactions."
                + (char)13 + "\n" + "Language: C#"
                + (char)13 + "\n" + "Platform: Windows"
                + (char)13 + "\n........................................"
            );
            currentClientSocket.socket.Send(data);
            AddToChat("Server information sent to Client");
            }

            //function that sends a list of connected clients to a requesting client
            public void SendClientList(ClientSocket currentClientSocket)
            {
                byte[] data;
                data = Encoding.ASCII.GetBytes((char)13 + "\nList of Connected Users:\n");
                currentClientSocket.socket.Send(data);
                foreach (ClientSocket c in clientSockets){
                    if (c.is_mod == true){ //Enables moderator tags on Client moderators.
                        data = Encoding.ASCII.GetBytes(c.username +"(Moderator)" + (char)13 + "\n");
                    }
                    else {
                        data = Encoding.ASCII.GetBytes(c.username + (char)13 + "\n");
                    }
                    currentClientSocket.socket.Send(data);
                }
                data = Encoding.ASCII.GetBytes((char)13 + "\n _________________________________________________________________________________________________");
                currentClientSocket.socket.Send(data);
                AddToChat("List of Connected CLients send to Requested Client.");
                {
                }
            }

            //function to handle clients thar are being forcefully kicked by the moderator.
            public void KickClient (ClientSocket client, string text)
            {
                byte[] data;
                bool user_found = false;
                if (client.is_mod == true) {
                    text = text.Remove(0, 6);
                    foreach (ClientSocket c in clientSockets) {
                        if (c.username == text) {
            user_found = true;
            if (c.username == client. username) { // won't allow a moderator to kick themselves
                data = Encoding.ASCII.GetBytes((char)13 + "\nYou cannot kick yourself from the server.");
                client.socket. Send(data);
                break;
                }
            if (c.is_mod == true) { // won't allow a moderator to kick a moderator
                data = Encoding.ASCII.GetBytes((char)13 + "\nYou cannot kick other server moderators.");
                client.socket. Send(data);
                break;
            }
                SendToAll(c.username, "was kicked from the server.", null);
                data = Encoding.ASCII.GetBytes("!exitcode: t^Dc@v*6ge-clientkicked"); // custom error message
                c.socket. Send(data);
                RemoveClient (c, EXIT.KICKED); // client removal type.
                break; 
                }

                if (user_found == false) { // displays message if a usernama is not found
                data = Encoding. ASCII.GetBytes((char)13+"Client username not found.");
                client.socket.Send(data);
            }
                else if (client.is_mod == false) { // this function cannot be called if the client is not a moderator
                data = Encoding.ASCII.GetBytes((char)13+ "Only a Server Moderator can perform this command ");
                client.socket.Send(data);

            }
            data = Encoding.ASCII.GetBytes
            ((char)13 + "\n_________________________________________________________________________________________________________");
            client.socket.Send(data);
                        }
                }
                }


            // Function used by only the server to handle enabling or disabling client moderators.
            public bool ServerMod (string name, string str)
            {
                if (name == "Server:") {
                bool user_found = false;
                List<string> mod_names= new List<string>();
                if (str.Contains("!mods")) { 
                bool mods_found = false;
                foreach (ClientSocket c in clientSockets) { // displays a list of all enabled server moderators
                if (c.is_mod == true) {
                mods_found = true;
                mod_names.Add(c.username);
                }
                }
                if (mods_found) {
                AddToChat((char) 13+ "\nServer Moderators:");
                foreach (string mod_name in mod_names) {
                AddToChat(mod_name);
                }
                }
                else if (!mods_found){
                AddToChat((char) 13+ "\nServer carrently has no moderators."); }
                return false;
                } 

                if (str.Contains("!mod")) { // checks if the server inputs the command string
                    str = str.Remove(0, 5);
                    foreach (ClientSocket c in clientSockets) {
                        if (c.username == str && c.is_mod == false) {
                        user_found = true;
                        c.is_mod = true;
                        str = "Client user ''"+ c.username + "''has been promoted to Server Moderator.";
                        SendToAll("Server: ", str, null); // promotes a client if conditions met
                        return false;
                        }
                    else if (c.username == str && c.is_mod == true) {
                        user_found = true;
                        c.is_mod = false;
                        str = "Client user ''"+ c.username + "'' has been demoted from Server Moderator.";
                        SendToAll("Server: ", str, null); // demotes a client if conditions met
                    }
                }
                    if (user_found == false) {
                    AddToChat("Usernane not found."); // displays message if username not found.
                    return false;
                }
            }
        }
        return true;
    }

            // Function used to add, change and compare usernames.
            public void ChangeAddUserName(string name, ClientSocket client)
            {
            byte[] data;
            foreach (ClientSocket c in clientSockets){

            if (name.ToLower() == "Server" && client.is_connected==true) { // won't allow a client to use 'server' as a user
            can_change_name = false;
            data = Encoding.ASCII.GetBytes("Cannot use the name Server'.");
            client.socket.Send(data);
                break;
            }

            if (name.ToLower() == "Server" && client. is_connected == false) { // won't allow a client to use 'server' as a
            can_connect = false;
            can_change_name = false;
            data = Encoding.ASCII.GetBytes("!exitcode: 7D-vojgT4-nameserver");
            client.socket.Send(data);
            RemoveClient (client, EXIT.KICKED); // client removal type.
            break;
            }

            if (c.username == name && client.is_connected == false) { // userneme is already in use
            can_connect = false;
            data = Encoding.ASCII.GetBytes("!exitcode: .zDA+#202q-samenase");
            client.socket.Send(data);
            RemoveClient(client, EXIT.KICKED); // client removal type
            break;
            }

            if (c.username == name && client.is_connected == true) { // usernase is already in use
            data = Encoding.ASCII.GetBytes("Could not change username, Name already in use.");
            client.socket.Send(data);
            can_change_name = false;
            break;
            }

            if (c.username != name)
            can_change_name = true;
            }

            if (client.is_connected == true && can_change_name == true) { // successfully changes a usernase
            SendToAll(client.username, "has changed their name to " + name, null);
            client.username = name;
            }

            if (client.is_connected == false && can_connect == true) { // welcomes a new user if conditions are et
            client.is_connected = true;
            client. username = name;
            SendToAll(client.username, "has joined the chat!", null);
            }
        }
    }
}

