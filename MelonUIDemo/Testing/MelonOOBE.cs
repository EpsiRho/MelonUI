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
            mongoInput.OnEnter += (res, source) =>
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
            usernameInput.OnEnter += (res, source) =>
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
            passwordInput.OnEnter += (res, source) =>
            {
                Password = res;
                queueContainer.PopElement();
                WindowManager.RemoveElement(queueContainer);
                WindowManager.SetStatus("[M/U/P/Library/C]");
                DemoDropout();
            };
            queueContainer.QueueElement(passwordInput);

            // Get Library Paths


            // Final Confirmation


            // Show QueueContainer
            WindowManager.SetTitle("[Melon OOBE]");
            WindowManager.SetStatus("[MongoDB/U/P/L/C]");
            WindowManager.AddElement(queueContainer);

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
            WindowManager.AddElement(fps, false);

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
                UseStatusBar = true,
                Background = Color.FromArgb(0, 255, 255, 255)
            };
            menu.Options.Add(new MenuItem("Show Music Player", () =>
            {
                var mgrid = new GridContainer(1, true, 1) // Create a Grid to put the item in so we can use it's animation model (true)
                {
                    X = "60%", 
                    Y = "10%",
                    Width = "35%",
                    Height = "40%",
                    Name = "MusicGrid",
                    Background = Color.FromArgb(0, 255, 255, 255)
                };

                MusicPlayerElement mp = new(null) // Takes in a PlaybackManager based class, so you can define an audio manager. I'm using a wrapper for NAudio, Windows Only. 
                {                                 // Here it's null because we haven't preloaded a file. You could open a FilePicker first or open it from the Music Player.
                    ShowBorder = false
                };

                mgrid.AddElement(mp, 0, 0);
                WindowManager.AddElement(mgrid);
            }
            ));
            menu.Options.Add(new MenuItem("Show Image", () =>
            {
                // Image
                ConsoleImage img = new ConsoleImage(@"", "40%", "99%")
                {
                    X = "60%",
                    Y = "0",
                    Name = "EpsiPic",
                    UseBg = true
                };
                int mode = 0;
                img.RegisterKeyboardControl(ConsoleKey.M, () =>
                {
                    switch (mode)
                    {
                        case 0:
                            img.UseBg = false;
                            img.UseColor = true;
                            img.InitializeImageAsync();
                            mode++;
                            break;
                        case 1:
                            img.UseBg = false;
                            img.UseColor = false;
                            img.InitializeImageAsync();
                            mode++;
                            break;
                        case 2:
                            img.UseBg = true;
                            img.UseColor = true;
                            img.InitializeImageAsync();
                            mode = 0;
                            break;
                    }
                }, "Change Diplay Mode");
                img.RegisterKeyboardControl(ConsoleKey.O, () =>
                {
                    try
                    {
                        var filepickergrid = new GridContainer(1, true, 1)
                        {
                            X = "25%",
                            Y = "25%",
                            Width = "55%",
                            Height = "55%",
                            Name = "MPFilePickerGrid"
                        };
                        FilePicker picker = new FilePicker(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures))
                        {
                            ShowBorder = false,
                            Name = "MPFilePicker"
                        };
                        picker.OnFileSelected += () =>
                        {
                            // On file select, close picker
                            filepickergrid.AnimateClose(() =>
                            {
                                // Remove File Picker
                                filepickergrid.RenderThreadDeleteMe = true;

                                Task.Run(() =>
                                {
                                    try
                                    {
                                        img.Path = picker.Path;
                                    }
                                    catch (Exception ex)
                                    {
                                        img.ParentWindow.SetStatus("File Loading Error!");
                                    }
                                });
                            });

                        };
                        filepickergrid.AddElement(picker, 0, 0);
                        img.ParentWindow.AddElement(filepickergrid, true);
                        picker.Show();
                    }
                    catch (Exception e)
                    {
                        img.ParentWindow.SetStatus("File Picker Error!");
                    }
                }, "Open File"); // When O is hit, add a filepicker and handle file input (done thru defining Actions)

                WindowManager.AddElement(img); // Add the image viewer to the grid
            }
            ));
            WindowManager.AddElement(menu, false);
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
