using System.Collections.Generic;
using System.Threading.Tasks;

using Avalonia.Media;

using Evergreen.Lib.Commands;
using Evergreen.Lib.Git.Models;
using Evergreen.Lib.Models;
using Evergreen.Lib.Queries;

using MediatR;

using ReactiveUI;
using Unit = System.Reactive.Unit;

namespace Evergreen.App.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IMediator mediator;

        private IEnumerable<CommitListItem> commits;
        public IEnumerable<CommitListItem> Commits
        {
            get => commits;
            set => this.RaiseAndSetIfChanged(ref commits, value);
        }

        private IEnumerable<BranchTreeItem> local;
        public IEnumerable<BranchTreeItem> Local
        {
            get => local;
            set => this.RaiseAndSetIfChanged(ref local, value);
        }

        private IEnumerable<BranchTreeItem> remote;
        public IEnumerable<BranchTreeItem> Remote
        {
            get => remote;
            set => this.RaiseAndSetIfChanged(ref remote, value);
        }

        public string RepositoryName { get; set; }

        public ReactiveCommand<string, Unit> OpenCommand { get; }
        public ReactiveCommand<Unit, Unit> FetchCommand { get; }

        public MainWindowViewModel(IMediator mediator)
        {
            this.mediator = mediator;

            OpenCommand = ReactiveCommand.CreateFromTask<string>(Open);
            FetchCommand = ReactiveCommand.CreateFromTask(Fetch);
        }

        private async Task Open(string path)
        {
            await mediator.Send(new OpenRepositoryQuery(path));
            await Refresh();
        }

        public async Task Fetch()
        {
            var result = await mediator.Send(new FetchCommand()).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                await Refresh();
            }
        }

        private async Task Refresh()
        {
            var branchTree = await mediator.Send(new GetBranchTreeQuery());

            Commits = await mediator.Send(new GetCommitsQuery());
            Local = new List<BranchTreeItem>
            {
                new BranchTreeItem("Repository", FontWeight.ExtraBold)
                    .SetChildren(branchTree.Local)
            };
            Remote = new List<BranchTreeItem>
            {
                new BranchTreeItem("Remotes", FontWeight.ExtraBold)
                    .SetChildren(branchTree.Remote)
            };
        }
    }
}
