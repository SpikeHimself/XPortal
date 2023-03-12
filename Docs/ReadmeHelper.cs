using ManyFormats;

namespace Docs
{
    public enum Targets
    {
        Thunderstore,
        NexusMods,
        GitHub,
    }

    public class NexusBbcode : ManyFormats.Formats.Bbcode
    {
        public NexusBbcode() : base("BBCode for Nexus Mods") { }

        public override string Heading(string text, HeadingSize size = HeadingSize.Largest)
        {
            return base.Heading(Colour(text, "#FFA03C"), size);
        }

        public override string Code(string text, CodeMode mode = CodeMode.Inline)
        {
            return (mode == CodeMode.Inline)
                ? Font(Bold("'" + text + "'"), "Courier New")
                : base.Code(text, mode);
        }
    }
}
