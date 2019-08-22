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
    }

    public class MatchAllMatcher : Matcher
    {
        public bool Matches(Entry Entry)
        {
            return true;
        }
    }

    public class RegexMatcher : Matcher
    {
        public Regex Pattern;
        public bool Matches(Entry Entry)
        {
            return Pattern.IsMatch(Entry.Description);
        }
    }

    public class TagMatcher : Matcher
    {
        public String Tag;
        public bool Matches(Entry Entry)
        {
            return Entry.Tags.Any(t => t == Tag);
        }
    }

    public class StatusMatcher : Matcher
    {
        public String Status;
        public bool Matches(Entry Entry)
        {
            return Entry.Status == Status;
        }
    }

    public class PriorityMatcher : Matcher
    {
        public UInt32 Priority;
        public bool Matches(Entry Entry)
        {
            return Entry.Priority >= Priority;
        }
    }

    public class AndMatcher : Matcher
    {
        public Matcher A;
        public Matcher B;

        public bool Matches(Entry Entry)
        {
            return A.Matches(Entry) && B.Matches(Entry);
        }
    }
}