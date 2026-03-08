namespace TodoInspector
{
    internal readonly struct TodoEntry
    {
        public readonly string FilePath;
        public readonly int LineNumber;
        public readonly string RawText;
        public readonly string User;
        public readonly TodoPriority Priority;
        public readonly string Message;

        public TodoEntry(string filePath, int lineNumber, string rawText, string user,
            TodoPriority priority, string message)
        {
            FilePath = filePath;
            LineNumber = lineNumber;
            RawText = rawText;
            User = user;
            Priority = priority;
            Message = message;
        }
    }
}

