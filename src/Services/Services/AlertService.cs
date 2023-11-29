namespace Turbo.Maui.Services;

public interface IAlertService
{
    Task ShowAlertAsync(string title, string message, string cancel = "OK");
    Task<bool> ShowConfirmationAsync(string title, string message, string accept = "Yes", string cancel = "No");
    Task<string> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel", string placeholder = null, int maxLength = -1, Keyboard keyboard = null, string initialValue = "");
    void ShowAlert(string title, string message, string cancel = "OK");
    /// <param name="callback">Action to perform afterwards.</param>
    void ShowConfirmation(string title, string message, Action<bool> callback, string accept = "Yes", string cancel = "No");
}

public class AlertService : IAlertService
{
    public async Task ShowAlertAsync(string title, string message, string cancel = "OK") => await Application.Current.MainPage.DisplayAlert(title, message, cancel);

    public async Task<bool> ShowConfirmationAsync(string title, string message, string accept = "Yes", string cancel = "No") => await Application.Current.MainPage.DisplayAlert(title, message, accept, cancel);

    public async Task<string> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel", string placeholder = null, int maxLength = -1, Keyboard keyboard = null, string initialValue = "")
    {
        return await Application.Current.MainPage.DisplayPromptAsync(title, message, accept, cancel, placeholder, maxLength, keyboard, initialValue);
    }

    /// <summary>
    /// "Fire and forget". Method returns BEFORE showing alert.
    /// </summary>
    public void ShowAlert(string title, string message, string cancel = "OK") => Application.Current.MainPage.Dispatcher.Dispatch(async () => await ShowAlertAsync(title, message, cancel));

    /// <summary>
    /// "Fire and forget". Method returns BEFORE showing alert.
    /// </summary>
    /// <param name="callback">Action to perform afterwards.</param>
    public void ShowConfirmation(string title, string message, Action<bool> callback, string accept = "Yes", string cancel = "No")
    {
        Application.Current.MainPage.Dispatcher.Dispatch(async () =>
        {
            bool answer = await ShowConfirmationAsync(title, message, accept, cancel);
            callback(answer);
        });
    }
}