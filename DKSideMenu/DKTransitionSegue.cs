//  Created by Daniil Konoplev on 13-08-25.
//  Copyright (c) 2011 Daniil Konoplev. All rights reserved.
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

