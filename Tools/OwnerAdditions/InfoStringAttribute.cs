using System;

namespace RuntimeInspectorNamespace
{
    public class InfoStringAttribute : Attribute
    {
        private readonly string info;
        public string Info => $"\t<size=10><color=#787878><i>({info})</i></color></size>";

        public InfoStringAttribute(string value)
        {
            this.info = value;
        }
    }
}
