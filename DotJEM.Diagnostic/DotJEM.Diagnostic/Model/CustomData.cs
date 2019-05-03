using System;

namespace DotJEM.Diagnostic.Model
{
    public class CustomData
    {
        private readonly ICustomDataFormatter formatter;

        public object Value { get; }

        public CustomData(object value, ICustomDataFormatter formatter = null)
        {
            this.formatter = formatter ?? new DefaultCustomDataFormatter();
            Value = value;
        }

        public override string ToString() => formatter.Format(this);
    }

    public interface ICustomDataFormatter
    {
        string Format(CustomData customData);
    }

    public class DefaultCustomDataFormatter : ICustomDataFormatter
    {
        public string Format(CustomData customData) => customData.Value?.ToString();
    }
}