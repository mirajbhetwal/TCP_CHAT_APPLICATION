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
using System.Windows.Forms;

namespace Windows_Forms_Chat
{

    public class TCPChatBase
    {
        public TextBox chatTextBox;
        public int port;
        public void SetChat(string str)
        {
            chatTextBox.Invoke((Action)delegate
            {
                if (str != null || str != "" || str !=" ")
                {
                    chatTextBox.Text = str;
                    chatTextBox.AppendText(Environment.NewLine);
                }
                
            });
        }
        public void AddToChat(string str)
        {
            //dumb https://iandotnet.wordpress.com/tag/multithreading-how-to-update-textbox-on-gui-from-another-thread/
            chatTextBox.Invoke((Action)delegate
            {
                if (str != null || str != "" || str !=" "){
                chatTextBox.AppendText(str);
                chatTextBox.AppendText(Environment.NewLine);
                }
            });
        }
    }
}
