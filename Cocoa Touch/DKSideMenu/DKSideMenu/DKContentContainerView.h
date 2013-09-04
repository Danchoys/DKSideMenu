//
//  DKContentContainerVIew.h
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

@class DKContentContainerView;

@protocol DKContentContainerViewDelegate <NSObject>

- (void)backButtonPressedInContainer:(DKContentContainerView *)containerView;

@end

@interface DKContentContainerView : UIView

@property (nonatomic, weak) id<DKContentContainerViewDelegate> delegate;

@property (nonatomic) BOOL dropShadow;

@property (nonatomic, weak) UINavigationBar *navigationBar;

@property (nonatomic, weak) UIView *contentView;

- (void)setNavigationBarHidden:(BOOL)hidden animated:(BOOL)animated;

@end

