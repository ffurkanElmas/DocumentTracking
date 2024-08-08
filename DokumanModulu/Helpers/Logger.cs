using System;
using System.Data.Entity;
using System.Web;
using Microsoft.AspNet.Identity;
using DokumanModulu.Entity;
using DokumanModulu.Identity;

namespace DokumanModulu.Helpers
{
    public static class Logger
    {
        public static void Log(string operationType, string entityId, string entityType)
        {
            using (var context = new DataContext())
            {
                var log = new Log
                {
                    Timestamp = DateTime.Now,
                    UserId = HttpContext.Current?.User?.Identity?.GetUserId(),
                    OperationType = operationType,
                    EntityId = entityId,
                    EntityType = entityType
                };
                context.Logs.Add(log);
                context.SaveChanges();
            }
        }
    }
}
