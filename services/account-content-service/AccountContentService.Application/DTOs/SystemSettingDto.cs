using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.DTOs
{
    public class SystemSettingDto
    {
        public Guid Id { get; set; }

        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        public string SettingType { get; set; } = string.Empty;

        public string Descreption { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdateAt { get; set; }
    }
}
