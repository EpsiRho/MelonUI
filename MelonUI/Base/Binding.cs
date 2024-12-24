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

        
    }
}
