namespace TodoInspector
{
    internal struct TodoEntryViewModel
    {
        public TodoEntry Entry;

        public string FileText;
        public string BadgeText;
        public string UserText;

        public float BadgeWidth;
        public float UserWidth;
        public bool WidthsCached;
    }
}

