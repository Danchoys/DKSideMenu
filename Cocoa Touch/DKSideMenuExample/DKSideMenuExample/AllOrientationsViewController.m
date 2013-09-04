//
//  AllOrientationsViewController.m
//  DKSideMenuExample
//
//  Created by Daniil Konoplev on 30.08.13.
//  Copyright (c) 2013 Danchoys. All rights reserved.
//

#import "AllOrientationsViewController.h"

@interface AllOrientationsViewController ()

@end

@implementation AllOrientationsViewController

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
    return UIInterfaceOrientationMaskAll;
}

@end
