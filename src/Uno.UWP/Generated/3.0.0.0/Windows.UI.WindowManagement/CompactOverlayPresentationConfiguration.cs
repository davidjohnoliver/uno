#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.WindowManagement
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CompactOverlayPresentationConfiguration : global::Windows.UI.WindowManagement.AppWindowPresentationConfiguration
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public CompactOverlayPresentationConfiguration() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.WindowManagement.CompactOverlayPresentationConfiguration", "CompactOverlayPresentationConfiguration.CompactOverlayPresentationConfiguration()");
		}
		#endif
		// Forced skipping of method Windows.UI.WindowManagement.CompactOverlayPresentationConfiguration.CompactOverlayPresentationConfiguration()
	}
}
