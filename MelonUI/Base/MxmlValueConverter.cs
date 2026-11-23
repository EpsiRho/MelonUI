using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;

namespace MelonUI.Base;

internal static class MxmlValueConverter
{
    internal static object? Convert(object? value, Type target)
    {
        var nullable = Nullable.GetUnderlyingType(target);
        if (value is null)
        {
            if (target.IsValueType && nullable is null) throw new InvalidCastException($"Null is not valid for {target.Name}.");
            return null;
        }
        if (target.IsInstanceOfType(value)) return value;
        if (nullable is not null) return Convert(value, nullable);
        if (target == typeof(string)) return System.Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        if (typeof(Delegate).IsAssignableFrom(target) && value is Delegate callback)
            return callback.Method.IsStatic ? callback.Method.CreateDelegate(target) : callback.Method.CreateDelegate(target, callback.Target);
        if (target.IsArray && value is IEnumerable arraySource && value is not string)
        {
            Type item = target.GetElementType()!; var values = arraySource.Cast<object?>().Select(v => Convert(v, item)).ToArray();
            var array = Array.CreateInstance(item, values.Length); for (int i = 0; i < values.Length; i++) array.SetValue(values[i], i); return array;
        }
        if (value is string text)
        {
            if (target.IsEnum)
            {
                var parsed = Enum.Parse(target, text, true);
                if (!target.IsDefined(typeof(FlagsAttribute), false) && !Enum.IsDefined(target, parsed)) throw new FormatException($"Unknown {target.Name} value '{text}'.");
                return parsed;
            }
            if (target == typeof(Color)) return ColorValue(text);
            if (target == typeof(Guid)) return Guid.Parse(text);
            if (target == typeof(TimeSpan)) return TimeSpan.Parse(text, CultureInfo.InvariantCulture);
            if (target == typeof(Uri)) return new Uri(text, UriKind.RelativeOrAbsolute);
            var converter = TypeDescriptor.GetConverter(target);
            if (converter.CanConvertFrom(typeof(string))) return converter.ConvertFromInvariantString(text);
        }
        if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(target)) return System.Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
        throw new InvalidCastException($"Cannot convert {value.GetType().Name} to {target.Name}.");
    }
    private static Color ColorValue(string text)
    {
        text = text.Trim();
        if (text.StartsWith('#'))
        {
            string hex = text[1..];
            if (hex.Length is not (6 or 8) || !uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint rgb)) throw new FormatException("Use #RRGGBB or #AARRGGBB.");
            return Color.FromArgb(hex.Length == 6 ? 255 : (int)(rgb >> 24), (int)((rgb >> 16) & 255), (int)((rgb >> 8) & 255), (int)(rgb & 255));
        }
        if (text.Contains(','))
        {
            var channels = text.Split(',').Select(p => int.Parse(p.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture)).ToArray();
            if (channels.Length is not (3 or 4) || channels.Any(c => c is < 0 or > 255)) throw new FormatException("Use three RGB or four ARGB byte values.");
            return channels.Length == 3 ? Color.FromArgb(channels[0], channels[1], channels[2]) : Color.FromArgb(channels[0], channels[1], channels[2], channels[3]);
        }
        if (Enum.TryParse<KnownColor>(text, true, out var known) && Enum.IsDefined(known)) return Color.FromKnownColor(known);
        throw new FormatException($"Unknown color '{text}'.");
    }
}
