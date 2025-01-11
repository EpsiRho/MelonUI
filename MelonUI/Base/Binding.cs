using System;
using System.Reflection;

namespace MelonUI.Base
{
    public class Binding
    {
        private object _instance;
        private PropertyInfo _property;
        private EventInfo _event;
        private MethodInfo _method;
        private bool _isMethod;
        private bool _isStatic;
        private Delegate _eventHandler;
        public bool IsStatic => _isStatic;
        public bool IsProperty => _property != null;

        public bool IsEvent => _event != null;
        public bool IsMethod => _method != null;

        public EventInfo EventInfo => _event;

        /// <summary>
        /// Binding for a property.
        /// </summary>
        public Binding(object instance, PropertyInfo property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            _instance = instance;
            _property = property;
            _isStatic = property.GetGetMethod().IsStatic;
        }
        /// <summary>
        /// Binding for a method.
        /// </summary>
        public Binding(object instance, MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            _instance = instance;
            _method = method;
            _isStatic = method.IsStatic;
        }

        /// <summary>
        /// Binding for an event.
        /// </summary>
        public Binding(object instance, EventInfo eventInfo)
        {
            if (eventInfo == null)
                throw new ArgumentNullException(nameof(eventInfo));

            _instance = instance;
            _event = eventInfo;
            _isStatic = eventInfo.GetAddMethod().IsStatic;
        }

        /// <summary>
        /// Gets the value of the bound property.
        /// </summary>
        public object GetValue()
        {
            if (_property != null)
            {
                return _property.GetValue(_isStatic ? null : _instance);
            }
            else if(_method != null)
            {
                var action = (Action)Delegate.CreateDelegate(typeof(Action), _instance, _method);
                return action;
            }
            return null;
        }

        /// <summary>
        /// Sets the value of the bound property.
        /// </summary>
        public void SetValue(object value)
        {
            if (_property == null)
                throw new InvalidOperationException("This binding is not for a property.");

            if (!_property.CanWrite)
                throw new InvalidOperationException($"Property '{_property.Name}' is read-only.");

            _property.SetValue(_isStatic ? null : _instance, value);
        }

        /// <summary>
        /// Subscribes a handler to the bound event.
        /// </summary>
        public void Subscribe(Delegate handler)
        {
            if (_event == null)
                throw new InvalidOperationException("This binding is not for an event.");

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (!_event.EventHandlerType.IsAssignableFrom(handler.GetType()))
                throw new ArgumentException($"Handler type does not match event type '{_event.EventHandlerType.Name}'.");

            _eventHandler = handler;
            _event.AddEventHandler(_isStatic ? null : _instance, _eventHandler);
        }

        /// <summary>
        /// Unsubscribes the handler from the bound event.
        /// </summary>
        public void Unsubscribe()
        {
            if (_event == null)
                throw new InvalidOperationException("This binding is not for an event.");

            if (_eventHandler != null)
            {
                _event.RemoveEventHandler(_isStatic ? null : _instance, _eventHandler);
                _eventHandler = null;
            }
        }

        public static Binding CreateStaticPropertyBinding(Type staticType, string propertyPath)
        {
            if (staticType == null)
                throw new ArgumentNullException(nameof(staticType));
            if (string.IsNullOrWhiteSpace(propertyPath))
                throw new ArgumentNullException(nameof(propertyPath));
            if (!staticType.IsAbstract || !staticType.IsSealed)
                throw new ArgumentException($"Type '{staticType.Name}' is not a static class.");

            PropertyInfo property = staticType.GetProperty(propertyPath,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (property == null)
                throw new ArgumentException($"Static property '{propertyPath}' not found on type '{staticType.Name}'.");

            return new Binding(null, property);
        }

        /// <summary>
        /// Creates a binding from a property path on an object instance.
        /// </summary>
        /// <param name="instance">The object instance containing the property</param>
        /// <param name="propertyPath">Name of the property</param>
        /// <returns>A new Binding instance for the property</returns>
        public static Binding CreatePropertyBinding(object instance, string propertyPath)
        {
            if (string.IsNullOrWhiteSpace(propertyPath))
                throw new ArgumentNullException(nameof(propertyPath));

            Type type = instance?.GetType() ?? throw new ArgumentNullException(nameof(instance));
            PropertyInfo property = type.GetProperty(propertyPath,
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static);

            if (property == null)
                throw new ArgumentException($"Property '{propertyPath}' not found on type '{type.Name}'.");

            return new Binding(instance, property);
        }

        /// <summary>
        /// Creates a binding from a method on an object instance.
        /// </summary>
        /// <param name="instance">The object instance containing the method</param>
        /// <param name="methodName">Name of the method</param>
        /// <returns>A new Binding instance for the method</returns>
        public static Binding CreateMethodBinding(object instance, string methodName)
        {
            if (string.IsNullOrWhiteSpace(methodName))
                throw new ArgumentNullException(nameof(methodName));

            Type type = instance?.GetType() ?? throw new ArgumentNullException(nameof(instance));
            MethodInfo method = type.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static);

            if (method == null)
                throw new ArgumentException($"Method '{methodName}' not found on type '{type.Name}'.");

            if (method.GetParameters().Length > 0)
                throw new ArgumentException($"Method '{methodName}' must have no parameters for binding.");

            return new Binding(instance, method);
        }

        /// <summary>
        /// Creates a binding from an event on an object instance.
        /// </summary>
        /// <param name="instance">The object instance containing the event</param>
        /// <param name="eventName">Name of the event</param>
        /// <returns>A new Binding instance for the event</returns>
        public static Binding CreateEventBinding(object instance, string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentNullException(nameof(eventName));

            Type type = instance?.GetType() ?? throw new ArgumentNullException(nameof(instance));
            EventInfo eventInfo = type.GetEvent(eventName,
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static);

            if (eventInfo == null)
                throw new ArgumentException($"Event '{eventName}' not found on type '{type.Name}'.");

            return new Binding(instance, eventInfo);
        }

        /// <summary>
        /// Creates a binding based on the member type (Property, Method, or Event).
        /// </summary>
        /// <param name="instance">The object instance containing the member</param>
        /// <param name="memberName">Name of the member</param>
        /// <returns>A new Binding instance for the specified member</returns>
        public static Binding Create(object instance, string memberName)
        {
            if (string.IsNullOrWhiteSpace(memberName))
                throw new ArgumentNullException(nameof(memberName));

            Type type = instance?.GetType() ?? throw new ArgumentNullException(nameof(instance));

            // Try to find property first
            PropertyInfo property = type.GetProperty(memberName,
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static);
            if (property != null)
                return new Binding(instance, property);

            // Try to find method
            MethodInfo method = type.GetMethod(memberName,
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static);
            if (method != null && method.GetParameters().Length == 0)
                return new Binding(instance, method);

            // Try to find event
            EventInfo eventInfo = type.GetEvent(memberName,
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static);
            if (eventInfo != null)
                return new Binding(instance, eventInfo);

            throw new ArgumentException($"Member '{memberName}' not found on type '{type.Name}' or is not bindable.");
        }
    }
}
