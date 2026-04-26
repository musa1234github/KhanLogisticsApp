using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KhanLogistics.Models
{
    public partial class TblUser
    {
        [Key]
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? City { get; set; }
        public string? Address { get; set; }
        public DateTime? Doj { get; set; }
        public DateTime? LastLogIn { get; set; }
        public string? Role { get; set; }
    }
}
