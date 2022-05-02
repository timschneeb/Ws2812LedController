using FluentAvalonia.UI.Controls;
using Ws2812RealtimeDesktopClient.Pages;
using Ws2812RealtimeDesktopClient.Services;

namespace Ws2812RealtimeDesktopClient.Utilities;

public static class DialogUtils
{
    public static async Task ShowNoSegmentsDialog(string instruction = "Please create a new segment before assigning an effect.")
    {
        var noSegmentsDialog = new ContentDialog()
        {
            Content = $"{instruction} Press 'Go to...' to navigate to the segment management page.",
            Title = "No segments defined",
            PrimaryButtonText = "Go to...",
            CloseButtonText = "Cancel"
        };
        noSegmentsDialog.PrimaryButtonClick += (_, _) => NavigationService.Instance.Navigate(typeof(SegmentPage));
        await noSegmentsDialog.ShowAsync();
    }  
    
    public static async Task ShowMessageDialog(string title, string content, string closeButtonText = "Close")
    {
        await new ContentDialog()
        {
            Content = content,
            Title = title,
            CloseButtonText = closeButtonText
        }.ShowAsync();
    }  
    
    public static async Task<bool> ShowYesNoMessageDialog(string title, string content, string acceptButtonText = "Okay", string dismissButtonText = "Cancel")
    {
        return await new ContentDialog()
        {
            Content = content,
            Title = title,
            PrimaryButtonText = acceptButtonText,
            CloseButtonText = dismissButtonText
        }.ShowAsync() == ContentDialogResult.Primary;
    }
}