//  Created by Daniil Konoplev on 13-08-13.
//  Copyright (c) 2013 Daniil Konoplev. All rights reserved.
//
//  Latest code can be found on GitHub: https://github.com/danchoys/DKSideMenu

//  This file is part of DKSideMenu.
//
//	DKSideMenu is free software: you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation, either version 3 of the License, or
//	(at your option) any later version.
//
//	DKSideMenu is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with DKSideMenu.  If not, see <http://www.gnu.org/licenses/>.

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

		internal static void BeginAppearanceTransitionIfNeeded (this UIViewController _this, bool isAppearing, bool animated)
		{
			DKSideMenuViewController sideMenuController = _this.GetDKSideMenu ();
			if (sideMenuController != null) {
				if (sideMenuController.ShouldControllerBeginAppearanceTransition (_this)) {
					_this.BeginAppearanceTransition (isAppearing, animated);
					sideMenuController.SetControllerTransitioningAppearance (_this, true);
				}
			}
		}

		internal static void EndAppearanceTransitionIfNeeded (this UIViewController _this)
		{
			DKSideMenuViewController sideMenuController = _this.GetDKSideMenu ();
			if (sideMenuController != null) {
				if (sideMenuController.ShouldControllerEndAppearanceTransition (_this)) {
					_this.EndAppearanceTransition ();
					sideMenuController.SetControllerTransitioningAppearance (_this, false);
				}
			}
		}
	}
}

