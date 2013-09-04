//
//  DKSideMenu.m
//  DKSideMenu
//
//  Created by Daniil Konoplev on 16.08.13.
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

#import "DKSideMenuViewController.h"
#import "DKContentContainerView.h"
#import "DKTransitionSegue.h"

NSString *const DKSideMenuEmbedMenuControllerSegueIdentifier = @"Embed menu view controller";
NSString *const DKSideMenuEmbedInitialContentControllerSegueIdentifier = @"Embed initial content view controller";

@interface DKSideMenuViewController () <UIGestureRecognizerDelegate, DKContentContainerViewDelegate> {
    UIPanGestureRecognizer *_panRecognizer;
    UIView *_menuContainerView;
    DKSideMenuState _state;
    float _menuWidth;
    NSMutableArray *_controllerStack;
    NSMutableArray *_containerStack;
    UIBarButtonItem *_toggleSideMenuBarButtonItem;
    BOOL _dropShadow;
    UIBarStyle _barStyle;
}

@property (nonatomic) UIBarButtonItem *toggleSideMenuBarButtonItem;

@property (nonatomic, readonly) DKContentContainerView *topContentContainerView;

@end

@implementation DKSideMenuViewController

#pragma mark - ctors

- (id)initWithCoder:(NSCoder *)aDecoder
{
    self = [super initWithCoder:aDecoder];
    if (self) {
        [self setup];
    }
    return self;
}

- (id)initWithMenuController:(UIViewController *)menuController initialContentController:(UIViewController *)initialContentController
{
    self = [super init];
    if (self) {
        self.menuController = menuController;
        self.initialContentController = initialContentController;
        
        [self setup];
    }
    return self;
}

- (void)setup
{
    _shouldAutomaticallySetupSideMenuController = YES;
    _state = DKSideMenuStateShown;
    _barStyle = UIBarStyleDefault;
    _menuWidth = 256;
    _dropShadow = YES;
    
    self.thresholdX = 128;
    self.thresholdVelocity = 800;
    self.returnToBoundsAnimationDuration = 0.25;
    self.minVelocity = 800;
    self.maxVelocity = 800;
    self.standardVelocity = 1000;
    
    _controllerStack = [NSMutableArray array];
    _containerStack = [NSMutableArray array];
}

#pragma mark - props

- (UIViewController *)rootContentController
{
    if (_controllerStack.count > 0) return _controllerStack [0];
    return nil;
}

- (void)setRootContentController:(UIViewController *)rootContentController
{
    // Save the current state as removeAllContentViewControllers
    // resets it to DKSideMenuStateShown
    DKSideMenuState currentState = self.state;
    // Remove all controllers from the hierarchy
    [self removeAllContentViewControllers];
    // Create new container for controller
    DKContentContainerView *newContentContainerView = [self addContentViewController:rootContentController];
    // According to the current controller and current state:
    // If the menu is shown or hidden while we are in landscape,
    // which is not supported by the controller, leave the menu shown,
    // otherwise show it fullscreen.
    float newX = (currentState == DKSideMenuStateShown || ![self shouldAllowTogglingWithController:rootContentController]) ? self.menuWidth : 0;
    CGRect newFrame = newContentContainerView.frame;
    newFrame.origin.x = newX;
    newContentContainerView.frame = newFrame;
    // Tell controller that its view is about to be presented
    [rootContentController beginAppearanceTransition:YES animated:NO];
    // Add container to the hierarchy
    [self.view addSubview:newContentContainerView];
    // Add gesture recognizer to the view
    [newContentContainerView addGestureRecognizer:_panRecognizer];
    // Tell controller that its view has been presented
    [rootContentController endAppearanceTransition];
    // Tell controller that it has moved to parent controller
    [rootContentController didMoveToParentViewController:self];
    // Set the new state
    _state = (newX > 0) ? DKSideMenuStateShown : DKSideMenuStateHidden;
    
    [_containerStack addObject:newContentContainerView];
    [_controllerStack addObject:rootContentController];
}

- (UIViewController *)topContentController
{
    if (_controllerStack.count > 0) return _controllerStack.lastObject;
    return nil;
}

- (void)setState:(DKSideMenuState)state
{
    [self setState:state animated:NO];
}

- (DKSideMenuState)state
{
    return _state;
}

- (BOOL)dropShadow
{
    return _dropShadow;
}

- (void)setDropShadow:(BOOL)dropShadow
{
    if (dropShadow != _dropShadow) {
        _dropShadow = dropShadow;
        for (DKContentContainerView *containerView in _containerStack)
            containerView.dropShadow = _dropShadow;
    }
}

- (BOOL)panGestureEnabled
{
    return _panRecognizer.enabled;
}

- (void)setPanGestureEnabled:(BOOL)panGestureEnabled
{
    _panRecognizer.enabled = panGestureEnabled;
}

- (UIBarStyle)navigationBarStyle
{
    return _barStyle;
}

- (void)setNavigationBarStyle:(UIBarStyle)navigationBarStyle
{
    if (_barStyle != navigationBarStyle) {
        _barStyle = navigationBarStyle;
        for (DKContentContainerView *containerView in _containerStack) {
            containerView.navigationBar.barStyle = _barStyle;
        }
    }
}

- (float)menuWidth
{
    return _menuWidth;
}

- (void)setMenuWidth:(float)menuWidth
{
    if (_menuWidth != menuWidth) {
        _menuWidth = menuWidth;
        
        CGRect newFrame = _menuContainerView.frame;
        newFrame.size.width = _menuWidth;        
        _menuContainerView.frame = newFrame;
        
        self.menuBackgroundView.frame = newFrame;
        self.contentBackgroundView.frame = CGRectMake(_menuWidth, 0, self.view.bounds.size.width - _menuWidth, self.view.bounds.size.height);
        
        if (self.state == DKSideMenuStateHidden)
            [self moveContentContainer:0 animated:NO completion:NULL];
        else
            [self moveContentContainer:_menuWidth animated:NO completion:NULL];
    }
}

- (UIBarButtonItem *)toggleSideMenuBarButtonItem
{
    if (!_toggleSideMenuBarButtonItem) {
        NSBundle *bundle = [NSBundle bundleWithURL:[[NSBundle mainBundle] URLForResource:@"Library Resources" withExtension:@"bundle"]];
        UIImage *toggleButtonImage = [UIImage imageWithContentsOfFile:[bundle pathForResource:@"dk-sidemenu-menu-icon" ofType:@"png"]];
        _toggleSideMenuBarButtonItem = [[UIBarButtonItem alloc] initWithImage:toggleButtonImage
                                                                        style:UIBarButtonItemStylePlain
                                                                       target:self
                                                                       action:@selector(handleToggleSideMenuBarButtonItemPressed:)];
    }
    return _toggleSideMenuBarButtonItem;
}

- (void)setToggleSideMenuBarButtonItem:(UIBarButtonItem *)toggleSideMenuBarButtonItem
{
    _toggleSideMenuBarButtonItem = toggleSideMenuBarButtonItem;
    _toggleSideMenuBarButtonItem.target = self;
    _toggleSideMenuBarButtonItem.action = @selector(handleToggleSideMenuBarButtonItemPressed);
}

- (DKContentContainerView *)topContentContainerView
{
    if (_containerStack.count > 0) return _containerStack.lastObject;
    return nil;
}

#pragma mark - view controller's lifecycle

- (void)viewDidLoad
{
    self.view.backgroundColor = [UIColor blackColor];
    
    // If not existent, create the menu background view
    if (!self.menuBackgroundView) {
        UIView *menuBackgroundView = [[UIView alloc] initWithFrame:CGRectMake(0, 0, self.menuWidth, self.view.bounds.size.height)];
        menuBackgroundView.autoresizingMask = UIViewAutoresizingFlexibleHeight;
        menuBackgroundView.backgroundColor = [UIColor clearColor];
        [self.view addSubview:menuBackgroundView];
        self.menuBackgroundView = menuBackgroundView;
    }
    
    // If not existent, create the content background view
    if (!self.contentBackgroundView) {
        UIView *contentBackgroundView = [[UIView alloc] initWithFrame:CGRectMake(self.menuWidth, 0, self.view.bounds.size.width - self.menuWidth, self.view.bounds.size.height)];
        contentBackgroundView.autoresizingMask = UIViewAutoresizingFlexibleHeight | UIViewAutoresizingFlexibleWidth;
        contentBackgroundView.backgroundColor = [UIColor clearColor];
        [self.view addSubview:contentBackgroundView];
        self.contentBackgroundView = contentBackgroundView;
    }
    
    // Create container for menu
    _menuContainerView = [[UIView alloc] initWithFrame:CGRectMake(0, 0, self.menuWidth, self.view.bounds.size.height)];
    _menuContainerView.autoresizingMask = UIViewAutoresizingFlexibleHeight | UIViewAutoresizingFlexibleRightMargin;
    _menuContainerView.backgroundColor = [UIColor clearColor];
    [self.view addSubview:_menuContainerView];
    
    _panRecognizer = [[UIPanGestureRecognizer alloc] initWithTarget:self action:@selector(handlePanGesture:)];
    _panRecognizer.delegate = self;
    
    if (_shouldAutomaticallySetupSideMenuController)
        [self setupSideMenuController];
}

- (void)viewWillAppear:(BOOL)animated
{
    [super viewWillAppear:animated];
    [self handleRotation:self.interfaceOrientation];
    for (UIViewController *childController in self.childViewControllers)
        [childController beginAppearanceTransition:YES animated:animated];
}

- (void)viewDidAppear:(BOOL)animated
{
    [super viewDidAppear:animated];
    for (UIViewController *childController in self.childViewControllers)
        [childController endAppearanceTransition];
}

- (void)viewWillDisappear:(BOOL)animated
{
    [super viewWillDisappear:animated];
    for (UIViewController *childController in self.childViewControllers)
        [childController beginAppearanceTransition:NO animated:animated];
}

- (void)viewDidDisappear:(BOOL)animated
{
    [super viewDidDisappear:animated];
    for (UIViewController *childController in self.childViewControllers)
        [childController endAppearanceTransition];
}

#pragma mark - rotation Handling

- (BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)toInterfaceOrientation
{
    return YES;
}

- (NSUInteger)supportedInterfaceOrientations
{
    return UIInterfaceOrientationMaskAll;
}

- (void)willAnimateRotationToInterfaceOrientation:(UIInterfaceOrientation)toInterfaceOrientation duration:(NSTimeInterval)duration
{
    [super willAnimateRotationToInterfaceOrientation:toInterfaceOrientation duration:duration];
    [self handleRotation:toInterfaceOrientation];
}

- (void)handleRotation:(UIInterfaceOrientation)orientation
{
    if (self.topContentController) {
        if (UIInterfaceOrientationIsLandscape(orientation) &&
            ![self controllerSupportsLandscape:self.topContentController]) {
            _toggleSideMenuBarButtonItem.enabled = NO;
            [self moveContentContainer:self.menuWidth animated:NO completion:NULL];
        } else
            _toggleSideMenuBarButtonItem.enabled = YES;
    }
}

#pragma mark - DKContentContainerViewDelegate implementation

- (void)backButtonPressedInContainer:(DKContentContainerView *)containerView
{
    [self popTopContentControllerAnimated:YES];
}

#pragma mark - UIGestureRecognizerDelegate implementation

- (BOOL)gestureRecognizer:(UIGestureRecognizer *)gestureRecognizer shouldRecognizeSimultaneouslyWithGestureRecognizer:(UIGestureRecognizer *)otherGestureRecognizer
{
    return NO;
}

#pragma mark - public API

- (void)prepareForSegue:(UIStoryboardSegue *)segue sender:(id)sender
{
    if ([segue.identifier isEqualToString:DKSideMenuEmbedInitialContentControllerSegueIdentifier]) {
        DKTransitionSegue *transitionSegue = (DKTransitionSegue *)segue;
        transitionSegue.animated = NO;
    } else if ([segue.identifier isEqualToString:DKSideMenuEmbedMenuControllerSegueIdentifier]) {
        self.menuController = segue.destinationViewController;
    }
}

- (BOOL)shouldAutomaticallyForwardAppearanceMethods
{
    return NO;
}

- (void)setupSideMenuController
{
    if (!self.menuController)
        [self performSegueWithIdentifier:DKSideMenuEmbedMenuControllerSegueIdentifier sender:self];
    else
        [self embedMenuViewController:self.menuController];
    
    if (!self.initialContentController) {
        @try {
            [self performSegueWithIdentifier:DKSideMenuEmbedInitialContentControllerSegueIdentifier sender:self];
        }
        @catch (NSException *exception) {
            NSLog(@"DKSideMenuViewController: No root content view controller found");
        }
    } else {
        [self pushContentController:self.initialContentController animated:NO];
        // Release a strong reference to this controller from the property
        self.initialContentController = nil;
    }
}

- (void)embedMenuViewController:(UIViewController *)menuController
{
    [self addChildViewController:menuController];
    menuController.view.frame = _menuContainerView.bounds;
    menuController.view.autoresizingMask = UIViewAutoresizingFlexibleHeight | UIViewAutoresizingFlexibleWidth;
//    [menuController beginAppearanceTransition:YES animated:NO];
    [_menuContainerView addSubview:self.menuController.view];
//    [menuController endAppearanceTransition];
    [menuController didMoveToParentViewController:self];
}

- (void)pushContentController:(UIViewController *)controller animated:(BOOL)animated
{
    // Create container for controller
    DKContentContainerView *newContentContainerView = [self addContentViewController:controller];
    // Move it offscreen
    CGRect newFrame = newContentContainerView.frame;
    newFrame.origin.x = self.view.bounds.size.width;
    newContentContainerView.frame = newFrame;
    
    // Tell old top content controller that its view is about to disappear
    if (self.topContentController)
        [self.topContentController beginAppearanceTransition:NO animated:animated];
    // Tell new content controller that its view is about to appear
    [controller beginAppearanceTransition:YES animated:animated];
    // Add the container to the hierarchy
    [self.view addSubview:newContentContainerView];
    
    // Animate the container appearance
    if (animated) {
        [UIView animateWithDuration:0.3 animations:^(void) {
            [self showNewController:controller withContainer:newContentContainerView];
        } completion:^(BOOL finished) {
            [self handleOnShowNewControllerComplete:controller withContainer:newContentContainerView];
        }];
    } else {
        [self showNewController:controller withContainer:newContentContainerView];
        [self handleOnShowNewControllerComplete:controller withContainer:newContentContainerView];
    }
}

- (void)popTopContentControllerAnimated:(BOOL)animated
{
    if (!self.topContentController) return;
    
    UIViewController *oldTopController = [_controllerStack lastObject];
    [_controllerStack removeLastObject];
    DKContentContainerView *oldTopContentContainerView = [_containerStack lastObject];
    [_containerStack removeLastObject];
    
    // Inform top content controller that it is moving out of parent view controller
    [oldTopController willMoveToParentViewController:nil];
    // Tell it that its view is going to disappear
    [oldTopController beginAppearanceTransition:NO animated:animated];
    // Detach gesture recognizer from the old top container
    [oldTopContentContainerView removeGestureRecognizer:_panRecognizer];
    
    // Add the continer just below the old top container
    if (self.topContentController) {
        CGRect contentContainerFrame = [self contentContainerFrameForController:(UIViewController *)self.topContentController];
        // If menu is shown or if it is hidden while the new top controller does not support it being shown,
        // hide it. Otherwise leave it where it is.
        contentContainerFrame.origin.x = (UIInterfaceOrientationIsLandscape(self.interfaceOrientation) &&
                                          ![self controllerSupportsLandscape:self.topContentController]) ||
                                            (self.state == DKSideMenuStateShown) ? self.menuWidth + 5 : 0;
        self.topContentContainerView.frame = contentContainerFrame;
        // Tell controller that its view is about to appear
        [self.topContentController beginAppearanceTransition:YES animated:animated];
        // Add gesture recognizer to the top container
        [self.topContentContainerView addGestureRecognizer:_panRecognizer];
        // Add new top container to the hierarchy below the old top container
        [self.view insertSubview:self.topContentContainerView belowSubview:oldTopContentContainerView];
        // Update the toggle button
        _toggleSideMenuBarButtonItem.enabled = [self shouldAllowToggling];
    }
    
    // Animate the controller's view appearance
    if (animated) {
        [UIView animateWithDuration:0.3 animations:^(void) {
            [self hideTopController:oldTopContentContainerView];
        } completion:^(BOOL finished) {
            [self handleOnHideTopControllerComplete:oldTopController withContainer:oldTopContentContainerView];
        }];
    } else {
        [self hideTopController:oldTopContentContainerView];
        [self handleOnHideTopControllerComplete:oldTopController withContainer:oldTopContentContainerView];
    }
        
}

- (void)setState:(DKSideMenuState)state animated:(BOOL)animated
{
    [self setState:state animated:animated completion:NULL];
}

- (void)setState:(DKSideMenuState)state animated:(BOOL)animated completion:(void (^)(void))completion
{
    if (state != _state)
        [self toggleSideMenuAnimated:animated completion:completion];
}

- (void)toggleSideMenuAnimated:(BOOL)animated
{
    [self toggleSideMenuAnimated:animated completion:NULL];
}

- (void)toggleSideMenuAnimated:(BOOL)animated completion:(void (^)(void))completion
{
    if ([self shouldAllowToggling]) {
        if (_state == DKSideMenuStateHidden)
            [self moveContentContainer:self.menuWidth animated:animated completion:completion];
        else
            [self moveContentContainer:0 animated:animated completion:completion];
    }
}

#pragma mark - private API

- (void)showNewController:(UIViewController *)newContentController withContainer:(DKContentContainerView *)newContentContainerView
{
    // If we are trying to present a controller which does not support lanscape
    // while being in lanscape orientation, lets make the menu shown. Otherwise
    // lets present it fullscreen
    float newX = ![self shouldAllowTogglingWithController:newContentController] ? self.menuWidth : 0;
    
    CGRect newContainerFrame = newContentContainerView.frame;
    newContainerFrame.origin.x = newX;
    newContentContainerView.frame = newContainerFrame;
    
    // Move top container to the right so it will not be visible once the new one has appeared
    if (self.topContentContainerView) {
        // If the new controller is going to be presented partscreen, move the old controller to the right
        // so it won't be visible.
        CGRect oldContainerFrame = self.topContentContainerView.frame;
        oldContainerFrame.origin.x = (newX > 0) ? self.menuWidth + 5 : oldContainerFrame.origin.x;
        self.topContentContainerView.frame = oldContainerFrame;
    }
}

- (void)handleOnShowNewControllerComplete:(UIViewController *)newContentController withContainer:(DKContentContainerView *)newContentContainerView
{
    // Delete old container from the hierarchy
    if (self.topContentController) {
        [self.topContentContainerView removeFromSuperview];
        [self.topContentController endAppearanceTransition];
    }
    
    [newContentContainerView addGestureRecognizer:_panRecognizer];
    // Lets tell controller that the view is completely shown
    [newContentController endAppearanceTransition];
    // Lets tell controller that it has moved to parent controller
    [newContentController didMoveToParentViewController:self];
    
    _state = (newContentContainerView.frame.origin.x > 0) ? DKSideMenuStateShown : DKSideMenuStateHidden;
    
    [_containerStack addObject:newContentContainerView];
    [_controllerStack addObject:newContentController];
}

- (void)hideTopController:(DKContentContainerView *)oldTopContentContainerView
{
    // Move old content container offscreen
    CGRect newOldTopContentContainerViewFrame = oldTopContentContainerView.frame;
    newOldTopContentContainerViewFrame.origin.x = self.view.bounds.size.width;
    oldTopContentContainerView.frame = newOldTopContentContainerViewFrame;
    
    // Show new top content container
    if (self.topContentController) {
        // If container is not clipped to the left border, lets
        // consider the menu shown and leave it there
        float newX = (self.topContentContainerView.frame.origin.x > 0) ? self.menuWidth : 0;
        CGRect newTopContentContainerViewFrame = self.topContentContainerView.frame;
        newTopContentContainerViewFrame.origin.x = newX;
        self.topContentContainerView.frame = newTopContentContainerViewFrame;
    }
}

- (void)handleOnHideTopControllerComplete:(UIViewController *)oldTopController withContainer:(DKContentContainerView *)oldTopContentContainerView
{
    // Remove old top content container from the hierarchy
    [oldTopContentContainerView removeFromSuperview];
    // Tell controller that the dismiss animation is completed
    // and the view is no longer in the hierarchy
    [oldTopController endAppearanceTransition];
    // Remove controller from the parent controller
    [self removeContentViewController:oldTopController withContainer:oldTopContentContainerView];
    
    if (self.topContentController)
        [self.topContentController endAppearanceTransition];
    
    // If controller stack is empty or container is not clipped
    // to the left border, consider the menu shown
    _state = (!self.topContentContainerView || self.topContentContainerView.frame.origin.x > 0) ? DKSideMenuStateShown : DKSideMenuStateHidden;
}

- (void)removeAllContentViewControllers
{
    // Top content controller is visible at the moment and to remove it
    // approprietly we have to ensure that viewWillDisappear and viewDidDisappear
    // are called. Other controllers' views are not in the hierarchy so they can be simply removed.
    if (self.topContentController) {
        // Lets tell the top controller that it is going to be removed
        // from the parent view controller
        [self.topContentController willMoveToParentViewController:nil];
        // Lets tell the top controller that its view is going offscreen
        [self.topContentController beginAppearanceTransition:NO animated:NO];
        // Lets detach the gesture recognizer
        [self.topContentContainerView removeGestureRecognizer:_panRecognizer];
        // Lets remove the container view from the hierarchy
        [self.topContentContainerView removeFromSuperview];
        // Lets tell the controller that the appearance transition is over
        [self.topContentController endAppearanceTransition];
        // Lets remove the top content controller from the parent view controller
        [self.topContentController removeFromParentViewController];
        
        [_controllerStack removeLastObject];
        [_containerStack removeLastObject];
    }
    
    for (int i = 0; i < _controllerStack.count; i++) {
        // Tell controller that it is going to be removed from the parent controller
        [_controllerStack [i] willMoveToParentViewController:nil];
        // Remove the controller from the parent controller and deinit container
        [self removeContentViewController:_controllerStack[i] withContainer:_containerStack[i]];
    }
    
    [_controllerStack removeAllObjects];
    [_containerStack removeAllObjects];
    
    _state = DKSideMenuStateShown;
}

- (DKContentContainerView *)addContentViewController:(UIViewController *)contentViewController
{
    CGRect frame = [self contentContainerFrameForController:contentViewController];
    
    // Create container and setup the navigation bar
    DKContentContainerView *newContentContainerView = [[DKContentContainerView alloc] initWithFrame:frame];
    if ([self controllerSupportsLandscape:contentViewController])
        newContentContainerView.autoresizingMask = UIViewAutoresizingFlexibleHeight | UIViewAutoresizingFlexibleWidth;
    newContentContainerView.dropShadow = self.dropShadow;
    newContentContainerView.navigationBar.barStyle = self.navigationBarStyle;
    
    UINavigationItem *navItem = contentViewController.navigationItem;
    if (!navItem)
        navItem = [[UINavigationItem alloc] init];
    if (_controllerStack.count > 0) {
        [newContentContainerView.navigationBar pushNavigationItem:[[UINavigationItem alloc] init] animated:NO];
        newContentContainerView.delegate = self;
    } else {
        navItem.leftBarButtonItem = self.toggleSideMenuBarButtonItem;
        self.toggleSideMenuBarButtonItem.enabled = [self shouldAllowTogglingWithController:contentViewController];
    }
    [newContentContainerView.navigationBar pushNavigationItem:navItem animated:NO];
    
    // Add controller to the parent view controller
    [self addChildViewController:contentViewController];
    contentViewController.view.frame = newContentContainerView.contentView.bounds;
    contentViewController.view.autoresizingMask = UIViewAutoresizingFlexibleWidth | UIViewAutoresizingFlexibleHeight;
    // Add content controller's view to the container
    [newContentContainerView.contentView addSubview:contentViewController.view];
    
    return newContentContainerView;
}

- (void)removeContentViewController:(UIViewController *)contentViewController withContainer:(DKContentContainerView *)contentContainerView
{
    // Remove controller's view from the container (maybe not needed)
    [contentViewController.view removeFromSuperview];
    // Remove controller from the parent view controller
    [contentViewController removeFromParentViewController];
    
    // Nothing is required to do with the contentContainerView at the moment
}

- (CGRect)contentContainerFrameForController:(UIViewController *)controller
{
    // If controller supports landscape while and we are in landscape, return wide frame
    BOOL isLandscapeFullscreen = UIInterfaceOrientationIsLandscape(self.interfaceOrientation) && [self controllerSupportsLandscape:controller];
    if (isLandscapeFullscreen)
        return CGRectMake(0, 0, self.view.bounds.size.width, self.view.bounds.size.height);
    else
        return CGRectMake(0, 0, [UIScreen mainScreen].bounds.size.width, self.view.bounds.size.height);
}

- (BOOL)controllerSupportsLandscape:(UIViewController *)controller
{
    return ([controller shouldAutorotateToInterfaceOrientation:UIInterfaceOrientationLandscapeLeft] &&
            [controller shouldAutorotateToInterfaceOrientation:UIInterfaceOrientationLandscapeRight]) ||
            (controller.supportedInterfaceOrientations & UIInterfaceOrientationMaskLandscape) != 0;
}

- (BOOL)shouldAllowToggling
{
    return [self shouldAllowTogglingWithController:self.topContentController];
}

- (BOOL)shouldAllowTogglingWithOrientation:(UIInterfaceOrientation)orientation
{
    return [self shouldAllowTogglingWithController:self.topContentController withOrientation:orientation];
}

- (BOOL)shouldAllowTogglingWithController:(UIViewController *)contentController
{
    return [self shouldAllowTogglingWithController:contentController withOrientation:self.interfaceOrientation];
}

- (BOOL)shouldAllowTogglingWithController:(UIViewController *)contentController withOrientation:(UIInterfaceOrientation)orientation
{
    return !UIInterfaceOrientationIsLandscape(orientation) || [self controllerSupportsLandscape:contentController];
}

- (void)handleToggleSideMenuBarButtonItemPressed:(UIBarButtonItem *)sender
{
    [self toggleSideMenuAnimated:YES];
}

- (void)moveContentContainer:(float)destionationX animated:(BOOL)animated completion:(void (^)(void))completion
{
    float velocity = animated ? self.standardVelocity : 0;
    [self moveContentContainer:destionationX velocity:velocity completion:completion];
}

- (void)moveContentContainer:(float)destionationX velocity:(float)velocity completion:(void (^)(void))completion
{
    if (!self.topContentController) return;
    
    double duration = (velocity > 0) ? (float)abs(self.topContentContainerView.frame.origin.x - destionationX) / velocity : 0;
    [self moveContentContainer:destionationX duration:duration completion:completion];
}

- (void)moveContentContainer:(float)destionationX duration:(double)duration completion:(void (^)(void))completion
{
    if (!self.topContentController) return;
    
    [UIView animateWithDuration:duration animations:^(void) {
        CGRect newFrame = self.topContentContainerView.frame;
        newFrame.origin.x = destionationX;
        self.topContentContainerView.frame = newFrame;
    } completion:^(BOOL finished) {
        if (completion) completion ();
    }];
    
    if (destionationX > 0)
        _state = DKSideMenuStateShown;
    else
        _state = DKSideMenuStateHidden;
}

- (void)handlePanGesture:(UIPanGestureRecognizer *)panGestureRecognizer
{
    if (panGestureRecognizer.state == UIGestureRecognizerStateBegan ||
        panGestureRecognizer.state == UIGestureRecognizerStateChanged) {
        // Gesture has begun or has changed, lets move the content container
        CGPoint translation = [panGestureRecognizer translationInView:self.topContentContainerView];
        
        CGRect frame = self.topContentContainerView.frame;
        float translationX = translation.x;
        // If the content container has moved out of borders or it is not allowed
        // to move, divide the translation by 10
        if (frame.origin.x < 0 || frame.origin.x > self.menuWidth || ![self shouldAllowToggling])
            translationX /= 10;
        frame.origin.x += translationX;
        self.topContentContainerView.frame = frame;
        // Reset translation
        [panGestureRecognizer setTranslation:CGPointZero inView:self.topContentContainerView];
    } else if (panGestureRecognizer.state == UIGestureRecognizerStateEnded) {
        CGPoint velocity = [panGestureRecognizer velocityInView:self.topContentContainerView];
        
        // First case: movement is not allowed.
        // Return to the original position with animation,
        // using returnToBoundsAnimationDuration setting to define
        // the animation duration
        if (![self shouldAllowToggling]) {
            if (self.state == DKSideMenuStateHidden)
                [self moveContentContainer:0 duration:self.returnToBoundsAnimationDuration completion:NULL];
            else
                [self moveContentContainer:self.menuWidth duration:self.returnToBoundsAnimationDuration completion:NULL];
            return;
        }
        
        // Second case: the panel has moved out of the screen's bounds,
        // while velocity is directed towards the panel position.
        // Return to the nearest fixed position w/animation, using
        // returnToBoundsAnimationDuration setting
        if (self.topContentContainerView.frame.origin.x < 0 && velocity.x < 0) {
            [self moveContentContainer:0 duration:self.returnToBoundsAnimationDuration completion:NULL];
            return;
        }
        if (self.topContentContainerView.frame.origin.x > self.menuWidth && velocity.y > 0) {
            [self moveContentContainer:self.menuWidth duration:self.returnToBoundsAnimationDuration completion:NULL];
            return;
        }
        
        // Third case: user has movd his finger with the velocity big enough
        // for the panel to conitue moving in the finger's direction. Movement velocity
        // is the same as the finger's but it can't be smaller than minVelocity or bigger than
        // maxVelocity settings
        if (abs (velocity.x) > self.thresholdVelocity) {
            float velocityX = abs (velocity.x);

            velocityX = MAX(self.minVelocity, velocityX);
            velocityX = MIN(self.maxVelocity, velocityX);
            
            if (velocity.x > 0)
                [self moveContentContainer:self.menuWidth velocity:velocityX completion:NULL];
            else
                [self moveContentContainer:0 velocity:velocityX completion:NULL];
            return;
        }
        
        // Fourth case: user has lifted his finger with velocity not big enough.
        // Determine the nearest position (according to the thresholdX setting) and
        // move the panel animated. The animation duration is defined by the setting
        // returnToBoundsAnimationDuration;
        if (self.topContentContainerView.frame.origin.x < self.thresholdX)
            [self moveContentContainer:0 duration:self.returnToBoundsAnimationDuration completion:NULL];
        else
            [self moveContentContainer:self.menuWidth duration:self.returnToBoundsAnimationDuration completion:NULL];            
    }
}

@end
