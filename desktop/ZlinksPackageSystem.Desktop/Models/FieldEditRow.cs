using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ZlinksPackageSystem.Desktop.Models
{
    public class FieldEditRow : ObservableObject
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public FieldEditor Editor { get; set; } = FieldEditor.Text;
        public string? Placeholder { get; set; }
        public bool Required { get; set; }
        public IReadOnlyList<string>? Options { get; set; }

        private string _value = string.Empty;
        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public bool IsText => Editor == FieldEditor.Text;
        public bool IsMultiLine => Editor == FieldEditor.MultilineText;
        public bool IsCombo => Editor == FieldEditor.Combo;
    }
}