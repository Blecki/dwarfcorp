using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class Message
    {
        public enum MessageType
        {
            OnChunkModified
        }

        public string MessageString { get; set; }
        public MessageType Type { get; set;}

        public Message(MessageType type, string messageString)
        {
            MessageString = messageString;
            Type = type;
        }
    }
}
