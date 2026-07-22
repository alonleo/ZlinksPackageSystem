using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZlinksPackageSystem.Desktop.Constants;
using ZlinksPackageSystem.Desktop.Models;
using ZlinksPackageSystem.Desktop.Services;

namespace ZlinksPackageSystem.Desktop.ViewModels
{
    public partial class GameListViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;
        private readonly IDialogService _dialogService;
        private readonly INetworkStatusService _networkService;
        private readonly ILocalCacheService _cacheService;

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
        private bool _isLoadingGames;

        [ObservableProperty]
        private string _emptyStateMessage = string.Empty;

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

        public bool HasGames => !IsLoadingGames && Games.Count > 0;
        public bool IsGamesEmpty => !IsLoadingGames && Games.Count == 0;

        public GameListViewModel(
            IApiService apiService,
            IDialogService dialogService,
            INetworkStatusService networkService,
            ILocalCacheService cacheService)
        {
            Title = "游戏管理";
            _apiService = apiService;
            _dialogService = dialogService;
            _networkService = networkService;
            _cacheService = cacheService;
            LoadGamesCommand.ExecuteAsync(null);
        }

        partial void OnGamesChanged(ObservableCollection<Game> value)
        {
            OnPropertyChanged(nameof(HasGames));
            OnPropertyChanged(nameof(IsGamesEmpty));
        }

        partial void OnIsLoadingGamesChanged(bool value)
        {
            OnPropertyChanged(nameof(HasGames));
            OnPropertyChanged(nameof(IsGamesEmpty));
        }

        [RelayCommand]
        private async Task LoadGamesAsync()
        {
            IsLoadingGames = true;
            EmptyStateMessage = string.Empty;

            if (_networkService.IsOnline)
            {
                try
                {
                    var endpoint = $"/games?current={CurrentPage}&size={PageSize}";
                    if (!string.IsNullOrEmpty(SearchText))
                        endpoint += $"&gameName={Uri.EscapeDataString(SearchText)}";

                    var page = await _apiService.GetAsync<PageResponse<Game>>(endpoint);

                    if (page == null)
                    {
                        await LoadFromCacheAsync();
                        return;
                    }

                    Games = new ObservableCollection<Game>(page.Records ?? new List<Game>());
                    TotalCount = page.Total;

                    var recordCount = page.Records?.Count ?? 0;
                    if (recordCount == 0 && CurrentPage == 1 && string.IsNullOrEmpty(SearchText))
                    {
                        EmptyStateMessage = "暂无游戏数据，请前往后台「游戏管理」添加";
                    }

                    await SaveCacheAsync(page);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"LoadGames API failed: {ex.Message}");
                    var fromCache = await LoadFromCacheAsync();
                    if (!fromCache)
                    {
                        await _dialogService.ShowMessageAsync("错误",
                            $"无法连接后台管理系统（{ex.Message}）。\n请确认后端已启动，然后点击「↻ 重新加载」重试。");
                    }
                }
            }
            else
            {
                await LoadFromCacheAsync();
            }

            IsLoadingGames = false;
        }

        private async Task SaveCacheAsync(PageResponse<Game> page)
        {
            var payload = new GamePageCache
            {
                CurrentPage = CurrentPage,
                PageSize = PageSize,
                SearchText = SearchText,
                Page = page,
                LastUpdated = DateTime.Now
            };
            await _cacheService.SaveAsync(CacheKeys.Games, payload);
        }

        private async Task<bool> LoadFromCacheAsync()
        {
            var lastUpdated = await _cacheService.GetLastUpdatedAsync(CacheKeys.Games);
            var cached = await _cacheService.LoadAsync<GamePageCache>(CacheKeys.Games);
            if (cached?.Page == null)
            {
                Games = new ObservableCollection<Game>();
                TotalCount = 0;
                EmptyStateMessage = _networkService.IsOnline
                    ? "加载游戏失败，请检查后端服务是否可用"
                    : "离线模式 · 暂无本地游戏数据,请联网后刷新";
                return false;
            }

            CurrentPage = cached.CurrentPage;
            PageSize = cached.PageSize;
            SearchText = cached.SearchText;
            Games = new ObservableCollection<Game>(cached.Page.Records ?? new List<Game>());
            TotalCount = cached.Page.Total;
            EmptyStateMessage = lastUpdated.HasValue
                ? $"离线模式 · 数据更新于 {lastUpdated:yyyy-MM-dd HH:mm}"
                : "离线模式 · 加载本地缓存";
            return true;
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
        private async Task OpenEditDialogAsync(Game? game)
        {
            if (game == null) return;

            Game? detail = null;
            try
            {
                detail = await _apiService.GetAsync<Game>($"/games/{game.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetById failed: {ex.Message}");
            }

            if (detail == null)
            {
                await _dialogService.ShowMessageAsync("错误", "获取游戏详情失败，请检查后端服务");
                return;
            }

            SelectedGame = detail;
            EditGameName = detail.GameName;
            EditGameDirection = detail.GameDirection;
            EditSource = detail.Source;
            EditManager = detail.Manager;
            EditStatus = detail.Status;
            EditPriority = detail.Priority.ToString();
            IsEditing = true;
        }

        [RelayCommand]
        private void CancelEdit()
        {
            IsEditing = false;
            SelectedGame = null;
        }

        [RelayCommand]
        private async Task SaveGameAsync()
        {
            if (string.IsNullOrWhiteSpace(EditGameName))
            {
                await _dialogService.ShowMessageAsync("提示", "请填写游戏名称");
                return;
            }
            if (!int.TryParse(EditPriority, out var priority)) priority = 0;

            var payload = new
            {
                gameName = EditGameName.Trim(),
                gameDirection = string.IsNullOrWhiteSpace(EditGameDirection) ? "未分类" : EditGameDirection,
                source = EditSource,
                manager = EditManager,
                status = string.IsNullOrWhiteSpace(EditStatus) ? "开发中" : EditStatus,
                priority,
            };

            try
            {
                Game? result;
                if (SelectedGame == null)
                {
                    result = await _apiService.PostAsync<Game>("/games", payload);
                    if (result == null)
                    {
                        await _dialogService.ShowMessageAsync("错误", "创建游戏失败：后端未返回数据");
                        return;
                    }
                    Games.Insert(0, result);
                    TotalCount++;
                    await _dialogService.ShowMessageAsync("成功", "创建成功");
                }
                else
                {
                    result = await _apiService.PutAsync<Game>($"/games/{SelectedGame.Id}", payload);
                    if (result == null)
                    {
                        await _dialogService.ShowMessageAsync("错误", "更新游戏失败：后端未返回数据");
                        return;
                    }
                    var idx = -1;
                    for (var i = 0; i < Games.Count; i++)
                    {
                        if (Games[i].Id == result.Id) { idx = i; break; }
                    }
                    if (idx >= 0) Games[idx] = result;
                    await _dialogService.ShowMessageAsync("成功", "更新成功");
                }
                IsEditing = false;
                SelectedGame = null;
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync("错误", $"保存失败：{ex.Message}");
            }
        }

        [RelayCommand]
        private async Task DeleteGameAsync(Game? game)
        {
            if (game == null) return;
            try
            {
                var ok = await _apiService.DeleteAsync($"/games/{game.Id}");
                if (!ok)
                {
                    await _dialogService.ShowMessageAsync("错误", "删除游戏失败：后端拒绝请求");
                    return;
                }
                Games.Remove(game);
                if (TotalCount > 0) TotalCount--;
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync("错误", $"删除失败：{ex.Message}");
            }
        }
    }
}
