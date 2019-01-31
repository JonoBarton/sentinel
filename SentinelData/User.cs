using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SentinelData
{
    public class User
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("Name")]
        public string Name { get; set; }

       
        [BsonElement("MachineId")]
        public string MachineId { get; set; }
        
        [BsonElement("SID")]
        public string SID { get; set; }

        [BsonElement("Description")]
        public string Description { get; set; }

        [BsonElement("LastLogin")]
        public DateTime LastLogin { get; set; }

        [BsonElement("Monday")]
        public int Monday { get; set; } = 60;
        [BsonElement("Tuesday")]
        public int Tuesday { get; set; } = 60;
        [BsonElement("Wednesday")]
        public int Wednesday { get; set; } = 60;
        [BsonElement("Thursday")]
        public int Thursday { get; set; } = 60;
        [BsonElement("Friday")]
        public int Friday { get; set; } = 90;
        [BsonElement("Saturday")]
        public int Saturday { get; set; } = 120;
        [BsonElement("Sunday")]
        public int Sunday { get; set; } = 120;

        [BsonElement("SamAccountName")]
        public string SamAccountName { get; set; }
        [BsonElement("DisplayName")]
        public string DisplayName { get; set; }
        [BsonElement("UserPrincipalName")]
        public string UserPrincipalName { get; set; }
        [BsonElement("LoginData")]
        public List<LoginData> LoginData { get; set; } = new List<LoginData>();

    }

    public class LoginData
    {
        [BsonElement("Date")]
        public DateTime Date { get; set; }

        [BsonElement("TimeAllocated")]
        public int TimeAllocated { get; set; } =60;

        [BsonElement("TimeElapsed")]
        public int TimeElapsed { get; set; } = 0;
    }
}
