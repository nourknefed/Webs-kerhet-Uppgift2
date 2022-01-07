using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.WebUtilities;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using System.Web;
using WebApplication1.Utilities;
using System.Net.Mime;

namespace WebApplication1.Controllers
{
    public class FileUpploadsController : Controller
    {
        private readonly WebApplication1Context Db;
        private readonly long fileSizeLimit = 10 * 1048576;
        private readonly string[] permittedExtensions = { ".jpg" };

        public FileUpploadsController(WebApplication1Context context)
        {
            Db = context;
        }

        // GET: FileUpploads
        public async Task<IActionResult> Index()
        {
            return View(await Db.FileUppload.ToListAsync());
        }


        [HttpPost]
        [Route(nameof(UploadFile))]
        public async Task<IActionResult> UploadFile()
        {
            var request = HttpContext.Request;

            // validation of Content-Type
            // 1. first, it must be a form-data request
            // 2. a boundary should be found in the Content-Type

            if(!request.HasFormContentType || !MediaTypeHeaderValue.TryParse(request.ContentType, out var mediaTypeHeader) || 
                string.IsNullOrEmpty(mediaTypeHeader.Boundary.Value))
               
            {
                return new UnsupportedMediaTypeResult();
            }
            MultipartReader reader = new MultipartReader(mediaTypeHeader.Boundary.Value, request.Body);
            var section = await reader.ReadNextSectionAsync();

            while(section != null)
            {
                var HasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDispositionOutput);
                if(HasContentDispositionHeader && contentDispositionOutput.DispositionType.Equals("form-data") && !string.IsNullOrEmpty(contentDispositionOutput.FileName.Value))
                {
                    // Don't trust any file name, file extension, and file data from the request unless you trust them completely
                    // Otherwise, it is very likely to cause problems such as virus uploading, disk filling, etc
                    // In short, it is necessary to restrict and verify the upload
                    // Here, we just use the temporary folder and a random file name

                    FileUppload fileUppload = new FileUppload();
                    fileUppload.UntrustedName = HttpUtility.HtmlEncode(contentDispositionOutput.FileName.Value);
                    fileUppload.Time = DateTime.UtcNow;
                    fileUppload.Content = await FileHelpers.ProcessStreamedFile(section, contentDispositionOutput, ModelState, permittedExtensions, fileSizeLimit);

                    if (fileUppload.Content.Length == 0)
                    {
                        return RedirectToAction("Index", "FileUpploads");
                    }
                    fileUppload.size = fileUppload.Content.Length;

                    await Db.FileUppload.AddAsync(fileUppload);
                    await Db.SaveChangesAsync();

                    return RedirectToAction("Index", "FileUpploads");
                }
                section = await reader.ReadNextSectionAsync();
            }

            // If the code runs to this location, it means that no files have been saved
            return BadRequest("No files data in the request.");

        }


        // GET: FileUpploads/Download/
        public async Task<IActionResult> Download(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fileUppload = await Db.FileUppload
                .FirstOrDefaultAsync(m => m.Id == id);
            if (fileUppload == null)
            {
                return NotFound();
            }

            return File(fileUppload.Content, MediaTypeNames.Application.Octet, fileUppload.UntrustedName);
        }


        // GET: FileUpploads/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fileUppload = await Db.FileUppload
                .FirstOrDefaultAsync(m => m.Id == id);
            if (fileUppload == null)
            {
                return NotFound();
            }

            return View(fileUppload);
        }

        // GET: FileUpploads/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: FileUpploads/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UntrustedName,size,Content")] FileUppload fileUppload)
        {
            if (ModelState.IsValid)
            {
                fileUppload.Id = Guid.NewGuid();
                Db.Add(fileUppload);
                await Db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(fileUppload);
        }

        // GET: FileUpploads/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fileUppload = await Db.FileUppload.FindAsync(id);
            if (fileUppload == null)
            {
                return NotFound();
            }
            return View(fileUppload);
        }

        // POST: FileUpploads/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,UntrustedName,size,Content")] FileUppload fileUppload)
        {
            if (id != fileUppload.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    Db.Update(fileUppload);
                    await Db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FileUpploadExists(fileUppload.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(fileUppload);
        }

        // GET: FileUpploads/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fileUppload = await Db.FileUppload
                .FirstOrDefaultAsync(m => m.Id == id);
            if (fileUppload == null)
            {
                return NotFound();
            }

            return View(fileUppload);
        }

        // POST: FileUpploads/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var fileUppload = await Db.FileUppload.FindAsync(id);
            Db.FileUppload.Remove(fileUppload);
            await Db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FileUpploadExists(Guid id)
        {
            return Db.FileUppload.Any(e => e.Id == id);
        }
    }
}
