using System;

namespace BrunoMikoski.Core.DevelopmentTools.Git
{
    public class GitCommitInfo
    {
        public readonly string sha;
        public readonly string author;
        public readonly string email;
        public readonly DateTime dateTime;
        public readonly string title;

        public GitCommitInfo(string sha, string author, string email, DateTime dateTime, string title)
        {
            this.sha = sha;
            this.author = author;
            this.email = email;
            this.dateTime = dateTime;
            this.title = title;
        }

        public override string ToString()
        {
            return $"Commit: {sha}\nAuthor: {author} <{email}>\nDate: {dateTime.ToShortDateString()} at {dateTime.ToShortTimeString()}\nTitle: {title}";
        }
    }
}
