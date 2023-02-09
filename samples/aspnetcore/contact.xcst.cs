using System.Net.Mail;
using System.Threading.Tasks;

namespace aspnetcore;

partial class _Page_contact : IPageInit {

   bool
   SendMail(Contact contact) {

      var message = new MailMessage {
         From = new MailAddress("noreply@example.com", Request.Host.Value),
         To = { contactTo },
         Subject = contactSubject,
         ReplyToList = { new MailAddress(contact.Email, contact.Name) },
         Body = MailBody(contact),
         IsBodyHtml = true
      };

      try {
         using var smtp = new SmtpClient();
         smtp.Send(message);

         return true;

      } catch (SmtpException) {

         ModelState.AddModelError("", "An unexpected error ocurred.");
         return false;
      }
   }

   public async Task
   InitAsync() {

      var contact = new Contact();
      var sent = false;

      if (IsPost
         && await Antiforgery.IsRequestValidAsync()
         && await TryUpdateModelAsync(contact)
         && SendMail(contact)) {

         sent = true;

         // clear form
         ModelState.Clear();
         contact = new();
      }

      Antiforgery.SetCookieTokenAndHeader();

      layout(new { contact, sent });
   }
}
