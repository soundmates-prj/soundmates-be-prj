using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Domain.Entities
{
    public class SystemSetting
    {
        public Guid Id { get; set; }

        public string Key { get; set; }
        public string Value { get; set; }

        public string SettingType { get; set; } = string.Empty;

        public string Descreption { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdateAt { get; set; }
    }
}
