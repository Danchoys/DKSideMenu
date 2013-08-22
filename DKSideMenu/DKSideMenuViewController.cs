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
using System.Collections.Generic;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace DKSideMenu
{
	[Register ("DKSideMenuViewController")]
	public partial class DKSideMenuViewController : UIViewController
	{
		#region fields
		private UIPanGestureRecognizer panRecognizer;
		private UIView menuContainerView;
		private DKSideMenuState state;
		private float menuWidth;
		private Stack<UIViewController> controllerStack;
		private Stack<DKContentContainerView> containerStack;
		private UIBarButtonItem toggleSideMenuBarButtonItem;
		private bool dropShadow;
		private UIBarStyle barStyle;
		#endregion

		#region ctors
		public DKSideMenuViewController (IntPtr handle) : base (handle)
		{
			SetupDefaults ();
		}

		public DKSideMenuViewController (UIViewController menuController, UIViewController initialContentController)
		{
			this.MenuController = menuController;
			this.InitialContentController = initialContentController;

			SetupDefaults ();
		}

		private void SetupDefaults ()
		{
			this.state = DKSideMenuState.SideMenuShown;
			this.barStyle = UIBarStyle.Default;
			this.menuWidth = 256;
			this.dropShadow = true;
			AutomaticallyDisposeChildControllers = false;
			ThresholdX = 128;
			ThresholdVelocity = 800;
			ReturnToBoundsAnimationDuration = 0.25;
			MinVelocity = 800;
			MaxVelocity = 800;
			StandardVelocity = 1000;

			controllerStack = new Stack<UIViewController> ();
			containerStack = new Stack<DKContentContainerView> ();
		}
		#endregion

		#region props
		[Outlet]
		public UIView MenuBackgroundView { get; set; }

		[Outlet]
		public UIView ContentBackgroundView { get; set; }

		public UIViewController MenuController { get; set; }

		public UIViewController InitialContentController { get; set; }

		public UIViewController RootContentController { 
			get {
				if (controllerStack.Count > 0)
					return controllerStack.ToArray () [0];
				return null;
			}
			set {
				// Запомним текущее состояние, так как метод RemoveAllContentViewControllers ()
				// сбрасывает его в DKSideMenuState.SideMenuShown
				DKSideMenuState currentState = State;
				// Удалим все контроллеры из иерархии
				RemoveAllContentViewControllers ();
				// Создадим контейнер под контроллер
				DKContentContainerView newContentContainerView = AddContentViewController (value);
				// В зависимости от контроллера и текущего состояния установим положение контейнера:
				// Если меню открыто, либо сокрыто, но мы в пейзаже, а контроллер его не поддерживает,
				// оставим меню видимым, иначе покажем контроллер во весь экран
				float newX = (currentState == DKSideMenuState.SideMenuShown || 
							!ShouldAllowToggling (value)) ? MenuWidth : 0;
				RectangleF newFrame = newContentContainerView.Frame;
				newFrame.X = newX;
				newContentContainerView.Frame = newFrame;
				// Сообщим контроллеру о том, что грядет анимация показа его вьюшки
				value.BeginAppearanceTransition (true, false);
				// Добавим контейнер в иерархию
				this.View.AddSubview (newContentContainerView);
				// Добавим контейнеру распознаватель жестов
				newContentContainerView.AddGestureRecognizer (panRecognizer);
				// Сообщим контроллеру о том, что его вьюшка попала в иерархию
				value.EndAppearanceTransition ();
				// Сообщим контроллеру о том, что он добавился к родительскому
				value.DidMoveToParentViewController (this);
				// Выставим текущее состояние меню
				this.state = (newX > 0) ? DKSideMenuState.SideMenuShown : DKSideMenuState.SideMenuHidden;
				// Добавим контроллер и контейнер в стек
				containerStack.Push (newContentContainerView);
				controllerStack.Push (value);
			}
		}

		public UIViewController TopViewController { 
			get {
				if (controllerStack.Count > 0)
					return controllerStack.Peek ();
				return null;
			}
		}

		public DKSideMenuState State { 
			get {
				return this.state;
			}
			set {
				SetState (value, false);
			}
		}

		/// <summary>
		/// Gets or sets the threshold x. When user lifts his finger with velocity smaller than ThresholdVelocity
		/// this parameter is used to determine whether the menu is shown (conteners's X is greater than the threshold)
		///  or hidden (container's X is smaller than the threshold). Default is 128.
		/// </summary>
		/// <value>The threshold x.</value>
		public float ThresholdX { get; set; }

		/// <summary>
		/// Gets or sets the threshold velocity. When user lifts his finger, the container will continue 
		/// to move in the orinal direction if the speed is greater or equal than this parameter.
		/// Default is 800.
		/// </summary>
		/// <value>The threshold velocity.</value>
		public float ThresholdVelocity { get; set; }

		/// <summary>
		/// Gets or sets the minimum velocity. This is the lowest speed at which the panel will continue moving
		/// after the user lifts his finger. Default is 800.
		/// </summary>
		/// <value>The minimum velocity.</value>
		public float MinVelocity { get; set; }

		/// <summary>
		/// Gets or sets the max velocity. This is the highest speed at which the panel will continue moving
		/// after the user lifts his finger. Default is 800.
		/// </summary>
		/// <value>The max velocity.</value>
		public float MaxVelocity { get; set; }

		/// <summary>
		/// Gets or sets the standard velocity. This is the speed at which the panel will move if
		/// the user lifts his finger with velocity less than ThresholdVelocity. Default is 1000.
		/// </summary>
		/// <value>The standard velocity.</value>
		public float StandardVelocity { get; set; }

		/// <summary>
		/// Gets or sets the duration of the return to bounds animation. This value is used when the user
		/// drags the panel out of bounds (i.e. partly offscreen). Default is 0.25.
		/// </summary>
		/// <value>The duration of the return to bounds animation.</value>
		public double ReturnToBoundsAnimationDuration { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether content containers should drop shadow. Default is true.
		/// </summary>
		/// <value><c>true</c> if drop shadow; otherwise, <c>false</c>.</value>
		public bool DropShadow { 
			get {
				return dropShadow;
			}
			set {
				if (dropShadow != value) {
					dropShadow = value;
					foreach (DKContentContainerView containerView in containerStack)
						containerView.DropShadow = dropShadow;
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the pan gesture is enabled. Default is true.
		/// </summary>
		/// <value><c>true</c> if pan gesture enabled; otherwise, <c>false</c>.</value>
		public bool PanGestureEnabled {
			get {
				return panRecognizer.Enabled;
			}
			set {
				panRecognizer.Enabled = value;
			}
		}

		/// <summary>
		/// Gets or sets the navigation bar style of all content containers. Default is UIBarStyle.Default.
		/// </summary>
		/// <value>The navigation bar style.</value>
		public UIBarStyle NavigationBarStyle {
			get {
				return barStyle;
			}
			set {
				if (barStyle != value) {
					barStyle = value;

					foreach (DKContentContainerView containerView in containerStack)
						containerView.NavigationBar.BarStyle = barStyle;
				}
			}
		}

		/// <summary>
		/// Gets or sets the width of the menu. Default is 256.
		/// </summary>
		/// <value>The width of the menu.</value>
		public float MenuWidth { 
			get {
				return this.menuWidth;
			}
			set {
				if (menuWidth != value) {
					this.menuWidth = value;

					RectangleF newFrame = this.menuContainerView.Frame;
					newFrame.Width = this.menuWidth;
					this.menuContainerView.Frame = newFrame;

					this.MenuBackgroundView.Frame = newFrame;
					this.ContentBackgroundView.Frame = new RectangleF (this.menuWidth, 0, this.View.Bounds.Width - this.menuWidth, this.View.Bounds.Height);

					if (state == DKSideMenuState.SideMenuHidden)
						MoveContentContainer (0, false, null);
					else
						MoveContentContainer (this.menuWidth, false, null);
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this controller automatically
		/// disposes child controllers. Default is false.
		/// </summary>
		/// <value><c>true</c> if automatically dispose child controllers; otherwise, <c>false</c>.</value>
		public bool AutomaticallyDisposeChildControllers { get; set; }

		private UIBarButtonItem ToggleSideMenuBarButtonItem {
			get {
				if (toggleSideMenuBarButtonItem == null) {
					UIImage toggleButtonImage = UIImage.FromResource (null, (UIScreen.MainScreen.Scale > 1.0)
					                                                  ? "DKSideMenu.Resources.dk-sidemenu-menu-icon@2x.png" : "DKSideMenu.Resources.dk-sidemenu-menu-icon.png");
					if (UIScreen.MainScreen.Scale > 1.0)
						toggleButtonImage = new UIImage (toggleButtonImage.CGImage, UIScreen.MainScreen.Scale, UIImageOrientation.Up);
					toggleSideMenuBarButtonItem = new UIBarButtonItem (toggleButtonImage, UIBarButtonItemStyle.Plain, HandleToggleSideMenuBarItemPressed);
				}

				return toggleSideMenuBarButtonItem;
			}
			set {
				toggleSideMenuBarButtonItem = value;
				toggleSideMenuBarButtonItem.Clicked += HandleToggleSideMenuBarItemPressed;
			}
		}

		private DKContentContainerView TopContentContainerView {
			get {
				if (containerStack.Count > 0)
					return containerStack.Peek ();
				return null;
			}
		}
		#endregion

		#region controller's lifecycle
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			this.View.BackgroundColor = UIColor.Black;

			// Создадим фоновую вьюшку меню
			if (MenuBackgroundView == null) {
				MenuBackgroundView = new UIView (new RectangleF (0, 0, MenuWidth, this.View.Bounds.Height));
				MenuBackgroundView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight;
				MenuBackgroundView.BackgroundColor = UIColor.Clear;
				this.View.AddSubview (MenuBackgroundView);
			}

			// Создадим фоновую вьюшку контентной области
			if (ContentBackgroundView == null) {
				ContentBackgroundView = new UIView (new RectangleF (MenuWidth, 0, this.View.Bounds.Width - MenuWidth, this.View.Bounds.Height));
				ContentBackgroundView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
				ContentBackgroundView.BackgroundColor = UIColor.Clear;
				this.View.AddSubview (ContentBackgroundView);
			}

			// Создадим конейнер для меню
			menuContainerView = new UIView (new RectangleF (0, 0, MenuWidth, this.View.Bounds.Height));
			menuContainerView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight;
			menuContainerView.BackgroundColor = UIColor.Clear;
			this.View.AddSubview (menuContainerView);

			// Создадим распознаватель жестов
			panRecognizer = new UIPanGestureRecognizer (HandlePanGesture);
			panRecognizer.WeakDelegate = this;
		}

		public override void ViewWillAppear (bool animated)
		{
			base.ViewWillAppear (animated);
			HandleRotation (this.InterfaceOrientation);
			foreach (UIViewController childController in this.ChildViewControllers)
				childController.BeginAppearanceTransition (true, animated);
		}

		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);
			foreach (UIViewController childController in this.ChildViewControllers)
				childController.EndAppearanceTransition ();
		}

		public override void ViewWillDisappear (bool animated)
		{
			base.ViewWillDisappear (animated);
			foreach (UIViewController childController in this.ChildViewControllers)
				childController.BeginAppearanceTransition (false, animated);
		}

		public override void ViewDidDisappear (bool animated)
		{
			base.ViewDidDisappear (animated);
			foreach (UIViewController childController in this.ChildViewControllers)
				childController.EndAppearanceTransition ();
		}
		#endregion

		#region rotation handling
		[Obsolete ("Deprecated in iOS6. Replace it with both GetSupportedInterfaceOrientations and PreferredInterfaceOrientationForPresentation")]
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			return true;
		}

		public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations ()
		{
			return UIInterfaceOrientationMask.All;
		}

		public override void WillRotate (UIInterfaceOrientation toInterfaceOrientation, double duration)
		{
			base.WillRotate (toInterfaceOrientation, duration);
			HandleRotation (toInterfaceOrientation);
		}

		private void HandleRotation (UIInterfaceOrientation orientation)
		{
			// Если мы повернули девайс в пейзаж, а контроллер 
			// такого не поддерживает, покажем меню без анимации.
			// Не используем метод SetState, так как он использует метод Toggle<..>,
			// а тот в свою очередь не даст изменить состояние при таком контроллере
			if (TopViewController != null) {
				if (IsLandscape (orientation) &&
					!ControllerSupportsLandscape (TopViewController)) {
					ToggleSideMenuBarButtonItem.Enabled = false;
					MoveContentContainer (MenuWidth, false, null);
				} else
					ToggleSideMenuBarButtonItem.Enabled = true;
			}
		}
		#endregion

		#region UIGestureRecognizerDelegate
		[Export("gestureRecognizer:shouldRecognizeSimultaneouslyWithGestureRecognizer:")]
		protected bool ShouldRecognizeSimultaneously (UIGestureRecognizer firstRecognizer, UIGestureRecognizer secondRecognizer)
		{
			return false;
		}
		#endregion

		#region public API
		public override void PrepareForSegue (UIStoryboardSegue segue, NSObject sender)
		{
			if (segue.Identifier == "Embed root view controller") {
				DKTransitionSegue transitionSegue = segue as DKTransitionSegue;
				if (transitionSegue != null)
					transitionSegue.Animated = false;
			}
		}

		 public override bool ShouldAutomaticallyForwardAppearanceMethods {
			get {
				return false;
			}
		}

		/// <summary>
		/// Pushs the view controller.
		/// </summary>
		/// <param name="controller">Controller.</param>
		/// <param name="animated">If set to <c>true</c> animated.</param>
		public void PushViewController (UIViewController controller, bool animated)
		{
			// Создадим контейнер под контроллер
			DKContentContainerView newContentContainerView = AddContentViewController (controller);
			// Передвинем его за правую границу экрана
			RectangleF newFrame = newContentContainerView.Frame;
			newFrame.X = this.View.Bounds.Width;
			newContentContainerView.Frame = newFrame;

			// Сообщим старому контроллеру о том, что он скоро пропадет
			if (TopViewController != null)
				TopViewController.BeginAppearanceTransition (false, animated);
			// Сообщим новому контроллеру, что его вьюшка скоро появится
			controller.BeginAppearanceTransition (true, animated);
			// Добавим контейнер в иерархию
			this.View.AddSubview (newContentContainerView);

			// Анимируем появление контейнера
			if (animated) {
				UIView.Animate (0.3, () => {
					ShowNewController (controller, newContentContainerView);
				}, () => {
					HandleOnShowNewControllerComplete (controller, newContentContainerView);
				});
			} else {
				ShowNewController (controller, newContentContainerView);
				HandleOnShowNewControllerComplete (controller, newContentContainerView);
			}
		}

		/// <summary>
		/// Pops the top view controller.
		/// </summary>
		/// <param name="animated">If set to <c>true</c> animated.</param>
		public void PopTopViewController (bool animated)
		{
			if (TopViewController == null)
				return;

			UIViewController oldTopController = controllerStack.Pop ();
			DKContentContainerView oldTopContentContainerView = containerStack.Pop ();

			// Предупредим контроллер о грядущем удалении из родительского
			oldTopController.WillMoveToParentViewController (null);
			// Предупредим контроллер о грядущей анимации ухода
			oldTopController.BeginAppearanceTransition (false, animated);
			// Контейнер более не топовый, отсоединим от него распознаватель жестов
			oldTopContentContainerView.RemoveGestureRecognizer (panRecognizer);
			
			// Добавим контейнер в иерархию под самую верхнюю вьюшку
			if (TopViewController != null) {
				RectangleF contentContainerFrame = GetContentContainerFrame (TopViewController);
				// Если меню отображено, но либо оно спрятано, но наш контроллер не показать его не может
				// (девайс в пейзаже, а контроллер пейзаж не поддерживет), поставим контейнер чуть правее конечной позиции
				contentContainerFrame.X = (IsLandscape (this.InterfaceOrientation) && !ControllerSupportsLandscape (TopViewController)) ||
											(State == DKSideMenuState.SideMenuShown) ? MenuWidth + 5 : 0;
				TopContentContainerView.Frame = contentContainerFrame;

				// Предупредим контроллер о предстоящей анимации показа
				TopViewController.BeginAppearanceTransition (true, animated);
				// Добавим контейнеру распознаватель жестов
				TopContentContainerView.AddGestureRecognizer (panRecognizer);
				// Добавим контейнер в иерархию
				this.View.InsertSubviewBelow (TopContentContainerView, oldTopContentContainerView);

				// Настроим кнопку управления панелью
				ToggleSideMenuBarButtonItem.Enabled = ShouldAllowToggling (TopViewController);
			}

			// Анимируем удаление контейнера
			if (animated)
				UIView.Animate (0.3, () => HideTopController (oldTopContentContainerView), () => HandleOnHideTopControllerComplete (oldTopController, oldTopContentContainerView));
			else {
				HideTopController (oldTopContentContainerView);
				HandleOnHideTopControllerComplete (oldTopController, oldTopContentContainerView);
			}		
		}

		/// <summary>
		/// Sets the state.
		/// </summary>
		/// <param name="state">State.</param>
		/// <param name="animated">If set to <c>true</c> animated.</param>
		public void SetState (DKSideMenuState state, bool animated)
		{
			SetState (state, animated, null);
		}

		/// <summary>
		/// Sets the state.
		/// </summary>
		/// <param name="state">State.</param>
		/// <param name="animated">If set to <c>true</c> animated.</param>
		/// <param name="completionHandle">Delegate invoked on comletion. May be null.</param>
		public void SetState (DKSideMenuState state, bool animated, NSAction completionHandle)
		{
			if (state != this.state)
				ToggleSideMenu (animated, completionHandle);
		}

		/// <summary>
		/// Toggles the side menu.
		/// </summary>
		/// <param name="animated">If set to <c>true</c> animated.</param>
		public void ToggleSideMenu (bool animated)
		{
			ToggleSideMenu (animated, null);
		}

		/// <summary>
		/// Toggles the side menu.
		/// </summary>
		/// <param name="animated">If set to <c>true</c> animated.</param>
		/// <param name="completionHandle">Delegate invoked on comletion. May be null.</param>
		public void ToggleSideMenu (bool animated, NSAction completionHandle)
		{
			if (ShouldAllowToggling ()) {
				if (State == DKSideMenuState.SideMenuHidden)
					MoveContentContainer (MenuWidth, animated, completionHandle);
				else
					MoveContentContainer (0, animated, completionHandle);
			}
		}
		#endregion

		#region private API
		private void ShowNewController (UIViewController newController, DKContentContainerView newContentContainerView)
		{
			// Если мы пытаемся показать контроллер, не поддерживающий пейзаж, будучи в пейзажной
			// ориентации, сделаем меню видимым, иначе покажем весь контроллер целиком
			float newX = !ShouldAllowToggling (newController) ? MenuWidth : 0;

			RectangleF newContainerFrame = newContentContainerView.Frame;
			newContainerFrame.X = newX;
			newContentContainerView.Frame = newContainerFrame;	

			// Отодвинем текущий контейнер вправо так, чтобы его не было
			// видно, когда вылезет новый
			if (TopContentContainerView != null) {
				// Если новый контроллер будет отображен не во весь экран, пододвинем
				// старый контроллер вправо, чтобы он ему не мешал
				RectangleF oldContainerFrame = TopContentContainerView.Frame;
				oldContainerFrame.X = (newX > 0) ? MenuWidth + 5 : oldContainerFrame.X;
				TopContentContainerView.Frame = oldContainerFrame;
			}
		}

		private bool IsLandscape (UIInterfaceOrientation orientation) 
		{
			return orientation == UIInterfaceOrientation.LandscapeLeft || orientation == UIInterfaceOrientation.LandscapeRight; 
		}

		private void HandleOnShowNewControllerComplete (UIViewController newContentController, DKContentContainerView newContentContainerView)
		{
			// Удалим старый контейнер из иерархии
			if (TopViewController != null) {
				TopContentContainerView.RemoveFromSuperview ();
				TopViewController.EndAppearanceTransition ();
			}

			newContentContainerView.AddGestureRecognizer (panRecognizer);
			// Сообщим контроллеру о том, что его вьюшка полностью показана
			newContentController.EndAppearanceTransition ();
			// Сообщим контроллеру о том, что он полностью поместился в родительский
			newContentController.DidMoveToParentViewController (this);

			this.state = (newContentContainerView.Frame.X > 0) ? DKSideMenuState.SideMenuShown : DKSideMenuState.SideMenuHidden;

			containerStack.Push (newContentContainerView);
			controllerStack.Push (newContentController);
		}

		private void HideTopController (DKContentContainerView oldTopContentContainerView)
		{
			// Выведем старый контейнер за пределы экрана
			RectangleF newOldTopContentContainerViewFrame = oldTopContentContainerView.Frame;
			newOldTopContentContainerViewFrame.X = this.View.Bounds.Width;
			oldTopContentContainerView.Frame = newOldTopContentContainerViewFrame;

			// Покажем новый контейнер
			if (TopViewController != null) {
				// Если контейнер не прижат к левому краю, значит следует оставить меню отображенным
				float newX = (TopContentContainerView.Frame.X > 0) ? MenuWidth : 0;
				RectangleF newTopContentConteinerViewFrame = TopContentContainerView.Frame;
				newTopContentConteinerViewFrame.X = newX;
				TopContentContainerView.Frame = newTopContentConteinerViewFrame;
			}
		}

		private void HandleOnHideTopControllerComplete (UIViewController oldTopViewController, DKContentContainerView oldTopContentContainerView)
		{
			// Удалим старый контейнер из иерархии
			oldTopContentContainerView.RemoveFromSuperview ();
			// Сообщим контроллеру о том, что анимация ухода окончена, 
			// а вьюшка более не в иерархии
			oldTopViewController.EndAppearanceTransition ();
			// Удалим контроллер из родительского
			RemoveContentViewController (oldTopViewController, oldTopContentContainerView);

			if (TopViewController != null)
				TopViewController.EndAppearanceTransition ();

			// Если контроллеров стеке нет, либо контейнер не прижат к левой границе экрана,
			// будем считать, что меню отображается
			this.state = (TopContentContainerView == null || TopContentContainerView.Frame.X > 0) ? DKSideMenuState.SideMenuShown : DKSideMenuState.SideMenuHidden;
		}

		private void RemoveAllContentViewControllers ()
		{
			// Топовый контроллер в данный момент отображается, и для его
			// правильного ухода из иерархии должны произойти события ViewWillDisappear
			// и ViewDidDisappear. Вьюшки остальных контролеров не находятся в иерархии,
			// поэтому для них эти события вызывать не нужно
			if (TopViewController != null) {
				// Сообщим контроолеру о грядущем удалении из родительского
				TopViewController.WillMoveToParentViewController (null);
				// Сообщим контроллеру о начале анимации ухода вьюшки из иерархии
				TopViewController.BeginAppearanceTransition (false, false);
				// Отсоединим от него распознаватель жестов
				TopContentContainerView.RemoveGestureRecognizer (panRecognizer);
				// Удалим контейнер из иерархии
				TopContentContainerView.RemoveFromSuperview ();
				// Сообщим контроллеру о том, что вьюшка покинула иерархию
				TopViewController.EndAppearanceTransition ();
				// Удалим контроллер из родительського и деинициализируем контейнер
				RemoveContentViewController (TopViewController, TopContentContainerView);

				controllerStack.Pop ();
				containerStack.Pop ();
			}

			UIViewController[] contentControllers = controllerStack.ToArray ();
			DKContentContainerView[] contentContainers = containerStack.ToArray ();

			for (int i = 0; i < contentControllers.Length; i++) {
				// Сообщим контроолеру о грядущем удалении из родительского
				contentControllers [i].WillMoveToParentViewController (null);
				// Удалим контроллер из родительського и деинициализируем контейнер
				RemoveContentViewController (contentControllers [i], contentContainers [i]);
			}

			controllerStack.Clear ();
			containerStack.Clear ();

			this.state = DKSideMenuState.SideMenuShown;
		}

		private DKContentContainerView AddContentViewController (UIViewController contentViewController)
		{
			RectangleF frame = GetContentContainerFrame (contentViewController);

			// Создадим контейнер и настроим навигационную панель
			DKContentContainerView newContentContainerView = new DKContentContainerView (frame);
			if (ControllerSupportsLandscape (contentViewController))
				newContentContainerView.AutoresizingMask = UIViewAutoresizing.FlexibleDimensions; 
			newContentContainerView.DropShadow = DropShadow;
			newContentContainerView.NavigationBar.BarStyle = NavigationBarStyle;

			UINavigationItem navItem = contentViewController.NavigationItem;
			if (navItem == null)
				navItem = new UINavigationItem ();
			if (controllerStack.Count > 0) {
				newContentContainerView.NavigationBar.PushNavigationItem (new UINavigationItem (), false);
				newContentContainerView.DidPressBackButton += HandleBackBarItemPressed;
			} else {
				navItem.LeftBarButtonItem = ToggleSideMenuBarButtonItem;
				ToggleSideMenuBarButtonItem.Enabled = ShouldAllowToggling (contentViewController);
			}
			newContentContainerView.NavigationBar.PushNavigationItem (navItem, false);

			// Добавим контроллер к родительскому
			this.AddChildViewController (contentViewController);
			contentViewController.View.Frame = newContentContainerView.ContentView.Bounds;
			contentViewController.View.AutoresizingMask = UIViewAutoresizing.FlexibleDimensions;
			// Добавим вьшку контроллера в контейнер
			newContentContainerView.ContentView.AddSubview (contentViewController.View);

			return newContentContainerView;
		}

		private void RemoveContentViewController (UIViewController contentViewController, DKContentContainerView contentContainerView)
		{
			// Удалим вьюшку контроллера из контейнера (возможно, лишний шаг)
			contentViewController.View.RemoveFromSuperview ();
			// Удалим контроллер из родительского
			contentViewController.RemoveFromParentViewController ();
			// Если указана соотв. настройка, очистим этот контроллер
			if (AutomaticallyDisposeChildControllers)
				contentViewController.Dispose ();

			// Удалим контейнер
			contentContainerView.DidPressBackButton -= HandleBackBarItemPressed;
			contentContainerView.Dispose ();
		}

		private RectangleF GetContentContainerFrame (UIViewController controller)
		{
			// Если контроллер поддерживает пейзажную ориентацию, а мы как раз 
			// в ней находимся, сделаем вьюшку широкой
			bool isLandscapeFullscreen = IsLandscape (this.InterfaceOrientation) && ControllerSupportsLandscape (controller);
			if (isLandscapeFullscreen)
				return new RectangleF (0, 0, this.View.Bounds.Width, this.View.Bounds.Height);
			else
				return new RectangleF (0, 0, UIScreen.MainScreen.Bounds.Width, this.View.Bounds.Height);
		}

		private void HandleBackBarItemPressed (object sender, EventArgs e)
		{
			PopTopViewController (true);
		}

		private void HandleToggleSideMenuBarItemPressed (object sender, EventArgs e)
		{
			ToggleSideMenu (true);
		}

		private bool ControllerSupportsLandscape (UIViewController controller)
		{
			return (controller.ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation.LandscapeLeft) &&
				controller.ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation.LandscapeRight)) ||
				((controller.GetSupportedInterfaceOrientations () & UIInterfaceOrientationMask.LandscapeLeft) != (UIInterfaceOrientationMask)0 &&
				(controller.GetSupportedInterfaceOrientations () & UIInterfaceOrientationMask.LandscapeRight) != (UIInterfaceOrientationMask)0);
		}

		/// <summary>
		/// Setups the side menu controller. Call this method to populate this controller
		/// with menu controller and/or initial content controller.
		/// </summary>
		protected void SetupSideMenuController ()
		{
			if (MenuController == null)
				this.PerformSegue ("Embed menu view controller", this);
			else
				EmbedMenuViewController (MenuController);

			if (InitialContentController == null) {
				// For some reason native "no segue" exception is not
				// catched by try-catch in monotouch. This is heavily unsafe
				// to leave this code here =(
				//	try {
				//		this.PerformSegue ("Embed root content view controller", this);
				//	} catch {
				//		Console.WriteLine ("DKSideMenyViewController: No root content view controller found");
				//	}
			} else
				PushViewController (InitialContentController, false);
		}

		internal void EmbedMenuViewController (UIViewController menuController)
		{
			this.AddChildViewController (menuController);
			menuController.View.Frame = menuContainerView.Bounds;
			menuController.View.AutoresizingMask = UIViewAutoresizing.FlexibleDimensions;
			menuContainerView.AddSubview (menuController.View);
			menuController.DidMoveToParentViewController (this);
		}

		private void HandlePanGesture (UIPanGestureRecognizer recognizer)
		{
			if (recognizer.State == UIGestureRecognizerState.Began ||
				recognizer.State == UIGestureRecognizerState.Changed) {
				// Жест начался или изменился, передвинем контентную область
				PointF translation = recognizer.TranslationInView (TopContentContainerView);

				RectangleF frame = TopContentContainerView.Frame;
				float translationX = translation.X;
				// Если контентная область вышла за границы экрана, либо ей
				// нельзя двигаться, уменьшим перемещение в три раза
				if (frame.X < 0 || frame.X > MenuWidth || !ShouldAllowToggling ())
					translationX /= 10;
				frame.X += translationX;

				TopContentContainerView.Frame = frame;
				recognizer.SetTranslation (PointF.Empty, TopContentContainerView);

			} else if (recognizer.State == UIGestureRecognizerState.Ended) {
				PointF velocity = recognizer.VelocityInView (TopContentContainerView);

				// Первый случай - перемещение запрещено.
				// Вернемся на исходную позицию анимированно,
				// используя настройку ReturnToBoundsAnimationDuration для
				// установки продолжительности анимации
				if (!ShouldAllowToggling ()) {
					if (State == DKSideMenuState.SideMenuHidden)
						MoveContentContainer (0, ReturnToBoundsAnimationDuration, null);
					else 
						MoveContentContainer (MenuWidth, ReturnToBoundsAnimationDuration, null);
					return;
				} 

				// Второй случай - панель ушла за границы экрана, но скорость
				// сонаправлена положению панели. Вернемся на ближаюшую 
				// фиксированную позицию анимированно, используя настройку 
				// ReturnToBoundsAnimationDuration для установки продолжительности анимации
				if (TopContentContainerView.Frame.X < 0 && velocity.X < 0) {
					MoveContentContainer (0, ReturnToBoundsAnimationDuration, null);
					return;
				}
				if (TopContentContainerView.Frame.X > MenuWidth && velocity.X > 0) {
					MoveContentContainer (MenuWidth, ReturnToBoundsAnimationDuration, null);
					return;
				}

				// Третий случай - пользователь двинул пальцем со скоростью, достаточной
				// для продолжения движения панельки по инерции по конца. Скрость анимации
				// соответствует скорости движения пальцем, но не может быть больше параметра
				// MaxVelocity и меньше MinVelocity.
				if (Math.Abs (velocity.X) > ThresholdVelocity) {
					float velocityX = Math.Abs (velocity.X);
					int sign = Math.Sign (velocity.X);
					velocityX = Math.Max (MinVelocity, velocityX);
					velocityX = Math.Min (MaxVelocity, velocityX);

					if (sign > 0)
						MoveContentContainer (MenuWidth, velocityX, null);
					else
						MoveContentContainer (0, velocityX, null);
					return;
				}

				// Четвертый случай - пользователь отпустил палец, когда скорость была
				// недостаточно большой. Определим ближайшее положение (основываясь на настройке)
				// и переместим панель. Продолжительность перемещния задается настройкой 
				// ReturnToBoundsAnimationDuration
				if (TopContentContainerView.Frame.X < ThresholdX)
					MoveContentContainer (0, ReturnToBoundsAnimationDuration, null);
				else
					MoveContentContainer (MenuWidth, ReturnToBoundsAnimationDuration, null);
			}
		}

		private bool ShouldAllowToggling ()
		{
			return ShouldAllowToggling (TopViewController);
		}

		private bool ShouldAllowToggling (UIInterfaceOrientation orientation)
		{
			return ShouldAllowToggling (TopViewController, orientation);
		}

		private bool ShouldAllowToggling (UIViewController controller)
		{
			return ShouldAllowToggling (controller, this.InterfaceOrientation);
		}

		private bool ShouldAllowToggling (UIViewController controller, UIInterfaceOrientation orientation)
		{
			return !IsLandscape (orientation) || ControllerSupportsLandscape (controller);
		}

		private void MoveContentContainer (float destinationX, bool animated, NSAction completionHandle)
		{
			float velocity = animated ? StandardVelocity : 0;
			MoveContentContainer (destinationX, velocity, completionHandle);
		}

		private void MoveContentContainer (float destinationX, float velocity, NSAction completionHandle)
		{
			if (TopContentContainerView == null)
				return;

			double duration = (velocity > 0) ? (float)Math.Abs (TopContentContainerView.Frame.X - destinationX) / velocity : 0;
			MoveContentContainer (destinationX, duration, completionHandle);
		}

		private void MoveContentContainer (float destinationX, double duration, NSAction completionHandle)
		{
			if (TopContentContainerView == null)
				return;

			if (completionHandle == null)
				completionHandle = () => {};

			UIView.Animate (duration, () => {
				RectangleF newFrame = TopContentContainerView.Frame;
				newFrame.X = destinationX;
				TopContentContainerView.Frame = newFrame;
			}, completionHandle);

			if (destinationX > 0)
				this.state = DKSideMenuState.SideMenuShown;
			else
				this.state = DKSideMenuState.SideMenuHidden;
		}
		#endregion
	}
}