using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Windows.Foundation;
using static System.Net.Mime.MediaTypeNames;

namespace ITunesShortcuts.Services;

public class Notifications
{
    public const string Action = "action";
    public const string Group = "group";
    public const string Content = "content";
    public const string ComboBoxToPage = "comboBoxToPage";

    public const string ViewAction = "view";
    public const string ButtonAction = "button";
    public const string ComboBoxAction = "comboBox";
    public const string ComboBoxToPageAction = "comboBoxToPage";

    public const int ItemsPerComboBoxPage = 5;


    readonly ILogger<Notifications> logger;
    readonly WindowHelper windowHelper;

    readonly AppNotificationManager notificationManager = AppNotificationManager.Default;
    readonly Dictionary<string, Action<string>> groupActionMapping = new();
    readonly Dictionary<string, string[]> groupItemsMapping = new();
    readonly Dictionary<string, (string text, string? appLogo)> groupNotificationInfoMapping = new();

    public Notifications(
        ILogger<Notifications> logger,
        WindowHelper windowHelper)
    {
        this.logger = logger;
        this.windowHelper = windowHelper;

        logger.LogInformation("[Notifications-.ctor] Notifications has been initialized.");
    }

    
    public void Register()
    {
        notificationManager.NotificationInvoked += (s, args) => Handle(args);
        notificationManager.Register();

        AppActivationArguments activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        if (activatedArgs.Kind == ExtendedActivationKind.AppNotification)
            Handle((AppNotificationActivatedEventArgs)activatedArgs.Data);

        logger.LogInformation("[Notifications-Register] Notifications has been initialized.");

    }

    public void Unregister()
    {
        notificationManager.UnregisterAll();
        groupActionMapping.Clear();
    }

    public IAsyncAction ClearAsync() =>
        notificationManager.RemoveAllAsync();


    public void Handle(
        AppNotificationActivatedEventArgs args) =>
        windowHelper.EnqueDispatcher(() =>
        {
            if (!args.Arguments.TryGetValue(Action, out string? actionType) || actionType is null)
            {
                logger.LogError("[Notifications-Handle] Notification could not be handled: action key was not found.");
                return;
            }

            switch (actionType)
            {
                case ViewAction:
                    windowHelper.SetVisibility(true);
                    break;

                case ButtonAction:
                    HandleButtonAction(args.Arguments);
                    break;
                case ComboBoxAction:
                    HandleComboBoxAction(args.Arguments, args.UserInput);
                    break;
                case ComboBoxToPage:
                    HandleComboBoxToPageAction(args.Arguments);
                    break;
            }

            logger.LogInformation("[Notifications-Handle] Notification was handled: {actionType}.", actionType);
        });

    void HandleButtonAction(
        IDictionary<string, string> arguments)
    {
        if (!arguments.TryGetValue(Group, out string? group) || group is null ||                    // if no group was set
            !arguments.TryGetValue(Content, out string? content) || content is null ||              // if no content was set
            !groupActionMapping.TryGetValue(group, out Action<string>? action) || action is null)   // if no action was found with group
        {
            logger.LogError("[Notifications-Handle] Notification could not be handled: group key, content key or action was not found.");
            return;
        }

        action.Invoke(content);
        groupActionMapping.Remove(group);
    }

    void HandleComboBoxAction(
        IDictionary<string, string> arguments,
        IDictionary<string, string> userInputs)
    {
        if (!arguments.TryGetValue(Group, out string? group) || group is null ||                    // if no group was set
            !userInputs.TryGetValue(group, out string? input) || input is null ||                   // if not user input was given
            !groupActionMapping.TryGetValue(group, out Action<string>? action) || action is null)   // if no action was found with group
        {
            logger.LogError("[Notifications-HandleComboBoxAction] Notification could not be handled: group key, user input or action was not found.");
            return;
        }

        action.Invoke(input);
        groupActionMapping.Remove(group);
        groupItemsMapping.Remove(group);
        groupNotificationInfoMapping.Remove(group);
    }

    void HandleComboBoxToPageAction(
        IDictionary<string, string> arguments)
    {
        if (!arguments.TryGetValue(Group, out string? group) || group is null ||                                  // if no group was set
            !arguments.TryGetValue(ComboBoxToPage, out string? pageStr) || !int.TryParse(pageStr, out int page))  // if no page number was provided
        {
            logger.LogError("[Notifications-HandleComboBoxToPageAction] Notification could not be handled: group key, page number was not found.");
            return;
        }

        SendWithComboBox(group, page);
    }


    public AppNotification Send(
        AppNotificationBuilder builder)
    {
        AppNotification notification = builder
            .AddArgument(Action, ViewAction)
            .BuildNotification();

        notificationManager.Show(notification);

        logger.LogInformation("[Notifications-Send] Notification was sent.");
        return notification;
    }

    public AppNotification Send(
        string text,
        string? appLogo = null)
    {
        AppNotificationBuilder builder = new AppNotificationBuilder()
            .AddText(text);
        if (appLogo is not null)
            builder.SetAppLogoOverride(new(appLogo));

        return Send(builder);
    }


    public AppNotification SendWithButton(
        string[] buttons,
        Action<string> buttonAction,
        string text,
        string? appLogo = null)
    {
        AppNotificationBuilder builder = new AppNotificationBuilder()
            .AddText(text);
        if (appLogo is not null)
            builder.SetAppLogoOverride(new(appLogo));

        string group = Guid.NewGuid().ToString();
        groupActionMapping.Add(group, buttonAction);

        foreach (string button in buttons)
        {
            builder.AddButton(new AppNotificationButton(button)
                .AddArgument(Action, ButtonAction)
                .AddArgument(Group, group)
                .AddArgument(Content, button));
        }

        return Send(builder);
    }


    public AppNotification SendWithComboBox(
        string[] items,
        Action<string?> selectAction,
        string text,
        string? appLogo = null,
        int page = 1)
    {
        string group = Guid.NewGuid().ToString();
        groupActionMapping.Add(group, selectAction);
        groupItemsMapping.Add(group, items);
        groupNotificationInfoMapping.Add(group, (text, appLogo));

        return SendWithComboBox(group, page)!;
    }

    public AppNotification? SendWithComboBox(
        string group,
        int page)
    {
        if (!groupItemsMapping.TryGetValue(group, out string[]? items) || items is null ||
            !groupNotificationInfoMapping.TryGetValue(group, out (string text, string? appLogo) notificationInfo))
        {
            logger.LogError("[Notifications-SendWithComboBox] Failed to send notification: items or notification info was not found.");
            return null;
        }

        AppNotificationBuilder builder = new AppNotificationBuilder()
            .AddText(notificationInfo.text);
        if (notificationInfo.appLogo is not null)
            builder.SetAppLogoOverride(new(notificationInfo.appLogo));

        AppNotificationComboBox comboBox = new(group);

        int totalPages = (int)Math.Ceiling((double)items.Length / ItemsPerComboBoxPage);
        int startIndex = (page - 1) * ItemsPerComboBoxPage;
        int endIndex = Math.Min(page * ItemsPerComboBoxPage, items.Length);
        for (int i = startIndex; i < endIndex; i++)
        {
            string item = items[i];
            comboBox.AddItem(item, item);
        }

        if (totalPages > 1 && page > 1)
            builder.AddButton(new AppNotificationButton("🡐 Previous")
                .AddArgument(Action, ComboBoxToPageAction)
                .AddArgument(Group, group)
                .AddArgument(ComboBoxToPage, (page - 1).ToString()));
        builder.AddComboBox(comboBox)
            .AddButton(new AppNotificationButton("Okay")
                .AddArgument(Action, ComboBoxAction)
                .AddArgument(Group, group));
        if (totalPages > 1 && page < totalPages)
            builder.AddButton(new AppNotificationButton("Next 🡒")
                .AddArgument(Action, ComboBoxToPageAction)
                .AddArgument(Group, group)
                .AddArgument(ComboBoxToPage, (page + 1).ToString()));

        return Send(builder);
    }
}