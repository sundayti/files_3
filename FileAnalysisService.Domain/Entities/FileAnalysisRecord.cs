using FileAnalysisService.Domain.ValueObjects;

namespace FileAnalysisService.Domain.Entities
{
    public sealed class FileAnalysisRecord
    {
        // Существующие свойства
        public FileId FileId { get; private set; }
        public ImageLocation CloudImageLocation { get; private set; }
        public int ParagraphCount { get; private set; }
        public int WordCount { get; private set; }
        public int CharacterCount { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }

        // 1) Добавляем параметрless-конструктор для EF Core
        private FileAnalysisRecord()
        {
            // EF Core нуждается в таком конструкторе, чтобы
            // при чтении из БД создать пустой объект, а затем заполнить поля напрямую.
        }

        // 2) Сохраняем «полный» приватный конструктор для фабричного метода
        private FileAnalysisRecord(
            FileId fileId,
            ImageLocation imageLocation,
            int paragraphCount,
            int wordCount,
            int characterCount,
            DateTime createdAtUtc)
        {
            FileId = fileId;
            CloudImageLocation = imageLocation;
            ParagraphCount = paragraphCount;
            WordCount = wordCount;
            CharacterCount = characterCount;
            CreatedAtUtc = createdAtUtc;
        }

        /// <summary>
        /// Фабричный метод для создания новой записи анализа.
        /// EF Core при чтении из БД использует приватный конструктор без параметров,
        /// затем напрямую устанавливает свойства через reflection.
        /// </summary>
        public static FileAnalysisRecord CreateNew(
            FileId fileId,
            ImageLocation imageLocation,
            int paragraphCount,
            int wordCount,
            int characterCount)
        {
            return new FileAnalysisRecord(
                fileId,
                imageLocation,
                paragraphCount,
                wordCount,
                characterCount,
                DateTime.UtcNow);
        }
    }
}