using System;
using System.Windows.Forms;

namespace ZeroPaper.PrintAgent;

static class Program
{
    [STAThread]
    static void Main()
    {
#if NETFRAMEWORK
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
#else
        ApplicationConfiguration.Initialize();
#endif
        Application.Run(new MainForm());
    }
}
