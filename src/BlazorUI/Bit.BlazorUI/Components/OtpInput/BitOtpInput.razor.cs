﻿using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitOtpInput
{
    protected override bool UseVisual => false;

    private ElementReference[] _inputRefs = default!;
    private string?[] _inputValue = default!;
    private string _inputType = default!;
    private string _inputMode = default!;

    [Inject] private IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// If true, the first input is focused.
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// The OtpInput direction in four available directions.
    /// </summary>
    [Parameter] public BitOtpInputDirection Direction { get; set; } = BitOtpInputDirection.LeftToRight;

    /// <summary>
    /// Type of input shown as text, number, and password.
    /// </summary>
    [Parameter] public BitOtpInputType InputType { get; set; } = BitOtpInputType.Text;

    /// <summary>
    /// Length of input in Otp.
    /// </summary>
    [Parameter] public int Length { get; set; }

    /// <summary>
    /// Callback for when OtpInput value changed.
    /// </summary>
    [Parameter] public EventCallback<ChangeEventArgs> OnInput { get; set; }

    /// <summary>
    /// Callback for when a keyboard key is pressed.
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    /// <summary>
    /// Callback for when OtpInput is focused in.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusIn { get; set; }

    /// <summary>
    /// Callback for when OtpInput is focused out.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusOut { get; set; }

    /// <summary>
    /// Callback for when in the OtpInput paste a content.
    /// </summary>
    [Parameter] public EventCallback<ClipboardEventArgs> OnPaste { get; set; }

    /// <summary>
    /// Callback for when the OtpInput value change.
    /// </summary>
    [Parameter] public EventCallback<string?> OnChange { get; set; }


    protected override string RootElementClass => "bit-otp";

    protected override async Task OnInitializedAsync()
    {
        _inputRefs = new ElementReference[Length];

        _inputValue = new string[Length];

        _inputType = InputType switch
        {
            BitOtpInputType.Text => "text",
            BitOtpInputType.Number => "number",
            BitOtpInputType.Password => "password",
            _ => string.Empty
        };

        _inputMode = InputType switch
        {
            BitOtpInputType.Text => "text",
            BitOtpInputType.Number => "numeric",
            BitOtpInputType.Password => "text",
            _ => string.Empty
        };

        OnValueChanging += HandleOnValueChanging;

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (CurrentValue != null && CurrentValue != string.Join("", _inputValue))
        {
            SetInputValue(CurrentValue);
        }

        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false || IsEnabled is false) return;

        if (AutoFocus)
        {
            await _inputRefs[0].FocusAsync();
        }

        foreach (var inputRef in _inputRefs)
        {
            var obj = DotNetObjectReference.Create(this);
            await _js.SetupOtpInput(obj, inputRef);
        }
    }

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => Direction switch
        {
            BitOtpInputDirection.LeftToRight => "left-to-right",
            BitOtpInputDirection.RightToLeft => "right-to-left",
            BitOtpInputDirection.TopToBottom => "top-to-bottom",
            BitOtpInputDirection.BottomToTop => "bottom-to-top",
            _ => string.Empty
        });
    }

    private void HandleOnValueChanging(object? sender, ValueChangingEventArgs<string?> args)
    {
        if (args.Value.HasNoValue()
            || args.Value!.Length <= Length && (InputType is not BitOtpInputType.Number || int.TryParse(args.Value, out _))) return;

        args.ShouldChange = false;

        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;
        if (Value == args.Value) return;

        _ = ValueChanged.InvokeAsync(Value);
    }

    private async Task HandleOnInput(ChangeEventArgs e, int index)
    {
        var oldValue = _inputValue[index];
        var newValue = e.Value!.ToString()!;

        _inputValue[index] = string.Empty;
        await Task.Delay(1); // waiting for input default behavior before setting a new value.

        if (IsEnabled is false || (ValueHasBeenSet && ValueChanged.HasDelegate is false))
        {
            _inputValue[index] = oldValue;
        }
        else if (newValue.HasValue())
        {
            var diff = BitOtpInput.DiffValues(oldValue ?? string.Empty, newValue);

            if (InputType is BitOtpInputType.Number && int.TryParse(diff, out _) is false)
            {
                _inputValue[index] = oldValue;
            }
            else
            {
                _inputValue[index] = diff;
                int nextIndex = index + 1;
                if (nextIndex < Length) await _inputRefs[nextIndex].FocusAsync();
            }
        }
        else
        {
            _inputValue[index] = null;
        }

        CurrentValue = string.Join("", _inputValue);

        await OnInput.InvokeAsync(e);
        await OnChange.InvokeAsync(CurrentValue);
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e, int index)
    {
        if (IsEnabled is false || e.Code is null) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        await NavigateInput(e.Code, e.Key, index);

        await OnKeyDown.InvokeAsync(e);
    }

    private async Task NavigateInput(string code, string key, int index)
    {
        int nextIndex = index + 1;
        int previousIndex = index - 1;

        if ((code is "Backspace" || key is "Backspace") && previousIndex >= 0)
        {
            await Task.Delay(1);
            await _inputRefs[previousIndex].FocusAsync();
        }
        else if (code is "ArrowLeft")
        {
            if (Direction is BitOtpInputDirection.LeftToRight && previousIndex >= 0)
            {
                await _inputRefs[previousIndex].FocusAsync();
            }
            else if (Direction is BitOtpInputDirection.RightToLeft && nextIndex < Length)
            {
                await _inputRefs[nextIndex].FocusAsync();
            }
        }
        else if (code is "ArrowRight")
        {
            if (Direction is BitOtpInputDirection.LeftToRight && nextIndex < Length)
            {
                await _inputRefs[nextIndex].FocusAsync();
            }
            else if (Direction is BitOtpInputDirection.RightToLeft && previousIndex >= 0)
            {
                await _inputRefs[previousIndex].FocusAsync();
            }
        }
        else if (code is "ArrowUp")
        {
            if (Direction is BitOtpInputDirection.TopToBottom && previousIndex >= 0)
            {
                await _inputRefs[previousIndex].FocusAsync();
            }
            else if (Direction is BitOtpInputDirection.BottomToTop && nextIndex < Length)
            {
                await _inputRefs[nextIndex].FocusAsync();
            }
        }
        else if (code is "ArrowDown")
        {
            if (Direction is BitOtpInputDirection.TopToBottom && nextIndex < Length)
            {
                await _inputRefs[nextIndex].FocusAsync();
            }
            else if (Direction is BitOtpInputDirection.BottomToTop && previousIndex >= 0)
            {
                await _inputRefs[previousIndex].FocusAsync();
            }
        }
    }

    private async Task HandleOnFocusIn(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnFocusIn.InvokeAsync(e);
    }

    private async Task HandleOnFocusOut(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnFocusOut.InvokeAsync(e);
    }

    private async Task HandleOnPaste(ClipboardEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnPaste.InvokeAsync(e);
    }

    [JSInvokable]
    public async Task SetPastedData(string pastedValue)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;
        if (pastedValue.HasNoValue()) return;
        if (InputType is BitOtpInputType.Number && int.TryParse(pastedValue, out _) is false) return;

        SetInputValue(pastedValue);

        CurrentValue = string.Join("", _inputValue);

        await OnChange.InvokeAsync(CurrentValue);
    }

    private void SetInputValue(string value)
    {
        var chars = value.Replace(" ", "", StringComparison.Ordinal).ToCharArray();

        for (int i = 0; i < Length; i++)
        {
            _inputValue[i] = chars.Length > i ? chars[i].ToString() : null;
        }
    }

    private static string DiffValues(string oldValue, string newValue)
    {
        if (newValue.Length == 1) return newValue;
        if (newValue.Length < oldValue.Length) return newValue;

        if (newValue[..^1] == oldValue) return newValue[^1].ToString();
        if (newValue[1..] == oldValue) return newValue[0].ToString();

        return newValue;
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out string? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value;
        validationErrorMessage = null;
        return true;
    }
}
