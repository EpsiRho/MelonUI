using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace MelonUI.Base
{
    public class MUIPage : UIElement
    {
        public string MXML { get; set; }
        private readonly List<Assembly> Assemblies = new List<Assembly>();
        private readonly List<string> Namespaces = new List<string>();
        public bool Failed = false;
        public List<string> CompilerMessages = new List<string>();
        public List<string> Backends = new List<string>();
        public Dictionary<string, object> BackendObjects = new Dictionary<string, object>();

        private int zCounter = 0;

        public MUIPage() {
            ConsiderForFocus = false;
        }


        // Public Methods
        /// <summary>
        /// Reads the file’s text content and compiles the MXML.
        /// </summary>
        public bool CompileFile(string filePath)
        {
            string xml = File.ReadAllText(filePath);
            return Compile(xml);
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

            XElement root;
            try
            {
                root = XElement.Parse(mxmlString);
            }
            catch (Exception e)
            {
                CompilerMessages.Add("Generic XML formatting error, see following line:");
                CompilerMessages.Add(e.Message);
                return false;
            }

            // Process top-level attributes (Namespaces, Assemblies, Backends, etc.)
            if (!ParseTopLevelAttributes(root)) return false;

            // Parse child elements (UIElements, etc.)
            foreach (var child in root.Elements())
            {
                var uiElement = ParseElement(child);
                if (Failed) return false;
                if (uiElement != null) AddElement((UIElement)uiElement);
            }

            // Reverse so items are in the same order as in MXML
            Children.Reverse();

            return true;
        }

        /// <summary>
        /// Adds a new UIElement to this page's Children list.
        /// </summary>
        public void AddElement(UIElement elm)
        {
            elm.Z = zCounter;
            Children.Add(elm);
        }

        /// <summary>
        /// Finds a property reference (Class.Property).
        /// </summary>
        public (object Instance, PropertyInfo Property) FindPropertyReference(string classAndProperty)
        {
            if (string.IsNullOrWhiteSpace(classAndProperty))
            {
                CompilerMessages.Add("Property Reference expected, but no name found!");
                Failed = true;
                return (null, null);
            }

            var parts = classAndProperty.Split('.');
            if (parts.Length != 2)
            {
                CompilerMessages.Add("Property Reference format is invalid, should look like \"ClassName.PropertyName\"");
                Failed = true;
                return (null, null);
            }

            string className = parts[0];
            string propertyName = parts[1];
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
                var staticProp = foundType.GetProperty(propertyName,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (staticProp != null) return (foundType, staticProp);

                if (BackendObjects.TryGetValue(className, out var backendInstance))
                {
                    var instanceProp = backendInstance.GetType().GetProperty(propertyName,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    if (instanceProp != null)
                        return (backendInstance, instanceProp);
                }

                CompilerMessages.Add($"Property Reference {classAndProperty} found a type, but could not instance or find the property. (Are you using Fields or Properties? Is your Property Public?)");
                Failed = true;
                return (null, null);
            }
            else
            {
                // No direct type found. Maybe a backend instance.
                if (BackendObjects.TryGetValue(className, out var backendInstance))
                {
                    var instanceProp = backendInstance.GetType().GetProperty(propertyName,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    if (instanceProp != null)
                        return (backendInstance, instanceProp);
                }

                CompilerMessages.Add($"Property Reference {classAndProperty} no type, instance, or property. (Are you using Fields or Properties? Is your Property Public?)");
                Failed = true;
                return (null, null);
            }
        }


        // Compiler Methods
        /// <summary>
        /// Given a name like "BackendClass.MemberName", tries to create a Binding object.
        /// </summary>
        private Binding CreateBinding(string classAndMemberPath)
        {
            if (string.IsNullOrWhiteSpace(classAndMemberPath))
            {
                Failed = true;
                CompilerMessages.Add("The Binding path must not be empty.");
                return null;
            }

            var parts = classAndMemberPath.Split('.');
            if (parts.Length < 2)
            {
                Failed = true;
                CompilerMessages.Add($"The Binding \"{classAndMemberPath}\" is invalid. Should be in format \"BackendClass.MemberName\".");
                return null;
            }

            string initialClassName = parts[0];
            object currentObject = null;
            Type currentType = null;

            if (BackendObjects.TryGetValue(initialClassName, out currentObject))
            {
                currentType = currentObject.GetType();
            }
            else
            {
                currentType = FindElementType(initialClassName);
                if (currentType == null)
                {
                    Failed = true;
                    CompilerMessages.Add($"Cannot find class or backend object named \"{initialClassName}\".");
                    return null;
                }
            }

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
                        Failed = true;
                        CompilerMessages.Add($"Property \"{memberName}\" on \"{currentType.Name}\" is null.");
                        return null;
                    }
                    currentType = currentObject.GetType();
                    continue;
                }

                // If property not found, check if there's a method with that name for event binding
                var method = currentType.GetMethod(memberName,BindingFlags.Static |
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
                    Failed = true;
                    CompilerMessages.Add($"Cannot traverse through event \"{memberName}\" in binding path.");
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
                Failed = true;
                CompilerMessages.Add($"Member \"{memberName}\" not found on type \"{currentType.Name}\".");
                return null;
            }

            Failed = true;
            CompilerMessages.Add("Invalid Binding path!");
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
                        SetProperty(this, attr.Name.LocalName, attr.Value, new Dictionary<string, string>());
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
            if (element.Name.LocalName.Contains("."))
            {
                Failed = true;
                CompilerMessages.Add("Top-Level elements cannot have dots in their name.");
                return null;
            }

            var elementType = FindElementType(element.Name.LocalName);
            if (elementType == null)
            {
                Failed = true;
                CompilerMessages.Add($"Unkown element type \"{element.Name.LocalName}\".");
                return null;
            }

            var uiElement = Activator.CreateInstance(elementType);

            // 1) Set properties from attributes
            var flagsFromElement = GetFlagsFromElement(element);
            foreach (var attr in element.Attributes())
            {
                SetProperty(uiElement, attr.Name.LocalName, attr.Value, flagsFromElement);
                if (Failed) return null;
            }

            // 2) Handle child elements
            foreach (var child in element.Elements())
            {
                if (Failed) return null;

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
            }

            return uiElement;
        }

        /// <summary>
        /// Parses any object that is not a UIElement (custom object).
        /// </summary>
        private object ParseCustomObject(XElement element)
        {
            if (element.Name.LocalName.Contains("."))
            {
                Failed = true;
                CompilerMessages.Add("Top-Level objects cannot have dots.");
                return null;
            }

            var objectType = FindElementType(element.Name.LocalName);
            if (objectType == null)
            {
                Failed = true;
                CompilerMessages.Add($"Unknown custom object type: {element.Name.LocalName}");
                return null;
            }

            var customObject = Activator.CreateInstance(objectType);

            // Set properties from attributes
            foreach (var attr in element.Attributes())
            {
                SetProperty(customObject, attr.Name.LocalName, attr.Value, new Dictionary<string, string>());
                if (Failed) return customObject;
            }

            // Process nested elements
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
                        SetProperty(customObject, child.Name.LocalName, parsedChild, new Dictionary<string, string>());
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
                CompilerMessages.Add($"Unknown type for element: {element.Name.LocalName}");
                Failed = true;
                return null;
            }

            if (typeof(UIElement).IsAssignableFrom(childType))
                return ParseElement(element);
            else
                return ParseCustomObject(element);
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
                CompilerMessages.Add("MXMLFlags cannot be empty, if you dont need to set any, remove the property.");
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
                CompilerMessages.Add($"MXMLFlag \"${mxmlFlags}\" is invalid!");
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
        private void SetProperty(object element, string propertyName, object value, Dictionary<string, string> mxmlMeta)
        {
            var prop = element.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            if (prop == null || !prop.CanWrite)
            {
                // If property not found, check if there's a method with that name for event binding
                var method = element.GetType().GetMethod(propertyName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                if (method != null)
                {
                    HandleMethodBinding(element, propertyName, value, method);
                }
                else
                {
                    Failed = true;
                    CompilerMessages.Add($"Element property \"{propertyName}\" cannot be found. (Are you using Fields or Properties? Is your Property Public?)");
                }
                return;
            }

            // If string looks like {BackendProp}, handle binding
            if (value is string str && str.StartsWith("{") && str.EndsWith("}"))
            {
                HandlePropertyBinding(element, propertyName, str);
                return;
            }

            // Determine property type (Action, List<T>, or simple property)
            var propType = prop.PropertyType;
            if (propType == typeof(Action))
                HandleActionProperty(element, prop, value, mxmlMeta);
            else if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(List<>))
                HandleListProperty(element, prop, value, propertyName);
            else
                HandleSimpleProperty(element, prop, value, propertyName);
        }

        /// <summary>
        /// Converts a value into a specified target type if possible.
        /// </summary>
        private object ConvertToType(object value, Type targetType)
        {
            if (value == null)
            {
                if (!targetType.IsValueType || (Nullable.GetUnderlyingType(targetType) != null))
                {
                    // null is okay
                }
                else
                {
                    Failed = true;
                    CompilerMessages.Add($"{targetType.Name} cannot be null, either set a value or remove the property from the MXML doc.");
                    return null;
                }
            }

            // If already assignable, return
            if (value != null && targetType.IsAssignableFrom(value.GetType()))
                return value;

            // If target is List<T> and value is enumerable, convert accordingly
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
                return ConvertList(value, targetType);

            // Try direct string conversions for primitives, enums, Color, etc.
            string strValue = value?.ToString() ?? string.Empty;
            if (TryPrimitiveConversion(strValue, targetType, out var result)) return result;

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

            // Try a string constructor
            var stringCtor = targetType.GetConstructor(new[] { typeof(string) });
            if (stringCtor != null) return stringCtor.Invoke(new object[] { strValue });

            // As a last fallback, try parameterless constructor
            if (targetType.GetConstructor(Type.EmptyTypes) != null)
                return Activator.CreateInstance(targetType);

            // If all else fails, mark as failed
            Failed = true;
            CompilerMessages.Add($"The value \"{value}\" for property type \"{targetType}\" cannot be converted.");
            return value;
        }


        // Helper Sub-Methods
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
                    CompilerMessages.Add($"Assembly \"{str}\" could not be found!");
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
                    CompilerMessages.Add($"Backend \"{str}\" could not be found!");
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
            var parts = child.Name.LocalName.Split('.');
            if (parts.Length != 2)
            {
                Failed = true;
                CompilerMessages.Add($"Too many parts, mxml cannot currently handled nested properties like this!");
                return false;
            }

            var propertyName = parts[1];
            var nestedEls = child.Elements().ToList();

            if (nestedEls.Count == 0)
            {
                var flags = GetFlagsFromElement(child);
                SetProperty(uiElement, propertyName, "", flags);
            }
            else if (nestedEls.Count == 1)
            {
                var propertyValue = ParseElement(nestedEls[0]);
                if (propertyValue != null)
                {
                    var flags = GetFlagsFromElement(child);
                    SetProperty(uiElement, propertyName, propertyValue, flags);
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
                        CompilerMessages.Add($"Child element \"{ne.Name.LocalName}\" is unknown");
                        return false;
                    }
                    if (childType != null)
                    {
                        if (typeof(UIElement).IsAssignableFrom(childType))
                        {
                            var childElement = ParseElement(ne);
                            AddElement((UIElement)childElement);
                        }
                        else
                        {
                            var childObject = ParseMXMLElement(ne);
                            if (childObject != null)
                            {
                                var flags = GetFlagsFromElement(ne);
                                SetProperty(uiElement, propertyName, childObject, flags);
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
                CompilerMessages.Add($"Child element \"{child.Name.LocalName}\" is unknown");
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
                        SetProperty(property, attr.Name.LocalName, attr.Value, flags);
                        if (Failed) return false;
                    }
                }
            }
            return !Failed;
        }

        private void HandleMethodBinding(object element, string propertyName, object value, MethodInfo method)
        {
            if (value is string strVal && strVal.StartsWith("{") && strVal.EndsWith("}"))
            {
                string bindingRef = strVal.Substring(1, strVal.Length - 2).Trim();
                try
                {
                    var binding = CreateBinding(bindingRef);
                    if (binding.IsEvent)
                    {
                        Delegate handlerDelegate = Delegate.CreateDelegate(binding.EventInfo.EventHandlerType, element, method);
                        binding.Subscribe(handlerDelegate);
                        ((UIElement)element).SetBinding(propertyName, binding);
                    }
                    else
                    {
                        CompilerMessages.Add($"Binding \"{bindingRef}\" is not an event and cannot be assigned to method \"{propertyName}\".");
                        Failed = true;
                    }
                }
                catch (Exception ex)
                {
                    Failed = true;
                    CompilerMessages.Add($"Failed to bind \"{bindingRef}\" to method \"{propertyName}\": {ex.Message}");
                }
            }
            else
            {
                CompilerMessages.Add($"Invalid binding value for method \"{propertyName}\". Expected a binding expression.");
                Failed = true;
            }
        }

        private void HandlePropertyBinding(object element, string propertyName, string str)
        {
            string name = str.Replace("{", "").Replace("}", "");
            var binding = CreateBinding(name);
            if (binding == null)
            {
                Failed = true;
                CompilerMessages.Add($"Property Binding \"{name}\" could not be found. (Check your backend properties, make sure they are not fields)");
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
                CompilerMessages.Add($"Property Binding \"{name}\" failed to bind. ({er.Message})");
            }
        }

        private void HandleActionProperty(object element, PropertyInfo prop, object value, Dictionary<string, string> mxmlMeta)
        {
            if (value is UIElement uiVal)
            {
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
                        showElm.IsFocused = true;
                        ParentWindow.MoveFocus(showElm);
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
                        var showElm = Children.Find(x => x.Name == mxmlMeta["Show"]);
                        if (focus)
                        {
                            showElm.IsVisible = true;
                            showElm.IsFocused = true;
                            ParentWindow.MoveFocus(showElm);
                        }
                    }
                };
                prop.SetValue(element, gen);
            }
        }

        private void HandleListProperty(object element, PropertyInfo prop, object value, string propertyName)
        {
            var propType = prop.PropertyType;
            var elementType = propType.GetGenericArguments()[0];

            if (value is System.Collections.IEnumerable en && !(value is string))
            {
                var convertedValue = ConvertToType(value, propType);
                if (Failed)
                {
                    CompilerMessages.Add($"Element property \"{propertyName}\"/'s set value is invalid.");
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
                var convertedItem = ConvertToType(value, elementType);
                if (Failed)
                {
                    CompilerMessages.Add($"Element property \"{propertyName}\"/'s set value is invalid.");
                    return;
                }
                var addMethod = propType.GetMethod("Add");
                addMethod.Invoke(listInstance, new[] { convertedItem });
            }
        }

        private void HandleSimpleProperty(object element, PropertyInfo prop, object value, string propertyName)
        {
            var convertedValue = ConvertToType(value, prop.PropertyType);
            if (Failed)
            {
                CompilerMessages.Add($"Element property \"{propertyName}\"/'s set value is invalid.");
                return;
            }
            prop.SetValue(element, convertedValue);
        }

        private object ConvertList(object value, Type targetType)
        {
            var elementType = targetType.GetGenericArguments()[0];
            if (value is System.Collections.IEnumerable en && !(value is string))
            {
                var listInstance = Activator.CreateInstance(targetType);
                var addMethod = targetType.GetMethod("Add");

                foreach (var item in en)
                {
                    var convertedItem = ConvertToType(item, elementType);
                    if (Failed) return listInstance;
                    addMethod.Invoke(listInstance, new[] { convertedItem });
                }
                return listInstance;
            }
            else
            {
                var singleConverted = ConvertToType(value, elementType);
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
            if (split.Length != 4) return false;

            if (!int.TryParse(split[0], out var a)
                || !int.TryParse(split[1], out var r)
                || !int.TryParse(split[2], out var g)
                || !int.TryParse(split[3], out var b))
            {
                return false;
            }

            color = Color.FromArgb(a, r, g, b);
            return true;
        }

        private bool TryMethodParse(string strValue, MethodInfo parseMethod, out object parsedVal)
        {
            parsedVal = null;

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
            return false;
        }

        // Render Page Elements
        protected override void RenderContent(ConsoleBuffer buffer)
        {
            if (!IsVisible) return;

            int innerWidth = ActualWidth - (ShowBorder ? 1 : 0);
            int innerHeight = ActualHeight - (ShowBorder ? 0 : 0);
            int startXOffset = ShowBorder ? 1 : 0;
            int startYOffset = ShowBorder ? 1 : 0;

            try
            {
                var buffers = new List<(UIElement element, ConsoleBuffer buffer)>();
                foreach (var pos in Children.Where(x => x.IsVisible))
                {
                    pos.CalculateLayout(startXOffset, startYOffset, innerWidth, innerHeight);
                    var elementBuffer = pos.Render();
                    buffers.Add((pos, elementBuffer));
                }

                var ordered = buffers.OrderBy(e => e.element.Z).ToList();
                foreach (var (element, elementBuffer) in ordered)
                {
                    buffer.WriteBuffer(element.ActualX, element.ActualY, elementBuffer);
                }
            }
            catch(Exception e) 
            {
                buffer.WriteStringWrapped(0, 0, e.Message, Console.WindowWidth - 2, Color.White, Color.Transparent);
            }
        }


    }
}
