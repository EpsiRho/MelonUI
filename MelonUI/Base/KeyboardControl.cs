using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Base
{
    public class KeyboardControl
    {
        protected Dictionary<string, Binding> _bindings = new Dictionary<string, Binding>();
        /// <summary>
        /// Sets a binding for a property or event.
        /// </summary>
        public void SetBinding(string propertyName, Binding binding)
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));

            _bindings[propertyName] = binding;
        }

        /// <summary>
        /// Gets the bound value or the local value.
        /// </summary>
        protected object GetBoundValue(string propertyName, object localValue)
        {
            if (_bindings.TryGetValue(propertyName, out var binding))
            {
                if (binding.IsProperty)
                {
                    return binding.GetValue();
                }
                else if (binding.IsMethod)
                {
                    return binding.GetValue();
                }
            }
            return localValue;
        }

        /// <summary>
        /// Sets the bound value or the local value.
        /// </summary>
        protected void SetBoundValue(string propertyName, object value, ref object localStorage)
        {
            if (_bindings.TryGetValue(propertyName, out var binding))
            {
                if (binding.IsProperty)
                {
                    binding.SetValue(value);
                    return;
                }
                // Event bindings are handled separately
            }

            // Not bound, set locally
            localStorage = value;
        }

        public object _Key;
        public ConsoleKey? Key
        {
            get => (ConsoleKey?)GetBoundValue(nameof(Key), _Key);
            set => SetBoundValue(nameof(Key), value, ref _Key);
        }
        public ConsoleKeyInfo? KeyInfo{ get; set; }
        public object _RequireShift = false;
        public bool RequireShift
        {
            get => (bool)GetBoundValue(nameof(RequireShift), _RequireShift);
            set => SetBoundValue(nameof(RequireShift), value, ref _RequireShift);
        }
        public object _RequireControl = false;
        public bool RequireControl
        {
            get => (bool)GetBoundValue(nameof(RequireControl), _RequireControl);
            set => SetBoundValue(nameof(RequireControl), value, ref _RequireControl);
        }
        public object _RequireAlt = false;
        public bool RequireAlt
        {
            get => (bool)GetBoundValue(nameof(RequireAlt), _RequireAlt);
            set => SetBoundValue(nameof(RequireAlt), value, ref _RequireAlt);
        }
        public object _Wildcard;
        public Func<ConsoleKeyInfo, bool> Wildcard
        {
            get => (Func<ConsoleKeyInfo, bool>)GetBoundValue(nameof(Wildcard), _Wildcard);
            set => SetBoundValue(nameof(Wildcard), value, ref _Wildcard);
        }
        public object _Action;
        public Action Action
        {
            get => (Action)GetBoundValue(nameof(Action), _Action);
            set => SetBoundValue(nameof(Action), value, ref _Action);
        }
        public object _Description = "";
        public string? Description
        {
            get => (string?)GetBoundValue(nameof(Description), _Description);
            set => SetBoundValue(nameof(Description), value, ref _Description);
        }

        public bool Matches(ConsoleKeyInfo keyInfo)
        {
            if (Wildcard != null) 
            {
                Key = keyInfo.Key;
                KeyInfo = keyInfo;
                var res = Wildcard(keyInfo);
                return res;
            }
            

            return keyInfo.Key == Key &&
                   keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift) == RequireShift &&
                   keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control) == RequireControl &&
                   keyInfo.Modifiers.HasFlag(ConsoleModifiers.Alt) == RequireAlt;
        }
    }

}
