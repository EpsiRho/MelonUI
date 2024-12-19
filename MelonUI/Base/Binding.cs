using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Base
{
    public class Binding
    {
        private object _instance;
        private PropertyInfo _property;
        private bool _isStatic;

        public Binding(PropertyInfo staticProperty)
        {
            if (staticProperty == null || !staticProperty.GetGetMethod().IsStatic)
                throw new ArgumentException("Provided property is not static.");

            _instance = null;
            _property = staticProperty;
            _isStatic = true;
        }

        public Binding(object instance, PropertyInfo instanceProperty)
        {
            if (instanceProperty == null || instance == null)
                throw new ArgumentException("Instance and instanceProperty must not be null.");

            _instance = instance;
            _property = instanceProperty;
            _isStatic = false;
        }

        public object GetValue()
        {
            return _property.GetValue(_instance);
        }

        public void SetValue(object value)
        {
            if (!_property.CanWrite)
                throw new InvalidOperationException($"Property '{_property.Name}' is read-only.");

            _property.SetValue(_instance, value);
        }

        public bool IsStatic => _isStatic;
    }
}
