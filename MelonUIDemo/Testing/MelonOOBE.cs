using MelonUI.Default;
using MelonUI.Managers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUIDemo.Testing
{
    public class MelonOOBE
    {
        ConsoleWindowManager WindowManager { get; set; } = null!;
        QueueContainer queueContainer { get; set; } = null!;
        public string MongoURL { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        private void SetStatus(string text)
        {
            if (WindowManager.Find<TextBlock>("Status") is { } status) status.Text = text;
        }

        public void InitObjects()
        {
            // Init Container
            queueContainer = new QueueContainer()
            {
                X = "0",
                Y = "0",
                Width = "50%",
                Height = "98%",
                Name = "MelonOOBEQueue",
                FocusedBorderColor = Color.FromArgb(255, 92, 196, 77),
                MaxStackDisplaySize = 6,
            };

            // Get Mongo URL
            TextBox mongoInput = new TextBox()
            {
                Label = "Enter the URL for your MongoDB",
                Name = "MongoDBInput",
            };
            mongoInput.OnEnter += (res, source) =>
            {
                MongoURL = res;
                queueContainer.PopElement();
                SetStatus("[M/Username/P/L/C]");
            };
            queueContainer.QueueElement(mongoInput);

            // Get Username
            TextBox usernameInput = new TextBox()
            {
                Label = "Enter a name for your admin user",
                Name = "UsernameInput"
            };
            usernameInput.OnEnter += (res, source) =>
            {
                Username = res;
                queueContainer.PopElement();
                SetStatus("[M/U/Password/L/C]");
            };
            queueContainer.QueueElement(usernameInput);

            // Get Password
            TextBox passwordInput = new TextBox()
            {
                HideCharacters = true,
                Label = "Enter a password (Tab: Show)",
                Name = "PasswordInput"
            };
            passwordInput.OnEnter += (res, source) =>
            {
                Password = res;
                queueContainer.PopElement();
                queueContainer.Dispose();
                SetStatus("[M/U/P/Library/C]");
                DemoDropout();
            };
            queueContainer.QueueElement(passwordInput);

            // Get Library Paths


            // Final Confirmation


            // Show QueueContainer
            WindowManager.Title = "Melon OOBE";
            SetStatus("[MongoDB/U/P/L/C]");
            WindowManager.Elements.Add(queueContainer);

            FPSCounter fps = new FPSCounter()
            {
                X = "98%",
                Y = "4",
                Width = "5",
                Height = "4",
                Name = "FPSCounter",
                FocusedBorderColor = Color.FromArgb(255, 255, 6, 77)
            };

            WindowManager.FrameRendered += fps.OnFrameRendered;
            WindowManager.Elements.Add(fps);

        }

        public void DemoDropout()
        {
            OptionsMenu menu = new()
            {
                X = "0",
                Y = "0",
                Width = "50%",
                Height = "50%",
                MenuName = "Default UI Objects",
                Background = Color.FromArgb(0, 255, 255, 255)
            };
            menu.Options.Add(new MenuItem("RGB color picker", () =>
            {
                var picker = new ColorPicker { Width = "60", Height = "8" };
                picker.BeginEdit(Color.Cyan);
                var popup = WindowManager.ShowPopup(picker);
                picker.Confirmed += _ => popup.Dispose();
                picker.Cancelled += _ => popup.Dispose();
            }));
            WindowManager.Elements.Add(menu);
        }

        public void ShowOOBE(ConsoleWindowManager manager)
        {
            Console.Clear();
            Console.WriteLine($"Welcome to Melon!");
            Console.WriteLine($"Does this console support fast, constant rendering?");
            Console.WriteLine($"(Unless connecting over the internet it likely does)");
            Console.WriteLine($"(Selecting No will launch a Simplified Setup UI, and then output logging)?");
            Console.Write("[Yes/No]> ");
            string input = Console.ReadLine() ?? "";
            input = input.ToLower();
            if(input == "yes" || input == "y")
            {
                if(manager != null)
                {
                    WindowManager = manager;
                    WindowManager.Elements.Add(new TextBlock { Name = "Status", Width = "100%", Height = "1", XYAlignment = MelonUI.Enums.Alignment.BottomLeft, ShowBorder = false, ConsiderForFocus = false });
                    InitObjects();
                }
                else
                {
                    Console.WriteLine("WindowManager is missing!");
                }
            }
            else
            {
                Console.WriteLine("The SSUI has not be finished, please use the AUI");
            }
        }
    }
}
