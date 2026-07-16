using System.Linq;
using Avalonia.Controls;
using ZlinksPackageSystem.Desktop.Models;
using ZlinksPackageSystem.Desktop.ViewModels;

namespace ZlinksPackageSystem.Desktop.Views
{
    public partial class ProductView : UserControl
    {
        private bool _columnsBuilt;

        public ProductView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            if (_columnsBuilt) return;
            if (DataContext is not ProductViewModel) return;

            BuildColumns();
            _columnsBuilt = true;
        }

        private void BuildColumns()
        {
            var grid = this.FindControl<DataGrid>("ProductsDataGrid");
            if (grid == null) return;

            // 找「操作」列位置（始终是最后一列，XAML 已定义）
            var actionCol = grid.Columns.FirstOrDefault(c => c.Header is string s && s == "操作");
            int insertAt = actionCol != null ? grid.Columns.IndexOf(actionCol) : grid.Columns.Count;

            foreach (var f in ProductFieldDescriptor.Columns)
            {
                var col = new DataGridTextColumn
                {
                    Header = f.Label,
                    Binding = new Avalonia.Data.Binding(f.Key),
                    IsReadOnly = true,
                };
                col.Width = ParseWidth(f.Width);
                grid.Columns.Insert(insertAt++, col);
            }
        }

        internal static DataGridLength ParseWidth(string width)
        {
            if (string.IsNullOrWhiteSpace(width)) return new DataGridLength(1, DataGridLengthUnitType.Star);

            var w = width.Trim();
            if (w.EndsWith("*"))
            {
                if (w.Length == 1) return new DataGridLength(1, DataGridLengthUnitType.Star);
                if (double.TryParse(w.Substring(0, w.Length - 1), out var star))
                {
                    return new DataGridLength(star, DataGridLengthUnitType.Star);
                }
                return new DataGridLength(1, DataGridLengthUnitType.Star);
            }
            if (double.TryParse(w, out var px))
            {
                return new DataGridLength(px, DataGridLengthUnitType.Pixel);
            }
            return new DataGridLength(1, DataGridLengthUnitType.Star);
        }
    }
}