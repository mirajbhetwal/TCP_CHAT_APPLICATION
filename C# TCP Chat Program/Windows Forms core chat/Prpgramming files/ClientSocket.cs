/////////////////////////////////////////////
/////////////////////////////////////////////
/// Student name:   Miraj Bhetwal
/// Student ID:     A00105794
/// Assessment_2:   Networking Project
////////////////////////////////////////////
/////////////////////////////////////////// 

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Windows_Forms_Chat
{
    public class ClientSocket
    {
        //add other attributes to this, e.g username, what state the client is in etc
        public Socket socket;
        public string username;
        public const int BUFFER_SIZE = 2048;
        public byte[] buffer = new byte[BUFFER_SIZE];
        public bool is_connected = false;
        public bool is_mod = false;
        public bool show_timestamps = false;
        public int state; //add a state variable to represent the client state, e.g. 0 = logged out, 1 = logged in, etc.
    }
}
