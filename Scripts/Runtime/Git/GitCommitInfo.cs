using System;

namespace BrunoMikoski.Core.Git
{
    public class GitCommitInfo
    {
        public readonly string author;
        public readonly DateTime dateTime;
        public readonly string title;

        public GitCommitInfo(string author, DateTime dateTime, string title)
        {
            this.author = author;
            this.dateTime = dateTime;
            this.title = title;
        }
        
        public override string ToString()
        {
            return $"Author: {author}\nDate: {dateTime.ToShortDateString()} at {dateTime.ToShortTimeString()}\nTitle: {title}";
        }
    }
}
