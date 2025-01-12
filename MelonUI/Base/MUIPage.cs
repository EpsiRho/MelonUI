#define DEBUG
using MelonUI.Attributes;
using MelonUI.Enums;
using MelonUI.Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Pastel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace MelonUI.Base
{
    public partial class MUIPage : UIElement
    {
        [Binding]
        public string mXMLFilePath;
        public string MXML { get; set; }
        private readonly List<Assembly> Assemblies = new();
        private readonly List<string> Namespaces = new();
        public bool Failed = false;
        public List<CompilerMessage> CompilerMessages = new();
        public CompilerMessage CompilerFocusedMessage = new CompilerMessage("", MessageSeverity.Debug);
        public List<string> Backends = new();
        public Dictionary<string, object> BackendObjects = new();
        public Dictionary<Type, Func<object, object>> TypeExtensions = new();
        private Dictionary<string, ConsoleBuffer> BufferCache = new();
        public TimeSpan CompilationFinished = TimeSpan.MinValue;
        //public MessageSeverity CompilerVerbosity = MessageSeverity.Debug | MessageSeverity.Info | MessageSeverity.Warning | MessageSeverity.Error  | MessageSeverity.Success;

        private int zCounter = 0;

        public MUIPage()
        {
            ConsiderForFocus = false;
        }


        // Public Methods
        /// <summary>
        /// Reads the file’s text content and compiles the MXML.
        /// </summary>
        public bool CompileFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                var msg = new CompilerMessage($"File \"{filePath}\" does not exist.", MessageSeverity.Error);
                CompilerMessages.Add(msg);
                CompilerFocusedMessage = msg;
                Failed = true;
                return false;
            }
            try
            {
                string xml = File.ReadAllText(filePath);
                CompilerMessages.Add(new CompilerMessage($"Loaded file \"{filePath}\" for compilation.", MessageSeverity.Info));
                MXMLFilePath = filePath;
                return Compile(xml);
            }
            catch (Exception)
            {
                var msg = new CompilerMessage($"Failed to read file \"{filePath}\".", MessageSeverity.Error);
                CompilerMessages.Add(msg);
                CompilerFocusedMessage = msg;
                Failed = true;
                return false;
            }
        }

        /// <summary>
        /// Main entry point for compiling an MXML string.
        /// </summary>
        public bool Compile(string mxmlString)
        {
            zCounter = Z;
            Failed = false;
            MXML = mxmlString;
            CompilerMessages.Clear();
            Children.Clear();
            CompilerMessages.Add(new CompilerMessage($"Begining Compilation.", MessageSeverity.Info));
            CompilerMessages.Add(new CompilerMessage($"Starting Z offset: {Z}.", MessageSeverity.Debug));

            XElement root;
            try
            {
                CompilerMessages.Add(new CompilerMessage($"Parsing XML.", MessageSeverity.Info));
                //root = XElement.Parse(mxmlString);

                var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
                using var stringReader = new StringReader(mxmlString);
                using var xmlReader = XmlReader.Create(stringReader, settings);
                root = XElement.Load(xmlReader, LoadOptions.SetLineInfo);
                CompilerMessages.Add(new CompilerMessage($"XML Parsed Successfully.", MessageSeverity.Success));

            }
            catch (Exception e)
            {
                var msg = new CompilerMessage($"Generic XML formatting error, see following line:\n{e.Message}", MessageSeverity.Error);
                CompilerMessages.Add(msg);
                CompilerFocusedMessage = msg;
                Failed = true;
                CompilationFinished = DateTime.Now.Subtract(CompilerMessages.First().DateTime);
                return false;
            }


            // Process top-level attributes (Namespaces, Assemblies, Backends, etc.)
            CompilerMessages.Add(new CompilerMessage($"Parsing Top-Level Attributes.", MessageSeverity.Info, GetLineNumber(root)));
            if (!ParseTopLevelAttributes(root))
            {
                CompilerMessages.Add(new CompilerMessage($"Top-Level MUIPage Attributes failed to compile!", MessageSeverity.Error, GetLineNumber(root)));
                CompilationFinished = DateTime.Now.Subtract(CompilerMessages.First().DateTime);
                return false;
            }
            CompilerMessages.Add(new CompilerMessage($"Top-Level Attributes Parsed", MessageSeverity.Success, GetLineNumber(root)));

            // Compiler Debug Info
            CompilerMessages.Add(new CompilerMessage($"Loaded {Assemblies.Count} Assemblies, {Namespaces.Count} Namespaces, {Backends.Count} Backends with {BackendObjects.Count} Managed Objects.", MessageSeverity.Debug));
            if (Name != null)
            {
                CompilerMessages.Add(new CompilerMessage($"Page Name: {Name}.", MessageSeverity.Debug));
            }
            if (!string.IsNullOrEmpty(Width) && !string.IsNullOrEmpty(Height))
            {
                CompilerMessages.Add(new CompilerMessage($"Width/Height: ({Width},{Height}).", MessageSeverity.Debug));
            }
            else
            {
                CompilerMessages.Add(new CompilerMessage($"Page Compilation will succeed, but you may not see any page if it has no size!\nSet am explicit Width and Height to remove this warning.", MessageSeverity.Warning, GetLineNumber(root)));
            }

            // Parse child elements (UIElements, etc.)
            CompilerMessages.Add(new CompilerMessage($"Parsing Page Elements.", MessageSeverity.Info, GetLineNumber(root)));
            foreach (var child in root.Elements())
            {
                CompilerMessages.Add(new CompilerMessage($"Parsing {child.Name}.", MessageSeverity.Info, GetLineNumber(child)));
                var uiElement = ParseMXMLElement(child); // Parse the element
                if (Failed) // If it failed, return out
                {
                    CompilerMessages.Add(new CompilerMessage($"MXML Element \"{child.Name}\" failed to compile!", MessageSeverity.Error, GetLineNumber(child)));
                    CompilationFinished = DateTime.Now.Subtract(CompilerMessages.First().DateTime);
                    return false;
                }
                if (uiElement != null) AddElement((UIElement)uiElement); // Add the element to the page's children
                if (string.IsNullOrEmpty(((UIElement)uiElement).Width) || string.IsNullOrEmpty(((UIElement)uiElement).Height))
                {
                    CompilerMessages.Add(new CompilerMessage($"{child.Name}'s Width/Height are not set, so this object will not be visible! (If you want this, ideally use IsVisible and pre-set the W/H)", MessageSeverity.Warning, GetLineNumber(child)));
                }
                if (string.IsNullOrEmpty(((UIElement)uiElement).Name))
                {
                    CompilerMessages.Add(new CompilerMessage($"{child.Name}'s object does not have a Name. The object will show and be usable, but will be harder to interact with in C#.", MessageSeverity.Warning, GetLineNumber(child)));
                }
                CompilerMessages.Add(new CompilerMessage($"{child.Name} was added to the MUIPage's Children.", MessageSeverity.Success, GetLineNumber(child)));
            }

            // Reverse so items are in the same order as in MXML
            CompilerMessages.Add(new CompilerMessage($"Reversing Element order to match MXML order.", MessageSeverity.Debug));
            Children.Reverse();

            CompilerMessages.Add(new CompilerMessage($"Compiled {Children.Count} objects in {DateTime.Now.Subtract(CompilerMessages.First().DateTime).TotalSeconds} seconds.", MessageSeverity.Info));
            CompilerMessages.Add(new CompilerMessage($"Noted {CompilerMessages.Where(x => x.Severity == MessageSeverity.Warning).Count()} warnings during compilation.", MessageSeverity.Info));
            CompilerMessages.Add(new CompilerMessage($"Successfully compiled this page!", MessageSeverity.Success));
            CompilerFocusedMessage = CompilerMessages.Last();
            CompilationFinished = DateTime.Now.Subtract(CompilerMessages.First().DateTime);
            return true;
        }

        /// <summary>
        /// Adds a new UIElement to this page's Children list.
        /// </summary>
        public void AddElement(UIElement elm)
        {
            if (elm == null)
            {
                CompilerMessages.Add(new CompilerMessage($"Attempted to add a null element!", MessageSeverity.Error));
                Failed = true;
                return;
            };
            elm.Z = zCounter;
            elm.Parent = this;
            elm.ParentWindow = this.ParentWindow;
            Children.Add(elm);
        }

        /// <summary>
        /// Finds a property reference (Class.Property).
        /// </summary>
        public (object Instance, PropertyInfo Property) FindPropertyReference(string classAndProperty)
        {
            if (string.IsNullOrWhiteSpace(classAndProperty))
            {
                CompilerMessages.Add(new CompilerMessage($"Property Reference expected, but no name found!", MessageSeverity.Error));
                Failed = true;
                CompilerFocusedMessage = CompilerMessages.Last();
                return (null, null);
            }

            var parts = classAndProperty.Split('.');
            if (parts.Length != 2)
            {
                CompilerMessages.Add(new CompilerMessage($"Property Reference format is invalid, should look like \"ClassName.PropertyName\".", MessageSeverity.Error));
                Failed = true;
                CompilerFocusedMessage = CompilerMessages.Last();
                return (null, null);
            }

            string className = parts[0];
            string propertyName = parts[1];
            CompilerMessages.Add(new CompilerMessage($"Looking for type in known assemblies/namespaces: {className}->{propertyName}.", MessageSeverity.Debug));
            Type foundType = null;

            // Try to locate a type in known assemblies/namespaces
            foreach (var asm in Assemblies)
            {
                foreach (var space in Namespaces)
                {
                    var t = asm.GetType($"{space}.{className}", throwOnError: false, ignoreCase: false);
                    if (t != null)
                    {
                        foundType = t;
                        break;
                    }
                }
                if (foundType != null) break;
            }

            // If found type, check static property or instance property
            if (foundType != null)
            {
                CompilerMessages.Add(new CompilerMessage($"Found a type, getting more info.", MessageSeverity.Debug));
                var staticProp = foundType.GetProperty(propertyName,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (staticProp != null)
                {
                    CompilerMessages.Add(new CompilerMessage($"Static type found, {staticProp.Name}.", MessageSeverity.Debug));
                    return (foundType, staticProp);
                }

                if (BackendObjects.TryGetValue(className, out var backendInstance))
                {
                    var instanceProp = backendInstance.GetType().GetProperty(propertyName,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    if (instanceProp != null)
                    {
                        CompilerMessages.Add(new CompilerMessage($"Instance type found, {instanceProp.Name}.", MessageSeverity.Debug));
                        return (backendInstance, instanceProp);
                    }
                }

                CompilerMessages.Add(new CompilerMessage($"Property Reference {classAndProperty} found a type, but could not instance or find the property. (Are you using Fields or Properties? Is your Property Public?).", MessageSeverity.Error));
                Failed = true;
                CompilerFocusedMessage = CompilerMessages.Last();
                return (null, null);
            }
            else
            {
                // No direct type found. Maybe a backend instance.
                CompilerMessages.Add(new CompilerMessage($"No direct type found, searching backend instances.", MessageSeverity.Debug));
                if (BackendObjects.TryGetValue(className, out var backendInstance))
                {
                    var instanceProp = backendInstance.GetType().GetProperty(propertyName,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    if (instanceProp != null)
                    {
                        CompilerMessages.Add(new CompilerMessage($"Instance type found, {instanceProp.Name}.", MessageSeverity.Debug));
                        return (backendInstance, instanceProp);
                    }
                }

                CompilerMessages.Add(new CompilerMessage($"Property Reference {classAndProperty} no type, instance, or property. (Are you using Fields or Properties? Is your Property Public?).", MessageSeverity.Error));
                Failed = true;
                CompilerFocusedMessage = CompilerMessages.Last();
                return (null, null);
            }
        }

        // Compiler Display
        private List<string> WrapText(string text, int maxWidth)
        {
            List<string> wrappedLines = new();
            if (string.IsNullOrEmpty(text))
            {
                return wrappedLines;
            }
            var paragraphs = text.Split('\n');

            int last = 0;
            foreach (var paragraph in paragraphs)
            {
                int start = 0;
                while (start < paragraph.Length)
                {
                    int length = Math.Min(maxWidth, paragraph.Length - start);
                    string line = paragraph.Substring(start, length);

                    if (start + length < paragraph.Length && paragraph[start + length] != ' ')
                    {
                        int lastSpace = line.LastIndexOf(' ');
                        if (lastSpace >= 0)
                        {
                            line = line.Substring(0, lastSpace);
                            length = lastSpace + 1;
                        }
                    }

                    wrappedLines.Add(line);
                    start += length;
                }
                last += paragraph.Length + 1;
            }

            return wrappedLines;
        }
        private List<string> WrapTextByChar(string text, int maxWidth)
        {
            List<string> wrappedLines = new();
            if (string.IsNullOrEmpty(text)) return wrappedLines;

            var paragraphs = text.Split('\n');
            foreach (var paragraph in paragraphs)
            {
                int start = 0;
                while (start < paragraph.Length)
                {
                    int length = 0;
                    int currentWidth = 0;

                    while (currentWidth < maxWidth && (start + length) < paragraph.Length)
                    {
                        // Check if this is a console color code
                        if (paragraph[start + length] == '\x1b' && (start + length + 1) < paragraph.Length && paragraph[start + length + 1] == '[')
                        {
                            // Find end of color code
                            int endIndex = paragraph.IndexOf('m', start + length);
                            if (endIndex != -1)
                            {
                                length = (endIndex + 1) - start;
                                continue;
                            }
                        }

                        length++;
                        currentWidth++;
                    }

                    wrappedLines.Add(paragraph.Substring(start, length));
                    start += length;
                }
            }
            return wrappedLines;
        }
        private string GetXmlContext(int lineNumber, int contextLines = 4)
        {
            if (lineNumber <= 0) return "";

            var lines = MXML.Split('\n');
            if (lineNumber > lines.Length) return "";

            var start = Math.Max(0, lineNumber - contextLines - 1);
            var end = Math.Min(lines.Length - 1, lineNumber + contextLines - 1);

            var result = new StringBuilder();
            for (int i = start; i <= end; i++)
            {
                var prefix = i == lineNumber - 1 ? ">" : " ";
                result.AppendLine($"{prefix} {i + 1,4}: {lines[i]}");
            }

            return result.ToString();
        }
        public string GetSimpleCompilerDisplay()
        {
            List<string> lines = new();
            if (!Failed)
            {
                lines.Add($"Successfully compiled this page!");
                lines.Add("{CLLine1}");
                lines.Add($" │ Relevant Logs");
                lines.Add("{CLLine2}");
                foreach (var msg in CompilerMessages.Where(x => x.Severity == MessageSeverity.Success))
                {
                    if (msg != CompilerFocusedMessage)
                    {
                        lines.Add($" ├╴{msg.Message}");
                    }
                }
                lines.Add("{CLLine3}");
                lines.Add($"   │ Compilation Info");
                lines.Add("{CLLine4}");
                lines.Add($"   ├╴Finished at {CompilerFocusedMessage.DateTime}");
                int warningCount = CompilerMessages.Where(x => x.Severity == MessageSeverity.Warning).Count();
                lines.Add($"   ├╴Compiled {Children.Count} objects in {CompilationFinished.TotalSeconds} seconds.");
                lines.Add($"   ├╴Noted {warningCount} warnings during compilation.");
                if (warningCount > 0)
                {
                    lines.Add("{CLLine5}");
                    lines.Add($"     │ Warnings");
                    lines.Add("{CLLine6}");
                    foreach (var msg in CompilerMessages.Where(x => x.Severity == MessageSeverity.Warning))
                    {
                        if (msg != CompilerFocusedMessage)
                        {
                            lines.Add($"     ├╴({msg.MxmlLineNumber.Line}) {msg.Message}");
                        }
                    }
                }

                int maxW = lines.Max(x => x.Length + 2);
                maxW = maxW > Console.WindowWidth - 2 ? Console.WindowWidth - 2 : maxW;
                List<int> indexes = new();
                indexes.Add(lines.IndexOf("{CLLine1}"));
                indexes.Add(lines.IndexOf("{CLLine2}"));
                indexes.Add(lines.IndexOf("{CLLine3}"));
                indexes.Add(lines.IndexOf("{CLLine4}"));
                indexes.Add(lines.IndexOf("{CLLine5}"));
                indexes.Add(lines.IndexOf("{CLLine6}"));
                lines[lines.IndexOf("{CLLine1}")] = $"═╤{string.Join("", Enumerable.Repeat("═", maxW - 2))}╕";
                lines[lines.IndexOf("{CLLine2}")] = $" ├{string.Join("", Enumerable.Repeat("─", maxW - 2))}┤";
                lines[lines.IndexOf("{CLLine3}")] = $" ╘═╤{string.Join("", Enumerable.Repeat("═", maxW - 4))}╡";
                lines[lines.IndexOf("{CLLine4}")] = $"   ├{string.Join("", Enumerable.Repeat("─", maxW - 4))}┤";
                if (warningCount > 0)
                {
                    lines[lines.IndexOf("{CLLine5}")] = $"   ╘═╤{string.Join("", Enumerable.Repeat("═", maxW - 6))}╡";
                    lines[lines.IndexOf("{CLLine6}")] = $"     ├{string.Join("", Enumerable.Repeat("─", maxW - 6))}┤";
                }
                lines.Add($"     └{string.Join("", Enumerable.Repeat("─", maxW - 6))}┘");
                indexes.Add(lines.Count() - 1);

                List<string> output = new();
                List<string> output2 = new();
                int count = 0;
                foreach (var line in lines)
                {
                    List<string> wrappedLines = new();
                    if (indexes.Contains(count))
                    {
                        wrappedLines = WrapTextByChar(line, maxW + 2);
                    }
                    else
                    {
                        wrappedLines = WrapTextByChar(line, maxW - 1);
                    }


                    string fLine = wrappedLines.First();
                    output.Add($"{fLine}");

                    int idx = fLine.IndexOf('╴') != -1 ? fLine.IndexOf('╴') : fLine.IndexOf("├──>") != -1 ? fLine.IndexOf("├──>") + 1 : -1;
                    string pre = idx != -1 ? fLine.Substring(0, idx - 1) : "";
                    foreach (var wLine in wrappedLines.Skip(1))
                    {
                        var wrappedLines2 = WrapTextByChar($"{pre}│ {wLine.Trim()}", maxW - (idx + 1));
                        output.Add(wrappedLines2.First());
                        foreach (var wl2 in wrappedLines2.Skip(1))
                        {
                            output.Add($"{pre}│ {wl2.Trim()}");
                        }
                    }
                    count++;
                }
                output2.Add(output.First());
                foreach (var line in output.Skip(1))
                {
                    int i = maxW - (ParamParser.GetVisibleLength(line));
                    if (i >= 0)
                    {
                        output2.Add($"{line}{string.Join("", Enumerable.Repeat(" ", i))}│");
                    }
                    else
                    {
                        output2.Add($"{line}");
                    }
                }
                return string.Join("\n", output2).Pastel(Color.White);
            }
            else
            {
                lines.Add($"MUIPage Compilation Failed!");
                lines.Add("{CLLine1}");
                lines.Add($" │ Relevant Logs");
                lines.Add("{CLLine2}");
                int cidx = CompilerMessages.FindIndex(x => x.Severity == MessageSeverity.Error);
                cidx = cidx - 3 > CompilerMessages.Count ? CompilerMessages.Count : cidx - 3;
                foreach (var msg in CompilerMessages.Skip(cidx).Where(x => x.Severity != MessageSeverity.Debug))
                {
                    if (msg != CompilerFocusedMessage)
                    {
                        lines.Add($" ├╴  {msg.Message}");
                    }
                    else
                    {
                        lines.Add($" ├─> {msg.Message}".Pastel(Color.FromArgb(255, 255, 50, 50)));
                    }
                }
                lines.Add("{CLLine3}");
                lines.Add($"   │ Debug View");
                lines.Add("{CLLine4}");
                int ContextSize = 8;
                if (CompilerFocusedMessage.MxmlLineNumber.Line != -1)
                {
                    var xmlCon = GetXmlContext(CompilerFocusedMessage.MxmlLineNumber.Line, ContextSize).Replace("\t", "  ").Replace("\r", "");
                    int lcount = CompilerFocusedMessage.MxmlLineNumber.Line - ContextSize;
                    foreach (var ln in xmlCon.Split("\n"))
                    {
                        if (lcount == CompilerFocusedMessage.MxmlLineNumber.Line)
                        {
                            lines.Add($"   ├─{ln}".Pastel(Color.FromArgb(255, 255, 50, 50)));
                        }
                        else
                        {
                            lines.Add($"   ├╴{ln}");
                        }
                        lcount++;
                    }
                }
                else
                {
                    lines.Add($"   ├╴Failed at {CompilerFocusedMessage.DateTime}");
                    int warningCount = CompilerMessages.Where(x => x.Severity == MessageSeverity.Warning).Count();
                    lines.Add($"   ├╴Compiled {Children.Count} objects in {CompilationFinished.TotalSeconds} seconds.");
                }

                int maxW = lines.Max(x => x.Length + 1);
                maxW = maxW > Console.WindowWidth - 2 ? Console.WindowWidth - 2 : maxW;
                List<int> indexes = new();
                indexes.Add(lines.IndexOf("{CLLine1}"));
                indexes.Add(lines.IndexOf("{CLLine2}"));
                indexes.Add(lines.IndexOf("{CLLine3}"));
                indexes.Add(lines.IndexOf("{CLLine4}"));
                lines[lines.IndexOf("{CLLine1}")] = $"═╤{string.Join("", Enumerable.Repeat("═", maxW - 2))}╕";
                lines[lines.IndexOf("{CLLine2}")] = $" ├{string.Join("", Enumerable.Repeat("─", maxW - 2))}┤";
                lines[lines.IndexOf("{CLLine3}")] = $" ╘═╤{string.Join("", Enumerable.Repeat("═", maxW - 4))}╡";
                lines[lines.IndexOf("{CLLine4}")] = $"   ├{string.Join("", Enumerable.Repeat("─", maxW - 4))}┤";
                lines.Add($"   └{string.Join("", Enumerable.Repeat("─", maxW - 4))}┘");
                indexes.Add(lines.Count() - 1);

                List<string> output = new();
                List<string> output2 = new();
                int count = 0;
                foreach (var line in lines)
                {
                    List<string> wrappedLines = new();
                    if (indexes.Contains(count))
                    {
                        wrappedLines = WrapTextByChar(line, maxW + 2);
                    }
                    else
                    {
                        wrappedLines = WrapTextByChar(line, maxW);
                    }


                    string fLine = wrappedLines.First();
                    output.Add($"{fLine}");

                    int idx = fLine.IndexOf('╴') != -1 ? fLine.IndexOf('╴') : fLine.IndexOf("├─>") != -1 ? fLine.IndexOf("├─>") + 1 : -1;
                    string pre = idx != -1 ? fLine.Substring(0, idx - 1) : "";
                    foreach (var wLine in wrappedLines.Skip(1))
                    {
                        var wrappedLines2 = WrapTextByChar($"{pre}│ {wLine.Trim()}", maxW - (ParamParser.GetVisibleLength(pre) + 1));
                        output.Add(wrappedLines2.First());
                        foreach (var wl2 in wrappedLines2.Skip(1))
                        {
                            output.Add($"{pre}│ {wl2.Trim()}");
                        }
                    }
                    count++;
                }
                output2.Add(output.First());
                foreach (var line in output.Skip(1))
                {
                    int i = maxW - (ParamParser.GetVisibleLength(line));
                    if (i >= 0)
                    {
                        output2.Add($"{line}{string.Join("", Enumerable.Repeat(" ", i))}│");
                    }
                    else
                    {
                        output2.Add($"{line}");
                    }
                }
                return string.Join("\n", output2).Pastel(Color.White);
            }
        }

        // Compiler Methods
        /// <summary>
        /// Given a name like "BackendClass.MemberName", tries to create a Binding object.
        /// </summary>
        private Binding CreateBinding(string classAndMemberPath, object elmattr)
        {
            var lineNumber = GetLineNumber(elmattr);
            if (string.IsNullOrWhiteSpace(classAndMemberPath))
            {
                CompilerMessages.Add(new CompilerMessage($"The Binding path must not be empty.", MessageSeverity.Error, lineNumber));
                Failed = true;
                CompilerFocusedMessage = CompilerMessages.Last();
                return null;
            }

            var parts = classAndMemberPath.Split('.');
            if (parts.Length < 2)
            {
                CompilerMessages.Add(new CompilerMessage($"The Binding \"{classAndMemberPath}\" is invalid. Should be in format \"BackendClass.MemberName\".", MessageSeverity.Error, lineNumber));
                Failed = true;
                CompilerFocusedMessage = CompilerMessages.Last();
                return null;
            }

            string initialClassName = parts[0];
            object currentObject = null;
            CompilerMessages.Add(new CompilerMessage($"Creating a binding to {classAndMemberPath}.", MessageSeverity.Debug, lineNumber));
            Type currentType = null;


            if (BackendObjects.TryGetValue(initialClassName, out currentObject))
            {
                CompilerMessages.Add(new CompilerMessage($"Found backend instanced object.", MessageSeverity.Debug, lineNumber));
                currentType = currentObject.GetType();
            }
            else
            {
                CompilerMessages.Add(new CompilerMessage($"Searching for static backend class.", MessageSeverity.Debug, lineNumber));
                currentType = FindElementType(initialClassName);
                if (currentType == null)
                {
                    CompilerMessages.Add(new CompilerMessage($"Cannot find class or backend object named \"{initialClassName}\".", MessageSeverity.Error, lineNumber));
                    Failed = true;
                    CompilerFocusedMessage = CompilerMessages.Last();
                    return null;
                }
            }

            CompilerMessages.Add(new CompilerMessage($"Searching for property", MessageSeverity.Debug, lineNumber));
            for (int i = 1; i < parts.Length; i++)
            {
                string memberName = parts[i];
                bool isLast = (i == parts.Length - 1);

                // Check for property
                var prop = currentType.GetProperty(memberName,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                if (prop != null)
                {
                    if (isLast) return new Binding(currentObject, prop);
                    currentObject = currentType.IsClass && prop.GetGetMethod(true).IsStatic
                        ? prop.GetValue(null)
                        : prop.GetValue(currentObject);

                    if (currentObject == null)
                    {
                        CompilerMessages.Add(new CompilerMessage($"Property \"{memberName}\" on \"{currentType.Name}\" is null.", MessageSeverity.Error, lineNumber));
                        Failed = true;
                        CompilerFocusedMessage = CompilerMessages.Last();
                        return null;
                    }
                    currentType = currentObject.GetType();
                    continue;
                }

                // If property not found, check if there's a method with that name for event binding
                var method = currentType.GetMethod(memberName, BindingFlags.Static |
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                if (method != null)
                {
                    if (isLast) return new Binding(currentObject, method);
                }


                // Check for event
                var evt = currentType.GetEvent(memberName,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                if (evt != null)
                {
                    if (isLast) return new Binding(currentObject, evt);
                    CompilerMessages.Add(new CompilerMessage($"Cannot traverse through event \"{memberName}\" in binding path.", MessageSeverity.Error, lineNumber));
                    Failed = true;
                    CompilerFocusedMessage = CompilerMessages.Last();
                    return null;
                }

                // Check for nested static class
                var nestedType = currentType.GetNestedType(memberName, BindingFlags.Public | BindingFlags.Static);
                if (nestedType != null)
                {
                    currentObject = null;
                    currentType = nestedType;
                    continue;
                }

                // Not found
                CompilerMessages.Add(new CompilerMessage($"Poperty \"{memberName}\" not found on type \"{currentType.Name}\". (Are you using fields or properties?)", MessageSeverity.Error, lineNumber));
                Failed = true;
                CompilerFocusedMessage = CompilerMessages.Last();
                return null;
            }

            CompilerMessages.Add(new CompilerMessage($"Couldn't find the property to bind to! (Are you using fields or properties?)", MessageSeverity.Error, lineNumber));
            Failed = true;
            CompilerFocusedMessage = CompilerMessages.Last();
            return null;
        }

        /// <summary>
        /// Parses and sets top-level attributes (Namespaces, Assemblies, Backends, etc.) for the MXML page.
        /// </summary>
        private bool ParseTopLevelAttributes(XElement root)
        {
            foreach (var attr in root.Attributes())
            {
                switch (attr.Name.LocalName)
                {
                    case "MXMLFlags":
                        // MXMLFlags at top-level is ignored
                        continue;

                    case "Namespaces":
                        foreach (var ns in attr.Value.Split(','))
                            Namespaces.Add(ns);
                        break;

                    case "Backends":
                        if (!HandleBackendAttribute(attr.Value)) return false;
                        break;

                    case "Assemblies":
                        if (!HandleAssembliesAttribute(attr.Value)) return false;
                        break;

                    default:
                        SetProperty(this, attr.Name.LocalName, attr.Value, new Dictionary<string, string>(), null);
                        break;
                }
                if (Failed) return false;
            }
            return true;
        }

        /// <summary>
        /// Parses a UI-element from an XElement.
        /// </summary>
        private object ParseElement(XElement element)
        {
            var lineNumber = GetLineNumber(element);
            if (element.Name.LocalName.Contains("."))
            {
                Failed = true;
                CompilerMessages.Add(new CompilerMessage($"Top-Level elements cannot have dots in their name.", MessageSeverity.Error, lineNumber));
                CompilerFocusedMessage = CompilerMessages.Last();
                return null;
            }

            var elementType = FindElementType(element.Name.LocalName);
            if (elementType == null)
            {
                Failed = true;
                CompilerMessages.Add(new CompilerMessage($"Unkown element type \"{element.Name.LocalName}\".", MessageSeverity.Error, lineNumber));
                CompilerFocusedMessage = CompilerMessages.Last();
                return null;
            }

            CompilerMessages.Add(new CompilerMessage($"Creating instance of {elementType.Name}.", MessageSeverity.Debug, lineNumber));
            object uiElement = null;
            try
            {
                uiElement = Activator.CreateInstance(elementType);
            }
            catch(Exception e)
            {
                Failed = true;
                CompilerMessages.Add(new CompilerMessage($"Cannot create an instance of type \"{element.Name.LocalName}\": {e.Message}", MessageSeverity.Error, lineNumber));
                CompilerFocusedMessage = CompilerMessages.Last();
                return null;
            }

            // 1) Set properties from attributes
            var flagsFromElement = GetFlagsFromElement(element);
            CompilerMessages.Add(new CompilerMessage($"Got flags \"{string.Join(", ", flagsFromElement.Select(x => $"{x.Key}={x.Value}"))}\".", MessageSeverity.Debug, lineNumber));
            CompilerMessages.Add(new CompilerMessage($"Setting Properties for {elementType.Name}.", MessageSeverity.Debug, lineNumber));
            foreach (var attr in element.Attributes())
            {
                SetProperty(uiElement, attr.Name.LocalName, attr.Value, flagsFromElement, attr);
                if (Failed) return null;
            }

            // 2) Handle child elements
            CompilerMessages.Add(new CompilerMessage($"Handling Properties for {elementType.Name}.", MessageSeverity.Info, lineNumber));
            foreach (var child in element.Elements())
            {
                CompilerMessages.Add(new CompilerMessage($"Parsing {child.Name}.", MessageSeverity.Info, lineNumber));

                if (child.Name.LocalName.Contains("."))
                {
                    // Nested property, e.g., <MenuItem.OnSelect>...</MenuItem.OnSelect>
                    if (!HandleNestedProperty(uiElement, child)) return null;
                }
                else
                {
                    // Normal child UI element or custom object
                    if (!HandleChildElement(elementType, uiElement, child)) return null;
                }

                if (Failed) return null;

                CompilerMessages.Add(new CompilerMessage($"{child.Name} was successfully parsed.", MessageSeverity.Success, GetLineNumber(child)));
            }

            return uiElement;
        }

        /// <summary>
        /// Parses any object that is not a UIElement (custom object).
        /// </summary>
        private object ParseCustomObject(XElement element)
        {
            var lineNumber = GetLineNumber(element);
            if (element.Name.LocalName.Contains("."))
            {
                Failed = true;
                CompilerMessages.Add(new CompilerMessage($"Top-Level objects cannot have dots.", MessageSeverity.Error, lineNumber));
                CompilerFocusedMessage = CompilerMessages.Last();
                return null;
            }

            var objectType = FindElementType(element.Name.LocalName);
            if (objectType == null)
            {
                Failed = true;
                CompilerMessages.Add(new CompilerMessage($"Unknown custom object type: {element.Name.LocalName}.", MessageSeverity.Error, lineNumber));
                CompilerFocusedMessage = CompilerMessages.Last();
                return null;
            }

            var customObject = Activator.CreateInstance(objectType);

            // Set properties from attributes
            CompilerMessages.Add(new CompilerMessage($"Setting Properties for {element.Name.LocalName}.", MessageSeverity.Debug, lineNumber));
            foreach (var attr in element.Attributes())
            {
                SetProperty(customObject, attr.Name.LocalName, attr.Value, new Dictionary<string, string>(), attr);
                if (Failed) return customObject;
            }

            // Process nested elements
            CompilerMessages.Add(new CompilerMessage($"Handling Properties for {element.Name.LocalName}.", MessageSeverity.Debug, lineNumber));
            foreach (var child in element.Elements())
            {
                if (child.Name.LocalName.Contains("."))
                {
                    if (!HandleNestedProperty(customObject, child)) return customObject;
                }
                else
                {
                    var parsedChild = ParseMXMLElement(child);
                    if (parsedChild != null && !Failed)
                        SetProperty(customObject, child.Name.LocalName, parsedChild, new Dictionary<string, string>(), child);
                    if (Failed) return customObject;
                }
            }

            return customObject;
        }

        /// <summary>
        /// Core method for parsing any XElement that might be UIElement or custom object.
        /// </summary>
        private object ParseMXMLElement(XElement element)
        {
            var childType = FindElementType(element.Name.LocalName);
            if (childType == null)
            {
                CompilerMessages.Add(new CompilerMessage($"Unknown type for element: {element.Name.LocalName}.", MessageSeverity.Error, GetLineNumber(element)));
                CompilerFocusedMessage = CompilerMessages.Last();
                Failed = true;
                return null;
            }
            if (typeof(MUIPage).IsAssignableFrom(childType))
                return ParseMUIPage(element);
            else if (typeof(UIElement).IsAssignableFrom(childType))
                return ParseElement(element);
            else
                return ParseCustomObject(element);
        }

        public object ParseMUIPage(XElement element)
        {
            var lineNumber = GetLineNumber(element);
            if (element.Name.LocalName.Contains("."))
            {
                Failed = true;
                CompilerMessages.Add(new CompilerMessage($"Top-Level elements cannot have dots in their name.", MessageSeverity.Error, lineNumber));
                CompilerFocusedMessage = CompilerMessages.Last();
                return null;
            }

            var elementType = FindElementType(element.Name.LocalName);
            if (elementType == null)
            {
                Failed = true;
                CompilerMessages.Add(new CompilerMessage($"Unkown element type \"{element.Name.LocalName}\".", MessageSeverity.Error, lineNumber));
                CompilerFocusedMessage = CompilerMessages.Last();
                return null;
            }

            CompilerMessages.Add(new CompilerMessage($"Creating instance of {elementType.Name}.", MessageSeverity.Debug, lineNumber));
            MUIPage page = (MUIPage)Activator.CreateInstance(elementType);

            var flagsFromElement = GetFlagsFromElement(element);
            CompilerMessages.Add(new CompilerMessage($"Got flags \"{string.Join(", ", flagsFromElement.Select(x => $"{x.Key}={x.Value}"))}\".", MessageSeverity.Debug, lineNumber));
            CompilerMessages.Add(new CompilerMessage($"Setting Properties for {elementType.Name}.", MessageSeverity.Debug, lineNumber));
            foreach (var attr in element.Attributes())
            {
                SetProperty(page, attr.Name.LocalName, attr.Value, flagsFromElement, attr);
                if (Failed) return null;
            }

            page.Assemblies.AddRange(Assemblies);
            page.Namespaces.AddRange(Namespaces);
            page.Backends.AddRange(Backends);
            foreach (var item in BackendObjects)
            {
                page.BackendObjects.Add(item.Key,item.Value);
            }

            if (File.Exists(page.MXMLFilePath))
            {
                CompilerMessages.Add(new CompilerMessage($"Compiling MXML Page \"{page.MXMLFilePath}\"", MessageSeverity.Debug, lineNumber));
                var chk = page.CompileFile(page.MXMLFilePath);
                if (!chk)
                {
                    Failed = true;
                    CompilerMessages.AddRange(page.CompilerMessages);
                    CompilerFocusedMessage = page.CompilerFocusedMessage;
                    return null;
                }

                //page.IsVisible = false;
                //Children.AddRange(page.Children);
                //page.Children.Clear();

                return page;
            }
            else
            {
                CompilerMessages.Add(new CompilerMessage($"Couldn't Find MXML File \"{page.MXMLFilePath}\"!", MessageSeverity.Error, lineNumber));
                Failed = true;
                CompilerFocusedMessage = CompilerMessages.Last();
                return null;

            }
        }

        /// <summary>
        /// Returns MXML flags (e.g., MXMLFlags="Show(Something),Hide(Else)") from an element.
        /// </summary>
        public Dictionary<string, string> GetFlagsFromElement(XElement element)
        {
            if (element == null) return new Dictionary<string, string>();

            var xmlFlagsAttr = element.Attribute("MXMLFlags");
            if (xmlFlagsAttr == null || string.IsNullOrWhiteSpace(xmlFlagsAttr.Value))
                return new Dictionary<string, string>();

            return ParseFlags(xmlFlagsAttr.Value);
        }

        /// <summary>
        /// Parses a comma-separated flags string into a dictionary.
        /// </summary>
        public Dictionary<string, string> ParseFlags(string mxmlFlags)
        {
            var results = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(mxmlFlags))
            {
                Failed = true;
                CompilerMessages.Add(new CompilerMessage($"MXMLFlags cannot be empty, if you dont need to set any, remove the property.", MessageSeverity.Error));
                CompilerFocusedMessage = CompilerMessages.Last();
                return results;
            }

            var flags = mxmlFlags.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var flag in flags)
            {
                var trimmedFlag = flag.Trim();
                int openParenIndex = trimmedFlag.IndexOf('(');
                int closeParenIndex = trimmedFlag.IndexOf(')');

                if (openParenIndex > 0 && closeParenIndex > openParenIndex)
                {
                    string action = trimmedFlag.Substring(0, openParenIndex).Trim();
                    string target = trimmedFlag.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1).Trim();
                    if (!string.IsNullOrEmpty(action) && !string.IsNullOrEmpty(target))
                    {
                        results.TryAdd(action, target);
                        continue;
                    }
                }
                Failed = true;
                CompilerMessages.Add(new CompilerMessage($"MXMLFlag \"${mxmlFlags}\" is invalid!", MessageSeverity.Error));
                CompilerFocusedMessage = CompilerMessages.Last();
            }
            return results;
        }

        /// <summary>
        /// Tries to find a Type from known assemblies and namespaces.
        /// </summary>
        private Type FindElementType(string typeName)
        {
            foreach (var asm in Assemblies)
            {
                foreach (var space in Namespaces)
                {
                    var t = asm.GetType($"{space}.{typeName}", throwOnError: false, ignoreCase: false);
                    if (t != null) return t;
                }
            }
            return null;
        }

        /// <summary>
        /// Main property setting logic (with some sub-routes for special property types).
        /// </summary>
        private void SetProperty(object element, string propertyName, object value, Dictionary<string, string> mxmlMeta, object elmattr)
        {
            var lineNumber = GetLineNumber(elmattr);
            var prop = element.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            if (prop == null || !prop.CanWrite)
            {
                // If property not found, check if there's a method with that name for event binding
                var method = element.GetType().GetMethod(propertyName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                if (method != null)
                {
                    HandleMethodBinding(element, propertyName, value, method, elmattr);
                }
                else
                {
                    Failed = true;
                    CompilerMessages.Add(new CompilerMessage($"Element property \"{propertyName}\" cannot be found. (Are you using Fields or Properties? Is your Property Public?).", MessageSeverity.Error, lineNumber));
                    CompilerFocusedMessage = CompilerMessages.Last();
                }
                return;
            }

            // If string looks like {BackendProp}, handle binding
            if (value is string str && str.StartsWith("{") && str.EndsWith("}"))
            {
                CompilerMessages.Add(new CompilerMessage($"Property \"{propertyName}\" is set to bind.", MessageSeverity.Debug, lineNumber));
                HandlePropertyBinding(element, propertyName, str, elmattr);
                return;
            }

            // Determine property type (Action, List<T>, or simple property)
            var propType = prop.PropertyType;
            if (propType == typeof(Action))
            {
                CompilerMessages.Add(new CompilerMessage($"Property \"{propertyName}\" is an Action.", MessageSeverity.Debug, lineNumber));
                HandleActionProperty(element, prop, value, mxmlMeta);

            }
            else if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(List<>))
            {
                CompilerMessages.Add(new CompilerMessage($"Property \"{propertyName}\" is a List of objects.", MessageSeverity.Debug, lineNumber));
                HandleListProperty(element, prop, value, propertyName, elmattr);
            }
            else
            {
                CompilerMessages.Add(new CompilerMessage($"Property \"{propertyName}\" is a primitive type.", MessageSeverity.Debug, lineNumber));
                HandleSimpleProperty(element, prop, value, propertyName, elmattr);
            }
        }

        /// <summary>
        /// Converts a value into a specified target type if possible.
        /// </summary>
        private object ConvertToType(object value, Type targetType, object element)
        {
            var lineNumber = GetLineNumber(element);
            if (value == null)
            {
                if (!targetType.IsValueType || (Nullable.GetUnderlyingType(targetType) != null))
                {
                    // null is okay
                }
                else
                {
                    Failed = true;
                    CompilerMessages.Add(new CompilerMessage($"\"{targetType.Name}\" cannot be set to a null value.", MessageSeverity.Error, lineNumber));
                    CompilerFocusedMessage = CompilerMessages.Last();
                    return null;
                }
            }

            // Use CompilerTypeExtensions to convert value to targetType
            CompilerMessages.Add(new CompilerMessage($"Attempting to use Compiler TypeExtensions.", MessageSeverity.Debug, lineNumber));
            var chk = TypeExtensions.TryGetValue(targetType, out var typeExtension);
            if (chk)
            {
                var obj = typeExtension(value);
                var castResult = Convert.ChangeType(obj, targetType);
                return castResult;
            }

            CompilerMessages.Add(new CompilerMessage($"Attempting to convert to assignable object.", MessageSeverity.Debug, lineNumber));
            // If already assignable, return
            if (value != null && targetType.IsAssignableFrom(value.GetType()))
                return value;

            CompilerMessages.Add(new CompilerMessage($"Attempting to convert to a generic list.", MessageSeverity.Debug, lineNumber));
            // If target is List<T> and value is enumerable, convert accordingly
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
                return ConvertList(value, targetType, element);

            CompilerMessages.Add(new CompilerMessage($"Attempting to use primitive type conversions.", MessageSeverity.Debug, lineNumber));
            // Try direct string conversions for primitives, enums, Color, etc.
            string strValue = value?.ToString() ?? string.Empty;
            if (TryPrimitiveConversion(strValue, targetType, out var result)) return result;

            CompilerMessages.Add(new CompilerMessage($"Checking if a .Parse or .TryParse method exists.", MessageSeverity.Debug, lineNumber));
            // Try parse methods, e.g., T.Parse(string) or T.TryParse(string, out T)
            var parseMethod = targetType.GetMethods(BindingFlags.Static | BindingFlags.Public)
                .FirstOrDefault(m =>
                    (m.Name == "Parse" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string)) ||
                    (m.Name == "TryParse" && m.GetParameters().Length == 2 &&
                     m.GetParameters()[0].ParameterType == typeof(string) &&
                     m.GetParameters()[1].IsOut)
                );
            if (parseMethod != null && TryMethodParse(strValue, parseMethod, out var parsedVal))
                return parsedVal;

            CompilerMessages.Add(new CompilerMessage($"Attempting to find a string constuctor", MessageSeverity.Debug, lineNumber));
            // Try a string constructor
            var stringCtor = targetType.GetConstructor(new[] { typeof(string) });
            if (stringCtor != null) return stringCtor.Invoke(new object[] { strValue });

            CompilerMessages.Add(new CompilerMessage($"Last try, can we Create an instance from a paramaterless constructor?", MessageSeverity.Debug, lineNumber));
            // As a last fallback, try parameterless constructor
            if (targetType.GetConstructor(Type.EmptyTypes) != null)
                return Activator.CreateInstance(targetType);

            // If all else fails, mark as failed
            Failed = true;
            CompilerMessages.Add(new CompilerMessage($"The value \"{value}\" for property type \"{targetType}\" cannot be converted.", MessageSeverity.Error, lineNumber));
            CompilerFocusedMessage = CompilerMessages.Last();
            return value;
        }


        // Helper Sub-Methods
        private (int line, int position) GetLineNumber(object element)
        {
            var lineInfo = element as IXmlLineInfo;
            return lineInfo != null ? (lineInfo.LineNumber, lineInfo.LinePosition) : (-1, -1);
        }
        private bool HandleAssembliesAttribute(string assembliesStr)
        {
            foreach (var str in assembliesStr.Split(','))
            {
                var ass = AppDomain.CurrentDomain
                                   .GetAssemblies()
                                   .SingleOrDefault(a => a.GetName().Name == str);
                if (ass == null)
                {
                    Failed = true;
                    CompilerMessages.Add(new CompilerMessage($"Assembly \"{str}\" could not be found!", MessageSeverity.Error));
                    CompilerFocusedMessage = CompilerMessages.Last();
                    return false;
                }
                Assemblies.Add(ass);
            }
            return true;
        }

        private bool HandleBackendAttribute(string backendsStr)
        {
            foreach (var str in backendsStr.Split(','))
            {
                var propType = FindElementType(str);
                if (propType == null)
                {
                    Failed = true;
                    CompilerMessages.Add(new CompilerMessage($"Backend \"{str}\" could not be found!", MessageSeverity.Error));
                    CompilerFocusedMessage = CompilerMessages.Last();
                    return false;
                }
                try
                {
                    Backends.Add(str);
                    var item = Activator.CreateInstance(propType);
                    BackendObjects.TryAdd(str, item);
                }
                catch { /* Exception on Create Instance */ }
            }
            return true;
        }

        private bool HandleNestedProperty(object uiElement, XElement child)
        {
            var lineNumber = GetLineNumber(child);
            var parts = child.Name.LocalName.Split('.');
            if (parts.Length != 2)
            {
                Failed = true;
                CompilerMessages.Add(new CompilerMessage($"Too many parts, mxml cannot currently handled nested properties like this!", MessageSeverity.Error, lineNumber));
                CompilerFocusedMessage = CompilerMessages.Last();
                return false;
            }

            var propertyName = parts[1];
            var nestedEls = child.Elements().ToList();

            if (nestedEls.Count == 0)
            {
                var flags = GetFlagsFromElement(child);
                SetProperty(uiElement, propertyName, "", flags, child);
            }
            else if (nestedEls.Count == 1)
            {
                var propertyValue = ParseElement(nestedEls[0]);
                if (propertyValue != null)
                {
                    var flags = GetFlagsFromElement(child);
                    SetProperty(uiElement, propertyName, propertyValue, flags, child);
                    if (Failed) return false;
                }
            }
            else
            {
                foreach (var ne in nestedEls)
                {
                    var childType = FindElementType(ne.Name.LocalName);
                    if (childType == null)
                    {
                        Failed = true;
                        CompilerMessages.Add(new CompilerMessage($"Child element \"{ne.Name.LocalName}\" is unknown", MessageSeverity.Error, lineNumber));
                        return false;
                    }
                    if (childType != null)
                    {
                        if (typeof(UIElement).IsAssignableFrom(childType))
                        {
                            var childElement = ParseElement(ne);
                            if (childElement == null)
                            {
                                Failed = true;
                                CompilerMessages.Add(new CompilerMessage($"Failed to parse child element {ne.Name.LocalName}", MessageSeverity.Error, lineNumber));
                                return false;
                            }
                            AddElement((UIElement)childElement);
                        }
                        else
                        {
                            var childObject = ParseMXMLElement(ne);
                            if (childObject != null)
                            {
                                var flags = GetFlagsFromElement(ne);
                                SetProperty(uiElement, propertyName, childObject, flags, child);
                                if (Failed) return false;
                            }
                        }
                    }
                }
            }
            return !Failed;
        }

        private bool HandleChildElement(Type elementType, object uiElement, XElement child)
        {
            var childType = FindElementType(child.Name.LocalName);
            if (Failed)
            {
                CompilerMessages.Add(new CompilerMessage($"Child element \"{child.Name.LocalName}\" is unknown", MessageSeverity.Error));
                CompilerFocusedMessage = CompilerMessages.Last();
                Failed = true;
                return false;
            }
            if (childType != null)
            {
                if (typeof(UIElement).IsAssignableFrom(childType))
                {
                    var childElement = ParseElement(child);
                    AddElement((UIElement)childElement);
                }
                else
                {
                    var property = (UIElement)Activator.CreateInstance(elementType);
                    var flags = GetFlagsFromElement(child);
                    foreach (var attr in child.Attributes())
                    {
                        SetProperty(property, attr.Name.LocalName, attr.Value, flags, child);
                        if (Failed) return false;
                    }
                }
            }
            return !Failed;
        }

        private void HandleMethodBinding(object element, string propertyName, object value, MethodInfo method, object elmattr)
        {
            var lineNumber = GetLineNumber(elmattr);
            if (value is string strVal && strVal.StartsWith("{") && strVal.EndsWith("}"))
            {
                string bindingRef = strVal.Substring(1, strVal.Length - 2).Trim();
                try
                {
                    var binding = CreateBinding(bindingRef, element);
                    if(binding == null)
                    {
                        Failed = true;
                        CompilerMessages.Add(new CompilerMessage($"Failed to bind \"{bindingRef}\" to method \"{propertyName}\"", MessageSeverity.Error, lineNumber));
                        CompilerFocusedMessage = CompilerMessages.Last();
                        return;
                    }
                    if (binding.IsEvent)
                    {
                        Delegate handlerDelegate = Delegate.CreateDelegate(binding.EventInfo.EventHandlerType, element, method);
                        binding.Subscribe(handlerDelegate);
                        ((UIElement)element).SetBinding(propertyName, binding);
                    }
                    else
                    {
                        CompilerMessages.Add(new CompilerMessage($"Binding \"{bindingRef}\" is not an event and cannot be assigned to method \"{propertyName}\".", MessageSeverity.Error, lineNumber));
                        CompilerFocusedMessage = CompilerMessages.Last();
                        Failed = true;
                    }
                }
                catch (Exception ex)
                {
                    Failed = true;
                    CompilerMessages.Add(new CompilerMessage($"Failed to bind \"{bindingRef}\" to method \"{propertyName}\": {ex.Message}.", MessageSeverity.Error, lineNumber));
                    CompilerFocusedMessage = CompilerMessages.Last();
                }
            }
            else
            {
                CompilerMessages.Add(new CompilerMessage($"Invalid binding value for method \"{propertyName}\". Expected a binding expression.", MessageSeverity.Error, lineNumber));
                CompilerFocusedMessage = CompilerMessages.Last();
                Failed = true;
            }
        }

        private void HandlePropertyBinding(object element, string propertyName, string str, object elmattr)
        {
            var lineNumber = GetLineNumber(elmattr);
            string name = str.Replace("{", "").Replace("}", "");
            var binding = CreateBinding(name, elmattr);
            if (binding == null)
            {
                Failed = true;
                CompilerMessages.Add(new CompilerMessage($"Invalid binding value for method \"{propertyName}\". Expected a binding expression.", MessageSeverity.Error, lineNumber));
                return;
            }
            try
            {
                var val = binding.GetValue();
                if (!binding.IsEvent)
                    ((dynamic)element).SetBinding(propertyName, binding);
            }
            catch (Exception er)
            {
                Failed = true;
                CompilerMessages.Add(new CompilerMessage($"Property Binding \"{name}\" failed to bind. ({er.Message})", MessageSeverity.Error, lineNumber));
            }
        }

        private void HandleActionProperty(object element, PropertyInfo prop, object value, Dictionary<string, string> mxmlMeta)
        {
            foreach(var meta in mxmlMeta.Values)
            {
                if (!Children.Any(x=>x.Name == meta.Split(":")[0]))
                {
                    CompilerMessages.Add(new CompilerMessage($"Couldn't find UIElement \"{meta.Split(":")[0]}\" on this page, this code will error if a UIElement by this name isn't added to the children after compilation!", MessageSeverity.Warning));
                }
            }
            if (value is UIElement uiVal)
            {
                CompilerMessages.Add(new CompilerMessage($"Handling Action Property W/ MXML Flags and UIElements", MessageSeverity.Debug));
                // If the UIElement has no Name, give it a unique one.
                string tempName = string.IsNullOrEmpty(uiVal.Name) ? Guid.NewGuid().ToString() : uiVal.Name;
                uiVal.Name = tempName;
                uiVal.IsVisible = false;
                AddElement(uiVal);

                Action gen = () =>
                {
                    if (!string.IsNullOrEmpty(tempName))
                    {
                        var elm = Children.Find(x => x.Name == tempName);
                        elm.IsVisible = true;
                        uiVal.IsVisible = true;
                        ParentWindow.MoveFocus(elm);
                    }
                    if (mxmlMeta.ContainsKey("Hide"))
                    {
                        var hideElm = Children.Find(x => x.Name == mxmlMeta["Hide"]);
                        hideElm.IsVisible = false;
                    }
                    if (mxmlMeta.ContainsKey("Show"))
                    {
                        string[] info = mxmlMeta["Show"].Split(":");
                        bool focus = info.Length == 2 ? bool.Parse(info[1]) : true;
                        var showElm = Children.Find(x => x.Name == mxmlMeta["Show"]);
                        showElm.IsVisible = true;
                        if (focus)
                        {
                            showElm.IsFocused = true;
                            ParentWindow.MoveFocus(showElm);
                        }
                    }
                };

                var existingValue = prop.GetValue(element);
                if (existingValue == null)
                {
                    prop.SetValue(element, gen);
                }
                else
                {
                    var collection = new Action(() =>
                    {
                        ((Action)existingValue)();
                        gen();
                    });
                    prop.SetValue(element, collection);
                }
            }
            else
            {
                CompilerMessages.Add(new CompilerMessage($"Handling Action Property W/ MXML Flags", MessageSeverity.Debug));
                // A plain action with possible show/hide
                Action gen = () =>
                {
                    if (mxmlMeta.ContainsKey("Hide"))
                    {
                        var hideElm = Children.Find(x => x.Name == mxmlMeta["Hide"]);
                        hideElm.IsVisible = false;
                    }
                    if (mxmlMeta.ContainsKey("Show"))
                    {
                        string[] info = mxmlMeta["Show"].Split(":");
                        bool focus = info.Length == 2 ? bool.Parse(info[1]) : true;
                        var showElm = Children.Find(x => x.Name == mxmlMeta["Show"].Split(":")[0]);
                        showElm.IsVisible = true;
                        if (focus)
                        {
                            showElm.IsFocused = true;
                            ParentWindow.MoveFocus(showElm);
                        }
                    }
                };
                prop.SetValue(element, gen);
            }
        }

        private void HandleListProperty(object element, PropertyInfo prop, object value, string propertyName, object elmattr)
        {
            var lineNumber = GetLineNumber(elmattr);
            var propType = prop.PropertyType;
            var elementType = propType.GetGenericArguments()[0];

            if (value is System.Collections.IEnumerable en && !(value is string))
            {
                var convertedValue = ConvertToType(value, propType, elmattr);
                if (Failed)
                {
                    CompilerMessages.Add(new CompilerMessage($"Element property \"{propertyName}\" has an invalid value.", MessageSeverity.Error, lineNumber));
                    return;
                }
                prop.SetValue(element, convertedValue);
            }
            else
            {
                var listInstance = prop.GetValue(element);
                if (listInstance == null)
                {
                    listInstance = Activator.CreateInstance(propType);
                    prop.SetValue(element, listInstance);
                }
                var convertedItem = ConvertToType(value, elementType, elmattr);
                if (Failed)
                {
                    CompilerMessages.Add(new CompilerMessage($"Element property \"{propertyName}\" has an invalid value.", MessageSeverity.Error, lineNumber));
                    return;
                }
                var addMethod = propType.GetMethod("Add");
                addMethod.Invoke(listInstance, new[] { convertedItem });
            }
        }

        private void HandleSimpleProperty(object element, PropertyInfo prop, object value, string propertyName, object elmattr)
        {
            var convertedValue = ConvertToType(value, prop.PropertyType, elmattr);
            if (Failed)
            {
                CompilerMessages.Add(new CompilerMessage($"Element property \"{propertyName}\" has an invalid value.", MessageSeverity.Error, GetLineNumber(elmattr)));
                return;
            }
            prop.SetValue(element, convertedValue);
        }

        private object ConvertList(object value, Type targetType, object elmattr)
        {
            var elementType = targetType.GetGenericArguments()[0];
            if (value is System.Collections.IEnumerable en && !(value is string))
            {
                var listInstance = Activator.CreateInstance(targetType);
                var addMethod = targetType.GetMethod("Add");

                foreach (var item in en)
                {
                    var convertedItem = ConvertToType(item, elementType, elmattr);
                    if (Failed) return listInstance;
                    addMethod.Invoke(listInstance, new[] { convertedItem });
                }
                return listInstance;
            }
            else
            {
                var singleConverted = ConvertToType(value, elementType, elmattr);
                if (Failed) return null;
                var singleList = Activator.CreateInstance(targetType);
                var addMethod = targetType.GetMethod("Add");
                addMethod.Invoke(singleList, new[] { singleConverted });
                return singleList;
            }
        }

        private bool TryPrimitiveConversion(string strValue, Type targetType, out object converted)
        {
            converted = null;

            var realType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (Nullable.GetUnderlyingType(targetType) != null && string.IsNullOrEmpty(strValue))
            {
                converted = null;
                return true;
            }


            // string
            if (realType == typeof(string))
            {
                converted = strValue;
                return true;
            }
            // int
            if (realType == typeof(int) && int.TryParse(strValue, out var i))
            {
                converted = i;
                return true;
            }
            // bool
            if (realType == typeof(bool) && bool.TryParse(strValue, out var b))
            {
                converted = b;
                return true;
            }
            // enum
            if (realType.IsEnum && Enum.TryParse(realType, strValue, out var enumVal))
            {
                converted = enumVal;
                return true;
            }
            // double
            if (realType == typeof(double) && double.TryParse(strValue, out var d))
            {
                converted = d;
                return true;
            }
            // float
            if (realType == typeof(float) && float.TryParse(strValue, out var f))
            {
                converted = f;
                return true;
            }
            // long
            if (realType == typeof(long) && long.TryParse(strValue, out var l))
            {
                converted = l;
                return true;
            }
            // short
            if (realType == typeof(short) && short.TryParse(strValue, out var s))
            {
                converted = s;
                return true;
            }
            // Color
            if (realType == typeof(Color))
            {
                if (TryConvertColor(strValue, out var colorValue))
                {
                    converted = colorValue;
                    return true;
                }
                return false;
            }
            return false;
        }

        private bool TryConvertColor(string strValue, out Color color)
        {
            color = default;
            var split = strValue.Split(",");
            if (split.Length != 3) return false;

            if (!int.TryParse(split[0], out var r)
                || !int.TryParse(split[1], out var g)
                || !int.TryParse(split[2], out var b))
            {
                return false;
            }

            color = Color.FromArgb(255, r, g, b);
            return true;
        }

        private bool TryMethodParse(string strValue, MethodInfo parseMethod, out object parsedVal)
        {
            parsedVal = null;
            try
            {

                if (parseMethod.Name == "Parse")
                {
                    parsedVal = parseMethod.Invoke(null, new object[] { strValue });
                    return true;
                }
                else if (parseMethod.Name == "TryParse")
                {
                    var parameters = new object[] { strValue, null };
                    bool success = (bool)parseMethod.Invoke(null, parameters);
                    if (success)
                    {
                        parsedVal = parameters[1];
                        return true;
                    }
                }
            }
            catch(Exception e)
            {
                return false;
            }
            return false;
        }

        // Render Page Elements
        public void RemoveElement(UIElement element)
        {
            int idx = Children.IndexOf(element);
            if (idx == -1)
            {
                return;
            }
            Children.RemoveAt(idx);
            if (ParentWindow.FocusedElement != null && ParentWindow.FocusedElement.Equals(element) && ParentWindow.RootElements.Count() >= 1)
            {
                ParentWindow.FocusedElement = ParentWindow.RootElements.OrderByDescending(x => x.Z).FirstOrDefault();
                ParentWindow.FocusedElement.IsFocused = true;
            }

            int count = 0;
            foreach (var elm in ParentWindow.RootElements.OrderBy(x => x.Z))
            {
                elm.Z = count;
                count++;
            }

            if (ParentWindow.HighestZ == element.Z)
            {
                var item = ParentWindow.RootElements.OrderByDescending(e => e.Z).FirstOrDefault();
                if (item == null)
                {
                    ParentWindow.HighestZ = 0;
                }
            }

        }
        protected override void RenderContent(ConsoleBuffer buffer)
        {
            if (!IsVisible) return;

            int innerWidth = ActualWidth - (ShowBorder ? 1 : 0);
            int innerHeight = ActualHeight - (ShowBorder ? 0 : 0);
            int startXOffset = ShowBorder ? 1 : 0;
            int startYOffset = ShowBorder ? 1 : 0;

            try
            {
                // Calculate layouts and render elements
                var objectBuffers = new ConcurrentBag<(ConsoleBuffer buffer, UIElement element)>();
                var delElms = Children.Where(e => e.RenderThreadDeleteMe);
                foreach (var item in delElms)
                {
                    RemoveElement(item);
                }
                var visibleElms = Children.Where(x => x.IsVisible).ToList();
                foreach (var element in visibleElms)
                {
                    ConsoleBuffer elementBuffer = null;
                    if (element.NeedsRecalculation)
                    {
                        element.CalculateLayout(startXOffset, startYOffset, innerWidth, innerHeight);
                        elementBuffer = element.Render();

                        if (element.EnableCaching)
                        {
                            lock (BufferCache)
                            {
                                BufferCache[element.UID] = elementBuffer;
                            }
                        }
                    }
                    else
                    {
                        element.CalculateLayout(startXOffset, startYOffset, innerWidth, innerHeight);
                        bool bufferFound = false;
                        if (element.EnableCaching)
                        {
                            lock (BufferCache)
                            {
                                if (BufferCache.TryGetValue(element.UID, out elementBuffer))
                                {
                                    bufferFound = true;
                                }
                            }
                        }

                        if (!bufferFound)
                        {
                            elementBuffer = element.Render();

                            if (element.EnableCaching)
                            {
                                lock (BufferCache)
                                {
                                    BufferCache[element.UID] = elementBuffer;
                                }
                            }
                        }
                    }

                    objectBuffers.Add((elementBuffer, element));
                };

                var lst = objectBuffers.OrderBy(e => e.element.Z).ToList();
                foreach (var element in lst)
                {
                    buffer.WriteBuffer(element.element.ActualX, element.element.ActualY, element.buffer, element.element.RespectBackgroundOnDraw);
                };
            }
            catch (Exception e)
            {
                buffer.WriteStringWrapped(0, 0, e.Message, Console.WindowWidth - 2, Color.White, Color.Transparent);
            }
        }


    }
}
