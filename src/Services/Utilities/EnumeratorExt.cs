using System;
namespace Turbo.Maui.Services.Utilities
{
	public static class EnumeratorExt
	{
        public static bool TryFirstOrDefault<T>(this IEnumerable<T> source, Func<T, bool> match, out T? value)
        {
            value = default;

            foreach (var current in source)
            {
                if (match(current))
                {
                    value = current;
                    return true;
                }
            }
            return false;
        }
    }
}

