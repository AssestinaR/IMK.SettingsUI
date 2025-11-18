using System;
using System.Reflection;

namespace IMK.SettingsUI.Table.Adapters
{
    public sealed class ReflectionRowAdapter<T> : IRowAdapter
    {
        private readonly T _instance;
        private readonly Func<string, MemberInfo> _resolver;
        public ReflectionRowAdapter(T instance) : this(instance, null) { }
        public ReflectionRowAdapter(T instance, Func<string, MemberInfo> resolver)
        {
            _instance = instance; _resolver = resolver;
        }
        public object Get(string columnId)
        {
            var m = Resolve(columnId);
            if (m is PropertyInfo p) return p.GetValue(_instance);
            if (m is FieldInfo f) return f.GetValue(_instance);
            return null;
        }
        public bool Set(string columnId, object value)
        {
            try
            {
                var m = Resolve(columnId);
                if (m is PropertyInfo p) { p.SetValue(_instance, ConvertValue(value, p.PropertyType)); return true; }
                if (m is FieldInfo f) { f.SetValue(_instance, ConvertValue(value, f.FieldType)); return true; }
            }
            catch { }
            return false;
        }
        private MemberInfo Resolve(string id)
        {
            if (_resolver != null)
            {
                var r = _resolver(id); if (r != null) return r;
            }
            var t = typeof(T);
            var p = t.GetProperty(id, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase); if (p != null) return p;
            var f = t.GetField(id, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase); if (f != null) return f;
            return null;
        }
        private object ConvertValue(object v, Type t)
        {
            if (v == null) return t.IsValueType ? Activator.CreateInstance(t) : null;
            try
            {
                if (t.IsEnum) return Enum.Parse(t, v.ToString(), true);
                if (t == typeof(string)) return v.ToString();
                if (t == typeof(int) || t == typeof(int?)) return System.Convert.ToInt32(v);
                if (t == typeof(float) || t == typeof(float?)) return System.Convert.ToSingle(v);
                if (t == typeof(double) || t == typeof(double?)) return System.Convert.ToDouble(v);
                if (t == typeof(bool) || t == typeof(bool?)) return System.Convert.ToBoolean(v);
                return v;
            }
            catch { return v; }
        }
    }
}
