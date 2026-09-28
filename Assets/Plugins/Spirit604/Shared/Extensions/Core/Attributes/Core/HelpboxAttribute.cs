using System;

namespace Spirit604.Attributes
{
    public enum MessageBoxType { None, Info, Warning, Error }

    [AttributeUsage(AttributeTargets.Field)]
    public class HelpboxAttribute : AttributeBase
    {
        public string Text { get; private set; }
        public string Condition { get; private set; }
        public MessageBoxType MessageType { get; private set; }
        public string Url { get; private set; }

        public HelpboxAttribute(string text, string condition, MessageBoxType messageType = MessageBoxType.Info, string url = "")
        {
            this.Text = text;
            this.Condition = condition;
            this.MessageType = messageType;
            this.Url = url;
        }
    }
}