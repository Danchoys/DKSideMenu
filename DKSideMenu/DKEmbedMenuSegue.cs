using System;
using System.Drawing;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace DKSideMenu
{
	[Register ("DKEmbedMenuSegue")]
	public class DKEmbedMenuSegue : UIStoryboardSegue
	{
		public DKEmbedMenuSegue (string identifier, UIViewController source, UIViewController destination) : base (identifier, source, destination)
		{
		}

		public DKEmbedMenuSegue (IntPtr handle) : base (handle)
		{
		}

		public override void Perform ()
		{
			DKSideMenuViewController sideMenuController = SourceViewController as DKSideMenuViewController;
			sideMenuController.EmbedMenuViewController (DestinationViewController);
		}
	}
}

