
namespace SEIDR.Doc
{

    class ValueHolder
    {
        public bool Aggregate = false;
        string _Name;
        object _Value;
        string _Notes;
        public string Notes { get { return _Notes; } set { _Notes = _Notes + FormatNewNote(value); } }
        public string Name { get { return _Name; } }
        public object Value { get { return _Value; } set { _Value = value; } }
        public ValueHolder(string name, object hold)
        {
            _Name = name;
            _Value = hold;
            _Notes = "";
        }
        private string FormatNewNote(string line)
        {
            int pad = Processor.lineLength - 5;
            int fullLength = Processor.lineLength;
            string work = "";
            if (line.Length <= pad)
            {
                return "\n" + ("".PadLeft(5) + line).PadRight(fullLength);
            }
            while (line.Length > pad)
            {
                string check = line.Substring(0, pad);
                int lastSpace = check.LastIndexOf(' ');
                if (lastSpace > 0)
                {
                    work = work + "\n" + ("".PadLeft(5) + check.Substring(0, lastSpace)).PadRight(fullLength);
                    line = line.Substring(lastSpace + 1); //Don't keep that starting space in the note if we find it.
                }
                else
                {
                    work = work + "\n" + ("".PadRight(5) + check).PadRight(fullLength);
                    line = line.Substring(pad);
                }
            }
            if (line.Length > 0)
            {
                work = work + "\n" + ("".PadLeft(5) + line).PadRight(fullLength);
            }
            return work;
        }
    }
}
