using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace DKSideMenu
{
	public static class UIViewController_DKSideMenu
	{
		public static DKSideMenuViewController GetDKSideMenu (this UIViewController _this)
		{
			if (typeof(DKSideMenuViewController).IsAssignableFrom (_this.GetType ()))
				return (DKSideMenuViewController)_this;
			
			UIViewController parentViewController = _this.ParentViewController;
			while (parentViewController != null && !typeof(DKSideMenuViewController).IsAssignableFrom (parentViewController.GetType ()))
				parentViewController = parentViewController.ParentViewController;

			return (DKSideMenuViewController)parentViewController;
		}
	}
}

