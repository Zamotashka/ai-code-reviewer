public static class PromptBuilder
{
    public static string Build(string fileName, string code)
    {
        if (code.Length > 12000)
            code = code[..12000] + "\n\n[... файл обрезан ...]";

        return
            "Ты опытный senior разработчик. Проведи code review файла \"" + fileName + "\".\n\n" +
            "Найди и опиши:\n" +
            "- Потенциальные баги и null reference ошибки\n" +
            "- Проблемы с производительностью\n" +
            "- Нарушения принципов SOLID и чистого кода\n" +
            "- Проблемы безопасности\n" +
            "- Конкретные улучшения с примерами\n\n" +
            "Формат — чёткие пункты:\n" +
            "BUG: ...\n" +
            "WARNING: ...\n" +
            "СОВЕТ: ...\n" +
            "ХОРОШО: ...\n\n" +
            "Отвечай на русском языке.\n\n" +
            "Код:\n```\n" + code + "\n```";
    }
}