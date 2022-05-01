using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Controls.Primitives;

namespace Ws2812RealtimeDesktopClient.Controls;

public sealed class CustomFlyout<T> : PickerFlyoutBase
{
    public T Picker => _picker;

    public event TypedEventHandler<CustomFlyout<T>, object>? Confirmed;
    public event TypedEventHandler<CustomFlyout<T>, object>? Dismissed;

    public CustomFlyout(T picker)
    {
        _picker = picker;
    }

    protected override Control CreatePresenter()
    {
        var pfp = new PickerFlyoutPresenter()
        {
            Content = _picker
        };
        pfp.Confirmed += OnFlyoutConfirmed;
        pfp.Dismissed += OnFlyoutDismissed;

        return pfp;
    }
    
    protected override void OnOpening(CancelEventArgs args)
    {
        base.OnOpening(args);
        (Popup.Child as PickerFlyoutPresenter)?.ShowHideButtons(ShouldShowConfirmationButtons());
    }

    protected override void OnConfirmed()
    {
        Confirmed?.Invoke(this, EventArgs.Empty);
        Hide();
    }
    
    private void OnFlyoutDismissed(PickerFlyoutPresenter sender, object args)
    {
        Dismissed?.Invoke(this, EventArgs.Empty);
        Hide();
    }

    private void OnFlyoutConfirmed(PickerFlyoutPresenter sender, object args)
    {
        OnConfirmed();
    }

    private T _picker;

    protected override bool ShouldShowConfirmationButtons() => false;
}