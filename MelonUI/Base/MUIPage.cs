using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MelonUI.Base
{
    public class MUIPage : UIElement
    {
        public string MXML { get; set; }
        private List<Assembly> Assemblies = new List<Assembly>();
        private List<string> Namespaces = new List<string>();
        public bool Failed = false;
        public List<string> CompilerMessages = new List<string>();
        public List<string> Backends = new List<string>();
        public Dictionary<string, object> BackendObjects = new Dictionary<string, object>();
        public override bool ConsiderForFocus { get; set; } = false;
        private int zCounter = 0;
        public void AddElement(UIElement elm)
        {
            elm.Z = zCounter;
            Children.Add(elm);
        }
        public bool Compile(string mxmlString)
        {
            zCounter = Z;
            Failed = false;
            MXML = mxmlString;
            CompilerMessages.Clear();
            Children.Clear();
            XElement root = null;
            try
            {
                root = XElement.Parse(mxmlString);
            }
            catch(Exception e)
            {
                CompilerMessages.Add($"Generic XML formatting error, see following line:");
                CompilerMessages.Add(e.Message);
                return false;
            }

            // Get Page     
            var pageAttrs = root.Attributes();
            foreach (var attr in pageAttrs)
            {
                // If special properties are found
                if (attr.Name.LocalName == "MXMLFlags")
                {
                    continue;
                }
                if (attr.Name.LocalName == "Namespaces") // We are importing a namespace
                {
                    string[] split = attr.Value.Split(',');
                    foreach(var str in split)
                    {
                        Namespaces.Add(str);
                    }
                    continue;
                }
                if (attr.Name.LocalName == "Backends") // Defining a backend
                {
                    string[] split = attr.Value.Split(',');
                    foreach (var str in split)
                    {
                        var propType = FindElementType(str);
                        if(propType == null)
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
                        catch (Exception)
                        {

                        }
                    }
                    continue;
                }
                if (attr.Name.LocalName == "Assemblies") // Defining an assembly by name
                {
                    string[] split = attr.Value.Split(',');
                    foreach (var str in split)
                    {
                        string name = str;
                        var ass = AppDomain.CurrentDomain.GetAssemblies().
                                    SingleOrDefault(assembly => assembly.GetName().Name == name);
                        Assemblies.Add(ass);
                    }
                    continue;

                }

                // Else, set normal properties
                SetProperty(this, attr.Name.LocalName, attr.Value, new Dictionary<string, string>());
            }
            

            // Compile Page
            foreach (var child in root.Elements())
            {
                var uiElement = ParseElement(child);
                if (Failed) return false;
                if (uiElement != null) AddElement((UIElement)uiElement);
            }

            // Reverse the list so that items are ordered the same as they are in mxml
            Children.Reverse();

            return true;
        }

        private Type FindElementType(string typeName)
        {
            // Looking thru each assembly and namespace to find the element
            foreach (var asm in Assemblies)
            {
                foreach (var space in Namespaces)
                {
                    var t = asm.GetType($"{space}.{typeName}", throwOnError: false, ignoreCase: false);
                    if (t != null)
                    {
                        return t;
                    }
                }
            }

            return null;
        }
        public (object Instance, PropertyInfo Property) FindPropertyReference(string classAndProperty)
        {
            if (string.IsNullOrWhiteSpace(classAndProperty))
            {
                CompilerMessages.Add($"Property Reference expected, but no name found!");
                Failed = true;
                return (null, null);
            }

            var parts = classAndProperty.Split('.');
            if (parts.Length != 2)
            {
                // Invalid format
                CompilerMessages.Add($"Property Reference format is invalid, should look like \"ClassName.PropertyName\"");
                Failed = true;
                return (null, null);
            }

            string className = parts[0];
            string propertyName = parts[1];

            // 1. Try to find a type in the known assemblies and namespaces
            Type foundType = null;
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

            if (foundType != null)
            {
                // Check if the property is static
                var staticProp = foundType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (staticProp != null)
                {
                    // It's a static property
                    return (foundType, staticProp);
                }

                // Not static, so needs an instance. Check BackendObjects for an instance
                if (BackendObjects.TryGetValue(className, out var backendInstance))
                {
                    var instanceProp = backendInstance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    if (instanceProp != null)
                    {
                        return (backendInstance, instanceProp);
                    }
                }

                // If we found the type but no suitable instance or property,
                CompilerMessages.Add($"Property Reference {classAndProperty} found a type, but could not instance or find the property. (Are you using Fields or Properties? Is your Property Public?)");
                Failed = true;
                return (null, null);
            }
            else
            {
                // No type found. Maybe it's only known at runtime via BackendObjects
                if (BackendObjects.TryGetValue(className, out var backendInstance))
                {
                    var instanceProp = backendInstance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    if (instanceProp != null)
                    {
                        return (backendInstance, instanceProp);
                    }
                }

                CompilerMessages.Add($"Property Reference {classAndProperty} no type, instance, or property. (Are you using Fields or Properties? Is your Property Public?)");
                Failed = true;
                return (null, null);
            }
        }
        public Dictionary<string, string> GetFlagsFromElement(XElement element)
        {
            if (element == null)
            {
                return new Dictionary<string, string>();
            }

            var xmlFlagsAttr = element.Attribute("MXMLFLags");
            if (xmlFlagsAttr == null || string.IsNullOrWhiteSpace(xmlFlagsAttr.Value))
                return new Dictionary<string, string>();

            return ParseFlags(xmlFlagsAttr.Value);
        }
        public Dictionary<string, string> ParseFlags(string mxmlFlags)
        {
            var results = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(mxmlFlags))
            {
                Failed = true;
                CompilerMessages.Add($"MXMLFlags cannot be empty, if you dont need to set any, remove the property.");
                return results;
            }

            // Split by comma to get individual flags
            var flags = mxmlFlags.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var flag in flags)
            {
                var trimmedFlag = flag.Trim();
                // Expecting pattern: Action(Target)
                // Find the opening and closing parentheses
                int openParenIndex = trimmedFlag.IndexOf('(');
                int closeParenIndex = trimmedFlag.IndexOf(')');

                if (openParenIndex > 0 && closeParenIndex > openParenIndex)
                {
                    string action = trimmedFlag.Substring(0, openParenIndex).Trim();
                    string target = trimmedFlag.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1).Trim();

                    // Only add if both action and target are non-empty
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
        private object ParseMXMLElement(XElement element)
        {
            var childType = FindElementType(element.Name.LocalName);
            if (childType == null)
            {
                CompilerMessages.Add(($"Unknown type for element: {element.Name.LocalName}"));
                Failed = true;
                return null;
            }

            // Check if it's a UIElement
            if (typeof(UIElement).IsAssignableFrom(childType))
            {
                return ParseElement(element); // Existing method for UIElements
            }
            else
            {
                // It's a custom object
                return ParseCustomObject(element);
            }
        }
        private object ParseElement(XElement element)
        {
            // The tag name corresponds to the type name. For example: <TextBox ...>
            // If there is a dot in the name (e.g., MenuItem.OnSelect), that indicates a nested property definition.
            // But at the top-level, we should not have a dot. If we do, it's actually a property of the parent element.
            if (element.Name.LocalName.Contains("."))
            {
                // This is a nested property element, not a root element. We should handle this differently.
                Failed = true;
                CompilerMessages.Add("Top-Level elements cannot have dots in their name.");
                return null;
            }

            var elementType = FindElementType(element.Name.LocalName);
            if (elementType == null)
            {
                // Unknown element type, Fail Compile.
                Failed = true;
                CompilerMessages.Add($"Unkown element type \"{element.Name.LocalName}\".");
                return null;
            }

            var uiElement = Activator.CreateInstance(elementType);

            // Set properties from attributes
            foreach (var attr in element.Attributes())
            {
                var flags = GetFlagsFromElement(element);
                SetProperty(uiElement, attr.Name.LocalName, attr.Value, flags);
                if (Failed) return null;
            }

            // Now handle child elements
            // Child elements can be:
            // 1. Normal UI elements (<OptionsMenu></OptionsMenu>)
            // 2. Nested property definitions (<MenuItem.OnSelect></MenuItem.OnSelect>)

            foreach (var child in element.Elements())
            {
                if (Failed) return null;
                if (child.Name.LocalName.Contains("."))
                {
                    // Nested property, something like <MenuItem.OnSelect> ... </MenuItem.OnSelect>
                    var parts = child.Name.LocalName.Split('.');
                    if (parts.Length == 2)
                    {
                        var propertyName = parts[1]; // e.g., OnSelect
                                                     // The content inside <MenuItem.OnSelect> could be one or multiple UIElements.
                        var nestedEls = child.Elements().ToList();
                        if(nestedEls.Count == 0)
                        {
                            var flags = GetFlagsFromElement(child);
                            SetProperty(uiElement, propertyName, "", flags);
                        }
                        if (nestedEls.Count == 1) // Only one nested object
                        {
                            var propertyValue = ParseElement(nestedEls[0]);
                            if (propertyValue != null)
                            {
                                var flags = GetFlagsFromElement(child);
                                SetProperty(uiElement, propertyName, propertyValue, flags);
                                if (Failed) return null;
                            }
                        }
                        else if (nestedEls.Count > 1) // A collection of nested objects
                        {
                            foreach (var ne in nestedEls)
                            {
                                var childType = FindElementType(ne.Name.LocalName);
                                if (Failed)
                                {
                                    CompilerMessages.Add($"Child element \"{ne.Name.LocalName}\" is unknown");
                                    return null;
                                }
                                if (childType != null)
                                {
                                    if (typeof(UIElement).IsAssignableFrom(childType))
                                    {
                                        var childElement = ParseElement(ne);
                                        AddElement((UIElement)childElement);
                                    }
                                    else // Custom Objects need special parsing
                                    {
                                        var childObject = ParseMXMLElement(ne);
                                        if (childObject != null)
                                        {
                                            var flags = GetFlagsFromElement(ne);
                                            SetProperty(uiElement, propertyName, childObject, flags);
                                            if (Failed) return null;
                                        }
                                    }
                                }
                            }
                        }


                    }
                }
                else
                {
                    // This is a normal child UI element
                    //var childElement = ParseElement(child);
                    var childType = FindElementType(child.Name.LocalName);
                    if (Failed)
                    {
                        CompilerMessages.Add($"Child element \"{child.Name.LocalName}\" is unknown");
                        return null;
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
                            foreach (var attr in element.Attributes())
                            {
                                var flags = GetFlagsFromElement(child);
                                SetProperty(property, attr.Name.LocalName, attr.Value, flags);
                                if (Failed) return null;
                            }
                        }

                    }
                }
            }

            return uiElement;
        }
        private object ParseCustomObject(XElement element)
        {
            // The element name gives us the type name. (e.g., <MenuItem ...>)
            if (element.Name.LocalName.Contains("."))
            {
                Failed = true;
                CompilerMessages.Add($"Top-Level objects cannot have dots.");
                return null;
            }

            var objectType = FindElementType(element.Name.LocalName);
            if (objectType == null)
            {
                Failed = true;
                CompilerMessages.Add($"Unknown custom object type: {element.Name.LocalName}");
                return null;
            }

            // Create an instance of the custom object
            var customObject = Activator.CreateInstance(objectType);

            // Set properties from attributes
            foreach (var attr in element.Attributes())
            {
                SetProperty(customObject, attr.Name.LocalName, attr.Value, new Dictionary<string, string>());
                if (Failed) return customObject; // If conversion failed
            }

            // Process nested elements, which can be:
            // 1. Nested properties (<Object.PropertyName>...</Object.PropertyName>)
            // 2. Child objects that should be assigned to a property that holds a single object or a collection
            //    - This can be either another custom object or a UIElement.

            foreach (var child in element.Elements())
            {
                // Check if this is a nested property definition, indicated by "ParentType.PropertyName"
                if (child.Name.LocalName.Contains("."))
                {
                    var parts = child.Name.LocalName.Split('.');
                    if (parts.Length == 2)
                    {
                        var propertyName = parts[1]; // the property name after the dot
                                                     // The content inside could be UIElements or other custom objects
                                                     // If multiple elements, consider if property is a list or single object.
                        var nestedElements = child.Elements().ToList();

                        // If there's only one nested element, we can directly parse it
                        if (nestedElements.Count == 0)
                        {
                            var flags = GetFlagsFromElement(child);
                            SetProperty(customObject, propertyName, "", flags);
                        }
                        else if (nestedElements.Count == 1)
                        {
                            var nested = ParseMXMLElement(nestedElements[0]);
                            if (Failed) return customObject;
                            if (nested != null)
                            {
                                var flags = GetFlagsFromElement(child);
                                SetProperty(customObject, propertyName, nested, flags);
                                if (Failed) return customObject;
                            }
                        }
                        else if (nestedElements.Count > 1)
                        {
                            foreach (var ne in nestedElements)
                            {
                                var childType = FindElementType(ne.Name.LocalName);
                                if (Failed)
                                {
                                    CompilerMessages.Add($"Child element \"{ne.Name.LocalName}\" is unknown");
                                    return null;
                                }
                                if (childType != null)
                                {
                                    if (typeof(UIElement).IsAssignableFrom(childType))
                                    {
                                        var childElement = ParseElement(ne);
                                        if (childElement != null)
                                        {
                                            var flags = GetFlagsFromElement(ne);
                                            SetProperty(customObject, propertyName, childElement, flags);
                                            if (Failed) return null;
                                            AddElement((UIElement)childElement);
                                        }
                                    }
                                    else // Custom Objects need special parsing
                                    {
                                        var childObject = ParseMXMLElement(ne);
                                        if (childObject != null)
                                        {
                                            var flags = GetFlagsFromElement(ne);
                                            SetProperty(childObject, propertyName, childObject, flags);
                                            if (Failed) return null;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    var parsedChild = ParseMXMLElement(child);
                    if (parsedChild != null && !Failed)
                    {
                        // Find a suitable collection property:
                        SetProperty(customObject, child.Name.LocalName, parsedChild, new Dictionary<string, string>());
                        if (Failed) return customObject;
                    }
                }
            }

            return customObject;
        }

        private void SetProperty(object element, string propertyName, object value, Dictionary<string, string> mxmlMeta)
        {
            //var props = element.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            var prop = element.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            if (prop == null || !prop.CanWrite)
            {
                // Property not found; attempt to find a method with the same name
                var method = element.GetType().GetMethod(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                if (method != null)
                {
                    // Assume that if the value is a binding expression, it's an event binding
                    if (value is string strVal && strVal.StartsWith("{") && strVal.EndsWith("}"))
                    {
                        string bindingRef = strVal.Substring(1, strVal.Length - 2).Trim();
                        try
                        {
                            Binding binding = CreateBinding(bindingRef);
                            bool fuck = false;
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
                                return;
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
                else
                {
                    // Property not found or not writable. Fail Compilation
                    Failed = true;
                    CompilerMessages.Add($"Element property \"{propertyName}\" cannot be found. (Are you using Fields or Properties? Is your Property Public?)");
                    return;

                }

            }

            // Search backends if string looks like {backendProp}
            if (value.GetType() == typeof(string))
            {
                string name = (string)value;
                if(name.StartsWith("{") && name.EndsWith("}"))
                {
                    name = name.Replace("{", "").Replace("}", "");
                    Binding binding = CreateBinding(name);
                    try
                    {
                        if (!binding.IsEvent)
                        {
                            ((UIElement)element).SetBinding(propertyName, binding);
                        }
                        else
                        {
                            return;
                        }
                    }
                    catch (Exception)
                    {
                        Failed = true;
                        CompilerMessages.Add($"Property Binding could not be found!");
                    }
                    return;
                }
            }

            var propType = prop.PropertyType;

            // Check if target property is a List<T>
            if (propType == typeof(Action))
            {
                // The item is an action
                // If we recieve a string we should search for it in the provided backends
                // If we recieve a UIElement, we need to create an action to handle showing it.
                //

                if (typeof(UIElement).IsAssignableFrom(value.GetType()))
                {
                    string tempName = "";
                    if(((UIElement)value).Name == "")
                    {
                        tempName = Guid.NewGuid().ToString();
                        ((UIElement)value).Name = tempName;
                    }
                    else
                    {
                        tempName = ((UIElement)value).Name;
                    }
                    ((UIElement)value).IsVisible = false;
                    AddElement((UIElement)value);
                    Action gen = () => // Auto Generated Action to show/hide Elements
                    {
                        if (tempName != "")
                        {
                            var elm = Children.Find(x => x.Name == tempName);
                            elm.IsVisible = true;
                            ((UIElement)value).IsVisible = true;
                            ParentWindow.MoveFocus(elm);
                        }
                        if (mxmlMeta.ContainsKey("Hide"))
                        {
                            var elm = Children.Find(x => x.Name == mxmlMeta["Hide"]);
                            elm.IsVisible = false;
                        }
                        if (mxmlMeta.ContainsKey("Show"))
                        {
                            string[] info = mxmlMeta["Show"].Split(":");
                            bool focus = info.Length == 2 ? bool.Parse(info[1]) : true;
                            var elm = Children.Find(x => x.Name == mxmlMeta["Show"]);
                            elm.IsVisible = true;
                            elm.IsFocused = true;
                            ParentWindow.MoveFocus(elm);
                        }
                    };
                    var existingValue = prop.GetValue(element);
                    if (existingValue == null)
                    {
                        // First item
                        prop.SetValue(element, gen);
                    }
                    else
                    {
                        // If there is currently an Action, 
                        // wrap them into a new Action:
                        Action collection = () =>
                        {
                            ((Action)existingValue)();
                            gen();
                        };
                        prop.SetValue(element, collection);
                    }

                }
                else
                {
                    Action gen = () => // Auto Generated Action to show/hide Elements
                    {
                        if (mxmlMeta.ContainsKey("Hide"))
                        {
                            var elm = Children.Find(x => x.Name == mxmlMeta["Hide"]);
                            elm.IsVisible = false;
                        }
                        if (mxmlMeta.ContainsKey("Show"))
                        {
                            string[] info = mxmlMeta["Show"].Split(":");
                            bool focus = info.Length == 2 ? bool.Parse(info[1]) : true;
                            var elm = Children.Find(x => x.Name == mxmlMeta["Show"]);
                            if (focus)
                            {
                                elm.IsVisible = true;
                                elm.IsFocused = true;
                                ParentWindow.MoveFocus(elm);
                            }
                        }
                    };
                    prop.SetValue(element, gen);
                }

            }
            else if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementType = propType.GetGenericArguments()[0];

                // If value is already a list or enumerable (and not a string)
                if (value is System.Collections.IEnumerable enumerable && !(value is string))
                {
                    // Convert using the normal convert method (it handles lists)
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
                    // Value is a single item. Convert it to the elementType and add it to the list.
                    var listInstance = prop.GetValue(element);
                    if (listInstance == null)
                    {
                        // Create a new list instance if null
                        listInstance = Activator.CreateInstance(propType);
                        prop.SetValue(element, listInstance);
                    }

                    // Convert the single value to the list's element type
                    object convertedItem = ConvertToType(value, elementType);
                    if (Failed)
                    {
                        CompilerMessages.Add($"Element property \"{propertyName}\"/'s set value is invalid.");
                        return;
                    }

                    // Add the converted item to the list
                    var addMethod = propType.GetMethod("Add");
                    addMethod.Invoke(listInstance, new[] { convertedItem });
                }
            }
            else
            {
                // Not a list property, just do a normal conversion and set 	MelonUI.dll!MelonUI.Managers.MUIC.Compile(string mxmlString) Line 33	C#

                object convertedValue = ConvertToType(value, prop.PropertyType);
                if (Failed)
                {
                    CompilerMessages.Add($"Element property \"{propertyName}\"/'s set value is invalid.");
                    return;
                }

                prop.SetValue(element, convertedValue);
            }
        }
        private object ConvertToType(object value, Type targetType)
        {
            if (value == null)
            {
                // If targetType is a class or nullable, null is okay; otherwise, set Failed.
                if (!targetType.IsValueType || (Nullable.GetUnderlyingType(targetType) != null))
                {
                    return null;
                }
                else
                {
                    Failed = true;
                    CompilerMessages.Add($"{targetType.Name} cannot be null, either set a value or remove the property from the MXML doc.");
                    return null;
                }
            }

            // If the value is already of the correct type or can be assigned to the target type directly:
            if (targetType.IsAssignableFrom(value.GetType()))
            {
                return value;
            }

            // Handle List<T> scenario if value is a list-like object
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementType = targetType.GetGenericArguments()[0];

                // Try to treat the value as something enumerable
                if (value is System.Collections.IEnumerable enumerable && !(value is string))
                {
                    var listInstance = Activator.CreateInstance(targetType);
                    var addMethod = targetType.GetMethod("Add");

                    foreach (var item in enumerable)
                    {
                        var convertedItem = ConvertToType(item, elementType);
                        if (Failed) return listInstance; // If failed on an element, stop.

                        addMethod.Invoke(listInstance, new[] { convertedItem });
                    }

                    return listInstance;
                }
                else
                {
                    // If the value is not enumerable and we need a list, try to convert it to a single element list
                    // or fail. Usually you'd expect a collection for a list property.
                    var singleConverted = ConvertToType(value, elementType);
                    if (Failed) return null;

                    var singleList = Activator.CreateInstance(targetType);
                    var addMethod = targetType.GetMethod("Add");
                    addMethod.Invoke(singleList, new[] { singleConverted });
                    return singleList;
                }
            }

            // If value is not directly assignable and not a list, try converting via string if possible.
            string strValue = value.ToString();

            // 1. Primitive and other direct conversions
            if (targetType == typeof(string)) return strValue;
            if (targetType == typeof(int) && int.TryParse(strValue, out var i)) return i;
            if (targetType == typeof(bool) && bool.TryParse(strValue, out var b)) return b;
            if (targetType.IsEnum && Enum.TryParse(targetType, strValue, out var enumVal)) return enumVal;
            if (targetType == typeof(double) && double.TryParse(strValue, out var d)) return d;
            if (targetType == typeof(float) && float.TryParse(strValue, out var f)) return f;
            if (targetType == typeof(long) && long.TryParse(strValue, out var l)) return l;
            if (targetType == typeof(short) && short.TryParse(strValue, out var s)) return s;
            if (targetType == typeof(Color))
            {
                try
                {
                    string[] split = strValue.Split(",");
                    if (split.Length != 4)
                    {
                        Failed = true;
                        CompilerMessages.Add($"The value \"{value}\" for property type \"{targetType}\" cannot be converted.");

                        return value;
                    }
                    int a = 0, r = 0, g = 0, v = 0;
                    if (!int.TryParse(split[0], out a) || !int.TryParse(split[1], out r) ||
                        !int.TryParse(split[2], out g) || !int.TryParse(split[3], out v))
                    {
                        CompilerMessages.Add($"The value \"{value}\" for property type \"{targetType}\" cannot be converted.");
                        Failed = true;
                        return value;
                    }
                    Color clr = Color.FromArgb(a, r, g, v);
                    return clr;
                }
                catch (Exception e)
                {
                    Failed = true;
                    CompilerMessages.Add($"The value \"{value}\" for property type \"{targetType}\" cannot be converted.");
                    CompilerMessages.Add($"{e.Message}");

                }
            }

            // 2. Use Parse/TryParse
            var parseMethod = targetType.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                        .FirstOrDefault(m =>
                                            (m.Name == "Parse" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string)) ||
                                            (m.Name == "TryParse" && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == typeof(string) && m.GetParameters()[1].IsOut)
                                        );

            if (parseMethod != null)
            {
                if (parseMethod.Name == "Parse")
                {
                    return parseMethod.Invoke(null, new object[] { strValue });
                }
                else if (parseMethod.Name == "TryParse")
                {
                    var parameters = new object[] { strValue, null };
                    bool success = (bool)parseMethod.Invoke(null, parameters);
                    if (success)
                    {
                        return parameters[1];
                    }
                }
            }

            // 3. Constructor that takes a single string
            var stringCtor = targetType.GetConstructor(new[] { typeof(string) });
            if (stringCtor != null)
            {
                return stringCtor.Invoke(new object[] { strValue });
            }

            // 4. Parameterless constructor fallback
            if (targetType.GetConstructor(Type.EmptyTypes) != null)
            {
                return Activator.CreateInstance(targetType);
            }

            // If no conversion possible
            Failed = true;
            CompilerMessages.Add($"The value \"{value}\" for property type \"{targetType}\" cannot be converted.");
            return value;
        }

        public Binding CreateBinding(string classAndMemberPath)
        {
            if (string.IsNullOrWhiteSpace(classAndMemberPath))
                throw new ArgumentException("Binding path cannot be null or empty.", nameof(classAndMemberPath));

            var parts = classAndMemberPath.Split('.');
            if (parts.Length < 2)
                throw new ArgumentException("Binding path must include at least a class name and a member name.", nameof(classAndMemberPath));

            // Start by resolving the first part, which could be a backend object or a static class
            string initialClassName = parts[0];
            object currentObject = null;
            Type currentType = null;

            // Attempt to resolve as an instance in BackendObjects
            if (BackendObjects.TryGetValue(initialClassName, out currentObject))
            {
                currentType = currentObject.GetType();
            }
            else
            {
                // Attempt to resolve as a static class from registered namespaces and assemblies
                currentType = FindElementType(initialClassName);
                if (currentType == null)
                    throw new Exception($"Cannot find class or backend object named \"{initialClassName}\".");
            }

            // Traverse the binding path
            for (int i = 1; i < parts.Length; i++)
            {
                string memberName = parts[i];
                bool isLast = (i == parts.Length - 1);

                // Attempt to find a property
                PropertyInfo prop = currentType.GetProperty(memberName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                if (prop != null)
                {
                    if (isLast)
                    {
                        return new Binding(currentObject, prop);
                    }
                    else
                    {
                        currentObject = prop.GetValue(currentType.IsClass && prop.GetGetMethod(true).IsStatic ? null : currentObject);
                        if (currentObject == null)
                            throw new Exception($"Property \"{memberName}\" on \"{currentType.Name}\" is null.");
                        currentType = currentObject.GetType();
                        continue;
                    }
                }

                // Attempt to find an event
                EventInfo evt = currentType.GetEvent(memberName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                if (evt != null)
                {
                    if (isLast)
                    {
                        return new Binding(currentObject, evt);
                    }
                    else
                    {
                        // Intermediate events are not traversable
                        throw new Exception($"Cannot traverse through event \"{memberName}\" in binding path.");
                    }
                }

                // Attempt to find a static class nested within the current type
                Type nestedType = currentType.GetNestedType(memberName, BindingFlags.Public | BindingFlags.Static);
                if (nestedType != null)
                {
                    currentObject = null; // Static class, no instance
                    currentType = nestedType;
                    continue;
                }

                // Member not found
                throw new Exception($"Member \"{memberName}\" not found on type \"{currentType.Name}\".");
            }

            throw new Exception($"Invalid binding path: \"{classAndMemberPath}\".");
        }

        protected override void RenderContent(ConsoleBuffer buffer)
        {
            if (!IsVisible)
            {
                return;
            }

            int innerWidth = ActualWidth - (ShowBorder ? 1 : 0);
            int innerHeight = ActualHeight - (ShowBorder ? 1 : 0);
            int startXOffset = (ShowBorder ? 1 : 0);
            int startYOffset = (ShowBorder ? 1 : 0);

            try
            {
                List<(UIElement element, ConsoleBuffer buffer)> buffers = new List<(UIElement element, ConsoleBuffer buffer)>();
                foreach (var pos in Children.Where(x => x.IsVisible))
                {
                    pos.CalculateLayout(startXOffset, startYOffset, innerWidth, innerHeight);
                    var elementBuffer = pos.Render();
                    buffers.Add((pos, elementBuffer));
                }

                var lst = buffers.OrderBy(e => e.element.Z).ToList();
                foreach (var element in lst)
                {
                    buffer.WriteBuffer(element.element.ActualX, element.element.ActualY, element.buffer);
                };
            }
            catch (Exception)
            {

            }
        }

    }
}
