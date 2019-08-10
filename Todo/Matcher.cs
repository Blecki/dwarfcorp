using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList
{
    public interface Matcher
    {
        bool Matches(Entry Entry);
        bool Hilite { get; }
    }

    public class MatchAllMatcher : Matcher
    {
        public bool Matches(Entry Entry)
        {
            return true;
        }

        public bool Hilite => false;
    }

    public class RegexMatcher : Matcher
    {
        public Regex Pattern;

        public bool Matches(Entry Entry)
        {
            return Pattern.IsMatch(Entry.Description);
        }

        public bool Hilite => true;

    }

    public class TagMatcher : Matcher
    {
        public String Tag;

        public bool Matches(Entry Entry)
        {
            return Entry.Tags.Any(t => t == Tag);
        }

        public bool Hilite => true;

    }

    public class CompoundMatcher : Matcher
    {
        public Matcher A;
        public Matcher B;

        public bool Matches(Entry Entry)
        {
            return A.Matches(Entry) && B.Matches(Entry);
        }

        public bool Hilite => true;

    }
}