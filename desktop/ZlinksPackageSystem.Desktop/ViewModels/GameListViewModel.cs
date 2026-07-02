using System.Collections.ObjectModel;
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
                {
                    endpoint += $"&gameName={SearchText}";
                }

                var result = await _apiService.GetAsync<PageResponse<Game>>(endpoint);
                if (result != null)
                {
                    Games = new ObservableCollection<Game>(result.Records);
                    TotalCount = result.Total;
                }
            }
            catch (Exception ex)
            {
                // Handle error
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
        private async Task DeleteGameAsync(Game game)
        {
            if (game == null) return;

            var success = await _apiService.DeleteAsync($"/games/{game.Id}");
            if (success)
            {
                Games.Remove(game);
                TotalCount--;
            }
        }
    }
}