//
//  DKTransitionSegue.m
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

#import "DKTransitionSegue.h"
#import "UIViewController+DKSideMenu.h"

@implementation DKTransitionSegue

- (id)initWithIdentifier:(NSString *)identifier source:(UIViewController *)source destination:(UIViewController *)destination
{
    self = [super initWithIdentifier:identifier source:source destination:destination];
    if (self) {
        self.animated = YES;
    }
    return self;
}

- (void)perform
{
    DKSideMenuViewController *sideMenuController = [self.sourceViewController sideMenu];
    if (!sideMenuController)
        NSLog(@"DKTransitionSegue can only be used with DKSideMenuController");
    [sideMenuController pushContentController:self.destinationViewController animated:self.animated];
}

@end
