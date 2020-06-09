using System.Runtime.CompilerServices;
using Microsoft.Identity.Client;

namespace Uno.UI.MSAL
{
	public static class AbstractApplicationBuilderExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T WithUnoHelpers<T>(this T builder)
			where T : AbstractApplicationBuilder<T>
		{
#if __ANDROID__
				.WithParentActivityOrWindow(() => Uno.UI.ContextHelper.Current as Android.App.Activity)
#elif __IOS__ || __MACOS__
				.WithParentActivityOrWindow(() => Window.RootViewController)
#elif __WASM__
			builder.WithHttpClientFactory(WasmHttpFactory.Instance);
#endif
			return builder;

		}
	}
}
