using System;
using System.Collections.Generic;

namespace ZlinksPackageSystem.Desktop.Models
{
    public static class ParamRouteRegistry
    {
        private static readonly IReadOnlyDictionary<string, string> Routes =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["honor"]  = "/honor-params",
                ["vivo"]   = "/vivo-params",
                ["huawei"] = "/huawei-params",
            };

        public static bool IsKnownCode(string? code) =>
            !string.IsNullOrWhiteSpace(code) && Routes.ContainsKey(code);

        public static string? GetRoute(string? code) =>
            !string.IsNullOrWhiteSpace(code) && Routes.TryGetValue(code, out var path)
                ? path : null;

        public static IReadOnlyDictionary<string, string> KnownCodes => Routes;
    }
}