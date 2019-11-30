using System;
using System.Net.Mail;

namespace AspNetPrecompiled {

   partial class _Page_contact {

      bool SendMail(Contact contact) {

         var message = new MailMessage {
            From = new MailAddress("noreply@example.com", Request.Url.Host),
            To = { contactTo },
            Subject = contactSubject,
            ReplyToList = { new MailAddress(contact.Email, contact.Name) },
            Body = MailBody(contact),
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
