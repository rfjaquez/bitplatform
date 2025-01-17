﻿namespace Bit.BlazorUI.Demo.Client.Shared.Pages.Components.SpinButton;

public class BitSpinButtonValidationModel
{
    [Required(ErrorMessage = "Enter an age")]
    [Range(1, 150, ErrorMessage = "Nobody is that old")]
    public double AgeInYears { get; set; }
}
