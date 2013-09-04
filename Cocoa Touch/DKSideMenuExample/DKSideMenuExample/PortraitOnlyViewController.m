//
//  PortraitOnlyViewController.m
//  DKSideMenuExample
//
//  Created by Daniil Konoplev on 30.08.13.
//  Copyright (c) 2013 Danchoys. All rights reserved.
//

#import "PortraitOnlyViewController.h"

@interface PortraitOnlyViewController ()

@end

@implementation PortraitOnlyViewController

- (id)initWithNibName:(NSString *)nibNameOrNil bundle:(NSBundle *)nibBundleOrNil
{
    self = [super initWithNibName:nibNameOrNil bundle:nibBundleOrNil];
    if (self) {
        // Custom initialization
    }
    return self;
}

- (void)viewDidLoad
{
    [super viewDidLoad];
	// Do any additional setup after loading the view.
}

- (void)didReceiveMemoryWarning
{
    [super didReceiveMemoryWarning];
    // Dispose of any resources that can be recreated.
}

- (NSUInteger)supportedInterfaceOrientations
{
    return UIInterfaceOrientationMaskPortrait | UIInterfaceOrientationMaskPortraitUpsideDown;
}

@end
