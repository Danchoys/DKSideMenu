//
//  DKSideMenu.h
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

#import <UIKit/UIKit.h>

/// Use this keys to set the segue identifiers for embedding menu and content controllers
extern NSString *const DKSideMenuEmbedMenuControllerSegueIdentifier;
extern NSString *const DKSideMenuEmbedInitialContentControllerSegueIdentifier;

typedef enum {
    DKSideMenuStateShown,
    DKSideMenuStateHidden
} DKSideMenuState;

@interface DKSideMenuViewController : UIViewController {
@protected
    BOOL _shouldAutomaticallySetupSideMenuController;
}

/// Gets or sets the menu background view.
@property (nonatomic, weak) IBOutlet UIView *menuBackgroundView;

/// Gets or sets the content background view.
@property (nonatomic, weak) IBOutlet UIView *contentBackgroundView;

/// Gets or sets the menu controller. It used as the menu controller
/// when SetupSideMenuController is called. If menu is embeded via a segue
/// this property is set automatically.
@property (nonatomic, strong) UIViewController *menuController;

/// Gets or sets the initial content controller. It is used
/// as the first content controller when setupSideMenuController is called.
/// Inside setupSideMenuController this property is set to nil.
@property (nonatomic, strong) UIViewController *initialContentController;

/// Gets or sets the root content controller.
@property (nonatomic) UIViewController *rootContentController;

/// Gets the top content view controller.
@property (nonatomic, readonly) UIViewController *topContentController;

/// Gets or sets the menu state.
@property (nonatomic) DKSideMenuState state;

/// Gets or sets the threshold x. When user lifts his finger with velocity smaller than ThresholdVelocity
/// this parameter is used to determine whether the menu is shown (container's X is greater than the threshold)
/// or hidden (container's X is smaller than the threshold). Default is 128.
@property (nonatomic) float thresholdX;

/// Gets or sets the threshold velocity. When user lifts his finger, the container will continue
/// to move in the orinal direction if the speed is greater or equal than this parameter.
/// Default is 800.
@property (nonatomic) float thresholdVelocity;

/// Gets or sets the minimum velocity. This is the lowest speed at which the panel will continue moving
/// after the user lifts his finger. Default is 800.
@property (nonatomic) float minVelocity;

/// Gets or sets the max velocity. This is the highest speed at which the panel will continue moving
/// after the user lifts his finger. Default is 800.
@property (nonatomic) float maxVelocity;

/// Gets or sets the standard velocity. This is the speed at which the panel will move if
/// the user lifts his finger with velocity less than ThresholdVelocity. Default is 1000.
@property (nonatomic) float standardVelocity;

/// Gets or sets the duration of the return to bounds animation. This value is used when the user
/// drags the panel out of bounds (i.e. partly offscreen). Default is 0.25.
@property (nonatomic) float returnToBoundsAnimationDuration;

/// Gets or sets a value indicating whether content containers should drop shadow. Default is true.
@property (nonatomic) BOOL dropShadow;

/// Gets or sets a value indicating whether the pan gesture is enabled. Default is true.
@property (nonatomic) BOOL panGestureEnabled;

/// Gets or sets the navigation bar style of all content containers. Default is UIBarStyleDefault.
@property (nonatomic) UIBarStyle navigationBarStyle;

/// Gets or sets the width of the menu. Default is 256.
@property (nonatomic) float menuWidth;

/// Instatiates a DKSideMenuController, setting its menuController and initialContentController properties.
- (id)initWithMenuController:(UIViewController *)menuController initialContentController:(UIViewController *)initialContentController;

/// Setups the side menu using segues or menuController and initialContentController properties.
/// Properties have the priority.
- (void)setupSideMenuController;

/// Pushes the view controller.
- (void)pushContentController:(UIViewController *)controller animated:(BOOL)animated;

/// Pops the top content controller.
- (void)popTopContentControllerAnimated:(BOOL)animated;

/// Embeds menu view controller. Called automatically from the DKEmbedMenuSegue
/// or from the setupSideMenuController method
- (void)embedMenuViewController:(UIViewController *)menuController;

/// Sets the state.
- (void)setState:(DKSideMenuState)state animated:(BOOL)animated;

/// Sets the state.
- (void)setState:(DKSideMenuState)state animated:(BOOL)animated completion:(void (^)(void))completion;

/// Toggles the side menu.
- (void)toggleSideMenuAnimated:(BOOL)animated;

/// Toggles the side menu.
- (void)toggleSideMenuAnimated:(BOOL)animated completion:(void (^)(void))completion;


@end
