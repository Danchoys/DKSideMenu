using System;
using System.Drawing;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace DKSideMenu
{
	public static class UIView_DKSideMenu
	{
		public static DKContentContainerView GetDKContentContainer (this UIView _this)
		{
			if (typeof(DKContentContainerView).IsAssignableFrom (_this.GetType ()))
				return (DKContentContainerView)_this;

			UIView parentView = _this.Superview;
			while (parentView != null && !typeof(DKContentContainerView).IsAssignableFrom (parentView.GetType ()))
				parentView = parentView.Superview;

			return (DKContentContainerView)parentView;
		}
	}
}

