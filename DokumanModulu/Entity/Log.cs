using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace DokumanModulu.Entity
{
    public class Log
    {
        public int Id { get; set; } 
        public DateTime Timestamp { get; set; } 
        public string UserId { get; set; } 
        public string OperationType { get; set; } 
        public string EntityId { get; set; } 
        public string EntityType { get; set; } 

        public string EntityName { get; set; }
    }
}
