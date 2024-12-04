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
        ConsoleWindowManager WindowManager { get; set; }
        QueueContainer queueContainer { get; set; }
        public string MongoURL { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
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
            mongoInput.OnEnter += (res) =>
            {
                MongoURL = res;
                queueContainer.PopElement();
                WindowManager.SetStatus("[M/Username/P/L/C]");
            };
            queueContainer.QueueElement(mongoInput);

            // Get Username
            TextBox usernameInput = new TextBox()
            {
                Label = "Enter a name for your admin user",
                Name = "UsernameInput"
            };
            usernameInput.OnEnter += (res) =>
            {
                Username = res;
                queueContainer.PopElement();
                WindowManager.SetStatus("[M/U/Password/L/C]");
            };
            queueContainer.QueueElement(usernameInput);

            // Get Password
            TextBox passwordInput = new TextBox()
            {
                HideCharacters = true,
                Label = "Enter a password (Tab: Show)",
                Name = "PasswordInput"
            };
            passwordInput.OnEnter += (res) =>
            {
                Password = res;
                queueContainer.PopElement();
                WindowManager.RemoveElement(queueContainer);
                WindowManager.SetStatus("[M/U/P/Library/C]");
            };
            queueContainer.QueueElement(passwordInput);

            // Get Library Paths


            // Final Confirmation


            // Show QueueContainer
            WindowManager.SetTitle("[Melon OOBE]");
            WindowManager.SetStatus("[MongoDB/U/P/L/C]");
            WindowManager.AddElement(queueContainer);
        }

        public void ShowOOBE(ConsoleWindowManager manager)
        {
            Console.Clear();
            Console.WriteLine($"Welcome to Melon!");
            Console.WriteLine($"Does this console support fast, constant rendering?");
            Console.WriteLine($"(Unless connecting over the internet it likely does)");
            Console.WriteLine($"(Selecting No will launch a Simplified Setup UI, and then output logging)?");
            Console.Write("[Yes/No]> ");
            string input = Console.ReadLine();
            input = input.ToLower();
            if(input == "yes" || input == "y")
            {
                if(manager != null)
                {
                    WindowManager = manager;
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
