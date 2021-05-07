using System.IO;
using System.Linq;
using System.Text;

using DiffPlex.DiffBuilder.Model;

using Gdk;

using GtkSource;

namespace Evergreen.Widgets
{
    public class CommitFileChanges
    {
        private readonly SourceView view;

        private string currentCommitId;
        private string currentPath;

        public CommitFileChanges(SourceView view)
        {
            this.view = view;
            this.view.Visible = true;

            Clear();

            this.view.SetMarkAttributes(
                "Inserted", new MarkAttributes
                {
                    IconName = "list-add",
                    Background = new RGBA
                    {
                        Alpha = 0.1,
                        Green = 1,
                        Red = 0,
                        Blue = 0,
                    },
                }, 10
            );

            this.view.SetMarkAttributes(
                "Deleted", new MarkAttributes
                {
                    IconName = "list-remove",
                    Background = new RGBA
                    {
                        Alpha = 0.1,
                        Green = 0,
                        Red = 1,
                        Blue = 0,
                    },
                }, 10
            );

            this.view.SetMarkAttributes(
                "Modified", new MarkAttributes
                {
                    Background = new RGBA
                    {
                        Alpha = 0.1,
                        Green = 0,
                        Red = 0,
                        Blue = 1,
                    },
                }, 10
            );
        }

        public bool Render(DiffPaneModel diff, string commitId, string path)
        {
            if (currentCommitId == commitId && currentPath == path)
            {
                return false;
            }

            if (diff is null)
            {
                return false;
            }

            currentCommitId = commitId;
            currentPath = path;

            view.Buffer = CreateBuffer();

            var buffer = diff.Lines.Aggregate(new StringBuilder(), (b, l) => b.AppendLine(l.Text));

            view.IsMapped = true;
            view.Buffer.Language = GetLanguage(path);
            view.Buffer.Text = buffer.ToString();

            Mark firstMark = null;

            for (var i = 0; i < diff.Lines.Count; i++)
            {
                var line = diff.Lines[i];
                var lineIter = view.Buffer.GetIterAtLine(i);

                var mark = line.Type switch
                {
                    ChangeType.Inserted =>
                        view.Buffer.CreateSourceMark($"{i}", "Inserted", lineIter),
                    ChangeType.Deleted =>
                        view.Buffer.CreateSourceMark($"{i}", "Deleted", lineIter),
                    ChangeType.Modified =>
                        view.Buffer.CreateSourceMark($"{i}", "Modified", lineIter),
                    _ => null,
                };

                if (mark is { } && firstMark is null)
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
            view.Buffer = CreateBuffer();

            return true;
        }

        private static Buffer CreateBuffer() => new()
        {
            HighlightSyntax = true,
        };

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
