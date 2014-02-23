using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Components can pass and receive messages. This is just a generic class for representing them.
    /// </summary>
    public class Message
    {
        public enum MessageType
        {
            OnChunkModified,
            OnHurt
        }

        public string MessageString { get; set; }
        public MessageType Type { get; set; }

        public Message(MessageType type, string messageString)
        {
            MessageString = messageString;
            Type = type;
        }
    }

}