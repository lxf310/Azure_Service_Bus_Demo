using System;

namespace ServiceBusDemo.Business.Models
{
    public class TextMessage
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
    }
}
