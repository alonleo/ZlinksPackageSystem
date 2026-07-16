using System.Linq;
using Avalonia.Controls;
using ZlinksPackageSystem.Desktop.Models;
using ZlinksPackageSystem.Desktop.ViewModels;

namespace ZlinksPackageSystem.Desktop.Views
{
    public partial class ParameterView : UserControl
    {
        private bool _columnsBuilt;

        public ParameterView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            if (_columnsBuilt) return;
            if (DataContext is not ParameterViewModel) return;

            BuildColumns();
            _columnsBuilt = true;
        }

        private void BuildColumns()
        {
            var grid = this.FindControl<DataGrid>("ParamsDataGrid");
            if (grid == null) return;

            var actionCol = grid.Columns.FirstOrDefault(c => c.Header is string s && s == "操作");
            int insertAt = actionCol != null ? grid.Columns.IndexOf(actionCol) : grid.Columns.Count;

            foreach (var f in ParamFieldDescriptor.Columns)
            {
                var col = new DataGridTextColumn
                {
                    Header = f.Label,
                    Binding = new Avalonia.Data.Binding(f.Key),
                    IsReadOnly = true,
                };
                col.Width = ProductView.ParseWidth(f.Width);
                grid.Columns.Insert(insertAt++, col);
            }
        }
    }
}