using System;
using System.Drawing;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace DKSideMenu
{
	[Register ("DKTransitionSegue")]
	public class DKTransitionSegue : UIStoryboardSegue
	{
		public DKTransitionSegue (string identifier, UIViewController source, UIViewController destination) : base (identifier, source, destination)
		{
			Animated = true;
		}

		public DKTransitionSegue (IntPtr handle) : base (handle)
		{
			Animated = true;
		}

		public bool Animated { get; set; }

		public override void Perform ()
		{
			DKSideMenuViewController sideMenuController = SourceViewController.GetDKSideMenu ();
			sideMenuController.PushViewController (DestinationViewController, Animated);
		}
	}
}

