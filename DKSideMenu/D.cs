using System;
using MonoTouch.UIKit;

namespace DKSideMenu
{
	public static class D
	{
		public static Version Version {
			get {
				return new Version (UIDevice.CurrentDevice.SystemVersion);
			}
		}
	}
}

