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

