﻿using System.Windows.Controls;
using PicView.ChangeImage;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls.Buttons
{
    public partial class ReloadButton : UserControl
    {
        public ReloadButton()
        {
            InitializeComponent();

            Loaded += delegate
            {
                SetIconButterMouseOverAnimations(TheButton, ButtonBrush, IconBrush);

                TheButton.Click += async (_, _) => await ErrorHandling.ReloadAsync().ConfigureAwait(false);
            };
        }
    }
}