﻿using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BEN.DTOs
{
    public class RegisterDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string Password { get; set; }

        private string Gender = "Male";
        public string Role { get; set; }
        public string Link { get; set; }
    }

   
}
