using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Identity.EntityFramework;

namespace DokumanModulu.Models
{
    public class User : IdentityUser
    {
        [DisplayName("İsim")]
        [Required(ErrorMessage = "İsim boş bırakılamaz.")]
        public string Name { get; set; }

        [DisplayName("Soyisim")]
        [Required(ErrorMessage = "Soyisim boş bırakılamaz.")]
        public string Surname { get; set; }

        [DisplayName("Telefon Numarası")]
        [Required(ErrorMessage = "Telefon numarası boş bırakılamaz.")]
        public string PhoneNumber { get; set; }
    }
}
