// Examples are compiled by examples/DocsExamples.csproj.
export const controls = [
{
  slug: "muipage",
  title: "MUIPage",
  group: "Pages",
  summary: "The MUIPage class is used to group UI elements into a page. You can add elements in C# through Children, or use MXML to load the page from a file.",
  code: `var control = new MUIPage { ShowBorder = true, Height = "7" };
control.Children.Add(new TextBlock
{
    Text = "Library",
    ShowBorder = false, Height = "1"
});
control.Children.Add(new Button { Text = "Open library", Y = "2", Height = "3" });`,
  mxml: `<MUIPage ShowBorder="true" Height="7">
  <TextBlock Text="Library" ShowBorder="false" Height="1"/>
  <Button Text="Open library" Y="2" Height="3"/>
</MUIPage>`,
  members: [
    [
      "Children",
      "Contains the elements that belong to this page. An element can only belong to one page or window at a time."
    ],
    [
      "Compile / CompileFile",
      "Builds the page from MXML and returns whether compilation succeeded. If it fails, the previous page is kept."
    ],
    [
      "BackendObjects",
      "Contains application objects that MXML can use for data binding and callbacks. Add these before compiling the page."
    ],
    [
      "CompilerOptions",
      "Allows you to register custom types, choose how they are created, and set the base directory and include limits."
    ]
  ],
  notes: [
    "Each nested MUIPage has its own MXML name scope. You can use the same name in different pages, but names inside one page must be unique.",
    "Children are positioned using their X, Y, Width and Height settings. They are not automatically placed below each other. For a layout with rows and columns, use GridContainer.",
    "MUIPage does not receive focus itself. Focusing a page selects an eligible element inside it."
  ],
  related: [
    "mxml",
    "ownership",
    "layout"
  ]
},
{
  slug: "textblock",
  title: "TextBlock",
  group: "Text and Input",
  summary: "The TextBlock control displays text in the console. It supports word wrapping, text alignment, and inline colors, making it useful for labels, descriptions, and status messages.",
  code: `var control = new TextBlock
{
    Text = "Now playing\\nExample track",
    Width = "42", Height = "5", ShowBorder = true,
    TextAlignment = Alignment.TopLeft
};`,
  mxml: `<MUIPage ShowBorder="false">
  <TextBlock Text="Now playing&#10;Example track"
             Width="42" Height="5" ShowBorder="true"
             TextAlignment="TopLeft"/>
</MUIPage>`,
  members: [
    [
      "Text",
      "The text that will be displayed. Text wraps to fit the available width, including long words and blank lines."
    ],
    [
      "TextAlignment",
      "Controls where the text is placed inside the TextBlock. This is separate from XYAlignment, which positions the control itself."
    ],
    [
      "SizeBasedOnText",
      "Resizes the control to fit its text while respecting MinWidth, MinHeight, MaxWidth and MaxHeight."
    ]
  ],
  notes: [
    "TextBlock is not included in keyboard focus by default. For a simple status message, set ShowBorder to false and Height to 1.",
    "You can insert [Color(18,171,239)] and [ColorReset] in the text to change its color. Supported ANSI SGR color codes also work. Other terminal escape commands are not executed."
  ],
  related: [
    "layout",
    "data-binding",
    "textbox"
  ]
},
{
  slug: "textbox",
  title: "TextBox",
  group: "Text and Input",
  summary: "The TextBox control allows users to enter and edit text. You can use it for single-line input, multiline input, or hidden input such as a password.",
  code: `var control = new TextBox
{
    Label = "Display name", Text = "Melon listener",
    Height = "5", Width = "42", MaxLength = 64
};
control.OnEnter = (text, input) => input.Label = $"Hello, {text}";`,
  mxml: `<MUIPage ShowBorder="false">
  <TextBox Label="Display name" Text="Melon listener"
           Height="5" Width="42" MaxLength="64"
           OnEnter="{App.Submit}"/>
</MUIPage>`,
  members: [
    [
      "Text / Label",
      "Text contains the value being edited. Label adds a heading above the input. A labeled TextBox with a border needs at least 5 rows to show the label, separator and input."
    ],
    [
      "OnEnter",
      "Called when the user submits the input. This is an Action<string, TextBox>, receiving the text and the TextBox that submitted it."
    ],
    [
      "OnTextChanged",
      "Called when the user edits the text or Clear is called. Assigning Text in C# does not call this callback."
    ],
    [
      "EnterHit",
      "A parameterless callback for submission. This is useful when you want to add MXML actions without using the submitted value."
    ],
    [
      "Multiline / ReadOnly / MaxLength",
      "Multiline allows line breaks. ReadOnly prevents user edits. MaxLength limits the number of UTF-16 units; 0 means there is no limit."
    ],
    [
      "HideCharacters / HiddenCharacter",
      "HideCharacters masks the input. HiddenCharacter sets the mask character, which is * by default."
    ]
  ],
  keys: [
    [
      "Left / Right, Home / End",
      "Move the cursor; editing/deletion respects text elements."
    ],
    [
      "Enter",
      "Submit in single-line mode; insert a newline in multiline mode."
    ],
    [
      "Shift+Enter",
      "Submit multiline text."
    ],
    [
      "Ctrl+R",
      "Reveal/conceal a focused password; focus loss conceals it."
    ]
  ],
  notes: [
    "In the MXML example, App.Submit is a public method with the signature void Submit(string text, TextBox input). The data binding guide includes the SettingsModel class used here.",
    "To bind the input to a property, set Text=\"{App.Name}\". User edits will then update App.Name.",
    "Hidden input is masked in normal CaptureText output, even if the user has revealed it. You must explicitly set includeSensitive=true to include it in a capture.",
    "TextBox does not currently provide text selection, clipboard, or autocomplete APIs."
  ],
  related: [
    "data-binding",
    "focus",
    "textblock"
  ]
},
{
  slug: "button",
  title: "Button",
  group: "Buttons and Menus",
  summary: "The Button control displays an action that the user can select. When the button is activated, it calls the method assigned to OnPressed.",
  code: `var control = new Button { Text = "Save changes", Width = "32", Height = "3" };
control.OnPressed = () => control.Text = "Saved";`,
  mxml: `<MUIPage ShowBorder="false">
  <Button Text="Save changes" Width="32" Height="3" OnPressed="{App.Save}"/>
</MUIPage>`,
  members: [
    [
      "Text",
      "The label displayed inside the button."
    ],
    [
      "OnPressed",
      "The Action that will be called when the button is activated. It takes no parameters."
    ],
    [
      "PerformClick()",
      "Activates the button from C#. The button must be visible, enabled and unlocked."
    ],
    [
      "Button(ConsoleKey)",
      "Sets an additional activation key when constructing the button. The parameterless constructor uses Enter."
    ]
  ],
  keys: [
    [
      "Enter / Space",
      "Activate the focused button."
    ],
    [
      "Configured key",
      "Activate without modifiers while the button receives input."
    ]
  ],
  notes: [
    "Buttons do not draw a border by default. Their label is centered inside the available area.",
    "A button shortcut is handled when that button receives input. To add a shortcut for the whole window, use ConsoleWindowManager.RegisterKeyboardControl.",
    "Setting IsEnabled=false prevents activation. The enabled and visible state of the parent is also checked."
  ],
  related: [
    "mxml-actions",
    "buttonpanel",
    "focus"
  ]
},
{
  slug: "buttonpanel",
  title: "ButtonPanel",
  group: "Buttons and Menus",
  summary: "The ButtonPanel control arranges buttons into an action bar. Buttons that do not fit are placed in an overflow menu, so the panel can still be used in a smaller console window.",
  code: `var control = new ButtonPanel { Width = "42", Height = "2", MaxLines = 2 };
control.Children.Add(new Button { Text = "Play" });
control.Children.Add(new Button { Text = "Queue" });
control.Children.Add(new Button { Text = "More information" });`,
  mxml: `<MUIPage ShowBorder="false">
  <ButtonPanel Width="42" Height="2" MaxLines="2">
    <Button Text="Play"/>
    <Button Text="Queue"/>
    <Button Text="More information"/>
  </ButtonPanel>
</MUIPage>`,
  members: [
    [
      "Children",
      "The buttons owned by the panel. Add buttons here; Buttons is a read-only list of the current buttons."
    ],
    [
      "MaxLines",
      "The maximum number of rows used for visible buttons. Height must also allow enough rows."
    ],
    [
      "MaxOverflowMenuLines",
      "The number of rows available to the overflow menu."
    ],
    [
      "AddButton / RemoveButton",
      "Adds or removes a button through Children. Removing a button does not dispose it."
    ]
  ],
  keys: [
    [
      "Left / Right",
      "Select the next visible action."
    ],
    [
      "Enter / Space",
      "Activate the selected button."
    ],
    [
      "Backtick",
      "Open or close overflow."
    ]
  ],
  notes: [
    "ButtonPanel appears as one item when moving focus with Tab. Left and Right select buttons inside the panel.",
    "The overflow menu is opened as a modal popup. It is cleaned up when the panel is detached. A button that has been removed, hidden, disabled or disposed cannot be activated through an old overflow menu."
  ],
  related: [
    "button",
    "overflowmenu",
    "focus"
  ]
},
{
  slug: "optionsmenu",
  title: "OptionsMenu",
  group: "Buttons and Menus",
  summary: "The OptionsMenu control displays a list of choices. Each choice is a MenuItem containing its label, callback and enabled state.",
  code: `var control = new OptionsMenu { MenuName = "Library", Width = "42", Height = "7" };
control.Options.Add(new MenuItem("Albums", () => { }));
control.Options.Add(new MenuItem("Artists", () => { }));
control.Options.Add(new MenuItem("Downloads", null) { IsEnabled = false });`,
  mxml: `<MUIPage ShowBorder="false">
  <OptionsMenu MenuName="Library" Width="42" Height="7">
    <OptionsMenu.Options>
      <MenuItem Option="Albums" OnSelect="{App.Save}"/>
      <MenuItem Option="Artists"/>
      <MenuItem Option="Downloads" IsEnabled="false"/>
    </OptionsMenu.Options>
  </OptionsMenu>
</MUIPage>`,
  members: [
    [
      "Options",
      "The List<MenuItem> displayed by the menu. MenuItem objects go in this property rather than Children."
    ],
    [
      "MenuItem.Option / OnSelect / IsEnabled",
      "Option is the label, OnSelect is the Action to call, and IsEnabled controls whether the item can be selected."
    ],
    [
      "SelectedIndex",
      "The current selection. This is -1 when the menu is empty, and is adjusted when the options change."
    ],
    [
      "MenuName / SelectedForeground / SelectedBackground",
      "MenuName is the heading displayed inside the menu. The color properties control the selected item."
    ]
  ],
  keys: [
    [
      "Up / Down",
      "Wrap through enabled choices."
    ],
    [
      "Home / End, PageUp / PageDown",
      "Navigate to an edge or scroll by a page."
    ],
    [
      "Enter / Right",
      "Activate the selected item."
    ],
    [
      "Letters / digits",
      "Cycle matching caption prefixes."
    ]
  ],
  notes: [
    "You can add or remove options while the menu is in use, including leaving it empty. Change the list on the UI thread while the control is attached.",
    "MenuItem is a data object, not a UIElement. Its OnSelect callback takes no parameters and runs on the UI thread."
  ],
  related: [
    "data-binding",
    "focus",
    "overflowmenu"
  ]
},
{
  slug: "overflowmenu",
  title: "OverflowMenu",
  group: "Buttons and Menus",
  summary: "The OverflowMenu class is an OptionsMenu that displays actions from a list of buttons. ButtonPanel uses it automatically when some of its buttons do not fit.",
  code: `var actions = new List<Button>
{
    new() { Text = "Play next" },
    new() { Text = "Add to playlist" }
};
var control = new OverflowMenu(actions) { Width = "42", Height = "5" };`,
  setup: "page.CompilerOptions.Factories.Add(typeof(OverflowMenu), () => new OverflowMenu(new List<Button> { new() { Text = \"Play next\" }, new() { Text = \"Add to playlist\" } }));",
  mxml: `<MUIPage ShowBorder="false">
  <OverflowMenu Width="42" Height="5"/>
</MUIPage>`,
  members: [
    [
      "OverflowMenu(List<Button>)",
      "Creates a menu using the supplied buttons. Since there is no parameterless constructor, direct MXML use requires a factory."
    ],
    [
      "MaxOverflowMenuLines",
      "A configuration value for the menu. ButtonPanel uses its own setting when choosing the size of its popup."
    ]
  ],
  notes: [
    "Normally you only need to create a ButtonPanel and let it manage overflow. The example below shows how to create an OverflowMenu directly if you need one.",
    "The menu refers to the source buttons without taking ownership of them. Keep those buttons alive, and do not move them to a different owner while the menu is open.",
    "The inherited OptionsMenu controls are used to navigate and select an action. If you open the menu as a popup yourself, you are also responsible for closing or disposing the popup handle."
  ],
  related: [
    "buttonpanel",
    "optionsmenu",
    "ownership"
  ]
},
{
  slug: "colorpicker",
  title: "ColorPicker",
  group: "Text and Input",
  summary: "The ColorPicker control allows users to edit the red, green and blue values of a color. It shows the original color alongside the current color, so changes can be previewed before they are saved.",
  code: `var control = new ColorPicker { Width = "54", Height = "8" };
control.BeginEdit(Color.FromArgb(18, 171, 239));
control.Confirmed += color => { /* persist the chosen color */ };`,
  mxml: `<MUIPage ShowBorder="false">
  <ColorPicker Width="54" Height="8" ColorValue="#12ABEF"
               Confirmed="{App.SaveColor}"/>
</MUIPage>`,
  members: [
    [
      "ColorValue / OriginalColor",
      "ColorValue is the color being edited. OriginalColor is the value that Cancel will restore. Alpha is preserved but cannot be edited here."
    ],
    [
      "BeginEdit(Color)",
      "Sets the starting color and resets the edit state. Attaching the picker also records its starting color."
    ],
    [
      "TrySetHex(string)",
      "Sets a color from six hex digits, with or without a leading #. Returns false if the input is invalid."
    ],
    [
      "Commit() / Cancel()",
      "Commit accepts the current color as the new starting value. Cancel restores the original color. Each method calls its corresponding event."
    ],
    [
      "ColorChanged / Confirmed / Cancelled",
      "Action<Color> events for previewing changes and handling completion."
    ]
  ],
  keys: [
    [
      "Up / Down",
      "Choose R, G or B."
    ],
    [
      "Left / Right; Shift+Left / Right",
      "Adjust by one with bounded repeat acceleration; Shift uses ten."
    ],
    [
      "Home / End, digits",
      "Set 0/255 or type a channel value."
    ],
    [
      "Enter / Escape",
      "Commit / cancel. The host decides whether the dialog closes."
    ]
  ],
  notes: [
    "RgbColorPicker is another name for the same control. You can use either name in C# or MXML.",
    "Commit and Cancel do not close the control. If you are using a popup, close or dispose its handle from the Confirmed and Cancelled callbacks."
  ],
  related: [
    "rgbcolorpicker",
    "popups",
    "data-binding"
  ]
},
{
  slug: "rgbcolorpicker",
  title: "RgbColorPicker",
  group: "Text and Input",
  summary: "The RgbColorPicker class provides the ColorPicker control under a name that makes its RGB behavior explicit. It uses the same properties, keyboard controls and events.",
  code: `var control = new RgbColorPicker { Width = "54", Height = "8" };
control.BeginEdit(Color.FromArgb(80, 210, 130));
control.ColorChanged += color => { /* update a live theme preview */ };
control.Confirmed += color => { /* save and close your popup */ };`,
  mxml: `<MUIPage ShowBorder="false">
  <RgbColorPicker Width="54" Height="8" ColorValue="#50D282"
                  ColorChanged="{App.PreviewColor}" Confirmed="{App.SaveColor}"/>
</MUIPage>`,
  members: [
    [
      "ColorValue",
      "The System.Drawing.Color being edited. MXML accepts named colors, RGB or ARGB values, and hex colors."
    ],
    [
      "Confirmed / Cancelled",
      "Called with the accepted or restored color. You can use these events to save a setting and close a popup."
    ]
  ],
  notes: [
    "See the ColorPicker documentation for the full list of properties and keyboard controls. RgbColorPicker inherits that behavior without changing it.",
    "App.PreviewColor and App.SaveColor in this example are public methods taking a Color parameter. The data binding guide shows the SettingsModel class and how to register it."
  ],
  related: [
    "colorpicker",
    "popups",
    "data-binding"
  ]
},
{
  slug: "filepicker",
  title: "FilePicker",
  group: "Text and Input",
  summary: "The FilePicker control allows users to browse for a file or directory. It keeps its own current directory, so using the picker does not change the working directory of your application.",
  code: `var control = new FilePicker(sampleDirectory)
{
    Width = "54", Height = "8", SelectionMode = FilePickerMode.File
};
control.Extensions.Add(".mxml");
control.Refresh();
control.SelectionConfirmed += path => { /* load the selected document */ };`,
  setup: "page.CompilerOptions.Factories.Add(typeof(FilePicker), () => { var picker = new FilePicker(sampleDirectory); picker.Extensions.Add(\".mxml\"); picker.Refresh(); return picker; });",
  mxml: `<MUIPage ShowBorder="false">
  <FilePicker Width="54" Height="8" SelectionMode="File"
              SelectionConfirmed="{App.OpenFile}"/>
</MUIPage>`,
  members: [
    [
      "SelectionMode",
      "Controls whether users can select a File, Directory, or FileOrDirectory."
    ],
    [
      "Extensions",
      "A case-insensitive set of file extensions, including the dot. An empty set allows all files. Call Refresh after changing the filter."
    ],
    [
      "NavigateTo / Refresh / NavigateToParent",
      "Changes or refreshes the directory being browsed. These return whether the operation succeeded; ErrorMessage describes a failure."
    ],
    [
      "Path / CurrentDirectory / SelectedEntry",
      "Path is the last confirmed selection. CurrentDirectory is the folder being browsed, and SelectedEntry is the highlighted item. Setting Path does not navigate."
    ],
    [
      "SelectionConfirmed / Cancelled",
      "SelectionConfirmed receives the selected path. Cancelled is a parameterless event for closing or handling cancellation."
    ],
    [
      "OnFileSelected / OnDirectorySelected",
      "Parameterless callback properties. Read Path from the picker to get the selection."
    ]
  ],
  keys: [
    [
      "Enter / Right",
      "Open a directory; Enter confirms a file."
    ],
    [
      "Space / F2",
      "Select a directory / the current directory when the mode permits it."
    ],
    [
      "Backspace / Left, F5",
      "Go up / refresh."
    ],
    [
      "Up / Down, Home / End, Page keys",
      "Navigate the list; typing cycles matching prefixes."
    ]
  ],
  notes: [
    "sampleDirectory is a path provided by your application. This example uses a temporary directory containing Layout.mxml and a Pages folder. The displayed path is shortened to /demo in the output example.",
    "The MXML example uses a factory to set the starting directory and extension filter before the control is created.",
    "Directory enumeration is synchronous. If you need to browse slow, remote or very large directories, you should provide your own asynchronous loading UI. FilePicker is not a filesystem permission boundary."
  ],
  related: [
    "popups",
    "mxml",
    "ownership"
  ]
},
{
  slug: "progressbar",
  title: "ProgressBar",
  group: "Displaying Information",
  summary: "The ProgressBar control displays progress for an operation. It can show a percentage and label, or use a loading animation when you do not know how much work remains.",
  code: `var control = new ProgressBar
{
    Text = "Scanning library", ProgressValue = 0.65f,
    ShowPercentage = true, Style = ProgressBar.ProgressBarStyle.Solid,
    Width = "42", Height = "4"
};`,
  mxml: `<MUIPage ShowBorder="false">
  <ProgressBar Text="Scanning library" ProgressValue="0.65" ShowPercentage="true"
               Style="Solid" Width="42" Height="4"/>
</MUIPage>`,
  members: [
    [
      "ProgressValue",
      "The progress as a value between 0 and 1. For example, 0.65f represents 65%. Invalid values are clamped for display without changing the bound property."
    ],
    [
      "Style",
      "The display style: Solid, Segmented, Dotted, Blocks, Ascii or Loading."
    ],
    [
      "Text / ShowPercentage",
      "The label and percentage displayed above the bar. A bordered control needs at least 4 rows to show both the label and bar."
    ],
    [
      "AnimateProgress",
      "Smooths the displayed progress towards the requested value using elapsed time."
    ],
    [
      "ProgressColor / EmptyColor",
      "The colors used for the completed and remaining parts of the bar."
    ]
  ],
  notes: [
    "Loading is an indeterminate animation. It does not need to know the operation's actual progress.",
    "ProgressBar only displays the value you give it; it does not run the operation. If the operation is on another thread, update the UI using Post or InvokeAsync."
  ],
  related: [
    "worklog",
    "data-binding",
    "window"
  ]
},
{
  slug: "worklog",
  title: "WorkLog",
  group: "Displaying Information",
  summary: "The WorkLog control displays the progress of multiple jobs. Each job is a WorkItem with its own label, status, item counts and running time.",
  code: `var control = new WorkLog
{
    Width = "54", Height = "7", ShowTime = false,
    ShowItemCount = true, ShowProgressBar = true
};
var done = new WorkItem("scan", "Read tags", 120, false) { CompletedItems = 120 };
done.Complete();
control.Tasks.Add(done);
control.Tasks.Add(new WorkItem("index", "Link artists", 60, false));`,
  mxml: `<MUIPage ShowBorder="false">
  <WorkLog Width="54" Height="7" ShowTime="false"
           ShowItemCount="true" ShowProgressBar="true">
    <WorkLog.Tasks>
      <WorkItem ElmName="scan" Label="Read tags" TotalItems="120"
                CompletedItems="120" Status="Completed"/>
      <WorkItem ElmName="index" Label="Link artists" TotalItems="60"/>
    </WorkLog.Tasks>
  </WorkLog>
</MUIPage>`,
  members: [
    [
      "Tasks",
      "The LockedList<WorkItem> shown by the control. WorkItem objects belong in this list rather than Children."
    ],
    [
      "WorkItem.Run / Wait / Complete / Error",
      "Changes the state of a job. The stopwatch starts when a job runs and stops when it is no longer running."
    ],
    [
      "ShowStatus / ShowTime / ShowItemCount / ShowProgressBar",
      "Controls which details are displayed for each job."
    ],
    [
      "FollowRunning / VisibleStartIndex",
      "FollowRunning keeps a running job visible. Set it to false to choose the starting row with VisibleStartIndex."
    ],
    [
      "GetItemByName",
      "Finds a job using its ElmName."
    ]
  ],
  notes: [
    "ElmName identifies the job; Label is the text displayed to the user.",
    "The list supports snapshot rendering, but WorkItem itself is mutable. Update job state on the UI thread while the WorkLog is attached.",
    "WorkLog does not execute jobs or handle their exceptions. Your application is responsible for running the work and updating the status."
  ],
  related: [
    "progressbar",
    "window",
    "data-binding"
  ]
},
{
  slug: "waveform",
  title: "Waveform",
  group: "Displaying Information",
  summary: "The Waveform control displays peak amplitudes supplied by your application. It can also highlight how far playback has progressed through the data.",
  code: `var control = new Waveform
{
    WaveformData = new float[] { .1f, .3f, .8f, .5f, 1f, .6f, .2f, .4f },
    ProgressValue = .4f, ScaleAmmount = 1,
    Width = "42", Height = "7"
};`,
  mxml: `<MUIPage ShowBorder="false">
  <Waveform WaveformData="{App.Peaks}"
            ProgressValue="0.4" ScaleAmmount="1" Width="42" Height="7"/>
</MUIPage>`,
  members: [
    [
      "WaveformData",
      "The float[] containing the peaks. Negative values are displayed using their magnitude, and nonfinite samples are ignored."
    ],
    [
      "ScaleAmmount",
      "Scales the displayed amplitude. Note that the current property name is spelled ScaleAmmount."
    ],
    [
      "ProgressValue",
      "The playback position as a value between 0 and 1. Completed cells use FocusedForeground."
    ]
  ],
  notes: [
    "Waveform only draws data. It does not decode music files, play audio, or manage audio devices.",
    "App.Peaks in the MXML example is a public float[] property. Bind an array from your backend; a comma-separated string does not create a float array.",
    "The data is resampled to fit the available width. The output example does not show the progress color because it is a plain text capture."
  ],
  related: [
    "progressbar",
    "layout",
    "custom-controls"
  ]
},
{
  slug: "line",
  title: "Line",
  group: "Displaying Information",
  summary: "The Line control draws a line between two points inside its own area. It can be used for dividers or simple horizontal, vertical and diagonal drawing.",
  code: `var control = new Line
{
    Width = "42", Height = "7", X1 = "0", Y1 = "100%",
    X2 = "100%", Y2 = "0", AntiAliased = false,
    Foreground = Color.FromArgb(80, 210, 130)
};`,
  mxml: `<MUIPage ShowBorder="false">
  <Line Width="42" Height="7" X1="0" Y1="100%" X2="100%" Y2="0"
        AntiAliased="false" Foreground="#50D282"/>
</MUIPage>`,
  members: [
    [
      "X1 / Y1 / X2 / Y2",
      "The start and end points, using cell counts or percentages of the content area. Endpoints are kept inside the control."
    ],
    [
      "AntiAliased / Intensity / Softness",
      "Controls edge smoothing, intensity and softness."
    ],
    [
      "DoubleWidth",
      "Draws an additional cell to the right of each point when there is room."
    ]
  ],
  notes: [
    "Line has no border and is not included in focus by default. For a horizontal divider, Height=1 is enough.",
    "All coordinates are relative to the control. They are not absolute console positions.",
    "The default drawing behavior preserves the background underneath the line."
  ],
  related: [
    "consolebuffer",
    "layout",
    "custom-controls"
  ]
},
{
  slug: "fpscounter",
  title: "FPSCounter",
  group: "Displaying Information",
  summary: "The FPSCounter control shows how many frames the window is actually presenting. It subscribes to the window when attached and removes that subscription when detached.",
  code: `var control = new FPSCounter { Width = "9", Height = "4" };`,
  mxml: `<MUIPage ShowBorder="false">
  <FPSCounter Width="9" Height="4"/>
</MUIPage>`,
  members: [
    [
      "GetFPS()",
      "Returns the most recent completed FPS measurement."
    ],
    [
      "OnFrameRendered",
      "The frame event handler. Normal use does not require you to subscribe this manually."
    ]
  ],
  notes: [
    "Add the control to the page and it will manage its own event subscription. Removing and reattaching it does not add duplicate subscriptions.",
    "The output example shows 000 because it was captured without running the console window.",
    "TargetFrameRate sets how often the window tries to update. GetFPS reports actual presented frames, which can differ from that setting."
  ],
  related: [
    "window",
    "ownership",
    "custom-controls"
  ]
},
{
  slug: "gridcontainer",
  title: "GridContainer",
  group: "Layout Containers",
  summary: "The GridContainer control divides its area into rows and columns. Each row can have a different number of columns, allowing you to build layouts without assigning a position to every control.",
  code: `var control = new GridContainer(2, false, 2, 1)
{
    Width = "42", Height = "9", ShowBorder = true
};
control.AddElement(new TextBlock { Text = "Albums", ShowBorder = true }, 0, 0);
control.AddElement(new TextBlock { Text = "Artists", ShowBorder = true }, 0, 1);
control.AddElement(new TextBlock { Text = "Now playing", ShowBorder = true }, 1, 0);`,
  setup: "page.CompilerOptions.Factories.Add(typeof(GridContainer), () => new GridContainer(2, false, 2, 1));",
  mxml: `<MUIPage ShowBorder="false">
  <GridContainer Width="42" Height="9" ShowBorder="true">
    <TextBlock Text="Albums" Grid.Row="0" Grid.Column="0"/>
    <TextBlock Text="Artists" Grid.Row="0" Grid.Column="1"/>
    <TextBlock Text="Now playing" Grid.Row="1" Grid.Column="0"/>
  </GridContainer>
</MUIPage>`,
  members: [
    [
      "GridContainer(rows, animateExpansion, columnsPerRow)",
      "Creates the grid. Each row needs a positive column count. The parameterless constructor creates one row with one column."
    ],
    [
      "AddElement / SetCell",
      "Adds an element at a zero-based row and column, or changes the cell of a child already in the grid. Children.Add initially places an element at (0,0)."
    ],
    [
      "Rows / ColumnsPerRow",
      "The row and column counts selected in the constructor. These cannot be changed afterwards."
    ],
    [
      "AnimateOpen / AnimateClose",
      "Opens or closes the grid with a short animation. AnimateClose can call a final action after closing."
    ]
  ],
  notes: [
    "The factory shown below is needed for this MXML example because row and column counts are constructor settings, not writable properties.",
    "Children keep their own size constraints inside the assigned cell. Removing a child also removes its cell placement.",
    "Cells fill the available space, including any extra cells left over from integer division."
  ],
  related: [
    "layout",
    "ownership",
    "mxml"
  ]
},
{
  slug: "queuecontainer",
  title: "QueueContainer",
  group: "Layout Containers",
  summary: "The QueueContainer control displays the first visible element in a queue, with a small stack behind it to show that more elements are waiting.",
  code: `var control = new QueueContainer
{
    Width = "42", Height = "7", StackDirection = Direction.Right,
    MaxStackDisplaySize = 3
};
control.QueueElement(new TextBlock { Text = "First card", ShowBorder = true });
control.QueueElement(new TextBlock { Text = "Second card", ShowBorder = true });
control.QueueElement(new TextBlock { Text = "Third card", ShowBorder = true });`,
  mxml: `<MUIPage ShowBorder="false">
  <QueueContainer Width="42" Height="7" StackDirection="Right" MaxStackDisplaySize="3">
    <TextBlock Text="First card"/>
    <TextBlock Text="Second card"/>
    <TextBlock Text="Third card"/>
  </QueueContainer>
</MUIPage>`,
  members: [
    [
      "CurrentElement",
      "The first visible child. Only that child is used for child focus and input."
    ],
    [
      "QueueElement / InsertElementAt",
      "Adds a child to the queue or inserts it at a chosen index."
    ],
    [
      "PopElement()",
      "Removes and returns the first child without disposing it. Returns null when the queue is empty."
    ],
    [
      "RemoveElementAt",
      "Removes a child at the specified index without disposing it."
    ],
    [
      "StackDirection / MaxStackDisplaySize",
      "Sets the direction and maximum depth of the stack preview."
    ]
  ],
  notes: [
    "PopElement removes the first child in the collection, even if it is hidden. In that case it can differ from CurrentElement, which uses the first visible child.",
    "You can reuse a popped element by adding it to another owner. Dispose it when you no longer need it.",
    "QueueContainer displays queued UI cards. It does not implement an audio play queue or automatically bind a key to advance to the next card."
  ],
  related: [
    "ownership",
    "layout",
    "focus"
  ]
}
];
