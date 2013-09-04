//
//  DKContentContainerVIew.m
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

#import "DKContentContainerView.h"
#import <QuartzCore/QuartzCore.h>

@implementation DKContentContainerView

- (id)initWithFrame:(CGRect)frame
{
    self = [super initWithFrame:frame];
    if (self) {        
        // Configure the view
        self.autoresizingMask = UIViewAutoresizingFlexibleHeight;
        self.backgroundColor = [UIColor clearColor];
        
        // Configure the nav bar
        UINavigationBar *navBar = [[UINavigationBar alloc] initWithFrame:CGRectMake(0, 0, frame.size.width, 44)];
        navBar.autoresizingMask = UIViewAutoresizingFlexibleWidth;
        navBar.delegate = self;
        [self addSubview:navBar];
        self.navigationBar = navBar;
        
        // Create the content area
        UIView *contentView = [[UIView alloc] initWithFrame:CGRectMake(0, 44, frame.size.width, frame.size.height - 44)];
        contentView.backgroundColor = [UIColor clearColor];
        contentView.autoresizingMask = UIViewAutoresizingFlexibleWidth | UIViewAutoresizingFlexibleHeight;
        contentView.clipsToBounds = YES;
        [self addSubview:contentView];
        self.contentView = contentView;
        
        // Configure the shadow
        self.layer.masksToBounds = NO;
        self.layer.shadowRadius = 3;
        self.layer.shadowOpacity = 1.0;
        self.layer.shadowColor = [UIColor blackColor].CGColor;
    }
    return self;
}

#pragma mark - props

- (BOOL)dropShadow
{
    return !self.layer.masksToBounds;
}

- (void)setDropShadow:(BOOL)dropShadow
{
    self.layer.masksToBounds = !dropShadow;
}

#pragma mark - UINavigationBarDelegate

- (BOOL)navigationBar:(UINavigationBar *)navigationBar shouldPopItem:(UINavigationItem *)item
{
    [self.delegate backButtonPressedInContainer:self];
    return NO;
}

#pragma mark - public API

- (void)layoutSubviews
{
    self.layer.shadowPath = [UIBezierPath bezierPathWithRect:CGRectMake(-2, 0, self.frame.size.width + 4, self.frame.size.height + 6)].CGPath;
}

- (void)setNavigationBarHidden:(BOOL)hidden animated:(BOOL)animated
{
    float newY = hidden ? -self.navigationBar.bounds.size.height : 0;
    [UIView animateWithDuration:animated ? 0.25 : 0 animations:^(void){
        CGRect navBarFrame = self.navigationBar.frame;
        navBarFrame.origin.y = newY;
        self.navigationBar.frame = navBarFrame;
        
        CGRect contentViewFrame = self.contentView.frame;
        contentViewFrame.origin.y = self.navigationBar.frame.origin.y + self.navigationBar.frame.size.height;
        contentViewFrame.size.height = self.bounds.size.height - (self.navigationBar.frame.origin.y + self.navigationBar.frame.size.height);
        self.contentView.frame = contentViewFrame;
    }];
}



@end
