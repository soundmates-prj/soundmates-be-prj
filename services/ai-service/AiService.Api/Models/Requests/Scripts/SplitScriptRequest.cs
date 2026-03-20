using System.ComponentModel.DataAnnotations;

namespace AiService.Api.Models.Requests.Scripts;

public class SplitScriptRequest
{
    [Range(200, 20000)]
    public int MaxCharsPerPart { get; set; } = 1500;
}

