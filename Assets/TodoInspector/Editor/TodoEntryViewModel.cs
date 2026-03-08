namespace TodoInspector
{
    internal struct TodoEntryViewModel
    {
        public TodoEntry Entry;

        // Cached strings (computed once in ApplyFilters, not every frame)
        public string FileText;
        public string BadgeText;
        public string UserText;

        // Cached widths (computed once when styles are ready)
        public float BadgeWidth;
        public float UserWidth;
        public bool WidthsCached;
    }
}

