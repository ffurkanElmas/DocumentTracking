using DokumanModulu.Entity;
using DokumanModulu.Helpers;
using DokumanModulu.Identity;
using DokumanModulu.Models;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Web;
using System;
using PagedList;

namespace DokumanModulu.Controllers
{
    public class DocumentTrackingController : Controller
    {
        private DataContext db = new DataContext();
        private IdentityDataContext identityDb = new IdentityDataContext();

        [Authorize]
        public ActionResult Index(string searchString, int? page)
        {
            var currentUserId = User.Identity.GetUserId();
            var userManager = new UserManager<User>(new UserStore<User>(identityDb));
            var isAdmin = userManager.IsInRole(currentUserId, "admin");

            IQueryable<DocumentTracking> documents;
            if (isAdmin)
            {
                documents = db.Documents;
            }
            else
            {
                documents = db.Documents
                    .Where(d => d.AllowedUsers.Contains(currentUserId));
            }

            if (!String.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                documents = documents.Where(d => d.Description.ToLower().Contains(searchString));
            }

            int pageSize = 5;
            int pageNumber = (page ?? 1);
            return View(documents.OrderBy(d => d.CreatedDate).ToPagedList(pageNumber, pageSize));
        }

        [Authorize]
        public ActionResult Create()
        {
            var uploadsDir = Server.MapPath("~/Uploads/");
            var model = new DocumentTracking();

            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            var directories = Directory.GetDirectories(uploadsDir).Select(Path.GetFileName).ToList();
            ViewBag.Folders = directories;

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(DocumentTracking documentTracking, HttpPostedFileBase file, string SelectedFolder, string NewFolderName)
        {
            var uploadsRootDir = Server.MapPath("~/Uploads/");

            if (!Directory.Exists(uploadsRootDir))
            {
                Directory.CreateDirectory(uploadsRootDir);
            }

            var directories = Directory.GetDirectories(uploadsRootDir).Select(Path.GetFileName).ToList();
            ViewBag.Folders = directories;

            if (string.IsNullOrEmpty(SelectedFolder) && string.IsNullOrEmpty(NewFolderName))
            {
                ModelState.AddModelError("", "Yeni klasör adı veya mevcut klasör seçilmelidir.");
            }
            else if (!string.IsNullOrEmpty(SelectedFolder) && !string.IsNullOrEmpty(NewFolderName))
            {
                ModelState.AddModelError("", "Hem yeni klasör adı hem de mevcut klasör seçilmemelidir.");
            }

            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(NewFolderName))
                {
                    var uploadsDir = Path.Combine(uploadsRootDir, NewFolderName);
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                        SelectedFolder = NewFolderName;
                    }
                    else
                    {
                        ModelState.AddModelError("NewFolderName", "Bu klasör zaten var.");
                        return View(documentTracking);
                    }
                }

                if (file != null && file.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(file.FileName);
                    var uploadsDir = Path.Combine(uploadsRootDir, SelectedFolder);

                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    documentTracking.Path = Path.Combine("/Uploads/", SelectedFolder, fileName).Replace("\\", "/");
                    var path = Path.Combine(uploadsDir, fileName);
                    file.SaveAs(path);
                }
                else
                {
                    ModelState.AddModelError("Path", "Dosya yolu alanı gereklidir.");
                    return View(documentTracking);
                }

                documentTracking.CreatedDate = DateTime.Now;
                documentTracking.CreatedUserId = User.Identity.GetUserId();
                documentTracking.AllowedUsers = User.Identity.GetUserId();

                db.Documents.Add(documentTracking);
                db.SaveChanges();

                Logger.Log("Oluştur", documentTracking.Id.ToString(), "Doküman");

                return RedirectToAction("Index");
            }

            return View(documentTracking);
        }

        [Authorize(Roles = "admin")]
        [HttpGet]
        public ActionResult Edit(int? id, int? page, string searchUser)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DocumentTracking documentTracking = db.Documents.Find(id);
            if (documentTracking == null)
            {
                return HttpNotFound();
            }

            var usersQuery = identityDb.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchUser))
            {
                searchUser = searchUser.ToLower();
                usersQuery = usersQuery.Where(u => (u.Name.ToLower() + " " + u.Surname.ToLower()).Contains(searchUser));
            }

            var users = usersQuery.ToList();

            var userRoles = new Dictionary<string, string>();

            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(identityDb));
            var userManager = new UserManager<User>(new UserStore<User>(identityDb));

            foreach (var user in users)
            {
                var roles = userManager.GetRoles(user.Id);
                userRoles[user.Id] = roles.Count > 0 ? string.Join(", ", roles) : "No Role";
            }

            var nonAdminUsers = users.Where(u => !userManager.IsInRole(u.Id, "admin")).ToList();

            int pageSize = 5;
            int pageNumber = (page ?? 1);
            ViewBag.UserList = nonAdminUsers.ToPagedList(pageNumber, pageSize);
            ViewBag.UserRoles = userRoles;
            ViewBag.SelectedUsers = (documentTracking.AllowedUsers?.Split(',') ?? new string[] { }).ToList();
            ViewBag.SearchUser = searchUser;

            return View(documentTracking);
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public JsonResult UpdateUserPermission(int documentId, string userId, bool allow)
        {
            try
            {
                var documentTracking = db.Documents.Find(documentId);
                if (documentTracking == null)
                {
                    return Json(new { success = false, message = "Doküman bulunamadı." });
                }

                var allowedUsers = documentTracking.AllowedUsers?.Split(',').ToList() ?? new List<string>();

                if (allow)
                {
                    if (!allowedUsers.Contains(userId))
                    {
                        allowedUsers.Add(userId);
                    }
                }
                else
                {
                    if (allowedUsers.Contains(userId))
                    {
                        allowedUsers.Remove(userId);
                    }
                }

                documentTracking.AllowedUsers = string.Join(",", allowedUsers);
                db.Entry(documentTracking).State = EntityState.Modified;
                db.SaveChanges();

                Logger.Log("Kullanıcı İzni Güncelle", documentTracking.Id.ToString(), "Doküman");

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [Authorize(Roles = "admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            DocumentTracking documentTracking = db.Documents.Find(id);
            if (documentTracking != null)
            {
                string filePath = Server.MapPath(documentTracking.Path);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                db.Documents.Remove(documentTracking);
                db.SaveChanges();

                Logger.Log("Sil", documentTracking.Id.ToString(), "Doküman");
            }
            return RedirectToAction("Index");
        }

        [Authorize]
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
                identityDb.Dispose();
            }
            base.Dispose(disposing);
        }

        [Authorize]
        public ActionResult ViewFile(int id)
        {
            var document = db.Documents.Find(id);
            if (document == null)
            {
                return HttpNotFound();
            }

            var currentUserId = User.Identity.GetUserId();
            var userManager = new UserManager<User>(new UserStore<User>(identityDb));
            var isAdmin = userManager.IsInRole(currentUserId, "admin");

            var allowedUsers = document.AllowedUsers?.Split(',') ?? new string[] { };

            if (!isAdmin && !allowedUsers.Contains(currentUserId))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Bu dosyayı görüntüleme izniniz yok.");
            }

            string filePath = Server.MapPath(document.Path);
            string contentType = MimeMapping.GetMimeMapping(filePath);

            Response.AppendHeader("Content-Disposition", "inline; filename=" + Path.GetFileName(filePath));
            return File(filePath, contentType);
        }

        public ActionResult DocumentDetails(int id)
        {
            var document = db.Documents.Find(id);
            if (document == null)
            {
                return HttpNotFound();
            }
            return PartialView("_DocumentDetails", document);
        }
    }
}
