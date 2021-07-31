
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        private readonly IMediator _mediator;

        private List<CommitListItem> _commits;
        public List<CommitListItem> Commits
        {
            get => _commits;
            set => this.RaiseAndSetIfChanged(ref _commits, value);
        }

        private List<BranchTreeItem> _branches;
        public List<BranchTreeItem> Branches
        {
            get => _branches;
            set => this.RaiseAndSetIfChanged(ref _branches, value);
        }

        public ReactiveCommand<string, Unit> OpenCommand { get; }

        public MainWindowViewModel(IMediator mediator)
        {
            _mediator = mediator;

            OpenCommand = ReactiveCommand.CreateFromTask<string>(Open);
        }

        private async Task Open(string path)
        {
            await _mediator.Send(new OpenRepositoryQuery(path));

            var commits = await _mediator.Send(new GetCommitsQuery());
            var branchTree = await _mediator.Send(new GetBranchTreeQuery());

            Commits = commits.ToList();
            Branches = branchTree.ToList();
        }
    }
}
