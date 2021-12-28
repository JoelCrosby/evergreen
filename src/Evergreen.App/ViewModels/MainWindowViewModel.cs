using System.Collections.ObjectModel;
using System.Threading.Tasks;

using Avalonia.Media;

using Evergreen.Core.Commands;
using Evergreen.Core.Git.Models;
using Evergreen.Core.Models;
using Evergreen.Core.Queries;

using MediatR;

using ReactiveUI;
using Unit = System.Reactive.Unit;

namespace Evergreen.App.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IMediator _mediator;

        private ObservableCollection<CommitListItem> _commits = new();
        public ObservableCollection<CommitListItem> Commits
        {
            get => _commits;
            set => this.RaiseAndSetIfChanged(ref _commits, value);
        }

        private ObservableCollection<BranchTreeItem> _local = new();
        public ObservableCollection<BranchTreeItem> Local
        {
            get => _local;
            set => this.RaiseAndSetIfChanged(ref _local, value);
        }

        private ObservableCollection<BranchTreeItem> _remote = new();
        public ObservableCollection<BranchTreeItem> Remote
        {
            get => _remote;
            set => this.RaiseAndSetIfChanged(ref _remote, value);
        }

        public string? RepositoryName { get; set; }

        public ReactiveCommand<string, Unit> OpenCommand { get; }
        public ReactiveCommand<Unit, Unit> FetchCommand { get; }

        public MainWindowViewModel(IMediator mediator)
        {
            _mediator = mediator;

            OpenCommand = ReactiveCommand.CreateFromTask<string>(Open);
            FetchCommand = ReactiveCommand.CreateFromTask(Fetch);
        }

        private async Task Open(string path)
        {
            await _mediator.Send(new OpenRepositoryQuery(path));
            await Refresh();
        }

        public async Task Fetch()
        {
            var result = await _mediator.Send(new FetchCommand()).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                await Refresh();
            }
        }

        private async Task Refresh()
        {
            var (local, remote) = await _mediator.Send(new GetBranchTreeQuery());

            Commits = new ObservableCollection<CommitListItem>(await _mediator.Send(new GetCommitsQuery()));
            Local = new ObservableCollection<BranchTreeItem>
            {
                new ("Repository", FontWeight.ExtraBold, local),
            };
            Remote = new ObservableCollection<BranchTreeItem>
            {
                new ("Remotes", FontWeight.ExtraBold, remote),
            };
        }
    }
}
