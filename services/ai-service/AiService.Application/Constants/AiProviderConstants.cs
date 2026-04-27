namespace AiService.Application.Constants;

public static class AiProviderConstants
{
    public const string Gemini = "gemini";
    public const string VieNeuTts = "vieneutts";
}

public static class PodcastPromptTemplateDefaults
{
    // Mau prompt mac dinh, co ho tro placeholder de thay the dong trong service.
    public const string ScriptTemplate =
        "Ban la bien tap vien podcast chuyen nghiep. " +
        "Hay viet noi dung podcast tieng {language} ve chu de: {topic}. " +
        "Phong cach: {style}. Do dai muc tieu: {duration}. " +
        "Chi tra ve van ban script thuan cho TTS, khong markdown, khong JSON, khong tieu de phu.";
}
