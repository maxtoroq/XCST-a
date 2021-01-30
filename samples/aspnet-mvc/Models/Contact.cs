using System;
using System.ComponentModel.DataAnnotations;

namespace AspNetMvc {

   public class Contact {

      [Required]
      [StringLength(50, MinimumLength = 2)]
      public string Name { get; set; }

      [Required]
      [EmailAddress]
      [StringLength(255)]
      [Display(Name = "E-mail")]
      public string Email { get; set; }

      [Required]
      [Phone]
      [StringLength(20, MinimumLength = 8)]
      public string Telephone { get; set; }

      [Required]
      [DataType(DataType.MultilineText)]
      [StringLength(2000)]
      public string Message { get; set; }
   }
}
