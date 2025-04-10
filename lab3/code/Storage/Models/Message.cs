using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storage.Models;

//[Table("Message")]
public class DBMessage
{
    public Guid Id { get; set; }
    public string Text { get; set; }
    public DateOnly DateSent { get; set; }
}

//[Table("UserMessage")]
public class DBUserMessage
{
    public Guid DBUserID { get; set; }
    public Guid DBMessageID { get; set; }
}
