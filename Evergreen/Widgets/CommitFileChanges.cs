using System.Diagnostics;
using System.Linq;
using System.IO;

using Evergreen.Lib.Git;

using GtkSource;

using LibGit2Sharp;
using DiffPlex.DiffBuilder.Model;
using System.Text;
using Gdk;

namespace Evergreen.Widgets
{
    public class CommitFileChanges
    {
        private GitService Git { get; }
        private SourceView View { get; }

        private string currentCommitId;
        private string currentPath;

        public CommitFileChanges(SourceView view, GitService git)
        {
            View = view;
            Git = git;
        }

        public CommitFileChanges Build()
        {
            Clear();

            View.SetMarkAttributes("Inserted", new MarkAttributes
            {
                IconName = "list-add",
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
                IconName = "list-remove",
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
            Debug.Assert(changes is {});

            if (currentCommitId == commitId && currentPath == path)
            {
                return false;
            }

            currentCommitId = commitId;
            currentPath = path;

            View.Buffer = CreateBuffer();

            var diff = Git.GetCommitDiff(commitId, path);

            if (diff is null)
            {
                return false;
            }

            var buffer = diff.Lines.Aggregate(new StringBuilder(), (b, l) => b.AppendLine(l.Text));

            View.IsMapped = true;
            View.Buffer.Language = GetLanguage(path);
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

            // TODO: Implement scrolling to first change in diff.

            // if (firstMark is {})
            // {
            //     View.ScrollToMark(firstMark, 4, true, 0, 4);
            // }

            return true;
        }

        public bool Clear()
        {
            View.Buffer = CreateBuffer();

            return true;
        }

        private static Buffer CreateBuffer()
        {
            return new Buffer
            {
                HighlightSyntax = true
            };
        }

        private static Language GetLanguage(string path)
        {
            var ext = Path.GetExtension(path);
            var mgr = new LanguageManager();

            return ext switch
            {
                ".cs" => mgr.GetLanguage("c-sharp"),
                ".html" => mgr.GetLanguage("html"),
                ".css" or ".scss" => mgr.GetLanguage("css"),
                ".sql" => mgr.GetLanguage("sql"),
                ".ts" => mgr.GetLanguage("typescript"),
                ".js" => mgr.GetLanguage("javascript"),
                ".json" => mgr.GetLanguage("json"),
                ".rs" => mgr.GetLanguage("rust"),
                ".toml" => mgr.GetLanguage("toml"),
                _ => mgr.GetLanguage("c-sharp"),
            };
        }
    }
}
