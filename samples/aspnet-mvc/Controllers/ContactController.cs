using System;
using System.Net.Mail;
using System.Web.Mvc;

namespace AspNetMvc {

   public class ContactController : Controller {

      string contactTo = "sales@example.com";
      string contactSubject = "Web Contact";

      public ActionResult Index() =>
         View(new Contact());

      [HttpPost]
      [ValidateAntiForgeryToken]
      public ActionResult Index(Contact contact) {

         if (ModelState.IsValid
            && SendMail(contact)) {

            ViewBag.sent = true;

            // clear form
            ModelState.Clear();
            contact = new Contact();
         }

         return View(contact);
      }

      bool SendMail(Contact contact) {

         var message = new MailMessage {
            From = new MailAddress("noreply@example.com", Request.Url.Host),
            To = { contactTo },
            Subject = contactSubject,
            ReplyToList = { new MailAddress(contact.Email, contact.Name) },
            Body = this.ControllerContext.RenderViewAsString("MailBody", contact),
            IsBodyHtml = true
         };

         try {
            using (var smtp = new SmtpClient()) {
               smtp.Send(message);
            }
            return true;

         } catch (SmtpException) {

            ModelState.AddModelError("", "An unexpected error ocurred.");
            return false;
         }
      }
   }
}
