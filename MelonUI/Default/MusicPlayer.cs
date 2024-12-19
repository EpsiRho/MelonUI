using System;
using System.Drawing;
using MelonUI.Base;
using MelonUI.Managers;

namespace MelonUI.Default
{
    public class MusicPlayerElement : UIElement
    {
        private bool showControls = false;
        public bool showWaveform = false;
        private GridContainer waveformGrid;
        private PlaybackManager playbackManager;
        private ATL.Track trackMetadata;
        private float volumeCache = 0.1f;
        private string filename = "";

        public MusicPlayerElement(PlaybackManager manager)
        {
            playbackManager = manager;
            waveformGrid = new GridContainer(1, true, 1);
            RegisterKeyboardControl(ConsoleKey.C, () =>
            {
                showControls = !showControls;
                NeedsRecalculation = true;
            }, "Toggle Controls Display");

            // Playback
            RegisterKeyboardControl(ConsoleKey.Spacebar, () =>
            {
                if(playbackManager == null)
                {
                    ParentWindow.SetStatus("No audio loaded");
                    return;
                }
                if (!playbackManager.GetPlayState())
                {
                    try
                    {
                        playbackManager.Play();
                        ParentWindow.SetStatus("Now Playing");
                    }
                    catch (Exception e)
                    {
                        ParentWindow.SetStatus("Playback Error!");
                    }
                }
                else
                {
                    try
                    {
                        playbackManager.Pause();
                        ParentWindow.SetStatus("Playback Paused");
                    }
                    catch (Exception e)
                    {
                        ParentWindow.SetStatus("Pause Error!");
                    }
                }
                Thread t = new Thread(() =>
                {
                    Thread.Sleep(1000);
                    ParentWindow.SetStatus("");
                });
                t.Start();
            }, "Play/Pause");
            RegisterKeyboardControl(ConsoleKey.LeftArrow, () =>
            {
                try
                {
                    playbackManager.SeekRelative(-10f);
                    ParentWindow.SetStatus("-10 Seconds");
                }
                catch (Exception e)
                {
                    ParentWindow.SetStatus("No audio loaded");
                }
                Thread t = new Thread(() =>
                {
                    Thread.Sleep(1000);
                    ParentWindow.SetStatus("");
                });
                t.Start();
            }, "Rewind");
            RegisterKeyboardControl(ConsoleKey.RightArrow, () =>
            {
                try
                {
                    playbackManager.SeekRelative(10f);
                    ParentWindow.SetStatus("+10 Seconds");
                }
                catch (Exception e)
                {
                    ParentWindow.SetStatus("No audio loaded");
                }
                Thread t = new Thread(() =>
                {
                    Thread.Sleep(1000);
                    ParentWindow.SetStatus("");
                });
                t.Start();
            }, "Skip");
            RegisterKeyboardControl(ConsoleKey.UpArrow, () =>
            {
                try
                {
                    playbackManager.SetVolume(playbackManager.GetVolume() + 0.05f);
                    volumeCache = playbackManager.GetVolume();
                    ParentWindow.SetStatus("+5 Volume");
                }
                catch (Exception e)
                {
                    ParentWindow.SetStatus("No audio loaded");
                }
                Thread t = new Thread(() =>
                {
                    Thread.Sleep(1000);
                    ParentWindow.SetStatus("");
                });
                t.Start();
            }, "Volume Up");
            RegisterKeyboardControl(ConsoleKey.DownArrow, () =>
            {
                try
                {
                    playbackManager.SetVolume(playbackManager.GetVolume() - 0.05f);
                    volumeCache = playbackManager.GetVolume();
                    ParentWindow.SetStatus("-5 Volume");
                }
                catch (Exception e)
                {
                    ParentWindow.SetStatus("No audio loaded");
                }
                Thread t = new Thread(() =>
                {
                    Thread.Sleep(1000);
                    ParentWindow.SetStatus("");
                });
                t.Start();
            }, "Volume Down");
            RegisterKeyboardControl(ConsoleKey.W, () =>
            {
                if (playbackManager == null)
                {
                    ParentWindow.SetStatus("No audio loaded");
                    return;
                }

                if (showWaveform)
                {
                    waveformGrid.AnimateClose(() =>
                    {
                        waveformGrid.RenderThreadDeleteMe = true;
                        showWaveform = false;
                    });
                    return;
                }

                showWaveform = true;

                Waveform wf = new Waveform(playbackManager)
                {
                    Name = "MPAudioWaveform",
                    ShowBorder = false
                };
                wf.RegisterKeyboardControl(ConsoleKey.W, () =>
                {
                    waveformGrid.RenderThreadDeleteMe = true;
                    showWaveform = false;
                }, "Open File");

                //var AbsPos = GetAbsolutePosition();
                var AbsRect = GetRootParentRelativeDimensions();

                if (AbsRect.Y.Contains("%"))
                {
                    var numY = int.Parse(AbsRect.Y.Replace("%", ""));
                    var numWidth = int.Parse(AbsRect.Width.Replace("%", ""));
                    var numHeight = int.Parse(AbsRect.Height.Replace("%", ""));
                    if (numY > 50 )
                    {
                        int statusBlock = (int)Math.Round(((Console.WindowHeight - 3.0f) / Console.WindowHeight * 100.0f));
                        int remaining = (statusBlock) - numY;
                        int h = numWidth * 2 > 99 ? remaining - 10 : numWidth * 2;
                        int rx = int.Parse(AbsRect.X.Replace("%", ""));
                        int ry = numY - h;
                        waveformGrid = new GridContainer(1, true, 1)
                        {
                            X = $"{rx}",
                            Y = $"{ry}",
                            Width = AbsRect.Width,
                            Height = $"{h}%",
                        };
                    }
                    else if (numY <= 50)
                    {
                        int statusBlock = (int)Math.Round(((Console.WindowHeight - 6.0f) / Console.WindowHeight * 100.0f));
                        int remaining = (statusBlock) - (numY + numHeight);
                        int h = numHeight * 2 > remaining ? remaining : numHeight * 2;
                        if (h + 10 < numHeight)
                        {
                            h += 10;
                        }
                        int rx = int.Parse(AbsRect.X.Replace("%", ""));
                        int ry = numY + int.Parse(AbsRect.Height.Replace("%", "")) - 1;
                        waveformGrid = new GridContainer(1, true, 1)
                        {
                            X = $"{rx}%",
                            Y = $"{ry}%",
                            Width = $"{AbsRect.Width}%",
                            Height = $"{h}%",
                        };
                    }
                }

                waveformGrid.AddElement(wf, 0, 0);
                ParentWindow.AddElement(waveformGrid);
            }, "Open Waveform Display");
            RegisterKeyboardControl(ConsoleKey.O, () =>
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
                    FilePicker picker = new FilePicker(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))
                    {
                        ShowBorder = false,
                        Name = "MPFilePicker"
                    };
                    picker.OnFileSelected += () =>
                    {
                        // On file select, close picker
                        filepickergrid.AnimateClose(() =>
                        {
                            filename = Path.GetFileName(picker.Path);
                            // Remove File Picker
                            filepickergrid.RenderThreadDeleteMe = true;

                            // Setup the audio manager based on the audio selected
                            if (playbackManager != null)
                            {
                                playbackManager.Dispose();
                                if (showWaveform)
                                {
                                    if (waveformGrid != null) 
                                    { 
                                        Waveform wf = new Waveform(playbackManager)
                                        {
                                            Name = "MPAudioWaveform",
                                            ShowBorder = false
                                        };
                                        wf.RegisterKeyboardControl(ConsoleKey.W, () =>
                                        {
                                            waveformGrid.RenderThreadDeleteMe = true;
                                            showWaveform = false;
                                        }, "Open File");
                                        waveformGrid.Children.Clear();
                                        waveformGrid.AddElement(wf, 0, 0);
                                    }

                                }
                            }

                            Task.Run(() =>
                            {
                                try
                                {
                                    playbackManager = new AudioPlaybackManager(picker.Path);
                                    playbackManager.SetVolume(volumeCache);
                                    trackMetadata = new ATL.Track(picker.Path);
                                }
                                catch (Exception ex)
                                {
                                    ParentWindow.SetStatus("File Loading Error!");
                                }
                            });
                        });

                    };
                    filepickergrid.AddElement(picker, 0, 0);
                    ParentWindow.AddElement(filepickergrid, true);
                    picker.Show();
                }
                catch (Exception e)
                {
                    ParentWindow.SetStatus("File Picker Error!");
                }
            }, "Open File");
        }

        protected override void RenderContent(ConsoleBuffer buffer)
        {
            if (showControls)
            {
                RenderControlsView(buffer);
            }
            else if (playbackManager == null)
            {
                RenderMissingFileView(buffer);
            }
            else
            {
                RenderPlayerView(buffer);
            }
        }
        private void RenderMissingFileView(ConsoleBuffer buffer)
        {
            int width = ActualWidth - 2;  // Account for borders
            int height = ActualHeight - 1 > 7 ? ActualHeight - 1 : 7;
            int bumpX = ShowBorder ? ActualX + 1 : ActualX;
            int bumpY = ShowBorder ? ActualY + 1 : ActualY;
            string playStatus = "No song loaded!";
            string helpText = "Press O to open the file picker";
            buffer.WriteString(bumpX, bumpY, CenterText(playStatus, width), Foreground, Background);
            buffer.WriteString(bumpX, bumpY, CenterText(helpText, width), Foreground, Background);
            
            string controlsHint = "Press C for Controls";
            buffer.WriteString(bumpX, height - 1, CenterText(controlsHint, width), Color.DarkGray, Background);

        }

        private void RenderPlayerView(ConsoleBuffer buffer)
        {
            if (playbackManager.GetPosition() >= playbackManager.Duration)
            {
                playbackManager.Pause();
                playbackManager.Dispose();
                playbackManager = null;
            }

            int width = ActualWidth - 2;  // Account for borders
            int height = ActualHeight - 1 > 7 ? ActualHeight - 1 : 7;
            int bumpX = ShowBorder ? ActualX + 1 : ActualX; 
            int bumpY = ShowBorder ? ActualY + 1 : ActualY;

            // Playback Status and Volume (Next line)
            string playStatus = trackMetadata != null ? "Now Playing:" : $"Loading [{filename}]";
            buffer.WriteString(ActualX, bumpY, playStatus, Foreground, Background);

            // Track Info Section
            string trackName = trackMetadata != null && !String.IsNullOrEmpty(trackMetadata.Title) ? $"╟{trackMetadata.Title}" : $"╟Loading...";
            string artistName = trackMetadata != null && !String.IsNullOrEmpty(trackMetadata.Artist) ? $"╟{BoxHorizontal}{trackMetadata.Artist}" : $"╟{BoxHorizontal}Loading...";
            string Line1 = $"╙{BoxHorizontal}{BoxHorizontal}{BoxHorizontal}{BoxHorizontal}{BoxTopRight}";
            string Line2 = $"     ╧";

            buffer.WriteString(bumpX, bumpY + 1, trackName, Foreground, Background);
            buffer.WriteString(bumpX, bumpY + 2, artistName, Foreground, Background);
            buffer.WriteString(bumpX, bumpY + 3, Line1, Foreground, Background);
            buffer.WriteString(bumpX, bumpY + 4, Line2, Foreground, Background);

            // Progress Bar
            int progressY = bumpY + 5;
            var curTimeSpan = playbackManager != null ? playbackManager.GetPosition() : TimeSpan.MinValue;
            var totalTimeSpan = playbackManager != null ? playbackManager.GetDuration() : TimeSpan.MinValue;
            string currentTime = curTimeSpan.ToString("hh\\:mm\\:ss");
            string totalTime = totalTimeSpan.ToString("hh\\:mm\\:ss");
            float progress = (float)curTimeSpan.Ticks / totalTimeSpan.Ticks;
            progress = Math.Clamp(progress, 0f, 1f);

            // Draw time markers
            buffer.WriteString(bumpX, progressY, currentTime, Foreground, Background);
            buffer.WriteString(width - totalTime.Length + 1, progressY, totalTime, Foreground, Background);

            // Draw progress bar
            int barStart = currentTime.Length + bumpX;
            int barWidth = width - totalTime.Length - currentTime.Length - 3;
            int progressPos = (int)(barWidth * progress);

            for (int i = 0; i < barWidth; i++)
            {
                char progressChar = i < progressPos ? '█' : '░';
                buffer.SetPixel(barStart + i, progressY, progressChar, Foreground, Background);
            }

            // Volume Bar
            int volumeY = bumpY + 6;
            float totalVol = 1.0f;
            float volumePercent = playbackManager != null ? playbackManager.GetVolume() : volumeCache;
            char volChar = GetPixelChar(volumePercent);
            string volumePercentString = $"{(volumePercent * 100).ToString("0")}% {volChar.ToString()}".ToString(); 

            // Draw time markers
            buffer.WriteString(width - volumePercentString.Length, bumpY, volumePercentString, Foreground, Background);

            // Controls Hint (Bottom)
            string controlsHint = "Press C for Controls";
            buffer.WriteString(bumpX, height - 1, CenterText(controlsHint, width), Color.DarkGray, Background);
        }
        public char GetPixelChar(double percentage)
        {
            string AsciiTable = "▁▂▃▄▅▆▇█";
            int index = (int)Math.Floor(AsciiTable.Length * percentage);

            if (index >= AsciiTable.Length)
            {
                index = AsciiTable.Length - 1;
            }

            char character = AsciiTable[index];
            return character;
        }

        private void RenderControlsView(ConsoleBuffer buffer)
        {
            int width = ActualWidth - 2;
            int startY = ActualY;

            string[] controls = {
                "Controls:",
                "",
                "Space    - Play/Pause             ",
                "Left     - -15 Seconds            ",
                "Right    - +15 Seconds            ",
                "Up/Down  - Volume                 ",
                "M        - Mute                   ",
                "O        - Open File              ",
                "W        - Toggle Waveform Display",
                "                                  ",
                "C        - Return to Player       "
            };

            foreach (string control in controls)
            {
                buffer.WriteString(1, startY++, CenterText(control, width), Foreground, Background);
            }
        }

        private string CenterText(string text, int width)
        {
            try
            {
                if (text.Length >= width) return text.Substring(0, width);
                int padding = (width - text.Length) / 2;
                return text.PadLeft(padding + text.Length).PadRight(width);
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}