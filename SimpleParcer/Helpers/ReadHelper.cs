namespace SimpleParser.Helpers
{
    internal class ReadHelper
    {
        /// <summary>
        /// Возвращает весь текст .txt-файла в string
        /// </summary>
        /// <param name="path">Путь к файлу</param>
        /// <returns>Текст файла как string</returns>
        public static string GetFromTxt(string path)
        {
            using var sr = new FileInfo(path).OpenText();
            return sr.ReadToEnd();
        }
    }
}
