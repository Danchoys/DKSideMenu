//
//  UIVewController+DKSideMenu.m
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

#import "UIViewController+DKSideMenu.h"

@implementation UIViewController (DKSideMenu)

- (DKSideMenuViewController *)sideMenu
{
    if ([self.class isSubclassOfClass:[DKSideMenuViewController class]])
        return (DKSideMenuViewController *)self;
    
    UIViewController *parentViewController = self.parentViewController;
    while (parentViewController && ![parentViewController.class isSubclassOfClass:[DKSideMenuViewController class]])
        parentViewController = parentViewController.parentViewController;
    
    return (DKSideMenuViewController *)parentViewController;
}

- (DKContentContainerView *)contentContainer
{
    if ([self.view.class isSubclassOfClass:[DKContentContainerView class]])
        return (DKContentContainerView *)self.view;
    
    UIView *parentView = self.view.superview;
    while (parentView && ![parentView.class isSubclassOfClass:[DKContentContainerView class]])
        parentView = parentView.superview;
    
    return (DKContentContainerView *)parentView;
}

@end
