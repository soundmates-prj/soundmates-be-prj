using System;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs.Request
{
    public class UpdateProfileOptionsRequest
    {
        [StringLength(500)]
        public string? Bio { get; set; }
        
        [StringLength(20)]
        public string? Phone { get; set; }
        
        [StringLength(20)]
        public string? Gender { get; set; }
        
        public DateTime? DateOfBirth { get; set; }
        
        [StringLength(500)]
        public string? ProfileImageUrl { get; set; }
        
        [StringLength(500)]
        public string? BackgroundImageUrl { get; set; }
        
        [StringLength(200)]
        public string? Location { get; set; }
        
        [StringLength(200)]
        public string? Website { get; set; }
    }
}

