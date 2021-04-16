using System.Linq;
using System.IO;

using Evergreen.Lib.Git;
using Evergreen.Lib.Session;

using GtkSource;

using LibGit2Sharp;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System.Text;
using Gdk;

namespace Evergreen.Widgets
{
    public class CommitFileChanges
    {
        private RepositorySession ActiveSession { get; set; }
        private GitService Git { get; set; }
        private SourceView View { get; set; }

        private string CurrentCommitId;
        private string CurrentPath;

            public CommitFileChanges(SourceView view, GitService git)
        {
            View = view;
            Git = git;
            ActiveSession = Git.Session;
        }

        public CommitFileChanges Build()
        {
            View.Buffer.HighlightSyntax = true;

            View.SetMarkAttributes("Inserted", new MarkAttributes
            {
                Background =  new RGBA
                {
                    Alpha = 0.1,
                    Green = 1,
                    Red = 0,
                    Blue = 0,
                }
            }, 10);

            View.SetMarkAttributes("Deleted", new MarkAttributes
            {
                Background =  new RGBA
                {
                    Alpha = 0.1,
                    Green = 0,
                    Red = 1,
                    Blue = 0,
                }
            }, 10);

            View.SetMarkAttributes("Modified", new MarkAttributes
            {
                Background =  new RGBA
                {
                    Alpha = 0.1,
                    Green = 0,
                    Red = 0,
                    Blue = 1,
                }
            }, 10);

            return this;
        }

        public bool Render(TreeChanges changes, string commitId, string path)
        {
            if (CurrentCommitId == commitId && CurrentPath == path)
            {
                return false;
            }

            CurrentCommitId = commitId;
            CurrentPath = path;

            View.Buffer = CreateBuffer();

            var diff = Git.GetCommitDiff(commitId, path);

            if (diff is null)
            {
                return false;
            }

            var buffer = diff.Lines.Aggregate(new StringBuilder(), (b, l) => b.AppendLine(l.Text));

            View.IsMapped = true;
            View.Buffer.Language =  GetLanguage(path);
            View.Buffer.Text = buffer.ToString();

            Mark firstMark = null;

            for (var i = 0; i < diff.Lines.Count; i++)
            {
                var line = diff.Lines[i];
                var lineIter = View.Buffer.GetIterAtLine(i);

                var mark = line.Type switch
                {
                    ChangeType.Inserted =>
                        View.Buffer.CreateSourceMark($"{i}", "Inserted", lineIter),
                    ChangeType.Deleted =>
                        View.Buffer.CreateSourceMark($"{i}", "Deleted", lineIter),
                    ChangeType.Modified =>
                        View.Buffer.CreateSourceMark($"{i}", "Modified", lineIter),
                    _ => null,
                };

                if (mark is {} && firstMark is null)
                {
                    firstMark = mark;
                }
            }

            if (firstMark is {})
            {
                View.ScrollToMark(firstMark, 4, true, 0, 4);
            }

            return true;
        }

        private Buffer CreateBuffer()
        {
            var buffer = new Buffer();
            View.Buffer.HighlightSyntax = true;

            return buffer;
        }

        private Language GetLanguage(string path)
        {
            var ext = Path.GetExtension(path);
            var mgr = new LanguageManager();

            var ls = mgr.LanguageIds;
            var lss = string.Join('\n', ls);

            switch (ext)
            {
                case ".cs":
                    return mgr.GetLanguage("c-sharp");
                case ".html":
                    return mgr.GetLanguage("html");
                case ".css":
                case ".scss":
                    return mgr.GetLanguage("css");
                case ".sql":
                    return mgr.GetLanguage("sql");
                case ".ts":
                    return mgr.GetLanguage("typescript");
                case ".js":
                    return mgr.GetLanguage("javascript");
                case ".json":
                    return mgr.GetLanguage("json");
                case ".rs":
                    return mgr.GetLanguage("rust");
                case ".toml":
                    return mgr.GetLanguage("toml");
                default:
                    return mgr.GetLanguage("c-sharp");
            }
        }
    }
}
