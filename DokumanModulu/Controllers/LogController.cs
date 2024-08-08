using System;
using System.Linq;
using System.Web.Mvc;
using DokumanModulu.Entity;
using PagedList;

namespace Staj.Controllers
{
    public class LogController : Controller
    {
        private DataContext db = new DataContext();

        [Authorize(Roles = "admin")]
        public ActionResult Index(string entityType, string searchString, int? page)
        {
            var logs = db.Logs.AsQueryable();

            if (!string.IsNullOrEmpty(entityType))
            {
                logs = logs.Where(l => l.EntityType == entityType);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                var searchTerms = searchString.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                logs = logs.Where(log => searchTerms.All(term =>
                    log.UserId.ToLower().Contains(term) ||
                    log.EntityId.ToLower().Contains(term) ||
                    log.OperationType.ToLower().Contains(term)
                ));
            }

            logs = logs.OrderByDescending(l => l.Timestamp);

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(logs.ToPagedList(pageNumber, pageSize));
        }
    }
}
