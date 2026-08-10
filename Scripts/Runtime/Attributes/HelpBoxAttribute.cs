using UnityEngine;

namespace BrunoMikoski.Core.Attributes
{
    /// <summary>
    /// Add the [HelpBox] attribute to a field to display a helpbox above it. If no message is
    /// supplied to the HelpBoxAttribute, the field is assumed to contain the message. The message
    /// is either taken from the field directly, or from a property with the same name
    /// </summary>
    public class HelpBoxAttribute : PropertyAttribute
    {
        public enum MessageIconType
        {
            None = 0,
            Info = 1,
            Warning = 2,
            Error = 3,
        }

        private const int DEFAULT_LINE_COUNT = 2;

        private string message;

        public string Message => message;

        private MessageIconType messageType;

        public MessageIconType MessageType => messageType;

        private int lineCount;

        public int LineCount => lineCount;

        public HelpBoxAttribute(string message, MessageIconType messageType, int lineCount)
        {
            this.message = message;
            this.messageType = messageType;
            this.lineCount = lineCount;
        }

        public HelpBoxAttribute(string message, MessageIconType messageType)
        {
            this.message = message;
            this.messageType = messageType;
            lineCount = DEFAULT_LINE_COUNT;
        }

        public HelpBoxAttribute(MessageIconType messageType)
        {
            this.message = null;
            this.messageType = messageType;
            lineCount = DEFAULT_LINE_COUNT;
        }
    }
}