using System.Collections.Generic;

namespace ZlinksPackageSystem.Desktop.Models
{
    public enum FieldEditor
    {
        Text,
        MultilineText,
        Combo,
    }

    public class ProductFieldDescriptor
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public FieldEditor Editor { get; set; } = FieldEditor.Text;
        public string? Placeholder { get; set; }
        public bool Required { get; set; }
        public string Width { get; set; } = "*";

        public static IReadOnlyList<ProductFieldDescriptor> Columns { get; } = new[]
        {
            new ProductFieldDescriptor { Key = "Id",            Label = "产品ID",    Width = "80" },
            new ProductFieldDescriptor { Key = "PackageName",   Label = "包名",      Width = "2*" },
            new ProductFieldDescriptor { Key = "CopyrightName", Label = "软著",      Width = "1.5*" },
            new ProductFieldDescriptor { Key = "GameName",      Label = "游戏",      Width = "1.5*" },
            new ProductFieldDescriptor { Key = "CompanyName",   Label = "公司",      Width = "1.5*" },
            new ProductFieldDescriptor { Key = "SdkVersion",    Label = "SDK版本号", Width = "1*" },
            new ProductFieldDescriptor { Key = "ApkVersion",    Label = "APK版本号", Width = "1*" },
            new ProductFieldDescriptor { Key = "Batch",         Label = "批次号",    Width = "1*" },
            new ProductFieldDescriptor { Key = "PackageMode",   Label = "打包模式",  Width = "1*" },
            new ProductFieldDescriptor { Key = "Status",        Label = "状态",      Width = "1*" },
            new ProductFieldDescriptor { Key = "CreateTime",    Label = "创建时间",  Width = "2*" },
            new ProductFieldDescriptor { Key = "UpdateTime",    Label = "更新时间",  Width = "2*" },
            new ProductFieldDescriptor { Key = "Remark",        Label = "备注",      Width = "3*" },
        };

        public static IReadOnlyList<ProductFieldDescriptor> EditFields { get; } = new[]
        {
            new ProductFieldDescriptor { Key = "PackageName", Label = "包名",     Required = true, Placeholder = "如 com.example.game" },
            new ProductFieldDescriptor { Key = "SdkVersion",  Label = "SDK 版本", Required = true, Placeholder = "如 1.2.3" },
            new ProductFieldDescriptor { Key = "ApkVersion",  Label = "APK 版本", Required = true, Placeholder = "如 100" },
            new ProductFieldDescriptor { Key = "Batch",       Label = "批次",     Placeholder = "如 2026-Q3" },
            new ProductFieldDescriptor { Key = "PackageMode", Label = "打包模式", Editor = FieldEditor.Combo },
            new ProductFieldDescriptor { Key = "Status",      Label = "状态",     Editor = FieldEditor.Combo, Required = true },
            new ProductFieldDescriptor { Key = "Remark",      Label = "备注",     Editor = FieldEditor.MultilineText },
        };

        public static IReadOnlyList<string> PackageModeOptions { get; } = new[]
        {
            "白包", "黑包", "马甲", "联运",
        };

        public static IReadOnlyList<string> StatusOptions { get; } = new[]
        {
            "pending", "packaging", "testing", "listed", "offline",
        };
    }
}