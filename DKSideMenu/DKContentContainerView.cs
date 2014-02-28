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
using System.Drawing;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace DKSideMenu
{
	public class DKContentContainerView : UIView, IUIBarPositioningDelegate
	{
		#region fields
		private bool navigationBarHidden = false;
		private UIView[] stripes; 
		#endregion

		#region ctors
		internal DKContentContainerView (RectangleF frame) : base (frame)
		{
			//  Настроим вьюшку
			this.AutoresizingMask = UIViewAutoresizing.FlexibleHeight;
			this.BackgroundColor = UIColor.Clear;

			// Создадим конентную область
			RectangleF contentViewFrame = new RectangleF (0, 44, frame.Width, frame.Height - 44);
			if (Version >= new Version ("7.0")) {
				contentViewFrame.Y = 0;
				contentViewFrame.Height = frame.Height;
			}
			ContentView = new UIView (contentViewFrame);			
			ContentView.BackgroundColor = UIColor.Clear;
			ContentView.AutoresizingMask = UIViewAutoresizing.FlexibleDimensions;
			ContentView.ClipsToBounds = true;
			this.Add (ContentView);

			// Настроим навигационную панель
			RectangleF navBarFrame = new RectangleF (0, 0, frame.Width, 44);
			if (Version >= new Version ("7.0"))
				navBarFrame.Y = 20;
			NavigationBar = new UINavigationBar (navBarFrame);
			NavigationBar.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleBottomMargin;
			NavigationBar.WeakDelegate = this;
			this.Add (NavigationBar);

			// Добавим вертикальные полоски на семерке
			if (Version >= new Version ("7.0")) {
				stripes = new UIView[2];

				UIView leftStripeView = new UIView (new RectangleF (-2, 20, 2, frame.Height));
				leftStripeView.BackgroundColor = this.TintColor;
				leftStripeView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleRightMargin;
				stripes [0] = leftStripeView;

				UIView rightStripeView = new UIView (new RectangleF (frame.Width, 20, 2, frame.Height));
				rightStripeView.BackgroundColor = this.TintColor;
				rightStripeView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleLeftMargin;
				stripes [1] = rightStripeView;

				this.AddSubview (leftStripeView);
				this.AddSubview (rightStripeView);
			} else {
				this.Layer.MasksToBounds = false;
				this.Layer.ShadowRadius = 3;
				this.Layer.ShadowOpacity = 1f;
				this.Layer.ShadowColor = UIColor.Black.CGColor;
			}
		}
		#endregion

		#region props
		/// <summary>
		/// Gets or sets a value indicating whether this container's navigation bar is hidden.
		/// </summary>
		/// <value><c>true</c> if navigation bar is hidden; otherwise, <c>false</c>.</value>
		public bool NavigationBarHidden {
			get {
				return navigationBarHidden;
			}
			set {
				SetNavigationBarHidden (value, false);
			}
		}
		#endregion

		#region internal API
		internal UINavigationBar NavigationBar { get; private set; }

		internal UIView ContentView { get; private set; }

		internal bool DropShadow {
			get {
				return !this.Layer.MasksToBounds;
			}
			set {
				this.Layer.MasksToBounds = !value;
			}
		}		

		internal event EventHandler BackButtonPressed;
		#endregion

		#region public API
		public override void TintColorDidChange ()
		{
			base.TintColorDidChange ();
			if (stripes != null)
				foreach (UIView stripe in stripes)
					stripe.BackgroundColor = this.TintColor;
		}

		public override void LayoutSubviews ()
		{
			base.LayoutSubviews ();
			this.Layer.ShadowPath = UIBezierPath.FromRect (new RectangleF (-2, 0, this.Bounds.Width + 4, this.Bounds.Height + 6)).CGPath;
		}

		/// <summary>
		/// Sets the navigation bar visibility state.
		/// </summary>
		/// <param name="hidden">If set to <c>true</c> hidden.</param>
		/// <param name="animated">If set to <c>true</c> animated.</param>
		public void SetNavigationBarHidden (bool hidden, bool animated) 
		{
			float newY = hidden ? -this.NavigationBar.Bounds.Height : 0;
			if (Version >= new Version ("7.0") && !hidden)
				newY = 20;
			UIView.Animate (animated ? 0.3 : 0, () => {
				RectangleF navBarFrame = this.NavigationBar.Frame;
				navBarFrame.Y = newY;
				this.NavigationBar.Frame = navBarFrame;

				if (Version < new Version ("7.0")) {
					RectangleF contentViewFrame = this.ContentView.Frame;
					contentViewFrame.Y = this.NavigationBar.Frame.Bottom;
					contentViewFrame.Height = this.Bounds.Height - this.NavigationBar.Frame.Bottom;
					this.ContentView.Frame = contentViewFrame;
				}
			});
		}
		#endregion

		#region private API
		public static Version Version {
			get {
				return new Version (UIDevice.CurrentDevice.SystemVersion);
			}
		}
		#endregion

		#region UINavigationBarDelegate implementation
		[Export ("navigationBar:shouldPopItem:")]
		protected virtual bool ShouldPopItem (UINavigationBar navigationBar, UINavigationItem item)
		{
			OnBackButtonPressed (this, EventArgs.Empty);
			return false;
		}

		[Export ("positionForBar:")]
		public MonoTouch.UIKit.UIBarPosition GetPositionForBar (MonoTouch.UIKit.IUIBarPositioning barPositioning)
		{
			return UIBarPosition.TopAttached;
		}
		#endregion

		#region Event raisers
		protected void OnBackButtonPressed (object sender, EventArgs e)
		{
			if (BackButtonPressed != null)
				BackButtonPressed (sender, e);
		}
		#endregion
	}
}

