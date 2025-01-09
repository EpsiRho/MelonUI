using MelonUI.Base;
using MelonUI.Default;
using MelonUI.Managers;
using MelonUIDemo.Testing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUIDemo.Backends
{
    public static class DemoWelcomeBackend
    {
        // Properties cannot be found without {get;set;} methods defined.
        public static ConsoleWindowManager CWM { get; set; }
        public static CancellationTokenSource CancelSource = new CancellationTokenSource();
        public static string WelcomeText { get; set; }
            = "[Color(72, 168, 241)]MelonUI V1.0b MXML Demo (1216122424)\n" +
              "[Color(255, 255, 255)]Any file you enter will be watched for changes, and update the MUIPage on file save.\n" +
              "[Color(199, 59, 59)]Support is limited, as are compiler messages. [Color(255, 255, 255)]If you're having trouble, contact Epsi :3";
        public static string ErrorInfo { get; set; }
        public static string MXML { get; set; }
        public static string MXMLPath { get; set; }
        public static MUIPage WatchedPage;
        public static FileSystemWatcher PageWatcher;
        public static Func<ConsoleKeyInfo, bool> CloseErrorDiagFunc { get; set; } = CloseErrorDialogCheck;
        public static bool CloseErrorDialogCheck(ConsoleKeyInfo key)
        {
            return true;
        }
        public static Action CloseErrorDiag { get; set; } = () =>
        {
            var elm = CWM.GetSubChildByName("ErrorInfo");
            var tbelm = CWM.GetSubChildByName("FileInput");
            elm.IsVisible = false;
            CWM.FocusedElement = tbelm;
            CWM.MoveFocus(tbelm);
        };

        public static Action<string, TextBox> FilePathInput { get; set; } = (string str, TextBox e) =>
        {
            str = str.Replace("\\", "/").Replace("\"", "");
            if(File.Exists(str))
            {
                e.Text = str;
                try
                {
                    MXML = File.ReadAllText(str);
                    MXMLPath = str;
                    CloseWelcomeScreen();
                    return;
                }
                catch(Exception ex)
                {
                    var er = (TextBlock)CWM.GetSubChildByName("ErrorInfo");
                    er.Text = $"The file {str} could not be opened!\n(Press any key to continue)";
                }
                return;
            }

            var err = (TextBlock)CWM.GetSubChildByName("ErrorInfo");
            err.Text = $"The file {str} was not found!\n(Press any key to continue)";
            err.IsVisible = true;
            CWM.FocusedElement = err;
            CWM.MoveFocus(err);

        };
        public static void CloseWelcomeScreen()
        {
            CWM.GetSubChildByName("DemoInfo").IsVisible = false;
            CWM.GetSubChildByName("FileInput").IsVisible = false;
            CWM.GetSubChildByName("ErrorInfo").IsVisible = false;

            // Compile MXML
            CompilePageChanges();

            // Remove the old page, since they would be overlapping.
            CWM.RemoveElement(CWM.GetSubChildByName("DemoWelcomePage"));

            // Start the watcher
            CreateWatcher();


        }

        public static void CreateWatcher()
        {
            PageWatcher = new FileSystemWatcher();
            PageWatcher.Path = Directory.GetParent(MXMLPath).FullName;
            PageWatcher.Filter = "*.xml";
            PageWatcher.IncludeSubdirectories = true;
            PageWatcher.EnableRaisingEvents = true;
            PageWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.LastAccess | NotifyFilters.Size;

            bool ping = false;
            PageWatcher.Changed += (s, e) =>
            {
                CompilePageChanges();
            };
        }

        public static void CompilePageChanges()
        {
            CWM.SetStatus("Changes Found");
            try
            {
                CWM.RemoveElement(WatchedPage);
                try
                {
                    MXML = File.ReadAllText(MXMLPath);
                }
                catch (Exception)
                {
                    int count = 0;
                    CWM.SetStatus("Waiting for file to unlock");
                    while (count < 5)
                    {
                        try
                        {
                            MXML = File.ReadAllText(MXMLPath);
                            break;
                        }
                        catch (Exception)
                        {
                            Thread.Sleep(1000);
                        }
                    }

                }
                CWM.SetStatus("Compiling MXML");
                WatchedPage = new MUIPage();
                var chk = WatchedPage.Compile(MXML);
                if (!chk)
                {
                    CWM.SetStatus($"Page compilation failed");
                    CancelSource.Cancel();
                    string sc = WatchedPage.GetSimpleCompilerDisplay();
                    while (CWM.IsManaged) { Thread.Sleep(20); }
                    Thread.Sleep(200);
                    Console.Clear();
                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine(sc);
                    return;
                }
                CWM.AddElement(WatchedPage);
                CWM.SetStatus($"Page compiled!");
                CWM.MoveFocus(WatchedPage.Children.FirstOrDefault(x => x.ConsiderForFocus && x.IsVisible));
                if(CancelSource.IsCancellationRequested)
                {
                    CancelSource = new CancellationTokenSource();
                    DemoWelcomeBackend.CWM.ManageConsole(DemoWelcomeBackend.CancelSource.Token);
                }
            }
            catch (Exception ex) { }
        }
    }
}
