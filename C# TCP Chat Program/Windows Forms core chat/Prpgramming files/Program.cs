 /////////////////////////////////////////////
/////////////////////////////////////////////
/// Student name:   Miraj Bhetwal
/// Student ID:     A00105794
/// Assessment_2:   Networking Project
////////////////////////////////////////////
/////////////////////////////////////////// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Windows_Forms_Chat
{
    static class Program
    {
        //<summary>
        ///  The main entry point for the application.
        //</summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
