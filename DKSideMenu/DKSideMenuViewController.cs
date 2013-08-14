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
				SetRootContentController (value, false);
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
					this.ContentBackgroundView = new UIView (new RectangleF (this.menuWidth, 0, this.View.Bounds.Width - this.menuWidth, this.View.Bounds.Height));

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
				if ((orientation == UIInterfaceOrientation.LandscapeLeft ||
					orientation == UIInterfaceOrientation.LandscapeRight) &&
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
		/// <summary>
		/// Sets the root content controller. Other view controllers will be removed from the queue.
		/// </summary>
		/// <param name="controller">Controller.</param>
		/// <param name="animated">If set to <c>true</c> animated.</param>
		public void SetRootContentController (UIViewController controller, bool animated)
		{
			RemoveAllContentViewControllers ();
			PushViewController (controller, animated);
		}

		public override void PrepareForSegue (UIStoryboardSegue segue, NSObject sender)
		{
			if (segue.Identifier == "Embed root view controller") {
				DKTransitionSegue transitionSegue = segue as DKTransitionSegue;
				if (transitionSegue != null)
					transitionSegue.Animated = false;
			}
		}

		/// <summary>
		/// Pushs the view controller.
		/// </summary>
		/// <param name="controller">Controller.</param>
		/// <param name="animated">If set to <c>true</c> animated.</param>
		public void PushViewController (UIViewController controller, bool animated)
		{
			// Зададим размер контейнера
			RectangleF frame = GetContentContainerFrame (controller);
			frame.X = this.View.Bounds.Width;

			// Создадим контейнер и настроим навигационную панель
			DKContentContainerView newContentContainerView = new DKContentContainerView (frame);
			if (ControllerSupportsLandscape (controller))
				newContentContainerView.AutoresizingMask = UIViewAutoresizing.FlexibleDimensions; 
			newContentContainerView.DropShadow = DropShadow;
			newContentContainerView.NavigationBar.BarStyle = NavigationBarStyle;

			UINavigationItem navItem = controller.NavigationItem;
			if (navItem == null)
				navItem = new UINavigationItem ();
			if (controllerStack.Count > 0) {
				newContentContainerView.NavigationBar.PushNavigationItem (new UINavigationItem (), false);
				newContentContainerView.DidPressBackButton += HandleBackBarItemPressed;
			} else {
				navItem.LeftBarButtonItem = ToggleSideMenuBarButtonItem;
				ToggleSideMenuBarButtonItem.Enabled = ShouldAllowToggling (controller);
			}
			newContentContainerView.NavigationBar.PushNavigationItem (navItem, false);

			// Добавим контроллер в иерархию вьюшек
			this.AddChildViewController (controller);
			controller.View.Frame = newContentContainerView.ContentView.Bounds;
			controller.View.AutoresizingMask = UIViewAutoresizing.FlexibleDimensions;
			newContentContainerView.ContentView.AddSubview (controller.View);
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
			
			// Добавим контейнер в иерархию под самую верхнюю вьюшку
			if (TopContentContainerView != null) {
				RectangleF contentContainerFrame = GetContentContainerFrame (TopViewController);
				// Если меню отображено, но либо оно спрятано, но наш контроллер не показать его не может
				// (девайс в пейзаже, а контроллер пейзаж не поддерживет), поставим контейнер чуть правее конечной позиции
				contentContainerFrame.X = (IsLandscape (this.InterfaceOrientation) && !ControllerSupportsLandscape (TopViewController)) ||
											(State == DKSideMenuState.SideMenuShown) ? MenuWidth + 5 : 0;
				TopContentContainerView.Frame = contentContainerFrame;

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
			float newX = (IsLandscape (this.InterfaceOrientation) && !ControllerSupportsLandscape (newController)) ? MenuWidth : 0;

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

		private void HandleOnShowNewControllerComplete (UIViewController controller, DKContentContainerView newContentContainerView)
		{
			controller.DidMoveToParentViewController (this);

			// Удалим старый контейнер из иерархии
			if (TopContentContainerView != null)
				TopContentContainerView.RemoveFromSuperview ();

			newContentContainerView.AddGestureRecognizer (panRecognizer);

			this.state = (newContentContainerView.Frame.X > 0) ? DKSideMenuState.SideMenuShown : DKSideMenuState.SideMenuHidden;

			containerStack.Push (newContentContainerView);
			controllerStack.Push (controller);
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
			RemoveContentViewController (oldTopViewController, oldTopContentContainerView);

			if (TopContentContainerView != null)
				TopContentContainerView.AddGestureRecognizer (panRecognizer);

			// Если контроллеров стеке нет, либо контейнер не прижат к левой границе экрана,
			// будем считать, что меню отображается
			this.state = (TopContentContainerView == null || TopContentContainerView.Frame.X > 0) ? DKSideMenuState.SideMenuShown : DKSideMenuState.SideMenuHidden;
		}

		private void RemoveAllContentViewControllers ()
		{
			UIViewController[] contentControllers = controllerStack.ToArray ();
			DKContentContainerView[] contentContainers = containerStack.ToArray ();

			for (int i = 0; i < contentControllers.Length; i++) {
				contentControllers [i].WillMoveToParentViewController (null);
				RemoveContentViewController (contentControllers [i], contentContainers [i]);
			}

			controllerStack.Clear ();
			containerStack.Clear ();
		}

		private void RemoveContentViewController (UIViewController contentViewController, DKContentContainerView contentContainerView)
		{
			contentViewController.View.RemoveFromSuperview ();
			contentViewController.RemoveFromParentViewController ();
			if (AutomaticallyDisposeChildControllers)
				contentViewController.Dispose ();

			contentContainerView.DidPressBackButton -= HandleBackBarItemPressed;
			contentContainerView.RemoveGestureRecognizer (panRecognizer);
			contentContainerView.RemoveFromSuperview ();
			contentContainerView.Dispose ();
		}

		private RectangleF GetContentContainerFrame (UIViewController controller)
		{
			// Если контроллер поддерживает пейзажную ориентацию, а мы как раз 
			// в ней находимся, сделаем вьюшку широкой
			bool isLandscapeFullscreen = (this.InterfaceOrientation == UIInterfaceOrientation.LandscapeLeft || 
			                              this.InterfaceOrientation == UIInterfaceOrientation.LandscapeRight) &&
											ControllerSupportsLandscape (controller);
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
//				try {
//					this.PerformSegue ("Embed root content view controller", this);
//				}
//				catch {
//					Console.WriteLine ("DKSideMenyViewController: No root content view controller found");
//				}
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
			return orientation == UIInterfaceOrientation.Portrait || 
				orientation == UIInterfaceOrientation.PortraitUpsideDown || 
					ControllerSupportsLandscape (controller);
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