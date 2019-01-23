namespace DotJEM.Diagnostic
{
    public class CustomData
    {
        private readonly string format;

        public string Name { get; }
        public object Value { get; }

        public CustomData(string name, object value, string format)
        {
            //TODO: Formatter instead!
            this.format = string.IsNullOrWhiteSpace(format) ? "{0}" : $"{{0:{format}}}";
            Name = name;
            Value = value;
        }

        public override string ToString() => string.Format(format, Value);
    }
}