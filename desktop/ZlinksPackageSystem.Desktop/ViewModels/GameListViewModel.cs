using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZlinksPackageSystem.Desktop.Models;
using ZlinksPackageSystem.Desktop.Services;

namespace ZlinksPackageSystem.Desktop.ViewModels
{
    public partial class GameListViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;

        [ObservableProperty]
        private ObservableCollection<Game> _games = new();

        [ObservableProperty]
        private Game? _selectedGame;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _pageSize = 10;

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private string _editGameName = string.Empty;

        [ObservableProperty]
        private string _editGameDirection = string.Empty;

        [ObservableProperty]
        private string _editSource = string.Empty;

        [ObservableProperty]
        private string _editManager = string.Empty;

        [ObservableProperty]
        private string _editStatus = string.Empty;

        [ObservableProperty]
        private string _editPriority = string.Empty;

        public GameListViewModel(IApiService apiService)
        {
            Title = "游戏管理";
            _apiService = apiService;
            LoadGamesCommand.ExecuteAsync(null);
        }

        [RelayCommand]
        private async Task LoadGamesAsync()
        {
            IsBusy = true;

            try
            {
                var endpoint = $"/games?current={CurrentPage}&size={PageSize}";
                if (!string.IsNullOrEmpty(SearchText))
                    endpoint += $"&gameName={SearchText}";

                var result = await _apiService.GetAsync<PageResponse<Game>>(endpoint);
                if (result != null)
                {
                    Games = new ObservableCollection<Game>(result.Records);
                    TotalCount = result.Total;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load games: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            CurrentPage = 1;
            await LoadGamesAsync();
        }

        [RelayCommand]
        private async Task NextPageAsync()
        {
            if (CurrentPage * PageSize < TotalCount)
            {
                CurrentPage++;
                await LoadGamesAsync();
            }
        }

        [RelayCommand]
        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadGamesAsync();
            }
        }

        [RelayCommand]
        private void OpenAddDialog()
        {
            SelectedGame = null;
            EditGameName = string.Empty;
            EditGameDirection = string.Empty;
            EditSource = string.Empty;
            EditManager = string.Empty;
            EditStatus = "开发中";
            EditPriority = "中";
            IsEditing = true;
        }

        [RelayCommand]
        private void OpenEditDialog(Game game)
        {
            if (game == null) return;
            SelectedGame = game;
            EditGameName = game.GameName;
            EditGameDirection = game.GameDirection;
            EditSource = game.Source;
            EditManager = game.Manager;
            EditStatus = game.Status;
            EditPriority = game.Priority.ToString();
            IsEditing = true;
        }

        [RelayCommand]
        private void SaveGame()
        {
            if (string.IsNullOrWhiteSpace(EditGameName)) return;

            if (SelectedGame == null)
            {
                Games.Insert(0, new Game
                {
                    Id = Games.Count + 1,
                    GameName = EditGameName,
                    GameDirection = EditGameDirection,
                    Source = EditSource,
                    Manager = EditManager,
                    Status = EditStatus,
                    Priority = int.TryParse(EditPriority, out var p) ? p : 0,
                    CreateTime = DateTime.Now
                });
                TotalCount++;
            }
            else
            {
                var index = Games.IndexOf(SelectedGame);
                if (index >= 0)
                {
                    Games[index] = new Game
                    {
                        Id = SelectedGame.Id,
                        GameName = EditGameName,
                        GameDirection = EditGameDirection,
                        Source = EditSource,
                        Manager = EditManager,
                        Status = EditStatus,
                        Priority = int.TryParse(EditPriority, out var p) ? p : 0,
                        CreateTime = SelectedGame.CreateTime
                    };
                }
            }

            IsEditing = false;
        }

        [RelayCommand]
        private void CancelEdit()
        {
            IsEditing = false;
        }

        [RelayCommand]
        private async Task DeleteGameAsync(Game game)
        {
            if (game == null) return;
            Games.Remove(game);
            TotalCount--;
        }
    }
}
