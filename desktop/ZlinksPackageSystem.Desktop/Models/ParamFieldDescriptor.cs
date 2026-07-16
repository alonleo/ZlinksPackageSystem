using System.Collections.Generic;

namespace ZlinksPackageSystem.Desktop.Models
{
    public static class ParamFieldDescriptor
    {
        public static IReadOnlyList<ProductFieldDescriptor> Columns { get; } = new[]
        {
            new ProductFieldDescriptor { Key = "Id",             Label = "参数ID",       Width = "80" },
            new ProductFieldDescriptor { Key = "ProductId",      Label = "产品ID",       Width = "80" },
            new ProductFieldDescriptor { Key = "PackageName",    Label = "包名",         Width = "1.5*" },
            new ProductFieldDescriptor { Key = "AppId",          Label = "AppId",        Width = "1.5*" },
            new ProductFieldDescriptor { Key = "AppSecret",      Label = "AppSecret",    Width = "1.5*" },
            new ProductFieldDescriptor { Key = "MediaId",        Label = "MediaId",      Width = "1.5*" },
            new ProductFieldDescriptor { Key = "ContractStatus", Label = "合同状态",     Width = "1*" },
            new ProductFieldDescriptor { Key = "AgconnectPath",  Label = "AGConnect路径", Width = "2*" },
            new ProductFieldDescriptor { Key = "TdAppId",        Label = "TDAppId",      Width = "1.5*" },
            new ProductFieldDescriptor { Key = "AdParamStatus",  Label = "广告参数",     Width = "1*" },
            new ProductFieldDescriptor { Key = "ListStatus",     Label = "上架状态",     Width = "1*" },
            new ProductFieldDescriptor { Key = "Operator",       Label = "操作人",       Width = "1*" },
            new ProductFieldDescriptor { Key = "Remark",         Label = "备注",         Width = "2*" },
        };

        public static IReadOnlyList<ProductFieldDescriptor> EditFields { get; } = new[]
        {
            new ProductFieldDescriptor { Key = "ProductId",      Label = "产品ID",        Required = true, Editor = FieldEditor.Text, Placeholder = "如 1001" },
            new ProductFieldDescriptor { Key = "PackageName",    Label = "包名",          Placeholder = "如 com.example.game" },
            new ProductFieldDescriptor { Key = "AppId",          Label = "AppId" },
            new ProductFieldDescriptor { Key = "AppSecret",      Label = "AppSecret" },
            new ProductFieldDescriptor { Key = "MediaId",        Label = "MediaId" },
            new ProductFieldDescriptor { Key = "ContractStatus", Label = "合同状态" },
            new ProductFieldDescriptor { Key = "AgconnectPath",  Label = "AGConnect路径" },
            new ProductFieldDescriptor { Key = "TdAppId",        Label = "TDAppId" },
            new ProductFieldDescriptor { Key = "AdParamStatus",  Label = "广告参数",      Editor = FieldEditor.Combo },
            new ProductFieldDescriptor { Key = "ListStatus",     Label = "上架状态",      Editor = FieldEditor.Combo },
            new ProductFieldDescriptor { Key = "Operator",       Label = "操作人" },
            new ProductFieldDescriptor { Key = "Remark",         Label = "备注",          Editor = FieldEditor.MultilineText },
        };

        public static IReadOnlyList<string> AdParamStatusOptions { get; } = new[]
        {
            "pending", "active", "inactive",
        };

        public static IReadOnlyList<string> ListStatusOptions { get; } = new[]
        {
            "listed", "unlisted", "paused",
        };
    }
}