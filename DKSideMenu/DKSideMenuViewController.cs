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
		private UIViewController statusBarAppearanceSettingViewController;
		private bool dropShadow;
		private UIBarStyle barStyle;
		private bool isPresented;
		private HashSet<UIViewController> appearanceTransitioningControllers;
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

			appearanceTransitioningControllers = new HashSet<UIViewController> ();
			controllerStack = new Stack<UIViewController> ();
			containerStack = new Stack<DKContentContainerView> ();
		}
		#endregion

		#region props
		/// <summary>
		/// Gets or sets the menu background view.
		/// </summary>
		/// <value>The menu background view.</value>
		public UIView MenuBackgroundView { get; private set; }

		/// <summary>
		/// Gets or sets the content background view.
		/// </summary>
		/// <value>The content background view.</value>
		public UIView ContentBackgroundView { get; private set; }

		/// <summary>
		/// Gets or sets the menu controller. It used as the menu controller
		/// when SetupSideMenuController is called. If menu is embeded via a segue
		/// this property is set automatically.
		/// </summary>
		/// <value>The menu controller.</value>
		public UIViewController MenuController { get; set; }

		/// <summary>
		/// Gets or sets the initial content controller. It is used
		/// as the first content controller  when SetupSideMenuController is called
		/// </summary>
		/// <value>The initial content controller.</value>
		public UIViewController InitialContentController { get; set; }

		/// <summary>
		/// Gets or sets the root content controller.
		/// </summary>
		/// <value>The root content controller.</value>
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
				// Добавим в стек новый контроллер
				PushContentController (value, false);
				// Выставим текущее состояние меню. Если контроллер такое состояние
				// не поддерживает, то никакое действие произведено не будет
				SetState (currentState, false);
			}
		}

		/// <summary>
		/// Gets the top content view controller.
		/// </summary>
		/// <value>The top view controller.</value>
		public UIViewController TopContentController { 
			get {
				if (controllerStack.Count > 0)
					return controllerStack.Peek ();
				return null;
			}
		}

		/// <summary>
		/// Gets or sets the menu state.
		/// </summary>
		/// <value>The state.</value>
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
			MenuBackgroundView = new UIView (new RectangleF (0, 0, MenuWidth, this.View.Bounds.Height));
			MenuBackgroundView.BackgroundColor = UIColor.Clear;
			MenuBackgroundView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleRightMargin;
			MenuBackgroundView.TranslatesAutoresizingMaskIntoConstraints = true;
			this.View.AddSubview (MenuBackgroundView);

			// Создадим фоновую вьюшку контентной области
			ContentBackgroundView = new UIView (new RectangleF (MenuWidth, 0, this.View.Bounds.Width - MenuWidth, this.View.Bounds.Height));
			ContentBackgroundView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
			ContentBackgroundView.BackgroundColor = UIColor.Clear;
			this.View.AddSubview (ContentBackgroundView);

			// Создадим конейнер для меню
			menuContainerView = new UIView (new RectangleF (0, 0, MenuWidth, this.View.Bounds.Height));
			menuContainerView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleRightMargin;
			menuContainerView.BackgroundColor = UIColor.Clear;
			this.View.AddSubview (menuContainerView);

			// Создадим распознаватель жестов
			panRecognizer = new UIPanGestureRecognizer (HandlePanGesture);
			panRecognizer.WeakDelegate = this;
		}

		public override void ViewWillAppear (bool animated)
		{
			base.ViewWillAppear (animated);
			isPresented = true;
			HandleRotation (this.InterfaceOrientation);
			foreach (UIViewController childController in this.ChildViewControllers)
				childController.BeginAppearanceTransitionIfNeeded (true, animated);
		}

		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);
			foreach (UIViewController childController in this.ChildViewControllers)
				childController.EndAppearanceTransitionIfNeeded ();
		}

		public override void ViewWillDisappear (bool animated)
		{
			base.ViewWillDisappear (animated);
			foreach (UIViewController childController in this.ChildViewControllers)
				childController.BeginAppearanceTransitionIfNeeded (false, animated);
		}

		public override void ViewDidDisappear (bool animated)
		{
			base.ViewDidDisappear (animated);
			isPresented = false;
			foreach (UIViewController childController in this.ChildViewControllers)
				childController.EndAppearanceTransitionIfNeeded ();
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

		public override void WillAnimateRotation (UIInterfaceOrientation toInterfaceOrientation, double duration)
		{
			base.WillAnimateRotation (toInterfaceOrientation, duration);
			HandleRotation (toInterfaceOrientation);
		}

		private void HandleRotation (UIInterfaceOrientation orientation)
		{
			// Если мы повернули девайс в пейзаж, а контроллер 
			// такого не поддерживает, покажем меню без анимации.
			// Не используем метод SetState, так как он использует метод Toggle<..>,
			// а тот в свою очередь не даст изменить состояние при таком контроллере
			if (TopContentController != null) {
				if (IsLandscape (orientation) &&
					!ControllerSupportsLandscape (TopContentController)) {
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
			} else if (segue.Identifier == "Embed menu view controller") {
				this.MenuController = segue.DestinationViewController;
			}
		}

		 public override bool ShouldAutomaticallyForwardAppearanceMethods {
			get {
				return false;
			}
		}

		public override UIViewController ChildViewControllerForStatusBarStyle ()
		{
			if (this.statusBarAppearanceSettingViewController != null)
				return this.statusBarAppearanceSettingViewController;
			return this.TopContentController;
		}

		/// <summary>
		/// Pushes the view controller.
		/// </summary>
		/// <param name="controller">Controller.</param>
		/// <param name="animated">If set to <c>true</c> animated.</param>
		public async void PushContentController (UIViewController controller, bool animated)
		{
			// Отключим всякое взаимодействие с приложением на время анимации
			UIApplication.SharedApplication.BeginIgnoringInteractionEvents ();
			// Сделаем новый контроллер используемым для определения
			// стиля статус бара. Требуется в iOS7 и выше.
			this.statusBarAppearanceSettingViewController = controller;

			// Создадим контейнер под контроллер
			DKContentContainerView newContentContainerView = AddContentViewController (controller);
			// Передвинем его за правую границу экрана
			RectangleF newFrame = newContentContainerView.Frame;
			newFrame.X = this.View.Bounds.Width;
			newContentContainerView.Frame = newFrame;

			// Сообщим старому контроллеру о том, что он скоро пропадет
			if (TopContentController != null)
				TopContentController.BeginAppearanceTransitionIfNeeded (false, animated);
			// Сообщим новому контроллеру, что его вьюшка скоро появится
			controller.BeginAppearanceTransitionIfNeeded (true, animated);
			// Добавим контейнер в иерархию
			this.View.AddSubview (newContentContainerView);
			// Пора обновить внешний вид статус бара
			SetNeedsStatusBarAppearanceUpdate ();

			// Анимируем появление контейнера
			if (animated)
				await UIView.AnimateAsync (0.3, () => ShowNewController (controller, newContentContainerView));
			else
				ShowNewController (controller, newContentContainerView);
			HandleNewControllerShown (controller, newContentContainerView);

			// Сбросим поле, содержащее контроллер, использумый для
			// определения стиля статус бара. Только в iOS7 и выше.
			this.statusBarAppearanceSettingViewController = null;
			// Возобновим взаимодействие приложения с пользователем
			UIApplication.SharedApplication.EndIgnoringInteractionEvents ();
		}

		/// <summary>
		/// Pops the top content controller.
		/// </summary>
		/// <param name="animated">If set to <c>true</c> animated.</param>
		public async void PopTopContentController (bool animated)
		{
			if (TopContentController == null)
				return;

			IDKSideMenuControllerDelegate sideMenuControllerDelegate = TopContentController as IDKSideMenuControllerDelegate;
			if (sideMenuControllerDelegate != null && !sideMenuControllerDelegate.ShouldPopFromViewControlleStack)
				return;

			// Отключим всякое взаимодействие с приложением на время анимации
			UIApplication.SharedApplication.BeginIgnoringInteractionEvents ();

			UIViewController oldTopController = controllerStack.Pop ();
			DKContentContainerView oldTopContentContainerView = containerStack.Pop ();

			// Предупредим контроллер о грядущем удалении из родительского
			oldTopController.WillMoveToParentViewController (null);
			// Предупредим контроллер о грядущей анимации ухода
			oldTopController.BeginAppearanceTransitionIfNeeded (false, animated);
			// Контейнер более не топовый, отсоединим от него распознаватель жестов
			oldTopContentContainerView.RemoveGestureRecognizer (panRecognizer);
			
			// Добавим контейнер в иерархию под самую верхнюю вьюшку
			if (TopContentController != null) {
				RectangleF contentContainerFrame = GetContentContainerFrame (TopContentController);
				// Если меню отображено, но либо оно спрятано, но наш контроллер не показать его не может
				// (девайс в пейзаже, а контроллер пейзаж не поддерживет), поставим контейнер чуть правее конечной позиции
				contentContainerFrame.X = (IsLandscape (this.InterfaceOrientation) && !ControllerSupportsLandscape (TopContentController)) ||
											(State == DKSideMenuState.SideMenuShown) ? MenuWidth + 5 : 0;
				TopContentContainerView.Frame = contentContainerFrame;

				// Предупредим контроллер о предстоящей анимации показа
				TopContentController.BeginAppearanceTransitionIfNeeded (true, animated);
				// Добавим контейнеру распознаватель жестов
				TopContentContainerView.AddGestureRecognizer (panRecognizer);
				// Добавим контейнер в иерархию
				this.View.InsertSubviewBelow (TopContentContainerView, oldTopContentContainerView);

				// Настроим кнопку управления панелью
				ToggleSideMenuBarButtonItem.Enabled = ShouldAllowToggling (TopContentController);
			}
			// Пора обновить внешний вид статус бара
			SetNeedsStatusBarAppearanceUpdate ();

			// Анимируем удаление контейнера
			if (animated)
				await UIView.AnimateAsync (0.3, () => HideTopController (oldTopContentContainerView));
			else
				HideTopController (oldTopContentContainerView);
			HandleTopControllerHidden (oldTopController, oldTopContentContainerView);

			// Возобновим взаимодействие приложения с пользователем
			UIApplication.SharedApplication.EndIgnoringInteractionEvents ();
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
			if (TopContentController != null) {
				// Если новый контроллер будет отображен не во весь экран, пододвинем
				// старый контроллер вправо, чтобы он ему не мешал
				RectangleF oldContainerFrame = TopContentContainerView.Frame;
				oldContainerFrame.X = (newX > 0) ? MenuWidth + 5 : oldContainerFrame.X;
				TopContentContainerView.Frame = oldContainerFrame;
			}
		}

		private void HandleNewControllerShown (UIViewController newContentController, DKContentContainerView newContentContainerView)
		{
			// Удалим старый контейнер из иерархии
			if (TopContentController != null) {
				TopContentContainerView.RemoveFromSuperview ();
				TopContentController.EndAppearanceTransitionIfNeeded ();
			}

			newContentContainerView.AddGestureRecognizer (panRecognizer);
			// Сообщим контроллеру о том, что его вьюшка полностью показана
			newContentController.EndAppearanceTransitionIfNeeded ();
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
			if (TopContentController != null) {
				// Если контейнер не прижат к левому краю, значит следует оставить меню отображенным
				float newX = (TopContentContainerView.Frame.X > 0) ? MenuWidth : 0;
				RectangleF newTopContentContainerViewFrame = TopContentContainerView.Frame;
				newTopContentContainerViewFrame.X = newX;
				TopContentContainerView.Frame = newTopContentContainerViewFrame;
			}
		}

		private void HandleTopControllerHidden (UIViewController oldTopViewController, DKContentContainerView oldTopContentContainerView)
		{
			// Удалим старый контейнер из иерархии
			oldTopContentContainerView.RemoveFromSuperview ();
			// Сообщим контроллеру о том, что анимация ухода окончена, 
			// а вьюшка более не в иерархии
			oldTopViewController.EndAppearanceTransitionIfNeeded ();
			// Удалим контроллер из родительского
			RemoveContentViewController (oldTopViewController, oldTopContentContainerView);

			if (TopContentController != null)
				TopContentController.EndAppearanceTransitionIfNeeded ();

			// Если контроллеров стеке нет, либо контейнер не прижат к левой границе экрана,
			// будем считать, что меню отображается
			this.state = (TopContentContainerView == null || TopContentContainerView.Frame.X > 0) ? DKSideMenuState.SideMenuShown : DKSideMenuState.SideMenuHidden;
		}

		private bool IsLandscape (UIInterfaceOrientation orientation) 
		{
			return orientation == UIInterfaceOrientation.LandscapeLeft || orientation == UIInterfaceOrientation.LandscapeRight; 
		}

		private void RemoveAllContentViewControllers ()
		{
			// Топовый контроллер в данный момент отображается, и для его
			// правильного ухода из иерархии должны произойти события ViewWillDisappear
			// и ViewDidDisappear. Вьюшки остальных контролеров не находятся в иерархии,
			// поэтому для них эти события вызывать не нужно
			if (TopContentController != null) {
				// Сообщим контроллеру о грядущем удалении из родительского
				TopContentController.WillMoveToParentViewController (null);
				// Сообщим контроллеру о начале анимации ухода вьюшки из иерархии
				TopContentController.BeginAppearanceTransitionIfNeeded (false, false);
				// Отсоединим от него распознаватель жестов
				TopContentContainerView.RemoveGestureRecognizer (panRecognizer);
				// Удалим контейнер из иерархии
				TopContentContainerView.RemoveFromSuperview ();
				// Сообщим контроллеру о том, что вьюшка покинула иерархию
				TopContentController.EndAppearanceTransitionIfNeeded ();
				// Удалим контроллер из родительського и деинициализируем контейнер
				RemoveContentViewController (TopContentController, TopContentContainerView);

				controllerStack.Pop ();
				containerStack.Pop ();
			}

			UIViewController[] contentControllers = controllerStack.ToArray ();
			DKContentContainerView[] contentContainers = containerStack.ToArray ();

			for (int i = 0; i < contentControllers.Length; i++) {
				// Сообщим контроллеру о грядущем удалении из родительского
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
				string prevControllerTitle = null;
				// Сперва посмотрим, задан ли у контроллера заголовок
				if (!string.IsNullOrEmpty (TopContentController.Title))
					prevControllerTitle = TopContentController.Title;
				// Если заголовок установлен не был, проверим, есть ли у контроллера
				// навигационный элемент и задан ли у него заголовок
				if (prevControllerTitle == null && TopContentController.NavigationItem != null && !string.IsNullOrEmpty (TopContentController.NavigationItem.Title))
					prevControllerTitle = TopContentController.NavigationItem.Title;
				newContentContainerView.NavigationBar.PushNavigationItem (prevControllerTitle != null ? new UINavigationItem (prevControllerTitle) : new UINavigationItem (), false);
				newContentContainerView.BackButtonPressed += HandleBackBarItemPressed;
			} else {
				navItem.LeftBarButtonItem = ToggleSideMenuBarButtonItem;
				ToggleSideMenuBarButtonItem.Enabled = ShouldAllowToggling (contentViewController);
			}
			newContentContainerView.NavigationBar.PushNavigationItem (navItem, false);

			// Добавим контроллер к родительскому
			this.AddChildViewController (contentViewController);
			contentViewController.View.Frame = newContentContainerView.ContentView.Bounds;
			contentViewController.View.AutoresizingMask = UIViewAutoresizing.FlexibleDimensions;
			// На iOS7 в том случае, когда включена автоматическая подстройка
			// отступов скролл вьюшек, произведем эту самую подстройку
			if (D.Version >= new Version ("7.0") && contentViewController.AutomaticallyAdjustsScrollViewInsets) {
				InterateOverViews (contentViewController.View, v => {
					UIScrollView scrollView = v as UIScrollView;
					if (scrollView != null) {
						scrollView.ContentInset = new UIEdgeInsets (64, 0, 0, 0);
						scrollView.ScrollIndicatorInsets = new UIEdgeInsets (64, 0, 0, 0);
					}
				});
			}
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
			contentContainerView.BackButtonPressed -= HandleBackBarItemPressed;
			contentContainerView.Dispose ();
		}

		private void InterateOverViews(UIView view, Action<UIView> action)
		{
			action (view);
			foreach (UIView subview in view.Subviews)
				InterateOverViews (subview, action);
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

		private new void SetNeedsStatusBarAppearanceUpdate ()
		{
			if (this.RespondsToSelector (new MonoTouch.ObjCRuntime.Selector ("setNeedsStatusBarAppearanceUpdate")))
				base.SetNeedsStatusBarAppearanceUpdate ();
		}

		private void HandleBackBarItemPressed (object sender, EventArgs e)
		{
			PopTopContentController (true);
		}

		private void HandleToggleSideMenuBarItemPressed (object sender, EventArgs e)
		{
			ToggleSideMenu (true);
		}

		private bool ControllerSupportsLandscape (UIViewController controller)
		{
			return ((controller.GetSupportedInterfaceOrientations () & UIInterfaceOrientationMask.LandscapeLeft) != (UIInterfaceOrientationMask)0 &&
				(controller.GetSupportedInterfaceOrientations () & UIInterfaceOrientationMask.LandscapeRight) != (UIInterfaceOrientationMask)0);
		}

		/// <summary>
		/// Sets up the side menu controller. Call this method to populate this controller
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
				//		Console.WriteLine ("DKSideMenuViewController: No root content view controller found");
				//	}
			} else
				PushContentController (InitialContentController, false);
		}

		internal void SetControllerTransitioningAppearance  (UIViewController controller, bool isBeingPresented) 
		{
			if (isBeingPresented)
				appearanceTransitioningControllers.Add (controller);
			else
				appearanceTransitioningControllers.Remove (controller);
		}

		internal bool ShouldControllerBeginAppearanceTransition (UIViewController controller) 
		{
			return this.isPresented && !appearanceTransitioningControllers.Contains (controller);
		}

		internal bool ShouldControllerEndAppearanceTransition (UIViewController controller)
		{
			return this.isPresented && appearanceTransitioningControllers.Contains (controller);
		}

		internal void EmbedMenuViewController (UIViewController menuController)
		{
			this.AddChildViewController (menuController);
			menuController.View.Frame = menuContainerView.Bounds;
			menuController.View.AutoresizingMask = UIViewAutoresizing.FlexibleDimensions;
			menuController.BeginAppearanceTransitionIfNeeded (true, false);
			menuContainerView.AddSubview (menuController.View);
			menuController.EndAppearanceTransitionIfNeeded ();
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
				// нельзя двигаться, уменьшим перемещение в десять раз
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
			return ShouldAllowToggling (TopContentController);
		}

		private bool ShouldAllowToggling (UIInterfaceOrientation orientation)
		{
			return ShouldAllowToggling (TopContentController, orientation);
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